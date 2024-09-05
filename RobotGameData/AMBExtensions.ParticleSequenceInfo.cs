using AssetManagementBase;
using RobotGameData.ParticleSystem;

namespace RobotGameData
{
	partial class AMBExtensions
	{
		private static AssetLoader<ParticleSequenceInfo> _particleSequenceInfoLoader = (manager, assetName, settings, tag) =>
		{
			var data = manager.ReadAsString(assetName);

			return null;
		};

		public static ParticleSequenceInfo LoadParticleSequenceInfo(this AssetManager assetManager, string assetName)
		{
			return assetManager.UseLoader(_particleSequenceInfoLoader, assetName);
		}
	}
}
