using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
namespace Server
{
	class Program
	{
		class ServerState
		{
			public Socket listener;
			public int numClients;
		}
		/// <summary>
		/// Starts listening for incoming connections to the server.
		/// </summary>
		static async Task StarListening()
		{
			//TODO Error checking for this method

			var endPoint = new IPEndPoint(IPAddress.Any, 23545);
			ServerState serverState = new ServerState
			{
				listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp),
				numClients = 0
			};
			serverState.listener.Bind(endPoint);
			serverState.listener.Listen(5);

			while (true)
			{
				Console.WriteLine("Listening for a client...");
				Socket client = await Task.Factory.FromAsync(serverState.listener.BeginAccept, serverState.listener.EndAccept, null);
				//RESOLVe
				_ = HandleClientAsync(client, serverState.numClients++);
			}
		}
		/// <summary>
		/// Handles newly connected client. 
		/// </summary>
		/// <param name="client">Incoming socket</param>
		/// <param name="ID">ID of the client</param>
		/// <returns>Task that completes when the client has been handled.</returns>
		static async Task HandleClientAsync(Socket client, int ID)
		{
			//IMPROVE Do prepare asynchronously for bigger messages.
			Console.WriteLine($"Connectd to {ID}");
			var msg = await Task.Run(() => PrepareMsg(ID));
			await SendMsgAsync(client, ID, msg);

			client.Shutdown(SocketShutdown.Both);
			client.Close();
			Console.WriteLine($"Disconnected from {ID}");
		}
		static byte[] PrepareMsg(int ID)
		{
			var text = Encoding.BigEndianUnicode.GetBytes("Testing message.");
			var playerID = BitConverter.GetBytes(ID);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(playerID);
			//4 is to encode the length
			var msgLen = text.Length + playerID.Length + 4;
			var msg = new byte[msgLen];

			var msgLenBytes = BitConverter.GetBytes(msgLen);
			Array.Copy(msgLenBytes, msg, msgLenBytes.Length);
			Array.Copy(playerID, 0, msg, msgLenBytes.Length, playerID.Length);
			Array.Copy(text, 0, msg, msgLenBytes.Length + playerID.Length, text.Length);
			return msg;
		}
		static async Task SendMsgAsync(Socket client, int ID, byte[] msg)
		{
			Console.WriteLine($"{ID}: SendMsg({msg.Length}Bytes)");
			try
			{
				int bytesSent = 0;
				//Make correct signature for Task.Factory
				Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) => client.BeginSend(msg, bytesSent, msg.Length - bytesSent, SocketFlags.None, callback, state);
				while (bytesSent < msg.Length)
				{
					var newBytes = await Task.Factory.FromAsync(begin, client.EndSend, null);
					//RESOLVE if(newBytes==0) error?
					bytesSent += newBytes;
					Console.WriteLine($"{ID}: Sent {bytesSent}/{msg.Length} bytes.");
				}
				Debug.Assert(bytesSent == msg.Length);
				Console.WriteLine($"{ID}: Message sent successfully.");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Forced disconnection from {ID}, reason:");
				Console.WriteLine(e.Message);
			}
		}
		static void Main(string[] args)
		{
			//RESOLVE
			_ = StarListening();
			System.Threading.Thread.Sleep(60000);
		}
	}

}

