using AssetManagementBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace RobotGameData
{
	public static partial class AMBExtensions
	{
		public static Texture2D LoadTexture2DPremultiply(this AssetManager assetManager, string assetName) =>
			assetManager.LoadTexture2D(FrameworkCore.GraphicsDevice, assetName, true, Color.Magenta);

		public static Effect LoadEffect2(this AssetManager manager, string assetName)
		{
			var folder = Path.GetDirectoryName(assetName);
			var file = Path.GetFileName(assetName);

#if FNA
			var path = folder + "/FNA/" + file;
#elif MONOGAME_DX
			var path = folder + "/MonoGameDX/" + file;
#else
			var path = folder + "/MonoGameGL/" + file;
#endif

			return manager.LoadEffect(FrameworkCore.GraphicsDevice, path);
		}
	}
}
