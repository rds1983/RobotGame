using NursiaModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace RobotGameData.Utility
{
	internal static class ModelUtils
	{
		public static Effect GetEffect(this NrmMeshPart submesh) => (Effect)submesh.Tag;

		public static void SetEffect(this NrmMeshPart submesh, Effect effect) => submesh.Tag = effect;

		public static void SetTransform(this NrmModelBone bone, Matrix transform)
		{
			bone.DefaultPose = new SrtTransform(transform);
		}

		public static Effect[] GetEffects(this NrmMesh mesh)
		{
			if (mesh.Tag != null)
			{
				return (Effect[])mesh.Tag;
			}

			var result = new List<Effect>();

			foreach (var meshpart in mesh.MeshParts)
			{
				var effect = meshpart.GetEffect();
				if (effect == null)
				{
					continue;
				}

				if (!result.Contains(effect))
				{
					result.Add(effect);
				}
			}

			mesh.Tag = result.ToArray();
			return (Effect[])mesh.Tag;
		}

		public static void Draw(this NrmMesh mesh)
		{
			var graphicsDevice = FrameworkCore.GraphicsDevice;

			for (int i = 0; i < mesh.MeshParts.Count; i++)
			{
				var meshpart = mesh.MeshParts[i];
				if (meshpart.PrimitiveCount > 0)
				{
					var effect = meshpart.GetEffect();
					for (int j = 0; j < effect.CurrentTechnique.Passes.Count; j++)
					{
						effect.CurrentTechnique.Passes[j].Apply();

						meshpart.Draw(graphicsDevice);
					}
				}
			}
		}

		public static Vector3 Center(this BoundingBox box)
		{
			return box.Min + (box.Max - box.Min) / 2;
		}

		public static BoundingSphere ToSphere(this BoundingBox b)
		{
			var m = (float)Math.Max(b.Max.X - b.Min.X, Math.Min(b.Max.Y - b.Min.Y, b.Max.Z - b.Min.Z));

			return new BoundingSphere(b.Center(), m / 2.0f);

		}
	}
}