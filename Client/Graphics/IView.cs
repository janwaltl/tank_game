using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Client.Graphics
{
	/// <summary>
	/// Interface that provides collection of view-related variables.
	/// </summary>
	interface IView
	{
		Matrix4 View { get; }
		Matrix4 Proj { get; }
		/// <summary>
		/// Pixel-resolution of the screen.
		/// </summary>
		Vector2 Viewport { get; }
		/// <summary>
		/// Equal to Viewport.x/Viewport.y
		/// </summary>
		float AspectRatio { get; }
	}
}
