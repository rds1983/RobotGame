using AssetManagementBase;
using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotGameData.GameObject;
using RobotGameData.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RobotGameData
{
	partial class AMBExtensions
	{
		private static void SetTexture(AssetManager assetManager, Dictionary<string, object> material, Effect effect, string name)
		{
			object texturePath;
			if (!material.TryGetValue(name, out texturePath))
			{
				return;
			}

			var texture = assetManager.LoadTexture(FrameworkCore.GraphicsDevice, texturePath.ToString());
			effect.Parameters[name].SetValue(texture);
		}

		private static Dictionary<string, Effect[]> LoadMaterial(AssetManager assetManager, string assetName)
		{
			var assetMaterial = Path.ChangeExtension(assetName, "material");
			var materialData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>[]>>(assetManager.ReadAsString(assetMaterial));

			var materials = new Dictionary<string, Effect[]>();
			foreach (var pair in materialData)
			{
				var effects = new List<Effect>();
				foreach (var effectData in pair.Value)
				{
					Effect effect;
					if (effectData["Effect"].ToString() == "BasicEffect")
					{
						var basicEffect = new BasicEffect(FrameworkCore.GraphicsDevice)
						{
							DiffuseColor = effectData.GetVector3("DiffuseColor"),
							EmissiveColor = effectData.GetVector3("EmissiveColor"),
							SpecularColor = effectData.GetVector3("SpecularColor"),
							SpecularPower = effectData.GetSingle("SpecularPower"),
							Alpha = effectData.GetSingle("Alpha"),
							VertexColorEnabled = effectData.GetBoolean("VertexColorEnabled"),
							PreferPerPixelLighting = effectData.GetBoolean("PreferPerPixelLighting"),
							TextureEnabled = effectData.GetBoolean("TextureEnabled")
						};

						if (effectData.ContainsKey("Texture"))
						{
							var texture = assetManager.LoadTexture2DPremultiply(effectData["Texture"].ToString());
							basicEffect.Texture = texture;
						}

						effect = basicEffect;
					}
					else
					{
						effect = assetManager.LoadEffect2("/Effects/ShaderModelEffect.efb").Clone();

						SetTexture(assetManager, effectData, effect, "NormalMap");
						SetTexture(assetManager, effectData, effect, "SpecularMap");
						SetTexture(assetManager, effectData, effect, "Texture");
						SetTexture(assetManager, effectData, effect, "EnvironmentMap");
					}

					effects.Add(effect);
				}

				materials[pair.Key] = effects.ToArray();
			}

			return materials;
		}


		private static AssetLoader<ModelData> _modelLoader = (manager, assetName, settings, tag) =>
		{
			var model = manager.LoadGltf(FrameworkCore.GraphicsDevice, assetName);
			var materials = LoadMaterial(manager, assetName);

			// Set material
			foreach (var pair in materials)
			{
				var mesh = (from m in model.Meshes where m.Name == pair.Key select m).FirstOrDefault();
				if (mesh == null)
				{
					continue;
				}

				for (var i = 0; i < pair.Value.Length; ++i)
				{
					var part = mesh.MeshParts[i];
					part.SetEffect(pair.Value[i]);
				}
			}

			var collideFile = Path.ChangeExtension(assetName, "collide");
			if (manager.Exists(collideFile))
			{
				var fileData = manager.ReadAsString(collideFile);
				var tagData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(fileData);

				var vertices = new List<Vector3>();
				for (var i = 0; i < tagData["Vertices"].GetArrayLength(); ++i)
				{
					var ve = tagData["Vertices"][i];
					var v = new Vector3(ve[0].GetSingle(), ve[1].GetSingle(), ve[2].GetSingle());
					vertices.Add(v);
				}

				var vs = tagData["BoundingSphere"];
				var boundingSphere = new BoundingSphere(
					new Vector3(vs[0].GetSingle(), vs[1].GetSingle(), vs[2].GetSingle()),
					vs[3].GetSingle());

				var tagData2 = new Dictionary<string, object>
				{
					["Vertices"] = vertices.ToArray(),
					["BoundingSphere"] = boundingSphere
				};

				model.Tag = tagData2;
			}


			return new ModelData(model);
		};

		public static ModelData LoadModel(this AssetManager assetManager, string assetName)
		{
			return assetManager.UseLoader(_modelLoader, assetName);
		}
	}
}
