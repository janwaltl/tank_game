using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Shared
{
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
			if (msgLen > 0)
				return await TCPReceiveNBytesAsync(from, msgLen);
			else
				return new byte[0];
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
}
