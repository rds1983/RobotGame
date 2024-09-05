using AssetManagementBase;
using RobotGameData.GameObject;

namespace RobotGameData
{
	partial class AMBExtensions
	{
		private static AssetLoader<AnimationSequence> _animationSequenceLoader = (manager, assetName, settings, tag) =>
		{
			return null;
		};

		public static AnimationSequence LoadAnimationSequence(this AssetManager assetManager, string assetName)
		{
			return assetManager.UseLoader(_animationSequenceLoader, assetName);
		}
	}
}
