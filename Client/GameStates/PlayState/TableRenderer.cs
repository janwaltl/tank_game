using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine;
using Client.Graphics;
using OpenTK;

namespace Client.Playing
{
	/// <summary>
	/// Renders table of players with their kills and deaths
	/// </summary>
	class TableRenderer
	{
		public void Draw(IEnumerable<Player> players,ITextRenderer textRenderer)
		{
			float yPos = 3.0f;
			textRenderer.DrawInScreen($"           Kills      Deaths", new Vector3(-7.0f, yPos, 1.0f), new Vector3(1.0f, 10f, 0.0f), 0.5f);
			yPos -= 0.5f;
			foreach (var p in players)
			{
				textRenderer.DrawInScreen($"Player{p.ID}    {p.KillCount}            {p.DeathCount}", new Vector3(-7.0f,yPos,1.0f), new Vector3(1.0f, 1.0f, 0.0f), 0.5f);
				yPos -= 0.5f;
			}
		}
	}
}
