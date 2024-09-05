using AssetManagementBase;
using RobotGameData.GameObject;
using RobotGameData.Utility;

namespace RobotGameData
{
	partial class AMBExtensions
	{
		private static AssetLoader<AnimationSequence> _animationSequenceLoader = (manager, assetName, settings, tag) =>
		{
			var data = manager.ReadAsString(assetName);

			return data.DeserializeXmlFromString<AnimationSequence>();
		};

		public static AnimationSequence LoadAnimationSequence(this AssetManager assetManager, string assetName)
		{
			return assetManager.UseLoader(_animationSequenceLoader, assetName);
		}
	}
}
