﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Linq;
using AssetManagementBase;
using glTFLoader;
using glTFLoader.Schema;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotGameData.Utility;
using static glTFLoader.Schema.Accessor;
using static glTFLoader.Schema.AnimationChannelTarget;
using Model = Microsoft.Xna.Framework.Graphics.Model;

namespace RobotGameData
{
	internal class GltfLoader
	{
		private const int MaximumBones = 96;

		struct VertexElementInfo
		{
			public VertexElementFormat Format;
			public VertexElementUsage Usage;
			public int UsageIndex;
			public int AccessorIndex;

			public VertexElementInfo(VertexElementFormat format, VertexElementUsage usage, int accessorIndex, int usageIndex)
			{
				Format = format;
				Usage = usage;
				AccessorIndex = accessorIndex;
				UsageIndex = usageIndex;
			}
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct VertexPositionNormalTextureSkin
		{
			public Vector3 Position;
			public Vector3 Normal;
			public Vector2 Texture;
			public byte i1, i2, i3, i4;
			public float w1, w2, w3, w4;
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct VertexNormalPosition
		{
			public Vector3 Normal;
			public Vector3 Position;
		}

		private struct PathInfo
		{
			public int Sampler;
			public PathEnum Path;

			public PathInfo(int sampler, PathEnum path)
			{
				Sampler = sampler;
				Path = path;
			}
		}

		private AssetManager _assetManager;
		private string _assetName;
		private Gltf _gltf;
		private readonly Dictionary<int, byte[]> _bufferCache = new Dictionary<int, byte[]>();
		private readonly List<ModelMesh> _meshes = new List<ModelMesh>();
		private Model _model;
		private readonly List<ModelBone> _allNodes = new List<ModelBone>();
		private readonly Dictionary<int, ModelBoneCollection> _skinCache = new Dictionary<int, ModelBoneCollection>();
		private readonly Dictionary<string, Effect[]> _materials = new Dictionary<string, Effect[]>();

		private byte[] FileResolver(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				using (var stream = _assetManager.Open(_assetName))
				{
					return Interface.LoadBinaryBuffer(stream);
				}
			}

			return _assetManager.ReadAsByteArray(path);
		}

		private byte[] GetBuffer(int index)
		{
			byte[] result;
			if (_bufferCache.TryGetValue(index, out result))
			{
				return result;
			}

			result = _gltf.LoadBinaryBuffer(index, path => FileResolver(path));
			_bufferCache[index] = result;

			return result;
		}

		private ArraySegment<byte> GetBufferView(int bufferViewIndex)
		{
			var bufferView = _gltf.BufferViews[bufferViewIndex];
			var buffer = GetBuffer(bufferView.Buffer);

			return new ArraySegment<byte>(buffer, bufferView.ByteOffset, bufferView.ByteLength);
		}

		private ArraySegment<byte> GetAccessorData(int accessorIndex)
		{
			var accessor = _gltf.Accessors[accessorIndex];
			if (accessor.BufferView == null)
			{
				throw new NotSupportedException("Accessors without buffer index arent supported");
			}

			var bufferView = _gltf.BufferViews[accessor.BufferView.Value];
			var buffer = GetBuffer(bufferView.Buffer);

			var size = accessor.Type.GetComponentCount() * accessor.ComponentType.GetComponentSize();
			return new ArraySegment<byte>(buffer, bufferView.ByteOffset + accessor.ByteOffset, accessor.Count * size);
		}

		private T[] GetAccessorAs<T>(int accessorIndex)
		{
			var type = typeof(T);
			if (type != typeof(float) && type != typeof(Vector3) && type != typeof(Vector4) && type != typeof(Quaternion) && type != typeof(Matrix))
			{
				throw new NotSupportedException("Only float/Vector3/Vector4 types are supported");
			}

			var accessor = _gltf.Accessors[accessorIndex];
			if (accessor.Type == Accessor.TypeEnum.SCALAR && type != typeof(float))
			{
				throw new NotSupportedException("Scalar type could be converted only to float");
			}

			if (accessor.Type == Accessor.TypeEnum.VEC3 && type != typeof(Vector3))
			{
				throw new NotSupportedException("VEC3 type could be converted only to Vector3");
			}

			if (accessor.Type == Accessor.TypeEnum.VEC4 && type != typeof(Vector4) && type != typeof(Quaternion))
			{
				throw new NotSupportedException("VEC4 type could be converted only to Vector4 or Quaternion");
			}

			if (accessor.Type == Accessor.TypeEnum.MAT4 && type != typeof(Matrix))
			{
				throw new NotSupportedException("MAT4 type could be converted only to Matrix");
			}

			var bytes = GetAccessorData(accessorIndex);

			var count = bytes.Count / Marshal.SizeOf(typeof(T));
			var result = new T[count];

			GCHandle handle = GCHandle.Alloc(result, GCHandleType.Pinned);
			try
			{
				IntPtr pointer = handle.AddrOfPinnedObject();
				Marshal.Copy(bytes.Array, bytes.Offset, pointer, bytes.Count);
			}
			finally
			{
				if (handle.IsAllocated)
				{
					handle.Free();
				}
			}

			return result;
		}

		private VertexElementFormat GetAccessorFormat(int index)
		{
			var accessor = _gltf.Accessors[index];

			switch (accessor.Type)
			{
				case Accessor.TypeEnum.VEC2:
					if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
					{
						return VertexElementFormat.Vector2;
					}
					break;
				case Accessor.TypeEnum.VEC3:
					if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
					{
						return VertexElementFormat.Vector3;
					}
					break;
				case Accessor.TypeEnum.VEC4:
					if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
					{
						return VertexElementFormat.Vector4;
					}
					else if (accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_BYTE)
					{
						return VertexElementFormat.Byte4;
					}
					else if (accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
					{
						return VertexElementFormat.Short4;
					}
					break;
			}

			throw new NotSupportedException($"Accessor of type {accessor.Type} and component type {accessor.ComponentType} isn't supported");
		}

		private IndexBuffer CreateIndexBuffer(MeshPrimitive primitive)
		{
			if (primitive.Indices == null)
			{
				throw new NotSupportedException("Meshes without indices arent supported");
			}

			var indexAccessor = _gltf.Accessors[primitive.Indices.Value];
			if (indexAccessor.Type != TypeEnum.SCALAR)
			{
				throw new NotSupportedException("Only scalar index buffer are supported");
			}

			if (indexAccessor.ComponentType != ComponentTypeEnum.SHORT &&
				indexAccessor.ComponentType != ComponentTypeEnum.UNSIGNED_SHORT &&
				indexAccessor.ComponentType != ComponentTypeEnum.UNSIGNED_INT)
			{
				throw new NotSupportedException($"Index of type {indexAccessor.ComponentType} isn't supported");
			}

			var indexData = GetAccessorData(primitive.Indices.Value);

			var elementSize = (indexAccessor.ComponentType == ComponentTypeEnum.SHORT ||
				indexAccessor.ComponentType == ComponentTypeEnum.UNSIGNED_SHORT) ?
				IndexElementSize.SixteenBits : IndexElementSize.ThirtyTwoBits;

			var indexBuffer = new IndexBuffer(FrameworkCore.GraphicsDevice, elementSize, indexAccessor.Count, BufferUsage.None);
			indexBuffer.SetData(0, indexData.Array, indexData.Offset, indexData.Count);

			// Since gltf uses ccw winding by default
			// We need to unwind it
			if (indexAccessor.ComponentType == ComponentTypeEnum.UNSIGNED_SHORT)
			{
				var data = new ushort[indexData.Count / 2];
				System.Buffer.BlockCopy(indexData.Array, indexData.Offset, data, 0, indexData.Count);

				for (var i = 0; i < data.Length / 3; i++)
				{
					var temp = data[i * 3];
					data[i * 3] = data[i * 3 + 2];
					data[i * 3 + 2] = temp;
				}

				indexBuffer.SetData(data);
			}
			else if (indexAccessor.ComponentType == ComponentTypeEnum.SHORT)
			{
				var data = new short[indexData.Count / 2];
				System.Buffer.BlockCopy(indexData.Array, indexData.Offset, data, 0, indexData.Count);

				for (var i = 0; i < data.Length / 3; i++)
				{
					var temp = data[i * 3];
					data[i * 3] = data[i * 3 + 2];
					data[i * 3 + 2] = temp;
				}

				indexBuffer.SetData(data);
			}
			else
			{
				var data = new uint[indexData.Count / 4];
				System.Buffer.BlockCopy(indexData.Array, indexData.Offset, data, 0, indexData.Count);

				for (var i = 0; i < data.Length / 3; i++)
				{
					var temp = data[i * 3];
					data[i * 3] = data[i * 3 + 2];
					data[i * 3 + 2] = temp;
				}

				indexBuffer.SetData(data);
			}

			return indexBuffer;
		}

		private void LoadMeshes()
		{
			foreach (var glbMesh in _gltf.Meshes)
			{
				var meshes = new List<ModelMeshPart>();
				var positions = new List<Vector3>();
				foreach (var primitive in glbMesh.Primitives)
				{
					positions.Clear();
					if (primitive.Mode != MeshPrimitive.ModeEnum.TRIANGLES)
					{
						throw new NotSupportedException($"Primitive mode {primitive.Mode} isn't supported.");
					}

					// Read vertex declaration
					var vertexInfos = new List<VertexElementInfo>();
					int? vertexCount = null;
					foreach (var pair in primitive.Attributes)
					{
						var accessor = _gltf.Accessors[pair.Value];
						var newVertexCount = accessor.Count;
						if (vertexCount != null && vertexCount.Value != newVertexCount)
						{
							throw new NotSupportedException($"Vertex count changed. Previous value: {vertexCount}. New value: {newVertexCount}");
						}

						vertexCount = newVertexCount;

						var element = new VertexElementInfo();
						if (pair.Key == "POSITION")
						{
							element.Usage = VertexElementUsage.Position;
						}
						else if (pair.Key == "NORMAL")
						{
							element.Usage = VertexElementUsage.Normal;
						}
						else if (pair.Key == "TANGENT" || pair.Key == "_TANGENT")
						{
							element.Usage = VertexElementUsage.Tangent;
						}
						else if (pair.Key == "BINORMAL" || pair.Key == "_BINORMAL")
						{
							element.Usage = VertexElementUsage.Binormal;
						}
						else if (pair.Key.StartsWith("TEXCOORD_"))
						{
							element.Usage = VertexElementUsage.TextureCoordinate;
							element.UsageIndex = int.Parse(pair.Key.Substring(9));
						}
						else if (pair.Key.StartsWith("JOINTS_"))
						{
							element.Usage = VertexElementUsage.BlendIndices;
							element.UsageIndex = int.Parse(pair.Key.Substring(7));
						}
						else if (pair.Key.StartsWith("WEIGHTS_"))
						{
							element.Usage = VertexElementUsage.BlendWeight;
							element.UsageIndex = int.Parse(pair.Key.Substring(8));
						}
						else if (pair.Key.StartsWith("COLOR_"))
						{
							element.Usage = VertexElementUsage.Color;
							element.UsageIndex = int.Parse(pair.Key.Substring(6));
						}
						else
						{
							throw new Exception($"Attribute of type '{pair.Key}' isn't supported.");
						}

						element.Format = GetAccessorFormat(pair.Value);
						element.AccessorIndex = pair.Value;

						vertexInfos.Add(element);
					}

					if (vertexCount == null)
					{
						throw new NotSupportedException("Vertex count is not set");
					}

					var vertexElements = new VertexElement[vertexInfos.Count];
					var offset = 0;
					for (var i = 0; i < vertexInfos.Count; ++i)
					{
						vertexElements[i] = new VertexElement(offset, vertexInfos[i].Format, vertexInfos[i].Usage, vertexInfos[i].UsageIndex);
						offset += vertexInfos[i].Format.GetSize();
					}

					var vd = new VertexDeclaration(vertexElements);
					var vertexBuffer = new VertexBuffer(FrameworkCore.GraphicsDevice, vd, vertexCount.Value, BufferUsage.None);

					// Set vertex data
					var vertexData = new byte[vertexCount.Value * vd.VertexStride];
					offset = 0;
					for (var i = 0; i < vertexInfos.Count; ++i)
					{
						var sz = vertexInfos[i].Format.GetSize();
						var data = GetAccessorData(vertexInfos[i].AccessorIndex);

						for (var j = 0; j < vertexCount.Value; ++j)
						{
							Array.Copy(data.Array, data.Offset + j * sz, vertexData, j * vd.VertexStride + offset, sz);

							if (vertexInfos[i].Usage == VertexElementUsage.Position)
							{
								unsafe
								{
									fixed (byte* bptr = &data.Array[data.Offset + j * sz])
									{
										Vector3* vptr = (Vector3*)bptr;
										positions.Add(*vptr);
									}
								}
							}
						}

						offset += sz;
					}

					vertexBuffer.SetData(vertexData);

					if (primitive.Indices == null)
					{
						throw new NotSupportedException("Meshes without indices arent supported");
					}

					var indexAccessor = _gltf.Accessors[primitive.Indices.Value];
					if (indexAccessor.Type != Accessor.TypeEnum.SCALAR)
					{
						throw new NotSupportedException("Only scalar index buffer are supported");
					}

					if (indexAccessor.ComponentType != Accessor.ComponentTypeEnum.SHORT &&
						indexAccessor.ComponentType != Accessor.ComponentTypeEnum.UNSIGNED_SHORT &&
						indexAccessor.ComponentType != Accessor.ComponentTypeEnum.UNSIGNED_INT)
					{
						throw new NotSupportedException($"Index of type {indexAccessor.ComponentType} isn't supported");
					}

					var indexBuffer = CreateIndexBuffer(primitive);

					Effect effect;
					if (_assetName.Contains("Sky"))
					{
						effect = new BasicEffect(FrameworkCore.GraphicsDevice);
					}
					else
					{
						effect = _assetManager.LoadEffect2("/Effects/ShaderModelEffect.efb").Clone();
					}

					var mesh = XNA.CreateModelMeshPart();
					mesh.SetVertexBuffer(vertexBuffer);
					mesh.SetIndexBuffer(indexBuffer);
					mesh.SetPrimitiveCount(indexBuffer.IndexCount / 3);

					meshes.Add(mesh);
				}

				var modelMesh = XNA.CreateModelMesh(meshes);
				modelMesh.SetName(glbMesh.Name);

				var sphere = BoundingSphere.CreateFromPoints(positions);
				modelMesh.SetBoundingSphere(sphere);

				_meshes.Add(modelMesh);
			}
		}

		private void LoadAllNodes()
		{
			// First run - load all nodes
			for (var i = 0; i < _gltf.Nodes.Length; ++i)
			{
				var glbNode = _gltf.Nodes[i];

				var modelBone = XNA.CreateModelBone();

				modelBone.SetName(glbNode.Name);
				modelBone.SetIndex(i);

				var defaultTranslation = glbNode.Translation != null ? glbNode.Translation.ToVector3() : Vector3.Zero;
				var defaultScale = glbNode.Scale != null ? glbNode.Scale.ToVector3() : Vector3.One;
				var defaultRotation = glbNode.Rotation != null ? glbNode.Rotation.ToQuaternion() : Quaternion.Identity;

				modelBone.Transform = Mathematics.CreateTransform(defaultTranslation, defaultScale, defaultRotation);

				if (glbNode.Matrix != null)
				{
					var matrix = glbNode.Matrix.ToMatrix();
					modelBone.Transform = matrix;
				}

				if (glbNode.Mesh != null)
				{
					var mesh = _meshes[glbNode.Mesh.Value];
					modelBone.AddMesh(mesh);

					if (string.IsNullOrEmpty(mesh.Name))
					{
						mesh.SetName(modelBone.Name);
					}

					mesh.SetParentBone(modelBone);
				}

				_allNodes.Add(modelBone);
			}

			// Second run - set children and skins
			for (var i = 0; i < _gltf.Nodes.Length; ++i)
			{
				var glbNode = _gltf.Nodes[i];
				var modelBone = _allNodes[i];

				if (glbNode.Children != null)
				{
					foreach (var childIndex in glbNode.Children)
					{
						var childNode = _allNodes[childIndex];

						childNode.SetParent(modelBone);
						modelBone.AddChild(childNode);
					}
				}
			}
		}

		private void SetTexture(Dictionary<string, object> material, Effect effect, string name)
		{
			object texturePath;
			if (!material.TryGetValue(name, out texturePath))
			{
				return;
			}

			var texture = _assetManager.LoadTexture(FrameworkCore.GraphicsDevice, texturePath.ToString());
			effect.Parameters[name].SetValue(texture);
		}

		private void LoadMaterial()
		{
			var assetMaterial = Path.ChangeExtension(_assetName, "material");
			var materialData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>[]>>(_assetManager.ReadAsString(assetMaterial));

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
							var texture = _assetManager.LoadTexture2DPremultiply(effectData["Texture"].ToString());
							basicEffect.Texture = texture;
						}

						effect = basicEffect;
					}
					else
					{
						effect = _assetManager.LoadEffect2("/Effects/ShaderModelEffect.efb").Clone();

						SetTexture(effectData, effect, "NormalMap");
						SetTexture(effectData, effect, "SpecularMap");
						SetTexture(effectData, effect, "Texture");
						SetTexture(effectData, effect, "EnvironmentMap");
					}

					effects.Add(effect);
				}

				_materials[pair.Key] = effects.ToArray();
			}
		}

		public Model Load(AssetManager manager, string assetName)
		{
			Debug.WriteLine(assetName);

			_meshes.Clear();
			_allNodes.Clear();
			_skinCache.Clear();
			_materials.Clear();

			_assetManager = manager;
			_assetName = assetName;

			LoadMaterial();

			using (var stream = manager.Open(assetName))
			{
				_gltf = Interface.LoadModel(stream);
			}

			LoadMeshes();
			LoadAllNodes();

			_model = XNA.CreateModel(_allNodes, _meshes);

			var scene = _gltf.Scenes[_gltf.Scene.Value];
			_model.SetRoot(_allNodes[scene.Nodes[0]]);

			// Set material
			foreach (var pair in _materials)
			{
				var mesh = (from m in _meshes where m.Name == pair.Key select m).FirstOrDefault();
				if (mesh == null)
				{
					continue;
				}

				for (var i = 0; i < pair.Value.Length; ++i)
				{
					var part = mesh.MeshParts[i];
					part.Effect = pair.Value[i];
				}
			}

			var totalVertices = 0;
			foreach(var mesh in _meshes)
			{
				foreach(var part in mesh.MeshParts)
				{
					totalVertices += part.VertexBuffer.VertexCount;
				}
			}

			var collideFile = Path.ChangeExtension(assetName, "collide");
			if (_assetManager.Exists(collideFile))
			{
				var fileData = _assetManager.ReadAsString(collideFile);
				var tagData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(fileData);

				var vertices = new List<Vector3>();
				for(var i = 0; i < tagData["Vertices"].GetArrayLength(); ++i)
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

				_model.Tag = tagData2;
			}

			return _model;
		}
	}
}