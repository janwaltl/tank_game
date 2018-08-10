using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Client.Graphics
{
	/// <summary>
	/// Represents game's camera.
	/// </summary>
	class Camera : IView
	{
		public static readonly Vector3 defaultUp = new Vector3(0.0f, 1.0f, 0.0f);
		/// <summary>
		/// Creates target-oriented camera in the world, up vector is set to `defaultUp`.
		/// </summary>
		/// <param name="viewport">Pixel resolution of the screen.</param>
		/// <param name="camPos">Position of the camera in the world coords.</param>
		/// <param name="targetPos">Position of the target at which will the camera look at.</param>
		public Camera(Vector2 viewport, Vector3 camPos, Vector3 targetPos)
			: this(camPos, targetPos, defaultUp)
		{
			Viewport = viewport;
			Proj = Matrix4.Identity;
		}
		/// <summary>
		/// Same as the other ctor except with custom `up` vector
		/// </summary>
		public Camera(Vector3 camPos, Vector3 targetPos, Vector3 up)
		{
			LookAt(camPos, targetPos, up);
		}
		/// <summary>
		/// Orients the camera in the world.
		/// </summary>
		public void LookAt(Vector3 camPos, Vector3 targetPos, Vector3 up)
		{
			View = Matrix4.LookAt(camPos, targetPos, up);
		}
		/// <summary>
		/// Orients the camera in the world.
		/// </summary>
		/// <param name="dir">Direction in which the camera looks</param>
		public void Look(Vector3 camPos, Vector3 dir, Vector3 up)
		{
			View = Matrix4.LookAt(camPos, camPos + dir, up);
		}

		public float AspectRatio => Viewport.X / Viewport.Y;
		public Matrix4 Proj { get; set; }
		public Matrix4 View { get; set; }
		public Vector2 Viewport { get; set; }

	}
}
