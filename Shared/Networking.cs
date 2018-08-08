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
	public static class Ports
	{
		public const int serverConnection = 23545;
		public const int clientUpdates = 23546;
		public const int serverUpdates = 23547;
	}
	/// <summary>
	/// Represent an update message sent by client to the server while playing.
	/// </summary>
	public class ClientUpdate
	{
		public ClientUpdate(string msg, int playerID) { this.msg = msg; this.playerID = playerID; }
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
	/// <summary>
	/// Represents a message sent by server to the client to update their state.
	/// </summary>
	public class ServerCommand
	{
		public string msg;
		public ServerCommand(string msg)
		{
			this.msg = msg;
		}
		public static byte[] Encode(ServerCommand c)
		{
			return Encoding.BigEndianUnicode.GetBytes(c.msg);
		}
		public static ServerCommand Decode(byte[] bytes)
		{
			return new ServerCommand(Encoding.BigEndianUnicode.GetString(bytes));
		}
	}
	/// <summary>
	/// Data sent from server to a client when the client connects.
	/// </summary>
	public class ConnectingStaticData
	{
		public int playerID;
		public string testMsg;
		public ConnectingStaticData(int playerID, string testMsg)
		{
			this.playerID = playerID; this.testMsg = testMsg;
		}
		public static byte[] Encode(ConnectingStaticData c)
		{
			var msgBytes = Encoding.BigEndianUnicode.GetBytes(c.testMsg);
			var pBytes = Serialization.Encode(c.playerID);
			return Serialization.CombineArrays(pBytes, msgBytes);
		}
		public static ConnectingStaticData Decode(byte[] bytes, int startIndex)
		{
			int playerID = Serialization.DecodeInt(bytes, startIndex);
			string msg = Encoding.BigEndianUnicode.GetString(bytes, startIndex + 4, bytes.Length - startIndex - 4);
			return new ConnectingStaticData(playerID, msg);
		}
		public static ConnectingStaticData Decode(byte[] bytes)
		{
			return Decode(bytes, 0);
		}
		//Message structure:
		//4					Bytes playerID
		//Rest				Test Message
	}
	/// <summary>
	/// Represents dynamic data about the game.
	/// Sent from server to the player when player signals they are ready while connecting.
	/// </summary>
	public class ConnectingDynamicData
	{
		public string testData;

		public ConnectingDynamicData(string testData)
		{
			this.testData = testData;
		}
		public static byte[] Decode(ConnectingDynamicData d)
		{
			return Encoding.BigEndianUnicode.GetBytes(d.testData);
		}
		public static ConnectingDynamicData Encode(byte[] bytes, int startIndex)
		{
			return new ConnectingDynamicData(Encoding.BigEndianUnicode.GetString(bytes, startIndex, bytes.Length - startIndex));
		}
		public static ConnectingDynamicData Encode(byte[] bytes)
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
		public static Task TCPSendMessageAsync(Socket target, byte[] message)
		{
			var msg = Serialization.PrependLength(message);
			return TCPSendNBytesAsync(target, msg);
		}
		/// <summary>
		/// Sends N bytes via TCP socket to target.
		/// </summary>
		/// <param name="target">Connected socket must be set to TCP.</param>
		/// <param name="message">Message to send</param>
		/// <returns>Task that finishes when the message has been sent.</returns>
		public static async Task TCPSendNBytesAsync(Socket target, byte[] message)
		{
			int bytesSent = 0;
			//Make correct signature for Task.Factory
			Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) => target.BeginSend(message, bytesSent, message.Length - bytesSent, SocketFlags.None, callback, state);
			while (bytesSent < message.Length)
			{
				var newBytes = await Task.Factory.FromAsync(begin, target.EndSend, null);
				//RESOLVE if(newBytes==0) error?
				bytesSent += newBytes;
			}
			Debug.Assert(bytesSent == message.Length);
		}
		/// <summary>
		/// Receives message using TCP socket.
		/// </summary>
		/// <param name="from">Connected socket to the server</param>
		/// <returns>Task representing the received message.</returns>
		public static async Task<byte[]> TCPReceiveMessageAsync(Socket from)
		{
			var msgLen = Serialization.DecodeInt(await TCPReceiveNBytesAsync(from, 4), 0);

			return await TCPReceiveNBytesAsync(from, msgLen);
		}
		/// <summary>
		/// Receives N bytes using TCP socket.
		/// </summary>
		/// <param name="from">Connected socket to the server</param>
		/// <param name="numBytes">Number of bytes to receive</param>
		/// <returns>Task representing the received bytes.</returns>
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
		/// <summary>
		/// Sends ACK message to the target via TCP socket.
		/// </summary>
		/// <param name="target">Connected socket, must be set to TCP.</param>
		/// <returns>Task that finishes when message has been sent.</returns>
		public static Task TCPSendACKAsync(Socket target)
		{
			return TCPSendNBytesAsync(target, new byte[1] { ACKByte });
		}
		/// <summary>
		/// Receives ACK from connected socket, sent by <code>TCPSendACKAsync</code> method.
		/// </summary>
		/// <param name="from">Connected socket that sent the ACK.</param>
		/// <returns>Task representing received message, true if it was ACK, false otherwise.</returns>
		public static async Task<bool> TCPReceiveACKAsync(Socket from)
		{
			var msg = await TCPReceiveNBytesAsync(from, 1);
			return msg.Length == 1 && msg[0] == ACKByte;
		}
		/// <summary>
		/// Sends passed message as UDP datagram to the target via passed socket.
		/// </summary>
		/// <param name="socket">UDP socket that will be used to send the message.</param>
		/// <param name="target">Destination of the message.</param>
		/// <param name="message">Length should be small enough to fit into a single datagram ~500bytes.</param
		/// <returns>Task representing sent message.</returns>
		public static async Task UDPSendMessageAsync(Socket socket, IPEndPoint target, byte[] message)
		{
			//TODO message should be small enough to fit into a datagram
			Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) =>
				socket.BeginSendTo(message, 0, message.Length, SocketFlags.None, target, callback, state);

			var bytesSent = await Task.Factory.FromAsync(begin, socket.EndSendTo, null);
			//UDP should send whole message at once.
			Debug.Assert(bytesSent == message.Length);
		}
		/// <summary>
		/// Received UDP datagram from bound socket.
		/// </summary>
		/// <param name="socket">Bound UDP socket</param>
		/// <param name="maxLen">Maximum length of the accepted message in bytes.</param>
		/// <returns>Task representing received message and its sender.</returns>
		public static async Task<Tuple<byte[], IPEndPoint>> UDPReceiveMessageAsync(Socket socket, int maxLen)
		{
			byte[] buffer = new byte[maxLen];
			EndPoint from1 = new IPEndPoint(IPAddress.Any, 0);
			EndPoint from2 = new IPEndPoint(IPAddress.Any, 0);

			Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) =>
					socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref from1, callback, state);
			Func<IAsyncResult, int> end = (result) => socket.EndReceiveFrom(result, ref from2);

			//Should read whole datagram.
			int numRead = await Task.Factory.FromAsync(begin, end, null);
			//TODO resolve numRead==0
			byte[] res = new byte[numRead];
			Array.Copy(buffer, res, res.Length);
			return Tuple.Create(res, from2 as IPEndPoint);
		}
		/// <summary>
		/// Byte representing ACK message.
		/// </summary>
		private const byte ACKByte = 17;
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
	public static class TaskExtensions
	{
		/// <summary>
		/// Do not wait for the task.
		/// </summary>
		/// <param name="t"></param>
		public static void Detach(this Task t)
		{
			//Only forget launched tasks for now.
			Debug.Assert(t.Status != TaskStatus.Created);
		}
	}
}

