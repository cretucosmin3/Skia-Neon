using System;
using System.IO;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using SkiaSharp;
using SkiaNeon.Audio;

namespace SkiaNeon
{
	public static class Program
	{
		// Windowing
		public static IWindow window;

		// Renderring
		public static SKSurface Surface;
		public static SKCanvas Canvas;
		public static GRBackendRenderTarget RenderTarget;
		public static GRGlInterface grGlInterface;
		public static GRContext grContext;

		public static SKPoint PlayerPosition = new SKPoint(40, 40);

		public static void SetWindow()
		{
			var options = WindowOptions.Default;
			options.Size = new Vector2D<int>(1300, 800);
			options.Title = "Ballz";
			options.PreferredStencilBufferBits = 4;
            options.PreferredBitDepth = new Vector4D<int>(4, 4, 4, 4);
            options.IsEventDriven = true;
			//options.VideoMode = new VideoMode(new Vector2D<int>(100, 100));

			GlfwWindowing.Use();

			window = Window.Create(options);
			window.Initialize();
            window.WindowState = WindowState.Fullscreen;

            window.Render += Window_Render;
		}

		public static void Start()
		{
			while (!window.IsClosing)
			{
				window.DoEvents();
				window.DoRender();
				window.ContinueEvents();
			}

			window.Dispose();
		}

		private static Random randomShake = new Random();
		private static bool isMatrixNormal = false;
		private static SKColor background = new SKColor(20, 20, 20);

		private static void Window_Render(double delta)
		{
			Game.DeltaTime = (float)delta;

			grContext.ResetContext();
			float ComboScale = Game.Player.Combo / 200f;
			Canvas.Clear(Tools.LerpColor(background, SKColors.DarkRed, Game.Player.TakingDamage));

			if (Game.Player.TakingDamage > 0f && !Game.IsGameOver)
			{
				Canvas.ResetMatrix();
				int scale = (int)(20 * Game.Player.TakingDamage);
				float shakeX = randomShake.Next(0 - scale, scale);
				float shakeY = randomShake.Next(0 - scale, scale);

				Canvas.Translate(shakeX, shakeY);
				isMatrixNormal = false;
			}
			else {
				if (Game.RageMode)
				{
					Canvas.ResetMatrix();
					Canvas.Translate(10, 10);
					isMatrixNormal = false;
				}
				else if (Game.Player.Combo > 0 && !Game.IsGameOver)
				{
					Canvas.ResetMatrix();
					int ShakeFactor = (int)Tools.smoothLerp(0, 15, ComboScale);
					float shakeX = randomShake.Next(-ShakeFactor, ShakeFactor);
					float shakeY = randomShake.Next(-ShakeFactor, ShakeFactor);

					Canvas.Translate(shakeX, shakeY);
					isMatrixNormal = false;
				}
				else if (!isMatrixNormal)
				{
					Canvas.ResetMatrix();
					isMatrixNormal = true;
				}

			}

			// Game Cycle
			Game.Update();
			Game.Render();

			Canvas.Flush();
		}

		private static void RenewCanvas(int width, int height)
		{
			RenderTarget?.Dispose();
			Canvas?.Dispose();
			Surface?.Dispose();

			RenderTarget = new GRBackendRenderTarget(width, height, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
			Surface = SKSurface.Create(grContext, RenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
			Canvas = Surface.Canvas;
		}

		public static void SetCanvas()
		{
			grGlInterface = GRGlInterface.Create();
			grGlInterface.Validate();

			grContext = GRContext.CreateGl(grGlInterface);

			RenewCanvas(window.Size.X, window.Size.Y);

			window.FramebufferResize += newSize =>
			{
				RenewCanvas(newSize.X, newSize.Y);
				window.DoRender();
			};
		}

		public static void SetEvents()
		{
			IInputContext input = window.CreateInput();

			// Register mouse events
			foreach (IMouse mouse in input.Mice)
			{
                mouse.Cursor.CursorMode = CursorMode.Raw;

                mouse.Click += Game.MouseClick;
				mouse.MouseMove += (i, v) =>
				{
					if (v.X < 0) v.X = 0;
					else if (v.X > window.Size.X) v.X = window.Size.X;

					if (v.Y < 0) v.Y = 0;
					else if (v.Y > window.Size.Y) v.Y = window.Size.Y;

					mouse.Position = v;
					Game.MousePosition = v;
				};
			}

			// Register keyboard events
			foreach (IKeyboard keyboard in input.Keyboards)
			{
				keyboard.KeyDown += (IKeyboard _, Key key, int i) =>
				{
					if (i == 0) return;
					if (key == Key.Space)
					{
						Game.TutorialShown = true;
						if(Game.IsGameOver) Game.Restart();
					}
					if (key == Key.Escape) window.Close();
				};
			}
		}

		public static void Main(string[] args)
		{
			AudioManager.Load();
			AudioManager.Preload("click");
			AudioManager.Preload("no-energy");
			AudioManager.Preload("spawn");
			AudioManager.Preload("spawn-boss");
			AudioManager.Preload("death");
			AudioManager.Preload("super-combo");
			AudioManager.Preload("rage-click");
			AudioManager.Preload("high-wound");
			AudioManager.Play("background", true);

            SetWindow();

			SetCanvas();
			SetEvents();

			Game.Initialize();

			Start();

			AudioManager.Clean();
		}
	}
}