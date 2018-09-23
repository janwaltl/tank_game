using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Client.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Client.Playing
{
	/// <summary>
	/// Renders active shield pickups located in the arena
	/// </summary>
	class ShieldPickupRenderer
	{
		const float shieldDims = 1.0f;
		/// <summary>
		/// Renders shields as big 'O'
		/// </summary>
		public void Render(IEnumerable<Engine.ShieldPickup> pickups, ITextRenderer text)
		{
			foreach (var p in pickups)
			{
				if (p.Active)
					text.DrawInWorld("O", p.pos - new Vector3(shieldDims / 2, shieldDims / 2, 0.0f), new Vector3(0.3f, 0.3f, 1.0f), shieldDims);
			}
		}
	}
}
