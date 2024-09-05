#region File Description
//-----------------------------------------------------------------------------
// FrameworkCore.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RobotGameData.Helper;
using RobotGameData.Render;
using RobotGameData.Input;
using RobotGameData.Text;
using RobotGameData.Collision;
using RobotGameData.Screen;
using RobotGameData.Sound;
using RobotGameData.ParticleSystem;
using RobotGameData.GameEvent;
using FontStashSharp;
using AssetManagementBase;
#endregion


/*
 * Xbox Display Setting         DisplayMode Width and Height 
 * AV (Composite)                   640×480 
 * 480p (Normal)                    640×480 
 * 480p (Widescreen)                640×480  
 * 720p (Widescreen)                1280×720 
 * 1080i/1080p (Widescreen)         1920×1080 
 * 
 */

namespace RobotGameData
{
	/// <summary>
	/// Graphics information
	/// </summary>
	public struct GraphicsInfo
	{
		public int screenWidth;
		public int screenHeight;

		//public ShaderProfile pixelShaderProfile;
		//public ShaderProfile vertexShaderProfile;

		public bool isMultiSampling;
	}

	/// <summary>
	/// There are the engine classes which are independent from the game. 
	/// There are all of the graphic information and the engine’s managers.
	/// It initializes/updates every manager.
	/// </summary>
	public class FrameworkCore : Microsoft.Xna.Framework.Game
	{
		#region Fields

		static Game game = null;
		static GraphicsInfo graphicsInfo;
		static GraphicsDeviceManager graphicsDeviceManager = null;
		static Viewer viewer = null;
		static InputComponentManager inputManager = null;
		static ParticleManager particleManager = null;
		static CollisionContext collisionContext = null;
		static SoundManager soundManager = null;
		static TextManager textManager = null;
		static SpriteFontBase debugFont = null;
		static GameScreenManager screenManager = null;
		static GameEventManager gameEventManager = null;

		static FpsCounter fpsCounter = null;
		static TextItem textFPS = null;

		static TimeSpan elapsedDeltaTime = TimeSpan.Zero;

		#endregion

		#region Properties

		public static Game Game
		{
			get { return game; }
		}

		public static new GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

		public static FpsCounter FpsCounter
		{
			get { return fpsCounter; }
		}

		public static GraphicsInfo GraphicsInfo
		{
			get { return graphicsInfo; }
		}

		public static InputComponentManager InputManager
		{
			get { return inputManager; }
		}

		public static RenderContext RenderContext
		{
			get { return Viewer.RenderContext; }
		}

		public static ParticleManager ParticleManager
		{
			get { return particleManager; }
		}

		public static SoundManager SoundManager
		{
			get { return soundManager; }
		}

		public static CollisionContext CollisionContext
		{
			get { return collisionContext; }
		}

		public static TextManager TextManager
		{
			get { return textManager; }
		}

		public static Viewer Viewer
		{
			get { return viewer; }
		}

		public static ViewCamera CurrentCamera
		{
			get { return Viewer.CurrentCamera; }
		}

		public static SpriteFontBase DebugFont
		{
			get { return debugFont; }
		}

		public static SpriteBatch SpriteBatch
		{
			get { return RenderContext.SpriteBatch; }
		}

		public static GameScreenManager ScreenManager
		{
			get { return screenManager; }
		}

		public static GameEventManager GameEventManager
		{
			get { return gameEventManager; }
		}

		public static AssetManager AssetManager { get; set; }

		public static GameScreen CurrentScreen
		{
			get { return ScreenManager.CurrentScreen; }
		}

		public static TimeSpan ElapsedDeltaTime
		{
			get { return elapsedDeltaTime; }
		}

		public static Viewport DefaultViewport
		{
			get { return Viewer.DefaultViewport; }
		}

		public static Viewport CurrentViewport
		{
			get { return Viewer.CurrentViewport; }
		}

		public static int ViewPosX
		{
			get { return Viewer.ViewPosX; }
		}

		public static int ViewPosY
		{
			get { return Viewer.ViewPosY; }
		}

		public static int ViewWidth
		{
			get { return Viewer.ViewWidth; }
		}

		public static int ViewHeight
		{
			get { return Viewer.ViewHeight; }
		}

		public static GameSceneNode Scene3DRoot
		{
			get { return Viewer.Scene3DRoot; }
		}

		public static GameSceneNode[] Scene2DLayers
		{
			get { return RenderContext.Scene2DLayers; }
		}

		public static GameSceneNode Scene2DFadeLayer
		{
			get { return RenderContext.Scene2DFadeLayer; }
		}

		public static GameSceneNode Scene2DTopLayer
		{
			get { return RenderContext.Scene2DTopLayer; }
		}

		#endregion

		/// <summary>
		/// Core framework constructor.
		/// </summary>
		public FrameworkCore()
			: base()
		{
			game = this;

			graphicsDeviceManager = new GraphicsDeviceManager(this);
			graphicsDeviceManager.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(graphicsDeviceManager_PreparingDeviceSettings);
			viewer = new Viewer(this);
			inputManager = new InputComponentManager();
			screenManager = new GameScreenManager(this);
			textManager = new TextManager(this);
			particleManager = new ParticleManager();
			collisionContext = new CollisionContext();
			soundManager = new SoundManager();
			gameEventManager = new GameEventManager();
			fpsCounter = new FpsCounter();

			//  Entry GameScreenManager
			AddComponent(screenManager);

			// Disable vertical retrace to get highest framerates possible for
			// testing performance.
			//graphicsDeviceManager.SynchronizeWithVerticalRetrace = false;

			// Update as fast as possible, do not use fixed time steps
			IsFixedTimeStep = false;
		}

		void graphicsDeviceManager_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
		{
			e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
		}

		/// <summary>
		/// Initialize all manangers and informations.
		/// </summary>
		protected override void Initialize()
		{
			//  Creates debug font
			debugFont = Resources.DefaultFontSystem.GetFont(12);

			//  Initialize input manager
			InputManager.Initialize();

			//  Initialize viewer
			Viewer.Initialize();

			//  Initialize text manager
			TextManager.Initialize();

			//  Initialize FPS counter
			FpsCounter.Initialize();

			//  Add FPS info
			Vector2 pos = new Vector2(0, 0);
			pos = ClampSafeArea(pos);

			textFPS = TextManager.AddText(debugFont,
					string.Format("FPS : {0}", FrameworkCore.FpsCounter.Fps.ToString()),
					(int)pos.X, (int)pos.Y, Color.White);
#if DEBUG
			textFPS.Visible = true;
#else
            textFPS.Visible = false;
#endif

			base.Initialize();

			System.Diagnostics.Debug.WriteLine("Framework Initialize OK...");
		}

		/// <summary>
		/// Update all managers and informations.
		/// </summary>
		protected override void Update(GameTime gameTime)
		{
			elapsedDeltaTime = gameTime.ElapsedGameTime;

			//  Update processing before input
			OnPreUpdate();

			//  Update input device
			InputManager.Update(gameTime);

			//  Update input handle
			OnHandleInput(gameTime);

			//  Update Xna.Framework.Game component 
			base.Update(gameTime);

			//  Update game events
			GameEventManager.Update(gameTime);

			//  Update scene
			Viewer.Update(gameTime);

			//  Update 3D sound emitter
			SoundManager.ApplyEmitter(CurrentCamera.FirstCamera.Position,
									  CurrentCamera.FirstCamera.Direction,
									  CurrentCamera.FirstCamera.Up,
									  CurrentCamera.FirstCamera.Velocity);

			//  Update sound engine
			SoundManager.Update();

			//  Update processing after input
			OnPostUpdate();

			//  Update FPS counter
			FpsCounter.Update(gameTime);

			//  Update FPS infomation
			if (textFPS.Visible)
			{
				textFPS.Text = string.Format("FPS : {0} ({1} x {2})",
								FrameworkCore.FpsCounter.Fps,
								CurrentViewport.Width, CurrentViewport.Height);
			}
		}

		/// <summary>
		/// Dispose all managers.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (fpsCounter != null)
			{
				fpsCounter = null;
			}

			if (textManager != null)
			{
				textManager.Dispose();
				textManager = null;
			}

			inputManager = null;

			if (collisionContext != null)
			{
				collisionContext.ClearAllLayer();
				collisionContext = null;
			}

			if (particleManager != null)
			{
				particleManager.ClearAllParticles();
				particleManager = null;
			}

			if (soundManager != null)
			{
				soundManager.Dispose();
				soundManager = null;
			}

			if (screenManager != null)
			{
				screenManager.Dispose();
				screenManager = null;
			}

			if (viewer != null)
			{
				viewer.Dispose();
				viewer = null;
			}

			if (debugFont != null)
				debugFont = null;

			base.Dispose(disposing);
		}

		protected override void UnloadContent()
		{
			base.UnloadContent();
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		protected override void Draw(GameTime gameTime)
		{
			//  Draw all objects (models, particles, text, images, etc.)
			Viewer.Draw(gameTime);

			base.Draw(gameTime);
		}

		public virtual void OnHandleInput(GameTime gameTime) { }

		/// <summary>
		/// calling when before framework update.
		/// </summary>
		public virtual void OnPreUpdate()
		{
			InputManager.PreUpdate();
		}

		/// <summary>
		/// calling when after framework has updated.
		/// </summary>
		public virtual void OnPostUpdate()
		{
			InputManager.PostUpdate();
		}

		/// <summary>
		/// calling when screen size has changed.
		/// </summary>
		/// <param name="viewRect">new view area</param>
		public virtual void OnSize(Rectangle newRect)
		{
			screenManager.OnSize(newRect);
		}

		/// <summary>
		/// calling when screen size has changed.
		/// </summary>
		public void OnSize()
		{
			Viewport viewport = Viewer.CurrentViewport;

			OnSize(new Rectangle(viewport.X, viewport.Y,
									   viewport.Width, viewport.Height));
		}

		/// <summary>
		/// Change graphic information
		/// </summary>
		public void ChangeGraphics(GraphicsInfo info)
		{
			graphicsInfo = info;

			//// Check all available adapters on the system.
			//foreach (GraphicsAdapter adapter in GraphicsAdapter.Adapters)
			//{
			//    // Get the capabilities of the hardware device.
			//    GraphicsDeviceCapabilities caps =
			//        adapter.GetCapabilities(DeviceType.Hardware);

			//    if (caps.MaxPixelShaderProfile < info.pixelShaderProfile)
			//    {
			//        // This adapter does not support Shader Model
			//        throw new NotSupportedException(
			//                "This adapter does not support Shader Model (" +
			//                info.pixelShaderProfile.ToString() + ")");
			//    }
			//}

			//  Initialize Graphics Device setting
			graphicsDeviceManager.PreferredBackBufferWidth =
				graphicsInfo.screenWidth;
			graphicsDeviceManager.PreferredBackBufferHeight =
				graphicsInfo.screenHeight;
			//graphicsDeviceManager.MinimumVertexShaderProfile =
			//    graphicsInfo.vertexShaderProfile;
			//graphicsDeviceManager.MinimumPixelShaderProfile =
			//    graphicsInfo.pixelShaderProfile;
			graphicsDeviceManager.PreferMultiSampling = graphicsInfo.isMultiSampling;
			graphicsDeviceManager.PreferredDepthStencilFormat = SelectStencilMode();

			//  Apply preparing device setting
			graphicsDeviceManager.IsFullScreen = false;

			graphicsDeviceManager.ApplyChanges();

			//  Set to new default viewport
			Viewer.DefaultViewport = GraphicsDevice.Viewport;

			OnSize();
		}

		/// <summary>
		/// Add a game component
		/// </summary>
		/// <param name="item">game component</param>
		public void AddComponent(GameComponent item)
		{
			Components.Add(item);
		}

		public static Vector2 ClampSafeArea(Vector2 vPos)
		{
			Rectangle Area = GetTitleSafeArea(.9f);
			vPos.X += Area.X;
			vPos.Y += Area.Y;
			if (vPos.X < Area.X) vPos.X = Area.X;
			if (vPos.X > Area.Width) vPos.X = Area.Width;
			if (vPos.Y < Area.Y) vPos.Y = Area.Y;
			if (vPos.Y > Area.Height) vPos.Y = Area.Height;

			return vPos;
		}

		public static Rectangle GetTitleSafeArea(float percent)
		{
			//  calculates the area of the current Viewport that is safe, 
			//  given a specified safety percentage.

			Rectangle retval = new Rectangle(Game.GraphicsDevice.Viewport.X,
											Game.GraphicsDevice.Viewport.Y,
											Game.GraphicsDevice.Viewport.Width,
											Game.GraphicsDevice.Viewport.Height);
			// Find Title Safe area of Xbox 360.
			float border = (1 - percent) / 2;
			retval.X = (int)(border * retval.Width);
			retval.Y = (int)(border * retval.Height);
			retval.Width = (int)(percent * retval.Width);
			retval.Height = (int)(percent * retval.Height);
			return retval;
		}

		private static DepthFormat SelectStencilMode()
		{
			// Check stencil formats
			GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
			SurfaceFormat format = adapter.CurrentDisplayMode.Format;
			//if (adapter..CheckDepthStencilMatch(DeviceType.Hardware, format, format,
			//                                DepthFormat.Depth24Stencil8))
			//{
			return DepthFormat.Depth24Stencil8;
			//}
			//else if (adapter.CheckDepthStencilMatch(DeviceType.Hardware, format, format,
			//                                DepthFormat.Depth24Stencil8Single))
			//{
			//    return DepthFormat.Depth24Stencil8Single;
			//}
			//else if (adapter.CheckDepthStencilMatch(DeviceType.Hardware, format, format,
			//                                DepthFormat.Depth24Stencil4))
			//{
			//    return DepthFormat.Depth24Stencil4;
			//}
			//else if (adapter.CheckDepthStencilMatch(DeviceType.Hardware, format, format,
			//                                DepthFormat.Depth15Stencil1))
			//{
			//    return DepthFormat.Depth15Stencil1;
			//}
			//else
			//{
			//    throw new InvalidOperationException(
			//        "Could Not Find Stencil Buffer for Default Adapter");
			//}
		}
	}
}
