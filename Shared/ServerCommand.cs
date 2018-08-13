using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

namespace Shared
{
	/// <summary>
	/// Commands that when executed sets players' states.
	/// </summary>
	internal sealed class PlayersStatesCommand : ServerCommand
	{
		internal PlayersStatesCommand(CommandType cmd, List<Engine.PlayersStateCommand.PlayerState> playerStates) :
			base(cmd)
		{
			this.playerStates = playerStates;
		}
		internal PlayersStatesCommand(CommandType cmd, byte[] bytes) :
			base(cmd)
		{
			int numPlayers = (bytes.Length - headerSize) / bytesPerPlayer;

			playerStates = new List<Engine.PlayersStateCommand.PlayerState>();
			int offset = headerSize;
			while (numPlayers-- > 0)
			{
				var ID = Serialization.DecodeInt(bytes, offset);
				offset += 4;
				var pos = Serialization.DecodeVec3(bytes, offset);
			}
		}
		protected override Engine.EngineCommand DoTranslate()
		{
			return new Engine.PlayersStateCommand(playerStates);
		}

		protected override byte[] DoEncode()
		{
			int numBytes = headerSize + playerStates.Count * bytesPerPlayer;
			var bytes = new byte[numBytes];
			int offset = headerSize;
			foreach (var p in playerStates)
			{
				var ID = Serialization.Encode(p.playerID);
				Array.Copy(ID, 0, bytes, offset, ID.Length);
				offset += ID.Length;
				var pos = Serialization.Encode(p.pos);
				Array.Copy(pos, 0, bytes, offset, pos.Length);
				offset += pos.Length;
			}
			return bytes;
		}
		List<Engine.PlayersStateCommand.PlayerState> playerStates;

		static readonly int bytesPerPlayer = Vector3.SizeInBytes + 4;
	}
	/// <summary>
	/// Represents a message sent by server to the client that can be translated to the engine.
	/// </summary>
	public abstract class ServerCommand
	{
		protected const int headerSize = 1;//Just CommandType

		/// <summary>
		/// Translates the message from the server to the EngineCommand
		/// </summary>
		/// <returns>EngineCommand sent by the server.</returns>
		public Engine.EngineCommand Translate()
		{
			return DoTranslate();
		}
		/// <summary>
		/// Serializes this class to be sent over the network.
		/// </summary>
		/// <returns></returns>
		public byte[] Encode()
		{
			var bytes = DoEncode();
			EncodeHeader(bytes, cmdType);
			return bytes;
		}
		public static ServerCommand Decode(byte[] bytes)
		{
			//Switch on type
			//Pas decoded header
			switch ((CommandType)bytes[0])
			{
				case CommandType.PlayersStates:
					return new PlayersStatesCommand(CommandType.PlayersStates, bytes);
				default:
					throw new NotImplementedException();
			}
		}
		/// <summary>
		/// Builds ServerCommand that when translated and executed on the engine sets passed players' states.
		/// </summary>
		/// <param name="playerStates">States to set the players to.</param>
		public static ServerCommand SetPlayersStates(List<Engine.PlayersStateCommand.PlayerState> playerStates)
		{
			return new PlayersStatesCommand(CommandType.PlayersStates, playerStates);
		}

		/// <summary>
		/// Serializes the header which encodes type of the command and writes it to the first 'headerSize' bytes of the 'bytes' array.
		/// </summary>
		/// <param name="bytes">Array with 'headerSize' bytes reserved at the beginning</param>
		static void EncodeHeader(byte[] bytes, CommandType cmd)
		{
			bytes[0] = (byte)CommandType.PlayersStates;
		}

		internal ServerCommand(CommandType cmd)
		{
			cmdType = cmd;
		}
		/// <summary>
		/// Translates the ServerCommand to EngineCommand.
		/// </summary>
		protected abstract Engine.EngineCommand DoTranslate();
		/// <summary>
		/// Serializes the derived command into an array. First 'headerSize' bytes must be reserved for the header.
		/// </summary>
		protected abstract byte[] DoEncode();

		internal enum CommandType : byte
		{
			PlayersStates,
		}

		readonly CommandType cmdType;
	}

}
