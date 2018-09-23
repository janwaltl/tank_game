using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Engine
{
	public class ShieldPickup
	{
		public ShieldPickup(Vector3 pos, bool state)
		{
			this.pos = pos;
			Active = state;
		}
		public Vector3 pos;
		public bool Active { get; set; }
	}
}
