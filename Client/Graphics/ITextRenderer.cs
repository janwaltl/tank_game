using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Client.Graphics
{
	interface ITextRenderer
	{
		/// <summary>
		/// Draws the text in world coordinates.
		/// </summary>
		/// <param name="text">Text to draw.</param>
		/// <param name="pos">World coordinates of the first character.(botom left corner)</param>
		/// <param name="color">Color of the text.</param>
		/// <param name="glyphSize">Text height in world coordinates.</param>
		void DrawInWorld(string text, Vector3 pos, Vector3 color, float glyphSize);

	}
}
