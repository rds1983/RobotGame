using AssetManagementBase;
using FontStashSharp;

namespace RobotGameData
{
	internal static class Resources
	{
		private static readonly AssetManager _assetManager = AssetManager.CreateResourceAssetManager(typeof(Resources).Assembly, "Resources");
		private static FontSystem _fontSystem;

		public static FontSystem DefaultFontSystem
		{
			get
			{
				if (_fontSystem == null)
				{
					_fontSystem = _assetManager.LoadFontSystem("Inter-Regular.ttf");
				}

				return _fontSystem;
			}
		}
	}
}
