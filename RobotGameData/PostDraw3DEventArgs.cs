using System;
using Microsoft.Xna.Framework.Graphics;

namespace RobotGame
{
	public class PostDraw3DEventArgs : EventArgs
	{
		public RenderTarget2D Scene { get; set; }

		public PostDraw3DEventArgs(RenderTarget2D scene)
			: base()
		{
			this.Scene = scene;
		}
	}
}