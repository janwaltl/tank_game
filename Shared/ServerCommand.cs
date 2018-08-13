﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using OpenTK;

namespace Shared
{
	internal sealed class PlayerConnectedCmd : ServerCommand
	{
		internal PlayerConnectedCmd(int pID, Vector3 pColor, Vector3 pPosition) :
			base(CommandType.PlayerConnected)
		{
			this.pID = pID;
			pCol = pColor;
			pPos = pPosition;
		}
		internal PlayerConnectedCmd(byte[] bytes) :
			base(CommandType.PlayerConnected)
		{
			pID = Serialization.DecodeInt(bytes, headerSize);
			pCol = Serialization.DecodeVec3(bytes, headerSize + 4);
			pPos = Serialization.DecodeVec3(bytes, headerSize + 4 + Vector3.SizeInBytes);
		}
		protected override byte[] DoEncode()
		{
			var bytes = new byte[headerSize + 4 + 2 * Vector3.SizeInBytes];
			int offset = headerSize;

			var ID = Serialization.Encode(pID);
			Array.Copy(ID, 0, bytes, offset, ID.Length);
			offset += ID.Length;
			var col = Serialization.Encode(pCol);
			Array.Copy(col, 0, bytes, offset, col.Length);
			offset += col.Length;
			var pos = Serialization.Encode(pPos);
			Array.Copy(pos, 0, bytes, offset, pos.Length);
			offset += pos.Length;
			return bytes;
		}

		protected override EngineCommand DoTranslate()
		{
			return new Engine.PlayerConnectedCmd(pID, pCol, pPos);
		}

		int pID;
		Vector3 pCol;
		Vector3 pPos;
	}
	internal sealed class PlayerDisconnectedCmd : ServerCommand
	{
		internal PlayerDisconnectedCmd(int playerID) :
			base(CommandType.PlayerDisconnected)
		{
			pID = playerID;
		}
		internal PlayerDisconnectedCmd(byte[] bytes) :
			base(CommandType.PlayerDisconnected)
		{
			pID = Serialization.DecodeInt(bytes, headerSize);
		}
		protected override byte[] DoEncode()
		{
			var bytes = new byte[headerSize + 4];
			var ID = Serialization.Encode(pID);
			Array.Copy(ID, 0, bytes, headerSize, ID.Length);
			return bytes;
		}
		protected override EngineCommand DoTranslate()
		{
			return new Engine.PlayerDisconnectedCmd(pID);
		}
		int pID;
	}

	/// <summary>
	/// Commands that when executed sets players' states.
	/// </summary>
	internal sealed class PlayersStateCmd : ServerCommand
	{
		internal PlayersStateCmd(List<Engine.PlayersStateCommand.PlayerState> playerStates) :
			base(CommandType.PlayersStates)
		{
			this.playerStates = playerStates;
		}
		internal PlayersStateCmd(byte[] bytes) :
			base(CommandType.PlayersStates)
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
					return new PlayersStateCmd(bytes);
				case CommandType.PlayerConnected:
					return new PlayerConnectedCmd(bytes);
				case CommandType.PlayerDisconnected:
					return new PlayerDisconnectedCmd(bytes);
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
			return new PlayersStateCmd(playerStates);
		}
		/// <summary>
		/// Builds ServerCommands that when translated and executed creates a new player in the world.
		/// </summary>
		/// <param name="pID">player's ID in the world</param>
		/// <param name="pCol">player's color</param>
		/// <param name="pPos">player's position in world coordinates.</param>
		/// <returns></returns>
		public static ServerCommand ConnectPlayer(int pID, Vector3 pCol, Vector3 pPos)
		{
			return new PlayerConnectedCmd(pID, pCol, pPos);
		}
		public static ServerCommand DisconnectPlayer(int pID)
		{
			return new PlayerDisconnectedCmd(pID);
		}
		/// <summary>
		/// Serializes the header which encodes type of the command and writes it to the first 'headerSize' bytes of the 'bytes' array.
		/// </summary>
		/// <param name="bytes">Array with 'headerSize' bytes reserved at the beginning</param>
		static void EncodeHeader(byte[] bytes, CommandType cmd)
		{
			bytes[0] = (byte)cmd;
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
			PlayerConnected,
			PlayerDisconnected,
		}

		readonly CommandType cmdType;
	}

}
