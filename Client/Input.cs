using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Input;
namespace Client
{
	/// <summary>
	/// Provides access to keyboard, mouse and window input.
	/// </summary>
	interface IInput
	{
		bool IsKeyPressed(Key k);
		bool IsMousePressed(MouseButton m);
		/// <summary>
		/// Pixel coordinates of the mouse in the screen. Negative if the mouse is not in the screen.
		/// </summary>
		/// <returns></returns>
		Vector2 MousePos();
		/// <summary>
		/// Returns pixel resolution of the window
		/// </summary>
		/// <returns></returns>
		Vector2 Viewport();
	}
	/// <summary>
	/// Encapsulates the mouse and keyboard and provides means for polling their state.
	/// </summary>
	class Input : IInput
	{
		public static readonly Vector2 mouseOut = new Vector2(-1.0f, -1.0f);
		public Input(Vector2 viewport)
		{
			this.viewport = viewport;
		}
		public void SetKey(Key k, bool pressed)
		{
			keys[(int)k] = pressed;
		}
		public void SetMouse(MouseButton m, bool pressed)
		{
			mouse[(int)m] = pressed;
		}
		public void SetMousePos(Vector2 newPos)
		{
			mousePos = newPos; ;
		}
		public void SetViewport(Vector2 newRes)
		{
			viewport = newRes;
		}
		public bool IsKeyPressed(Key k)
		{
			return keys[(int)k];
		}
		public bool IsMousePressed(MouseButton m)
		{
			return mouse[(int)m];
		}
		public Vector2 MousePos()
		{
			return mousePos;
		}
		public Vector2 Viewport()
		{
			return viewport;
		}

		readonly bool[] keys = new bool[(int)Key.LastKey];
		readonly bool[] mouse = new bool[(int)MouseButton.LastButton];
		Vector2 mousePos;
		Vector2 viewport;
	}
}
