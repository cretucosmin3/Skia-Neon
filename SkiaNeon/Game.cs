using QuadTrees;
using Silk.NET.Input;
using SkiaSharp;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using SkiaNeon.Audio;
using System.IO;

namespace SkiaNeon
{
	public static class Game
	{
		public static float DeltaTime = 0f;
		public static Vector2 MousePosition = new Vector2(0, 0);
		public static SKCanvas Canvas { get => Program.Canvas; }
		static SKImage TutorialImage;
		public static QuadTreeRectF<EntityTracker> Entities = new(
			float.MinValue / 2f, float.MinValue / 2f,
			float.MaxValue, float.MaxValue
		);

		public static Dictionary<EntityTracker, Enemy> Enemies = new Dictionary<EntityTracker, Enemy>();
		public static Player Player = new Player(150, 150);
		public static Stopwatch EnemyTimer = new Stopwatch();
		public static Stopwatch RageTimer = new Stopwatch();
		public static bool IsGameOver = false;
		public static int Score = 0;
		public static int EnemiesSpawned = 0;
		public static bool RageMode = false;
		public static bool TutorialShown = false;

		// Drawing
		public static SKPaint EnemyPaint = new SKPaint()
		{
			StrokeWidth = 2f
		};

		public static SKPaint CursorPaint = new SKPaint()
		{
			IsAntialias = true,
			Color = SKColors.Coral,
			StrokeWidth = 4f
		};

		public static SKPaint TextPaint = new SKPaint()
		{
			IsAntialias = true,
			TextSize = 27,
			Color = SKColors.White
		};

		static SKRoundRect OutlineHealth = new SKRoundRect(new SKRect(10, 10, 340, 30), 2);
		static SKRoundRect OutlineEnergy = new SKRoundRect(new SKRect(10, 40, 200, 60), 2);

		public static EntityTracker AddTracker(Transform transform, TrackedEntity entity)
		{
			var Tracker = new EntityTracker(ref transform, entity);
			Entities.Add(Tracker);

			return Tracker;
		}

		static int LastCorner = 0;
		public static RectangleF GetRandomSpawner(float enemySize)
        {
			var corner = Number.Random(0, 4);

			while(corner == LastCorner)
            {
				corner = Number.Random(0, 4);
			}

			RectangleF Position = new RectangleF(0, 0, 0, 0);

			switch (corner)
            {
				case 0:
					Position = new RectangleF(-200, -200, 200, 200);
					break;
				case 1:
					Position = new RectangleF(Program.window.Size.X, -200, 200, 200);
					break;
				case 2:
					Position = new RectangleF(-200, Program.window.Size.Y, 200, 200);
					break;
				case 3:
					Position = new RectangleF(Program.window.Size.X, Program.window.Size.Y, 200, 200);
					break;
            }
			LastCorner = corner;
			return Position;
		}

		private static bool isAnyEnemy(List<EntityTracker> trackers)
		{
			foreach (var tracker in trackers)
			{
				if (tracker.entity.Type == EntityType.Player)
				{
					return true;
				}
			}
			return false;
		}

		public static void SpawnEnemy(bool isBoss = false)
        {
			if (Enemies.Count > (RageMode ? 500 : 350)) return;

			var randomPos = Tools.RandomWindowPoint();
			var enemySpeed = Number.Random(3, 15 + (EnemiesSpawned / 140));
			var enemySize = isBoss ? 70 : 25 - enemySpeed;

			if (enemySize < 10) enemySize = 10;
			if (enemySpeed > 35) enemySpeed = 35;
			
			if (RageMode)
			{
				enemySpeed *= 2;
				if (enemySize < 15) enemySize = 15;
			}

			var SpawnArea = GetRandomSpawner(enemySize);

			RectangleF SpawnPoint = new RectangleF()
			{
				X = Number.Random(SpawnArea.X, SpawnArea.X + SpawnArea.Width),
				Y = Number.Random(SpawnArea.Y, SpawnArea.Y + SpawnArea.Height),
				Width = enemySize,
				Height = enemySize
			};

			var collided = Entities.GetObjects(SpawnPoint);

            while (isAnyEnemy(collided))
            {
				SpawnPoint = new RectangleF()
				{
					X = Number.Random(SpawnArea.X, SpawnArea.Width),
					Y = Number.Random(SpawnArea.Y, SpawnArea.Height),
					Width = enemySize,
					Height = enemySize
				};

				collided = Entities.GetObjects(SpawnPoint);
			}

			var NewEnemy = new Enemy(SpawnPoint.X, SpawnPoint.Y, enemySpeed, enemySize);
			NewEnemy.Tracker = AddTracker(NewEnemy.Transform, NewEnemy);

			NewEnemy.OnDestroy += NewEnemy_OnDestroy;

			NewEnemy.isBoss = isBoss;

			Enemies.Add(NewEnemy.Tracker, NewEnemy);
			EnemiesSpawned++;

			if(isBoss) AudioManager.Play("spawn-boss");
		}

		public static void Initialize()
        {
			// Load tutorial image
			TutorialImage = SKImage.FromBitmap(SKBitmap.Decode(File.ReadAllBytes("tutorial.png")));

			EnemyTimer.Restart();
			EnemyPaint.Color = SKColors.Aquamarine;
			EnemyPaint.Style = SKPaintStyle.Fill;
			Player.MoveTo(new Vector2(Program.window.Size.X / 2f, Program.window.Size.Y / 2f));

			// Set player up
			Player.Tracker = AddTracker(Player.Transform, Player);
		}

        private static void NewEnemy_OnDestroy(TrackedEntity e)
        {
			if (Game.RageMode && Player.Health < Player.MaxHealth)
			{
				Player.Health += 0.2f;
			}

			Entities.Remove(e.Tracker);
			Enemies.Remove(e.Tracker);
		}

        public static void MouseClick(IMouse m, MouseButton btn, Vector2 pos)
        {
			AudioManager.Play(Player.Energy < 10 ? "no-energy" : (RageMode ? "rage-click" : "click"));

			Player.MoveTo(pos - new Vector2(Player.SIZE / 2f, Player.SIZE / 2f));
		}

		private static bool SuperComboSound = false;
        public static void Update()
        {
			if (IsGameOver || !TutorialShown) return;
			
			Player.Update();

			if(Player.Combo > 250 && !SuperComboSound)
            {
				AudioManager.Play("super-combo");
				SuperComboSound = true;
			}

			if(SuperComboSound && Player.Combo < 20)
			{
				SuperComboSound = false;
			}

			if(!RageMode && Player.Combo > 300)
            {
				AudioManager.Play("rage-mode");
				RageMode = true;
				Player.Speed = 350f;
				Player.Combo = 0;

				RageTimer.Restart();
			}

			if(RageTimer.ElapsedMilliseconds > 10000)
			{
				Player.Speed = 500f;
				RageMode = false;
				RageTimer.Stop();
				EnemyPaint.Style = SKPaintStyle.Fill;
			}

			if (Player.TakingDamage > 0.7)
            {
				AudioManager.Play("high-wound");
			}

			if(EnemyTimer.ElapsedMilliseconds > (RageMode ? 40 : 150))
            {
				if (Number.Random(0, 100) > 98)
					SpawnEnemy(true);
				else
					SpawnEnemy();
			
				EnemyTimer.Restart();
			}

            foreach (var enemy in Enemies.Values)
            {
				enemy.Update();
            }
        }

		public static void DrawHealth()
		{
			if (Player.Health < 0f) Player.Health = 0f;

			SKPaint paint = new SKPaint();
			paint.Color = SKColors.OrangeRed;
			paint.Style = SKPaintStyle.StrokeAndFill;
			paint.StrokeWidth = 2f;
			paint.IsAntialias = true;

			float healthRatio = Tools.Lerp(0, 1, Player.Health / Player.MaxHealth);
			var Inner = new SKRoundRect(new SKRect(10, 10, 340 * healthRatio, 30), 2);

			Canvas.DrawRoundRect(Inner, paint);

			paint.Color = SKColors.LightPink;
			paint.Style = SKPaintStyle.Stroke;
			Canvas.DrawRoundRect(OutlineHealth, paint);
		}

		public static void DrawEnergy()
		{
			if (Player.Energy < 0f) Player.Energy = 0f;

			float energyRatio = Tools.Lerp(0, 1, Player.Energy / 100f);
			if (RageMode) energyRatio = 1f;

			SKPaint paint = new SKPaint();
			paint.Color = Tools.LerpColor(SKColors.Gray, SKColors.Yellow, energyRatio);
			paint.Style = SKPaintStyle.StrokeAndFill;
			paint.StrokeWidth = 2f;
			paint.IsAntialias = true;

			var Inner = new SKRoundRect(new SKRect(10, 40, 200 * energyRatio, 60), 2);

			Canvas.DrawRoundRect(Inner, paint);

			paint.Color = SKColors.YellowGreen;
			paint.Style = SKPaintStyle.Stroke;
			Canvas.DrawRoundRect(OutlineEnergy, paint);
		}

		public static void DrawCombo()
		{
			if (Player.Combo > 0)
			{
				TextPaint.TextSize = 25;
				Canvas.DrawText($"{Player.Combo}X Combo!", 365, 30, TextPaint);
			}
		}

		public static void DrawScore()
		{
			TextPaint.Color = SKColors.Red;
			TextPaint.TextSize = 45;
			Canvas.DrawText("GAME OVER!", (Program.window.Size.X / 2) - 130, 80, TextPaint);

			TextPaint.Color = SKColors.White;
			TextPaint.TextSize = 25;
			Canvas.DrawText($"You killed {Score} Squares!", (Program.window.Size.X / 2) - 130, 140, TextPaint);
			Canvas.DrawText("Press Space to restart!", (Program.window.Size.X / 2) - 130, 175, TextPaint);
		}

		public static void DrawRageMode()
		{
			TextPaint.Color = SKColors.Red;
			TextPaint.TextSize = 30;
			var seconds = 10f - (RageTimer.ElapsedMilliseconds / 1000f);
			Canvas.DrawText($"Rage Mode {seconds.ToString("0.00")} ", (Program.window.Size.X / 2) - 150, 50, TextPaint);
		}

		public static void DrawCursor()
        {
			var MouseRect = new SKRoundRect(
				new SKRect(
					Game.MousePosition.X - 6,
					Game.MousePosition.Y - 6,
					Game.MousePosition.X + 6,
					Game.MousePosition.Y + 6), 50);

			CursorPaint.Style = SKPaintStyle.Fill;
			CursorPaint.Color = SKColors.Salmon;
			Canvas.DrawRoundRect(MouseRect, CursorPaint);
		}

		static void DrawTutorial()
        {
			Canvas.DrawImage(
				TutorialImage,
					new SKPoint(
						(Program.window.Size.X / 2) - (TutorialImage.Width / 2f),
						(Program.window.Size.Y / 2) - (TutorialImage.Height / 2f)
				));
        }

		public static void Render()
		{
            if (!TutorialShown)
            {
				DrawTutorial();
				return;
            }

			DrawCursor();
			foreach (var enemy in Enemies.Values)
			{
				enemy.Draw();
			}

			Player.Draw();
			DrawHealth();
			DrawEnergy();

			if (IsGameOver) DrawScore();
			else if (RageMode) DrawRageMode();

			if(!RageMode) DrawCombo();
			DrawCursor();
		}

		public static void Restart()
        {
			IsGameOver = false;
			Score = 0;
			Player.Combo = 0;
			Player.Health = Player.MaxHealth;
			Player.Energy = 100f;
			EnemiesSpawned = 0;

			Enemies.Clear();
			Entities.Clear();

			Initialize();
		}
    }
}
