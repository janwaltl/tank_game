using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{

	/// <summary>
	/// Wraps a serverCommand so it can be reliably send to a client.
	/// </summary>
	public sealed class CmdServerUpdate : ServerUpdate
	{
		//Update will contain passed command that can be executed on the client
		public CmdServerUpdate(ServerCommand command) :
			base(Type.sCommand)
		{
			Cmd = command;
		}
		public CmdServerUpdate(byte[] bytes, int offset = 0) :
			base(Type.sCommand)
		{
			Cmd = ServerCommand.Decode(bytes, offset);
		}

		public byte[] Encode()
		{
			var cmdBytes = Cmd.Encode();
			var bytes = EncodeBase(cmdBytes.Length, out int reserved);
			Buffer.BlockCopy(cmdBytes, 0, bytes, reserved, cmdBytes.Length);
			return bytes;
		}

		public ServerCommand Cmd { get; }
	}
	/// <summary>
	/// Represent data that are send via reliable channel from the server to the client.
	/// </summary>
	public abstract class ServerUpdate
	{
		internal ServerUpdate(Type type)
		{
			this.type = type;
		}
		public static ServerUpdate Decode(byte[] bytes)
		{
			Debug.Assert(bytes.Length > 0);
			switch ((Type)bytes[0])
			{
				case Type.sCommand:
					return new CmdServerUpdate(bytes, 1);
				default:
					Debug.Assert(false, "Forgot to add case to enum");
					return null;
			}
		}
		/// <summary>
		/// Serializes the base class
		/// </summary>
		/// <param name="length"></param>
		/// <param name="reserved"></param>
		/// <returns></returns>
		protected internal byte[] EncodeBase(int length, out int reserved)
		{
			reserved = 1;
			var bytes = new byte[length + reserved];
			bytes[0] = (byte)type;
			return bytes;
		}
		protected internal enum Type : byte
		{
			sCommand = 1,
			//msg,
			//...
		}
		readonly Type type;
	}
}
