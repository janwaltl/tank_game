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
		/// <summary>
		/// Server listens at this port for incoming connections (TCP).
		/// </summary>
		public const int serverConnection = 23545;
		/// <summary>
		/// Server listenst at this port for incoming client updates (UDP).
		/// </summary>
		public const int clientUpdates = 23546;
		/// <summary>
		/// Client listens at this port for incoming server updates (UDP).
		/// </summary>
		public const int serverUpdates = 23547;
		/// <summary>
		/// Client listenst at this port for incoming server updates (TCP).
		/// </summary>
		public const int relServerUpdates = 23647;
	}

	/// <summary>
	/// Data sent from server to a client when the client connects.
	///  - Contains assigned playerID and the map.
	/// </summary>
	public class ConnectingStaticData
	{
		/// <summary>
		/// Assigned ID for the new player
		/// </summary>
		public int PlayerID { get; }
		/// <summary>
		/// Current map on the server
		/// </summary>
		public Engine.Arena Arena { get; }
		public ConnectingStaticData(int playerID, Engine.Arena arena)
		{
			PlayerID = playerID;
			Arena = arena;
		}
		public static byte[] Encode(ConnectingStaticData c)
		{
			var bytes = new byte[4 + c.Arena.Size * c.Arena.Size];

			var pID = Serialization.Encode(c.PlayerID);
			Array.Copy(pID, 0, bytes, 0, pID.Length);

			for (int y = 0; y < c.Arena.Size; y++)
				for (int x = 0; x < c.Arena.Size; x++)
					bytes[pID.Length + x + y * c.Arena.Size] = (byte)c.Arena[x, y];
			return bytes;
		}
		public static ConnectingStaticData Decode(byte[] bytes, int startIndex)
		{
			int pID = Serialization.DecodeInt(bytes, startIndex);
			int arenaSize = (int)Math.Sqrt(bytes.Length - 4 - startIndex);
			var arena = new Engine.Arena(arenaSize);
			for (int y = 0; y < arena.Size; y++)
				for (int x = 0; x < arena.Size; x++)
					arena[x, y] = (Engine.Arena.CellType)bytes[4 + startIndex + x + y * arena.Size];
			return new ConnectingStaticData(pID, arena);
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
	/// Contains state of all players and pickups in the arena.
	/// </summary>
	public class ConnectingDynamicData
	{
		public Dictionary<int, Engine.Player> Players { get; }
		public Dictionary<int, Engine.ShieldPickup> Pickups { get; }
		public ConnectingDynamicData(Dictionary<int, Engine.Player> players, Dictionary<int, Engine.ShieldPickup> pickups)
		{
			Players = players;
			Pickups = pickups;
		}
		public static byte[] Encode(ConnectingDynamicData d)
		{
			var bytes = new byte[4 + 4 + bytesPerPlayer * d.Players.Count + bytesPerPickup * d.Pickups.Count];

			int offset = 0;
			var numPlayers = Serialization.Encode(d.Players.Count);
			var numPickups = Serialization.Encode(d.Pickups.Count);
			Array.Copy(numPlayers, 0, bytes, offset, numPlayers.Length);
			offset += numPlayers.Length;
			Array.Copy(numPickups, 0, bytes, offset, numPickups.Length);
			offset += numPickups.Length;
			foreach (var p in d.Players.Values)
			{
				var pID = Serialization.Encode(p.ID);
				var pos = Serialization.Encode(p.Position);
				var col = Serialization.Encode(p.Color);
				var killCount = Serialization.Encode(p.KillCount);
				var deathCount = Serialization.Encode(p.DeathCount);
				Array.Copy(pID, 0, bytes, offset, pID.Length);
				offset += pID.Length;
				Array.Copy(pos, 0, bytes, offset, pos.Length);
				offset += pos.Length;
				Array.Copy(col, 0, bytes, offset, col.Length);
				offset += col.Length;
				Array.Copy(killCount, 0, bytes, offset, killCount.Length);
				offset += killCount.Length;
				Array.Copy(deathCount, 0, bytes, offset, deathCount.Length);
				offset += deathCount.Length;
			}
			foreach (var p in d.Pickups)
			{
				var pID = Serialization.Encode(p.Key);
				var pos = Serialization.Encode(p.Value.pos);
				var state = Serialization.Encode(p.Value.Active);

				Array.Copy(pID, 0, bytes, offset, pID.Length);
				offset += pID.Length;
				Array.Copy(pos, 0, bytes, offset, pos.Length);
				offset += pos.Length;
				Array.Copy(state, 0, bytes, offset, state.Length);
				offset += state.Length;
			}
			return bytes;
		}
		public static ConnectingDynamicData Decode(byte[] bytes, int startIndex)
		{
			int offset = startIndex;
			var numPlayers = Serialization.DecodeInt(bytes, offset);
			offset += 4;
			var numPickups = Serialization.DecodeInt(bytes, offset);
			offset += 4;
			var players = new Dictionary<int, Engine.Player>(numPlayers);
			for (int i = 0; i < numPlayers; ++i)
			{
				var pID = Serialization.DecodeInt(bytes, offset);
				offset += 4;
				var pos = Serialization.DecodeVec3(bytes, offset);
				offset += OpenTK.Vector3.SizeInBytes;
				var col = Serialization.DecodeVec3(bytes, offset);
				offset += OpenTK.Vector3.SizeInBytes;
				var killCount = Serialization.DecodeInt(bytes, offset);
				offset += 4;
				var deathCount = Serialization.DecodeInt(bytes, offset);
				offset += 4;
				players.Add(pID, new Engine.Player(pID, pos, col, killCount, deathCount));
			}
			var pickups = new Dictionary<int, Engine.ShieldPickup>(numPickups);
			for (int i = 0; i < numPickups; ++i)
			{
				var pID = Serialization.DecodeInt(bytes, offset);
				offset += 4;
				var pos = Serialization.DecodeVec3(bytes, offset);
				offset += OpenTK.Vector3.SizeInBytes;
				var state = Serialization.DecodeBool(bytes, offset);
				offset += 1;
				pickups.Add(pID, new Engine.ShieldPickup(pos, state));
			}
			return new ConnectingDynamicData(players, pickups);
		}
		public static ConnectingDynamicData Decode(byte[] bytes)
		{
			return Decode(bytes, 0);
		}
		static readonly int bytesPerPlayer = 4 + 8 + OpenTK.Vector3.SizeInBytes * 2;
		static readonly int bytesPerPickup = 4 + 1 + OpenTK.Vector3.SizeInBytes;
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

