using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// Contains shared classes between the Server and client.
/// Mostly communication oriented.
/// </summary>
namespace Shared
{
	/// <summary>
	/// Represent an update message sent by client to the server while playing
	/// </summary>
	public class ClientUpdate
	{
		public ClientUpdate(string msg, int playerID) { this.msg = msg; }
		//CURRENTLY just a message
		public string msg;
		public int playerID;

		public static byte[] Encode(ClientUpdate update)
		{
			var msg = Encoding.BigEndianUnicode.GetBytes(update.msg);
			var pID = Serialization.Encode(update.playerID);
			return Serialization.CombineArrays(pID, msg);
		}
		public static ClientUpdate Decode(byte[] bytes)
		{
			int pID = Serialization.DecodeInt(bytes, 0);
			string msg = Encoding.BigEndianUnicode.GetString(bytes, 4, bytes.Length - 4);
			return new ClientUpdate(msg, pID);
		}

	}
	public class ClientConnecting
	{
		public int playerID;
		public string testMsg;
		public ClientConnecting(int playerID, string testMsg)
		{
			this.playerID = playerID; this.testMsg = testMsg;
		}
		public static byte[] Encode(ClientConnecting c)
		{
			var msgBytes = Encoding.BigEndianUnicode.GetBytes(c.testMsg);
			var pBytes = Serialization.Encode(c.playerID);
			return Serialization.CombineArrays(pBytes, msgBytes);
		}
		public static ClientConnecting Decode(byte[] bytes, int startIndex)
		{
			int playerID = Serialization.DecodeInt(bytes, startIndex);
			string msg = Encoding.BigEndianUnicode.GetString(bytes, startIndex + 4, bytes.Length - startIndex - 4);
			return new ClientConnecting(playerID, msg);
		}
		public static ClientConnecting Decode(byte[] bytes)
		{
			return Decode(bytes, 0);
		}
		//Message structure:
		//4					Bytes playerID
		//Rest				Test Message
	}
	/// <summary>
	/// Represents dynamic data about the game.
	/// Sent from server to the player when player signals they are ready.
	/// </summary>
	public class ClientDynamicData
	{
		public string testData;

		public ClientDynamicData(string testData)
		{
			this.testData = testData;
		}
		public static byte[] Decode(ClientDynamicData d)
		{
			return Encoding.BigEndianUnicode.GetBytes(d.testData);
		}
		public static ClientDynamicData Encode(byte[] bytes, int startIndex)
		{
			return new ClientDynamicData(Encoding.BigEndianUnicode.GetString(bytes, startIndex, bytes.Length - startIndex));
		}
		public static ClientDynamicData Encode(byte[] bytes)
		{
			return Encode(bytes, 0);
		}
	}

	public static class Communication
	{
		/// <summary>
		/// Sends passed message via TCP socket.
		/// </summary>
		/// <param name="target">Connected socket must be set to TCP.</param>
		/// <param name="msg">Message to send</param>
		/// <returns>Task that finishes after the message has been send</returns>
		public static async Task TCPSendMessageAsync(Socket target, byte[] message)
		{
			var msg = Serialization.PrependLength(message);

			int bytesSent = 0;
			//Make correct signature for Task.Factory
			Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) => target.BeginSend(msg, bytesSent, msg.Length - bytesSent, SocketFlags.None, callback, state);
			while (bytesSent < msg.Length)
			{
				var newBytes = await Task.Factory.FromAsync(begin, target.EndSend, null);
				//RESOLVE if(newBytes==0) error?
				bytesSent += newBytes;
			}
			Debug.Assert(bytesSent == msg.Length);
		}
		/// <summary>
		/// Receives message using TCP socket.
		/// </summary>
		/// <param name="s">Connected socket to the server</param>
		/// <returns>Task representing the received message.</returns>
		public static async Task<byte[]> TCPReceiveMessageAsync(Socket from)
		{
			var msgLen = Serialization.DecodeInt(await TCPReceiveNBytesAsync(from, 4), 0);

			return await TCPReceiveNBytesAsync(from, msgLen);
		}
		public static async Task<byte[]> TCPReceiveNBytesAsync(Socket from, int numBytes)
		{
			byte[] res = new byte[numBytes];
			int numRead = 0;
			Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) => from.BeginReceive(res, numRead, numBytes - numRead, SocketFlags.None, callback, state);
			do
			{
				int newBytes = await Task.Factory.FromAsync(begin, from.EndReceive, null);
				if (newBytes == 0)//RESOLVE proper error checking
					throw new NotImplementedException("Connection has been closed by the server.");
				numRead += newBytes;
			} while (numRead < numBytes);
			Debug.Assert(numRead == numBytes);
			return res;
		}
	}
	public static class Serialization
	{
		/// <summary>
		/// Preprends the array with 4byte int length
		/// </summary>
		public static byte[] PrependLength(byte[] bytes)
		{
			return CombineArrays(Encode(bytes.Length), bytes);
		}
		public static byte[] StripLength(byte[] bytesWithLength)
		{
			Debug.Assert(bytesWithLength.Length >= 4);
			byte[] res = new byte[bytesWithLength.Length - 4];
			Array.Copy(bytesWithLength, 4, res, 0, bytesWithLength.Length - 4);
			return res;
		}
		public static byte[] CombineArrays(byte[] first, byte[] second)
		{
			var res = new byte[first.Length + second.Length];
			Array.Copy(first, res, first.Length);
			Array.Copy(second, 0, res, first.Length, second.Length);
			return res;
		}
		public static byte[] Encode(int x)
		{
			var bytes = BitConverter.GetBytes(x);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}
		public static int DecodeInt(byte[] bytes, int startIndex)
		{
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(bytes, startIndex, 4);
			return BitConverter.ToInt32(bytes, 0);
		}
	}
}

