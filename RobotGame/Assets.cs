using System.IO;
using System.Reflection;
using System;
using AssetManagementBase;
using FontStashSharp;
using RobotGameData;

namespace RobotGame
{
	internal static class Assets
	{
		private static readonly AssetManager _assetManager = AssetManager.CreateFileAssetManager(Path.Combine(ExecutingAssemblyDirectory, "Assets"));
		private static FontSystem _defaultFontSystem;

		private static string ExecutingAssemblyDirectory
		{
			get
			{
				string codeBase = Assembly.GetExecutingAssembly().Location;
				UriBuilder uri = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uri.Path);
				return Path.GetDirectoryName(path);
			}
		}

		public static AssetManager AssetManager => _assetManager;

		public static FontSystem FontSystem
		{
			get
			{
				if (_defaultFontSystem == null)
				{
					_defaultFontSystem = _assetManager.LoadFontSystem("Font/Inter-Regular.ttf");
				}

				return _defaultFontSystem;
			}
		}
	}
}
