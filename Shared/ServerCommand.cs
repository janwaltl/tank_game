using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using OpenTK;

namespace Shared
{
	public sealed class PlayerConnectedCmd : ServerCommand
	{
		public PlayerConnectedCmd(int pID, Vector3 pColor, Vector3 pPosition) :
			base(CommandType.PlayerConnected)
		{
			this.pID = pID;
			pCol = pColor;
			pPos = pPosition;
		}
		public PlayerConnectedCmd(byte[] bytes, int offset = 0) :
			base(CommandType.PlayerConnected)
		{
			pID = Serialization.DecodeInt(bytes, offset);
			pCol = Serialization.DecodeVec3(bytes, offset + 4);
			pPos = Serialization.DecodeVec3(bytes, offset + 4 + Vector3.SizeInBytes);
		}
		protected override byte[] DoEncode()
		{
			int offset = headerSize;

			var ID = Serialization.Encode(pID);
			var col = Serialization.Encode(pCol);
			var pos = Serialization.Encode(pPos);

			var bytes = new byte[offset + ID.Length + col.Length + pos.Length];
			Array.Copy(ID, 0, bytes, offset, ID.Length);
			offset += ID.Length;
			Array.Copy(col, 0, bytes, offset, col.Length);
			offset += col.Length;
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

		public override bool guaranteedExec => true;
	}
	public sealed class PlayerDisconnectedCmd : ServerCommand
	{
		public PlayerDisconnectedCmd(int playerID) :
			base(CommandType.PlayerDisconnected)
		{
			pID = playerID;
		}
		public PlayerDisconnectedCmd(byte[] bytes, int offset = 0) :
			base(CommandType.PlayerDisconnected)
		{
			pID = Serialization.DecodeInt(bytes, offset);
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

		public override bool guaranteedExec => true;
	}

	/// <summary>
	/// Commands that when executed sets players' states.
	/// </summary>
	public sealed class PlayersStateCmd : ServerCommand
	{
		public PlayersStateCmd(List<PlayersStateCommand.PlayerState> playerStates) :
			base(CommandType.PlayersStates)
		{
			this.playerStates = playerStates;
		}
		public PlayersStateCmd(byte[] bytes, int offset = 0) :
			base(CommandType.PlayersStates)
		{
			int numPlayers = (bytes.Length - headerSize) / bytesPerPlayer;

			playerStates = new List<PlayersStateCommand.PlayerState>();

			while (numPlayers-- > 0)
			{
				var ID = Serialization.DecodeInt(bytes, offset);
				offset += 4;
				var pos = Serialization.DecodeVec3(bytes, offset);
				offset += Vector3.SizeInBytes;
				var tankAngle = Serialization.DecodeFloat(bytes, offset);
				offset += 4;
				var towerAngle = Serialization.DecodeFloat(bytes, offset);
				offset += 4;
				var fireCooldown = Serialization.DecodeDouble(bytes, offset);
				offset += 8;
				var currShields = bytes[offset++];
				var currHealth = bytes[offset++];
				playerStates.Add(new PlayersStateCommand.PlayerState(ID, pos, tankAngle, towerAngle, fireCooldown, currHealth, currShields));
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
				var tankAngle = Serialization.Encode(p.tankAngle);
				Array.Copy(tankAngle, 0, bytes, offset, tankAngle.Length);
				offset += tankAngle.Length;
				var towerAngle = Serialization.Encode(p.towerAngle);
				Array.Copy(towerAngle, 0, bytes, offset, towerAngle.Length);
				offset += towerAngle.Length;
				var fireCooldown = Serialization.Encode(p.fireCooldown);
				Array.Copy(fireCooldown, 0, bytes, offset, fireCooldown.Length);
				offset += fireCooldown.Length;
				bytes[offset++] = p.currShields;
				bytes[offset++] = p.currHealth;

			}
			return bytes;
		}
		List<Engine.PlayersStateCommand.PlayerState> playerStates;
		//ID,position,tankAngle, towerAngle, fireCooldown
		static readonly int bytesPerPlayer = 4 + Vector3.SizeInBytes + 4 + 4 + 8 + 2;

		public override bool guaranteedExec => false;
	}
	/// <summary>
	/// Command that translates into Engine.PlayerFireCmd .
	/// </summary>
	public sealed class PlayerFireCmd : ServerCommand
	{
		public PlayerFireCmd(int playerID, Vector2 shootingDir, Vector2 shootingPos) :
			base(CommandType.PlayerFire)
		{
			pID = playerID;
			sDir = shootingDir;
			sPos = shootingPos;
		}
		public PlayerFireCmd(byte[] bytes, int offset = 0) :
			base(CommandType.PlayerFire)
		{
			pID = Serialization.DecodeInt(bytes, offset);
			offset += 4;
			sDir = Serialization.DecodeVec2(bytes, offset);
			offset += Vector2.SizeInBytes;
			sPos = Serialization.DecodeVec2(bytes, offset);
			offset += Vector2.SizeInBytes;
		}
		protected override byte[] DoEncode()
		{
			var bytes = new byte[headerSize + 4 + 2 * Vector2.SizeInBytes];
			int offset = headerSize;
			var ID = Serialization.Encode(pID);
			Array.Copy(ID, 0, bytes, offset, ID.Length);
			offset += ID.Length;
			var dir = Serialization.Encode(sDir);
			Array.Copy(dir, 0, bytes, offset, dir.Length);
			offset += dir.Length;
			var pos = Serialization.Encode(sPos);
			Array.Copy(pos, 0, bytes, offset, pos.Length);
			offset += pos.Length;

			return bytes;
		}

		protected override EngineCommand DoTranslate()
		{
			return new Engine.PlayerFireCmd(pID, sDir, sPos);
		}
		int pID;
		Vector2 sDir;
		Vector2 sPos;

		public override bool guaranteedExec => false;
	}
	/// <summary>
	/// When sent, translated and executed respawn a player at new location.
	/// </summary>
	public sealed class PlayerDeathCmd : ServerCommand
	{
		public PlayerDeathCmd(int killerID, int killedID, Vector3 respawnPos) :
			base(CommandType.PlayerDeath)
		{
			this.killedID = killedID;
			this.killerID = killerID;
			rPos = respawnPos;
		}
		public PlayerDeathCmd(byte[] bytes, int offset = 0) :
			base(CommandType.PlayerDeath)
		{
			killedID = Serialization.DecodeInt(bytes, offset);
			offset += 4;
			killerID = Serialization.DecodeInt(bytes, offset);
			offset += 4;
			rPos = Serialization.DecodeVec3(bytes, offset);
			offset += 4;
		}
		protected override byte[] DoEncode()
		{
			var bytes = new byte[headerSize + 8 + Vector3.SizeInBytes];
			int offset = headerSize;
			var killed = Serialization.Encode(killedID);
			Array.Copy(killed, 0, bytes, offset, killed.Length);
			offset += killed.Length;
			var killer = Serialization.Encode(killerID);
			Array.Copy(killer, 0, bytes, offset, killer.Length);
			offset += killer.Length;
			var pos = Serialization.Encode(rPos);
			Array.Copy(pos, 0, bytes, offset, pos.Length);
			offset += pos.Length;

			return bytes;
		}

		protected override EngineCommand DoTranslate()
		{
			return new Engine.PlayerDeathCmd(killedID, killerID, rPos);
		}
		int killedID, killerID;
		Vector3 rPos;
		public override bool guaranteedExec => true;
	}
	/// <summary>
	/// When sent, translated and executed respawn shield pickups in the client's arena
	/// </summary>
	public sealed class RespawnShieldsCmd : ServerCommand
	{
		public override bool guaranteedExec => true;

		public RespawnShieldsCmd() :
			base(CommandType.RespawnShields)
		{
		}
		public RespawnShieldsCmd(byte[] bytes, int offset = 0) :
			base(CommandType.RespawnShields)
		{
		}
		protected override byte[] DoEncode()
		{
			var bytes = new byte[headerSize];
			return bytes;
		}

		protected override EngineCommand DoTranslate()
		{
			return new Engine.RespawnPickupsCmd();
		}
	}
	public sealed class UseShieldPickupCmd : ServerCommand
	{
		public override bool guaranteedExec => true;

		public UseShieldPickupCmd(int pickupID) :
			base(CommandType.ShieldDespawn)
		{
			pID = pickupID;
		}
		public UseShieldPickupCmd(byte[] bytes, int offset = 0) :
			base(CommandType.ShieldDespawn)
		{
			pID = Serialization.DecodeInt(bytes, offset);
			offset += 4;
		}
		protected override byte[] DoEncode()
		{
			var bytes = new byte[headerSize + 4];
			int offset = headerSize;
			var ID = Serialization.Encode(pID);
			Array.Copy(ID, 0, bytes, offset, ID.Length);
			offset += 4;
			return bytes;
		}

		protected override EngineCommand DoTranslate()
		{
			return new Engine.UseShieldPickupCmd(pID);
		}
		int pID;
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
		/// <summary>
		/// If the command must be executed always on the client.
		/// If true, the command will be send as part of reliable updates.
		/// </summary>
		public abstract bool guaranteedExec { get; }
		public static ServerCommand Decode(byte[] bytes, int offset = 0)
		{
			//Switch on type
			//Pas decoded header
			switch ((CommandType)bytes[offset])
			{
				case CommandType.PlayersStates:
					return new PlayersStateCmd(bytes, offset + headerSize);
				case CommandType.PlayerConnected:
					return new PlayerConnectedCmd(bytes, offset + headerSize);
				case CommandType.PlayerDisconnected:
					return new PlayerDisconnectedCmd(bytes, offset + headerSize);
				case CommandType.PlayerFire:
					return new PlayerFireCmd(bytes, offset + headerSize);
				case CommandType.PlayerDeath:
					return new PlayerDeathCmd(bytes, offset + headerSize);
				case CommandType.RespawnShields:
					return new RespawnShieldsCmd(bytes, offset + headerSize);
				case CommandType.ShieldDespawn:
					return new UseShieldPickupCmd(bytes, offset + headerSize);
				default:
					Debug.Assert(false, "Forgot to add command to serialization logic.");
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Serializes the header which encodes type of the command and writes it to the first 'headerSize' bytes of the 'bytes' array.
		/// </summary>
		/// <param name="bytes">Array with 'headerSize' bytes reserved at the beginning</param>
		static void EncodeHeader(byte[] bytes, CommandType cmd)
		{
			bytes[0] = (byte)cmd;
		}

		public ServerCommand(CommandType cmd)
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

		public enum CommandType : byte
		{
			PlayersStates,
			PlayerConnected,
			PlayerDisconnected,
			PlayerFire,
			PlayerDeath,
			RespawnShields,
			ShieldDespawn,
		}

		readonly CommandType cmdType;
	}

}
