using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Text.Json;

namespace RobotGameData.Utility
{
	internal static class JsonExtensions
	{
		public static Vector3 GetVector3(this Dictionary<string, object> dictionary, string key)
		{
			var el = (JsonElement)dictionary[key];

			return new Vector3(el[0].GetSingle(), el[1].GetSingle(), el[2].GetSingle());
		}

		public static float GetSingle(this Dictionary<string, object> dictionary, string key)
		{
			var el = (JsonElement)dictionary[key];

			return el.GetSingle();
		}

		public static bool GetBoolean(this Dictionary<string, object> dictionary, string key)
		{
			var el = (JsonElement)dictionary[key];

			return el.GetBoolean();
		}
	}
}
