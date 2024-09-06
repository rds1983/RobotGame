﻿#region File Description
//-----------------------------------------------------------------------------
// GamePointSprite.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotGameData.Render;
using RobotGameData.Resource;
using RobotGameData.GameInterface;
using RobotGameData.Helper;
#endregion

namespace RobotGameData.GameObject
{
	/// <summary>
	/// This sprite constantly looks at the view matrix at anywhere in the 3D world.
	/// A rotation feature is not provided.
	/// </summary>
	public class GamePointSprite : GameMesh
	{
		#region PointSpriteObject

		public class PointSpriteObject : INamed
		{
			[Flags]
			public enum UpdateTypes
			{
				None = 0x00000000,
				Position = 0x00000001,
				Texturecoord = 0x00000002,
				Color = 0x00000004,
				Rotation = 0x00000010,
			}

			public enum RotationAngle
			{
				Angle0 = 0,
				Angle90,
				Angle180,
				Angle270,
			}

			String name = String.Empty;
			bool enable = true;
			Vector3 center = Vector3.Zero;
			float width = 1.0f;
			float height = 1.0f;
			Color color = new Color(255, 255, 255, 255);
			RotationAngle rotation = RotationAngle.Angle0;
			Vector2[] uv = new Vector2[2]
				{
					Vector2.Zero,
					Vector2.One,
				};
			bool flipX = false;
			bool flipY = false;
			uint updateFlag = 0;

			public string Name
			{
				get { return name; }
				set { name = value; }
			}

			public bool Enable
			{
				get { return this.enable; }
				set { this.enable = value; }
			}

			public Vector3 Center
			{
				get { return this.center; }
				set { this.center = value; }
			}

			public float Width
			{
				get { return this.width; }
				set { this.width = value; }
			}

			public float Height
			{
				get { return this.height; }
				set { this.height = value; }
			}

			public Color Color
			{
				get { return this.color; }
				set { this.color = value; }
			}

			public RotationAngle Rotation
			{
				get { return this.rotation; }
				set { this.rotation = value; }
			}

			public bool FlipX
			{
				get { return this.flipX; }
				set { this.flipX = value; }
			}

			public bool FlipY
			{
				get { return this.flipY; }
				set { this.flipY = value; }
			}

			public void AddUpdateType(UpdateTypes updateType)
			{
				this.updateFlag |= (uint)updateType;
			}

			public void RemoveUpdateType(UpdateTypes updateType)
			{
				this.updateFlag &= ~(uint)updateType;
			}

			public Vector2 MinUV
			{
				get { return this.uv[0]; }
				set { this.uv[0] = value; }
			}

			public Vector2 MaxUV
			{
				get { return this.uv[1]; }
				set { this.uv[1] = value; }
			}
		}

		#endregion

		#region Fields

		const int vertexStride = 4;
		const int indexStride = 6;

		List<PointSpriteObject> pointSpriteList = new List<PointSpriteObject>();

		RenderingSpace space = RenderingSpace.World;
		Matrix lastViewMatrix;
		int count = 0;

		bool needToUpdate = false;
		bool alwaysUpdate = false;

		#endregion

		#region Properties

		public int Count
		{
			get { return this.count; }
		}

		#endregion

		/// <summary>
		/// updates the vertex data and draws
		/// </summary>
		/// <param name="renderTracer">render information</param>
		protected override void OnDraw(RenderTracer renderTracer)
		{
			int objectCount = 0;

			for (int i = 0; i < this.count; i++)
			{
				if (pointSpriteList[i].Enable == false) continue;
				objectCount++;
			}

			if (objectCount == 0) return;

			PrimitiveCount = objectCount * 2;
			UpdateVertexCount = this.count * vertexStride;

			this.alwaysUpdate = true;

			//  needs to update?
			if (renderTracer.View != lastViewMatrix)
			{
				needToUpdate = true;
				this.lastViewMatrix = renderTracer.View;
			}

			if (this.alwaysUpdate || needToUpdate)
			{
				int vertexOffset = 0;
				int indexOffset = 0;

				// calculates inverse view matrix.
				Matrix transformMatrix =
					Helper3D.Transpose(this.TransformedMatrix * renderTracer.View);

				for (int i = 0; i < this.count; i++)
				{
					PointSpriteObject obj = pointSpriteList[i];

					if (obj.Enable == false) continue;

					//  updates vertex positions.
					SetBufferPosition(ref vertexData, obj, vertexOffset,
									  transformMatrix, this.space);

					//  updates texture coordinates.
					SetBufferTextureCoord(ref vertexData, obj,
										  vertexOffset, this.space);

					//  updates vertex colors.
					SetBufferColor(ref vertexData, obj, vertexOffset);

					indexData[indexOffset + 0] = (short)(vertexOffset + 0);
					indexData[indexOffset + 1] = (short)(vertexOffset + 1);
					indexData[indexOffset + 2] = (short)(vertexOffset + 2);
					indexData[indexOffset + 3] = (short)(vertexOffset + 3);
					indexData[indexOffset + 4] = (short)(vertexOffset + 0);
					indexData[indexOffset + 5] = (short)(vertexOffset + 2);

					vertexOffset += vertexStride;
					indexOffset += indexStride;
				}

				if (userPrimitive == false)
				{
					//  binds the vertex buffer.
					BindVertexBuffer();

					//  binds the index buffer.
					BindIndexBuffer();
				}

				if (needToUpdate)
					needToUpdate = false;
			}

			// draws mesh
			BlendState = BlendState.Additive;
			RasterizerState = RasterizerState.CullNone;
			base.OnDraw(renderTracer);
		}

		/// <summary>
		/// create point sprite objects using the texture.
		/// </summary>
		/// <param name="count">sprite object count</param>
		/// <param name="fileName">texture file name</param>
		/// <param name="space">3D render space</param>
		/// <param name="alwaysUpdate"></param>
		public void Create(int count, string fileName,
							RenderingSpace space, bool alwaysUpdate)
		{
			//  load a texture.
			GameResourceTexture2D resource =
							FrameworkCore.ResourceManager.LoadTexture(fileName);

			Create(count, resource.Texture2D, space, alwaysUpdate);
		}

		/// <summary>
		/// create point sprite objects using the texture.
		/// </summary>
		/// <param name="count">sprite object count</param>
		/// <param name="texture">rexture resource</param>
		/// <param name="space">3D render space</param>
		/// <param name="alwaysUpdate"></param>
		public void Create(int count, Texture2D texture,
							RenderingSpace space, bool alwaysUpdate)
		{
			this.count = count;
			this.space = space;
			this.alwaysUpdate = alwaysUpdate;

			// create sprite objects.
			for (int i = 0; i < count; i++)
				pointSpriteList.Add(new PointSpriteObject());

			base.Create(this.count * vertexStride, this.count * indexStride, texture);
		}

		protected override void UnloadContent()
		{
			pointSpriteList.Clear();

			base.UnloadContent();
		}

		/// <summary>
		/// enables/disables all sprite objects.
		/// </summary>
		public void SetObjectEnable(bool enable)
		{
			for (int i = 0; i < pointSpriteList.Count; i++)
				SetObjectEnable(i, enable);
		}

		/// <summary>
		/// enables/disables an individual sprite object.
		/// </summary>
		public void SetObjectEnable(int index, bool enable)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			if (pointSpriteList[index].Enable != enable)
			{
				pointSpriteList[index].Enable = enable;
				needToUpdate = true;
			}
		}

		/// <summary>
		/// configures a center position of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="position">a center position</param>
		public void SetCenter(int index, Vector3 position)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			pointSpriteList[index].Center = position;
			pointSpriteList[index].AddUpdateType(PointSpriteObject.UpdateTypes.Position);

			needToUpdate = true;
		}

		/// <summary>
		/// configures a center position of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="x">x-component of center position of the sprite object</param>
		/// <param name="y">y-component of center position of the sprite object</param>
		/// <param name="z">z-component of center position of the sprite object</param>
		public void SetCenter(int index, float x, float y, float z)
		{
			SetCenter(index, new Vector3(x, y, z));
		}

		/// <summary>
		/// gets a left top position of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="vec">out vector</param>
		public void GetLeftTop(int index, ref Vector3 vec)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			PointSpriteObject obj = pointSpriteList[index];

			vec.X = obj.Center.X - (obj.Width * 0.5f);
			vec.Y = obj.Center.Y - (obj.Height * 0.5f);
			vec.Z = obj.Center.Z;
		}

		/// <summary>
		/// gets a right down position of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="vec">out vector</param>
		public void GetRightBottom(int index, ref Vector3 vec)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			PointSpriteObject obj = pointSpriteList[index];

			vec.X = obj.Center.X + (obj.Width * 0.5f);
			vec.Y = obj.Center.Y + (obj.Height * 0.5f);
			vec.Z = obj.Center.Z;
		}

		/// <summary>
		/// configures a rotation value of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="rotation">rotation angle</param>
		public void SetRotation(int index, PointSpriteObject.RotationAngle rotation)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			pointSpriteList[index].Rotation = rotation;
			needToUpdate = true;
		}

		/// <summary>
		/// flips each sprite object around x-axis and/or y-axis.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="flipX">specifies whether to rotate around x-axis</param>
		/// <param name="flipY">specifies whether to rotate around y-axis</param>
		public void SetFlip(int index, bool flipX, bool flipY)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			pointSpriteList[index].FlipX = flipX;
			pointSpriteList[index].FlipY = flipY;

			needToUpdate = true;
		}

		/// <summary>
		/// configures the size of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="size">size vector</param>
		public void SetSize(int index, Vector2 size)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			pointSpriteList[index].Width = size.X;
			pointSpriteList[index].Height = size.Y;

			pointSpriteList[index].AddUpdateType(PointSpriteObject.UpdateTypes.Position);
			needToUpdate = true;
		}

		/// <summary>
		/// configures the size of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="width">size of width</param>
		/// <param name="height">size of height</param>
		public void SetSize(int index, float width, float height)
		{
			SetSize(index, new Vector2(width, height));
		}

		/// <summary>
		/// gets the size of each sprite object
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <returns>size vector</returns>
		public Vector2 GetSize(int index)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			return new Vector2(pointSpriteList[index].Width,
								pointSpriteList[index].Height);
		}

		/// <summary>
		/// configures the texture coordinates of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="min">texture coordinates of minimum</param>
		/// <param name="max">texture coordinates of maximum</param>
		public void SetTextureCoord(int index, Vector2 min, Vector2 max)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			pointSpriteList[index].MinUV = min;
			pointSpriteList[index].MaxUV = max;

			pointSpriteList[index].AddUpdateType(
								PointSpriteObject.UpdateTypes.Texturecoord);

			needToUpdate = true;
		}

		/// <summary>
		/// configures the texture coordinates of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="u1">texture "u1" coordinate</param>
		/// <param name="v1">texture "v1" coordinate</param>
		/// <param name="u2">texture "u2" coordinate</param>
		/// <param name="v2">texture "v2" coordinate</param>
		public void SetTextureCoord(int index, float u1, float v1, float u2, float v2)
		{
			SetTextureCoord(index, new Vector2(u1, v1), new Vector2(u2, v2));
		}

		/// <summary>
		/// configures the vertex color of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="color">color</param>
		public void SetColor(int index, Color color)
		{
			if (pointSpriteList.Count <= index || 0 > index)
				throw new ArgumentException("Invalid index.");

			pointSpriteList[index].Color = color;
			pointSpriteList[index].AddUpdateType(PointSpriteObject.UpdateTypes.Color);

			needToUpdate = true;
		}

		/// <summary>
		/// configures the vertex color of each sprite object.
		/// </summary>
		/// <param name="index">an index of sprite object</param>
		/// <param name="r">a red component of color</param>
		/// <param name="g">a green component of color</param>
		/// <param name="b">a blue component of color</param>
		/// <param name="a">an alpha component of color</param>
		public void SetColor(int index, byte r, byte g, byte b, byte a)
		{
			SetColor(index, new Color(r, g, b, a));
		}

		/// <summary>
		/// configures a transformed matrix to the vertex component data 
		/// using the sprite object.
		/// </summary>
		/// <param name="vertexData">target vertex component data</param>
		/// <param name="obj">source sprite object</param>
		/// <param name="startIndex">start index of the vertex component data</param>
		/// <param name="transformMatrix">transformed matrix</param>
		/// <param name="space">3D render space</param>
		private static void SetBufferPosition(
			ref VertexPositionColorTexture[] vertexData, PointSpriteObject obj,
			int startIndex, Matrix transformMatrix, RenderingSpace space)
		{
			float cx = obj.Width * 0.5f;
			float cy = obj.Height * 0.5f;

			switch (obj.Rotation)
			{
				case PointSpriteObject.RotationAngle.Angle0:
					{
						vertexData[startIndex + 0].Position =
							new Vector3(-cx, -cy, 0.0f);
						vertexData[startIndex + 1].Position =
							new Vector3(+cx, -cy, 0.0f);
						vertexData[startIndex + 2].Position =
							new Vector3(+cx, +cy, 0.0f);
						vertexData[startIndex + 3].Position =
							new Vector3(-cx, +cy, 0.0f);
					}
					break;
				case PointSpriteObject.RotationAngle.Angle90:
					{
						vertexData[startIndex + 0].Position =
							new Vector3(+cx, -cy, 0.0f);
						vertexData[startIndex + 1].Position =
							new Vector3(+cx, +cy, 0.0f);
						vertexData[startIndex + 2].Position =
							new Vector3(-cx, +cy, 0.0f);
						vertexData[startIndex + 3].Position =
							new Vector3(-cx, -cy, 0.0f);
					}
					break;
				case PointSpriteObject.RotationAngle.Angle180:
					{
						vertexData[startIndex + 0].Position =
							new Vector3(+cx, +cy, 0.0f);
						vertexData[startIndex + 1].Position =
							new Vector3(-cx, +cy, 0.0f);
						vertexData[startIndex + 2].Position =
							new Vector3(-cx, -cy, 0.0f);
						vertexData[startIndex + 3].Position =
							new Vector3(+cx, -cy, 0.0f);
					}
					break;
				case PointSpriteObject.RotationAngle.Angle270:
					{
						vertexData[startIndex + 0].Position =
							new Vector3(-cx, +cy, 0.0f);
						vertexData[startIndex + 1].Position =
							new Vector3(-cx, -cy, 0.0f);
						vertexData[startIndex + 2].Position =
							new Vector3(+cx, -cy, 0.0f);
						vertexData[startIndex + 3].Position =
							new Vector3(+cx, +cy, 0.0f);
					}
					break;
			}

			for (int i = 0; i < 4; i++)
			{
				int arrayIndex = startIndex + i;

				Vector3.TransformNormal(ref vertexData[arrayIndex].Position,
										ref transformMatrix,
										out vertexData[arrayIndex].Position);

				vertexData[arrayIndex].Position += obj.Center;

				if (space == RenderingSpace.Screen)
				{
					vertexData[arrayIndex].Position =
								HelperMath.Make2DCoord(vertexData[arrayIndex].Position);
				}
			}
		}

		/// <summary>
		/// configures texture coordinates to the vertex component data
		/// using the sprite object.
		/// </summary>
		/// <param name="vertexData">target vertex component data</param>
		/// <param name="obj">source sprite object</param>
		/// <param name="startIndex">start index of the vertex component data</param>
		/// <param name="space">3D render space</param>
		private static void SetBufferTextureCoord(
			ref VertexPositionColorTexture[] vertexData, PointSpriteObject obj,
			int startIndex, RenderingSpace space)
		{
			float u1 = 0.0f, v1 = 0.0f, u2 = 0.0f, v2 = 0.0f;

			// Differ Y axis of the 3D and 2D
			bool flipY = (space == RenderingSpace.Screen ? !obj.FlipY : obj.FlipY);

			if (obj.FlipX)
			{
				if (flipY)
				{
					u1 = obj.MaxUV.X;
					v1 = obj.MaxUV.Y;
					u2 = obj.MinUV.X;
					v2 = obj.MinUV.Y;
				}
				else
				{
					u1 = obj.MaxUV.X;
					v1 = obj.MinUV.Y;
					u2 = obj.MinUV.X;
					v2 = obj.MaxUV.Y;
				}
			}
			else
			{
				if (flipY)
				{
					u1 = obj.MinUV.X;
					v1 = obj.MaxUV.Y;
					u2 = obj.MaxUV.X;
					v2 = obj.MinUV.Y;
				}
				else
				{
					u1 = obj.MinUV.X;
					v1 = obj.MinUV.Y;
					u2 = obj.MaxUV.X;
					v2 = obj.MaxUV.Y;
				}
			}

			vertexData[startIndex + 0].TextureCoordinate = new Vector2(u1, v1);
			vertexData[startIndex + 1].TextureCoordinate = new Vector2(u2, v1);
			vertexData[startIndex + 2].TextureCoordinate = new Vector2(u2, v2);
			vertexData[startIndex + 3].TextureCoordinate = new Vector2(u1, v2);
		}

		/// <summary>
		/// configures vertex color to the vertex component data using the sprite object.
		/// </summary>
		/// <param name="vertexData">target vertex component data</param>
		/// <param name="obj">source sprite object</param>
		/// <param name="startIndex">start index of the vertex component data</param>
		private static void SetBufferColor(ref VertexPositionColorTexture[] vertexData,
									PointSpriteObject obj, int startIndex)
		{
			for (int i = 0; i < 4; i++)
				vertexData[startIndex + i].Color = obj.Color;
		}
	}
}