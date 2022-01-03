using SkiaNeon.Audio;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace SkiaNeon
{
    public class Player : TrackedEntity
    {
        public const float SIZE = 30f;

		public float Health = 200f;
		public float MaxHealth = 200f;

		public float Defence = 200;
        public float Speed = 500f;
		public float Damage = 5f;
		public float Energy = 100f;
		public int Combo = 0;

		public float TakingDamage = 0f;
        public bool IsMoving = false;		

        private Vector2 Target;
		private Vector2 OldPosition;

		private Stopwatch timer = new Stopwatch();
        public float PathProgress {
			get
			{
				var progress = timer.ElapsedMilliseconds / Speed;
				return progress > 1f ? 1f : progress;
			}
		}

		public void TakeDamage(float damage)
        {
			var dmg = damage / Defence;
			Health -= Game.RageMode ? dmg / 3f : dmg;

			if(Health <= 0 && !Game.IsGameOver)
            {
				Health = 0;
				Game.IsGameOver = true;
				AudioManager.Play("death");
			}

			TakingDamage += 0.1f;
			if (TakingDamage > 1f) TakingDamage = 1f;
		}

        // Visuals
        readonly SKPaint Paint = new SKPaint();

        public Player(int x, int y)
        {
            Transform.Rect = new RectangleF(x, y, SIZE, SIZE);

            Paint.Color = SKColors.Black;
            Paint.StrokeWidth = 4f;
            Paint.IsAntialias = true;
			Type = EntityType.Player;
        }

		public void Move()
        {
            if (Game.RageMode)
            {
				Transform.X = Tools.Lerp(OldPosition.X, Target.X, PathProgress);
				Transform.Y = Tools.Lerp(OldPosition.Y, Target.Y, PathProgress);
			}
			else
            {
				Transform.X = Tools.smoothLerp(OldPosition.X, Target.X, PathProgress);
				Transform.Y = Tools.smoothLerp(OldPosition.Y, Target.Y, PathProgress);
			}

			if (IsMoving && PathProgress >= 1f)
			{
				IsMoving = false;
				Damage = 5f;
				Combo = 0;
			}
		}

		public void CheckCollision()
		{
			var CollidedEnemies = Game.Entities.GetObjects(Transform.Rect);
			if (CollidedEnemies.Count > 0)
			{
                foreach (var EnemyTracker in CollidedEnemies)
				{
					if(EnemyTracker.entity.Type == EntityType.Enemy)
                    {
						Enemy enemy = (Enemy)EnemyTracker.entity;
						if (IsMoving)
						{
							enemy.DealDamage(Damage);
							Damage += 2f;
							Combo += Game.RageMode ? 0 : 1;
						}
						else
						{
							enemy.Attacking = true;
                            TakeDamage(enemy.Damage);
							enemy.Destroy();
                        }
					}
                }
			}
		}

		public void Regenerate()
        {
			Energy += IsMoving ? 0.2f : 1.5f;

			if (Energy > 100f) Energy = 100f;
        }

        public void Update()
        {
            TakingDamage -= 0.025f;
			if (TakingDamage < 0f) TakingDamage = 0f;

			Regenerate();
			Move();
			CheckCollision();
		}

        public void Draw()
        {
			var TargetRect = new SKRect(Target.X - 4, Target.Y - 4, Target.X + SIZE + 4, Target.Y + SIZE + 4);
			var PlayerRect = new SKRect(Transform.X, Transform.Y, Transform.X + SIZE, Transform.Y + SIZE);

			if (IsMoving)
			{
				Paint.Style = SKPaintStyle.Fill;
				Paint.Color = SKColors.Red;
				Paint.ImageFilter = SKImageFilter.CreateBlur(10, 10);
				Game.Canvas.DrawRoundRect(new SKRoundRect(PlayerRect, 50), Paint);
				Paint.ImageFilter = null;
			}

			if (Combo > 0)
			{
				var RageScale = Combo / 150f;
				var RageFactor = Tools.smoothLerp(0, 20, RageScale);
				TargetRect.Inflate(RageFactor, RageFactor);
			}

			Paint.Style = SKPaintStyle.Stroke;
			var k = SKColors.LightPink;
			var alpha = (byte)Tools.smoothLerp(255, 0, PathProgress);
			Paint.Color = IsMoving ? new SKColor(k.Red, k.Green, k.Blue, alpha) : SKColors.HotPink;
			Game.Canvas.DrawRoundRect(new SKRoundRect(TargetRect, 50), Paint);

			Paint.Style = SKPaintStyle.Fill;
			SKColor bodyColor = Tools.LerpColor(SKColors.DeepPink, SKColors.White, Combo / 200f);
			Paint.Color = Tools.LerpColor(bodyColor, SKColors.Purple, TakingDamage);
			Game.Canvas.DrawRoundRect(new SKRoundRect(PlayerRect, 50), Paint);
		}

        public void MoveTo(Vector2 target)
        {
			if (Energy < 10) return;

			Energy -= Game.RageMode ? 0 : 10f;
			if (Energy < 0f) Energy = 0f;

			timer.Restart();
			IsMoving = true;
			Target = target;
			OldPosition = new Vector2(Transform.X, Transform.Y);
		}
    }
}
