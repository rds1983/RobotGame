using AssetManagementBase;
using RobotGameData.ParticleSystem;
using RobotGameData.Utility;
using System.IO;
using System.Xml.Serialization;

namespace RobotGameData
{
	partial class AMBExtensions
	{
		private static AssetLoader<ParticleSequenceInfo> _particleSequenceInfoLoader = (manager, assetName, settings, tag) =>
		{
			if (!assetName.EndsWith(".Particle"))
			{
				assetName += ".Particle";
			}

			var data = manager.ReadAsString(assetName);
			return data.DeserializeXmlFromString<ParticleSequenceInfo>();
		};

		public static ParticleSequenceInfo LoadParticleSequenceInfo(this AssetManager assetManager, string assetName)
		{
			return assetManager.UseLoader(_particleSequenceInfoLoader, assetName);
		}
	}
}
