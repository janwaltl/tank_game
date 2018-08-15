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
	/// </summary>
	public class ConnectingDynamicData
	{
		public Dictionary<int, Engine.Player> Players { get; }
		public ConnectingDynamicData(Dictionary<int, Engine.Player> players)
		{
			Players = players;
		}
		public static byte[] Encode(ConnectingDynamicData d)
		{
			var bytes = new byte[bytesPerPlayer * d.Players.Count];

			int offset = 0;
			foreach (var p in d.Players.Values)
			{
				var pID = Serialization.Encode(p.ID);
				var pos = Serialization.Encode(p.Position);
				var col = Serialization.Encode(p.Color);

				Array.Copy(pID, 0, bytes, offset, pID.Length);
				offset += pID.Length;
				Array.Copy(pos, 0, bytes, offset, pos.Length);
				offset += pos.Length;
				Array.Copy(col, 0, bytes, offset, col.Length);
				offset += col.Length;
			}
			return bytes;
		}
		public static ConnectingDynamicData Decode(byte[] bytes, int startIndex)
		{
			int numPlayers = (bytes.Length - startIndex) / bytesPerPlayer;
			var players = new Dictionary<int, Engine.Player>(numPlayers);
			int offset = startIndex;
			for (int i = 0; i < numPlayers; ++i)
			{
				var pID = Serialization.DecodeInt(bytes, offset);
				offset += 4;
				var pos = Serialization.DecodeVec3(bytes, offset);
				offset += OpenTK.Vector3.SizeInBytes;
				var col = Serialization.DecodeVec3(bytes, offset);
				offset += OpenTK.Vector3.SizeInBytes;

				players.Add(pID, new Engine.Player(pID, pos, col));
			}
			return new ConnectingDynamicData(players);
		}
		public static ConnectingDynamicData Decode(byte[] bytes)
		{
			return Decode(bytes, 0);
		}
		static readonly int bytesPerPlayer = 4 + OpenTK.Vector3.SizeInBytes * 2;
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

