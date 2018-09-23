﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Shared;
using Engine;
using OpenTK;
namespace Server
{
	class Program : IDisposable
	{
		/// <summary>
		/// Creates server
		/// </summary>
		/// <param name="tickTime">How much one tick takes in milliseconds</param>
		/// <param name="timeoutTime">How much time should pass from last client's update before the player is timed out.In seconds</param>
		public Program(double tickTime, double timeoutTime)
		{
			this.tickTime = tickTime;
			ticksToTimeout = (int)(timeoutTime / tickTime * 1000.0);

			eCmdsToExecute = new List<EngineCommand>();
			sCmdsToBroadcast = new List<ServerCommand>();
			BuildEngine();

			clientsManager = new ClientsManager(pID => new ConnectingStaticData(pID, engine.World.Arena));
			newDeathIDs = new Queue<Tuple<int, int>>();
		}

		void BuildEngine()
		{
			var world = new Engine.World(Arena.FromFile("Res/arena1.txt"));
			engine = new Engine.Engine(world);
			engine.PlayerHitEvent += Engine_PlayerHitEvent;
		}

		private void Engine_PlayerHitEvent(Player player, TankShell shell)
		{
			if (player.CurrShields > 0)
			{
				player.CurrShields = (byte)Math.Max(0, player.CurrShields - TankShell.shellDmg);
			}
			else
			{
				player.CurrHealth -= TankShell.shellDmg;
				if (player.CurrHealth <= 0)
					newDeathIDs.Enqueue(new Tuple<int, int>(shell.OwnerPID,player.ID));
			}
		}

		/// <summary>
		/// Infinite loop that implementes server logic
		/// </summary>
		void RunUpdateLoop()
		{
			Stopwatch watch = Stopwatch.StartNew();
			double accumulator = 0;
			while (true)
			{
				sCmdsToBroadcast.Clear();
				eCmdsToExecute.Clear();

				TickClients();
				ProcessReadyClients();

				ProcessClientUpdates(tickTime / 1000.0);
				UpdateEngine(tickTime / 1000.0, eCmdsToExecute);

				ProcessNewDeaths();
				BroadcastUpdates();

				accumulator = TickTiming(tickTime, watch, accumulator);
				watch.Restart();
			}
		}
		private void UpdateEngine(double dt, IEnumerable<EngineCommand> commands)
		{
			engine.RegenShields(dt);
			if (engine.RespawnPickups(dt))//Notify the clients to respawn their shield pickups
			{
				Console.WriteLine("R");
				sCmdsToBroadcast.Add(new RespawnShieldsCmd());
			}
			engine.ExecuteCommands(commands);
			engine.MoveShells(dt);

			engine.ResolvePlayersArenaCollisions(dt);
			engine.ResolvePlayersInterCollisions(dt);
			engine.ResolveShellCollisions(dt, true);
			var pickups = engine.ResolvePlayerPickupCollisions(dt);
			foreach (var p in pickups)
			{
				Console.WriteLine("Pick");
				sCmdsToBroadcast.Add(new Shared.UseShieldPickupCmd(p.Item2));
			}
		}
		/// <summary>
		/// Processes death from newDeathIDs queue.
		///  - generates PlayerDeathCmds.
		/// </summary>
		private void ProcessNewDeaths()
		{
			var deathCmds = new List<EngineCommand>();
			while (newDeathIDs.Count > 0)
			{
				var tuple = newDeathIDs.Dequeue();

				var respawnPos = engine.GetEmptySpawnPoint();
				var sCmd = new Shared.PlayerDeathCmd(tuple.Item1,tuple.Item2, new Vector3(respawnPos.X, respawnPos.Y, 0.0f));

				deathCmds.Add(sCmd.Translate());
				sCmdsToBroadcast.Add(sCmd);
			}
			engine.ServerExecDeathCmds(deathCmds);
		}
		/// <summary>
		/// Computes proper timing of the server loop. Waits if server is running too fast, accumulates debt if too slow.
		/// </summary>
		/// <param name="tickTime">How long should each server tick take</param>
		/// <param name="watch"></param>
		/// <param name="accumulator">Debt from previous tick</param>
		/// <returns>Time debt carried over to the next tick</returns>
		private static double TickTiming(double tickTime, Stopwatch watch, double accumulator)
		{
			var timeTicks = watch.ElapsedTicks;
			double elapsedMS = timeTicks / 1000.0 / Stopwatch.Frequency;
			//We didn't spend enough time processing
			//=>Use it to pay off the accumulator
			if (elapsedMS < tickTime)
			{
				double excessTime = tickTime - elapsedMS;
				if (accumulator > excessTime)//Can't pay everything
					accumulator -= excessTime;
				else//Sleep for the remainder
					Task.Delay((int)(excessTime - accumulator)).Wait();
			}
			else//We've spent too much time processing, subtract it from next tick
				accumulator += tickTime - elapsedMS;
			return accumulator;
		}
		//static int counter = 0;
		/// <summary>
		/// Empties current update queue and processes it. Resets timeout counter for players that sent an update.
		/// </summary>
		/// <param name="dt">Delta time in seconds</param>
		void ProcessClientUpdates(double dt)
		{
			foreach (var u in clientsManager.PollClientUpdates())
			{
				if (engine.World.players.TryGetValue(u.PlayerID, out Player player))
				{
					//Tick down the cooldown
					if (player.CurrFireCooldown > 0.0)
						player.CurrFireCooldown -= u.DT;
					eCmdsToExecute.Add(u.GenPlayerMovement());
					eCmdsToExecute.Add(u.GenTowerCmd());

					if (u.LeftMouse && player.CurrFireCooldown <= 0.0)
					{
						//Reset the cooldown
						player.CurrFireCooldown += Player.fireCooldown;
						//Shift to have zero angle=(1,0) dir
						var shootingAngle = player.TowerAngle - MathHelper.PiOver2;
						var dir = new Vector2((float)Math.Cos(shootingAngle), (float)Math.Sin(shootingAngle));
						var cmd = new Shared.PlayerFireCmd(u.PlayerID, dir, player.Position.Xy);
						sCmdsToBroadcast.Add(cmd);
						eCmdsToExecute.Add(cmd.Translate());
					}
				}
			}
		}
		/// <summary>
		/// Tick connected players, enqueues ServerCmds,EngineCmds representing timed-out players.
		/// </summary>
		void TickClients()
		{
			foreach (var timedOutID in clientsManager.TickClients(ticksToTimeout))
			{
				var sCmd = new Shared.PlayerDisconnectedCmd(timedOutID);
				sCmdsToBroadcast.Add(sCmd);
				eCmdsToExecute.Add(sCmd.Translate());
			}
		}
		/// <summary>
		/// Enqueques sCmds,eCmds creating players' tanks for the newly connected clients.
		/// </summary>
		void ProcessReadyClients()
		{
			var readyClients = clientsManager.ProcessReadyClients(new ConnectingDynamicData(engine.World.players, engine.World.shieldPickups));
			foreach (var pID in readyClients)
			{
				var spawnPos = engine.GetEmptySpawnPoint();

				var sCmd = new Shared.PlayerConnectedCmd(pID, new Vector3(0.0f, 0.0f, 1.0f),
					 new Vector3(spawnPos.X, spawnPos.Y, 0.0f));
				sCmdsToBroadcast.Add(sCmd);
				eCmdsToExecute.Add(sCmd.Translate());
			}
		}

		/// <summary>
		/// Sends new server state to all connected clients.
		/// </summary>
		private void BroadcastUpdates()
		{
			List<byte[]> messages = new List<byte[]>();
			var stateCmd = PreparePlayersUpdate();
			messages.Add(stateCmd.Encode());

			clientsManager.BroadCastServerCommand(0, stateCmd);
			foreach (var cmd in sCmdsToBroadcast)
				clientsManager.BroadCastServerCommand(0, cmd);
		}
		/// <summary>
		/// Builds and returns a command representing update to all players states.
		/// </summary>
		ServerCommand PreparePlayersUpdate()
		{
			var pStates = new List<PlayersStateCommand.PlayerState>();
			foreach (var p in engine.World.players.Values)
			{
				pStates.Add(new PlayersStateCommand.PlayerState(p.ID, p.Position, p.TankAngle, p.TowerAngle,
					p.CurrFireCooldown, p.CurrHealth, p.CurrShields));
			}
			return new PlayersStateCmd(pStates);
		}

		private List<ServerCommand> sCmdsToBroadcast;
		private List<EngineCommand> eCmdsToExecute;
		/// <summary>
		/// Client will be timed-out if they won't sent any ClientUpdates for this amount of server ticks.
		/// </summary>
		private readonly int ticksToTimeout;
		/// <summary>
		/// Amount of time between two server ticks in miliseconds.
		/// </summary>
		private readonly double tickTime;

		private ClientsManager clientsManager;
		private Engine.Engine engine;
		/// <summary>
		/// Killer,Killed IDs
		/// </summary>
		private Queue<Tuple<int,int>> newDeathIDs;
		static void Main(string[] args)
		{
			using (Program server = new Program(16.6, 10.0))
			{
				server.RunUpdateLoop();
			}
		}

		public void Dispose()
		{
			((IDisposable)clientsManager).Dispose();
		}
	}

}

