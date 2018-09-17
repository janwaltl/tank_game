using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Shared;
namespace Client.GameStates
{
	class PlayingState : IGameState, IDisposable
	{
		/// <summary>
		/// Waits for staticData to be completed.
		/// </summary>
		public PlayingState(ServerManager serverManager, Input input)
		{
			sManager = serverManager;
			sManager.StaticData.Wait();
			var staticData = sManager.StaticData.Result;
			BuildEngine(staticData);

			this.input = input;
			this.playerID = staticData.PlayerID;

			renderer = new Playing.Renderer(input, engine, playerID);
		}
		public IGameState UpdateState(double dt)
		{
			//Wait for the dynamic data
			if (finishConnecting.IsCompleted)
			{
				SendClientupdate(dt);
				engine.ClientUpdate(ProcessServerCommands(), dt);
				//TEMP Do not rung Physics
			}
			//RESOLVE faulted status
			//else if finishConnecting.Status == TaskStatus.Faulted
			return this;
		}

		public void OnSwitch()
		{
			finishConnecting = FinishConnecting();
		}
		public void RenderState(double dt)
		{
			if (finishConnecting.IsCompleted)
				renderer.Render(dt);
		}
		public void Dispose()
		{
			sManager?.Dispose();
		}
		private void BuildEngine(ConnectingStaticData sData)
		{
			var world = new Engine.World(sData.Arena);
			engine = new Engine.Engine(world);
		}
		private async Task FinishConnecting()
		{
			var dynData = await sManager.FinishConnecting();
			engine.World.players = dynData.Players;
		}
		/// <summary>
		/// Sends updated client state/inputs to the server.
		/// </summary>
		private void SendClientupdate(double dt)
		{
			ClientUpdate.PressedKeys pressed = ClientUpdate.PressedKeys.None;
			//Polls input
			if (input.IsKeyPressed(OpenTK.Input.Key.W))
				pressed |= ClientUpdate.PressedKeys.W;
			if (input.IsKeyPressed(OpenTK.Input.Key.A))
				pressed |= ClientUpdate.PressedKeys.A;
			if (input.IsKeyPressed(OpenTK.Input.Key.S))
				pressed |= ClientUpdate.PressedKeys.S;
			if (input.IsKeyPressed(OpenTK.Input.Key.D))
				pressed |= ClientUpdate.PressedKeys.D;
			var left = input.IsMousePressed(OpenTK.Input.MouseButton.Left);
			var right = input.IsMousePressed(OpenTK.Input.MouseButton.Right);
			var cU = new ClientUpdate(playerID, pressed, input.CalcMouseAngle(), left, right, dt);
			sManager.SendClientUpdateAsync(cU).Detach();
		}

		/// <summary>
		/// Polls ServerCommands and translates them to engine commands.
		/// </summary>
		/// <returns></returns>
		private List<Engine.EngineCommand> ProcessServerCommands()
		{
			List<Engine.EngineCommand> commands = new List<Engine.EngineCommand>();
			foreach (var item in sManager.PollServerCommands())
				commands.Add(item.Translate());
			return commands;
		}

		readonly ServerManager sManager;
		readonly int playerID;

		Task finishConnecting;

		readonly Input input;
		Engine.Engine engine;
		Playing.Renderer renderer;
	}
}
