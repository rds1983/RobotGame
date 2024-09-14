using RobotGameData.ParticleSystem;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;
using System.Xml.Serialization;

namespace RobotGameData.Utility
{
	internal static class XmlExtensions
	{
		public static T DeserializeXmlFromString<T>(this string data)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (var reader = new StringReader(data))
			{
				return (T)serializer.Deserialize(reader);
			}
		}
	}
}
