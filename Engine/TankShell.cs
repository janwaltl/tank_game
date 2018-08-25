using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Engine
{
	public class TankShell
	{
		public static float shellSpeed = 1.0f;
		public TankShell(Vector2 dir, Vector2 startPos, int ownerPID)
		{
			Dir = dir;
			position = startPos;
			OwnerPID = ownerPID;
		}
		public int OwnerPID { get; }
		public Vector2 Dir { get; }
		public Vector2 position;
	}
}
