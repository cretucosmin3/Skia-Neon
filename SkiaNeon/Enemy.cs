using SkiaSharp;
using System;
using System.Collections.Generic;

namespace SkiaNeon
{
    public class Enemy : TrackedEntity
    {
        float Defence = 10f;
        float Speed = 0;
        float Wound = 0;
        public bool isBoss = false;

        public float Damage = 45f;
        public bool Attacking = false;

        public override event Destroy OnDestroy;

        public Enemy(float x, float y, float speed, float size)
        {
            Defence = size * 12;
            Damage = size * 40;
            Speed = speed * 50;
            Transform.Rect = new(x, y, size, size);
        }

        public void DealDamage(float force)
        {
            Wound += force / Defence;

            if (Wound > 1f)
            {
                Game.Score++;
                Wound = 1f;
                Destroy();
            }
        }

        SKPoint EscapePoint()
        {
            Random randomizer = new Random();
            var xx = randomizer.Next((int)Transform.X - 10, (int)Transform.X + 10);
            var yy = randomizer.Next((int)Transform.Y - 10, (int)Transform.Y + 10);

            return new SKPoint(xx, yy);
        }

        private bool isAnyThePlayer(List<EntityTracker> trackers)
        {
            foreach (var tracker in trackers)
            {
                if(tracker.entity.Type == EntityType.Player)
                {
                    return true;
                }
            }
            return false;
        }

        int ticks = 0;
        public void Update()
        {
            float deltaSpeed = Game.DeltaTime * Speed;
            ticks++;

            if(ticks == 2)
            {
                Attacking = false;
                ticks = 0;
                if(Attacking) Destroy();
            }

            var oldX = Transform.X;
            var oldY = Transform.Y;

            var newPoint = Tools.MovePointTowards(
                new SKPoint(Transform.X, Transform.Y),
                new SKPoint(Game.Player.Transform.X, Game.Player.Transform.Y), deltaSpeed);

            Transform.X = newPoint.X;
            Transform.Y = newPoint.Y;

            var CollidedEntities = Game.Entities.GetObjects(Transform.Rect);
            if (CollidedEntities.Count > 1)
            {
                if(isAnyThePlayer(CollidedEntities) && !Game.Player.IsMoving)
                {
                    Game.Player.TakeDamage(Damage);
                    Destroy();
                    return;
                }

                Transform.X = oldX;
                Transform.Y = oldY;

                for (int i = 0; i < 4; i++)
                {
                    var scapePoint = EscapePoint();
                    var newP = Tools.MovePointTowards(
                        new SKPoint(Transform.X, Transform.Y),
                        new SKPoint(scapePoint.X, scapePoint.Y), deltaSpeed);

                    Transform.X = newP.X;
                    Transform.Y = newP.Y;

                    if (Game.Entities.GetObjects(Transform.Rect).Count == 1) break;
                    else
                    {
                        Transform.X = oldX;
                        Transform.Y = oldY;
                    }
                }
            }
        }

        public void Draw()
        {
            var defualt = isBoss ? SKColors.GreenYellow : SKColors.Aquamarine;

            // Set wound color;
            if (Wound > 0f)
                Game.EnemyPaint.Color = Tools.LerpColor(defualt, SKColors.Red, Wound);
            else
                Game.EnemyPaint.Color = defualt;

            if(Attacking)
                Game.EnemyPaint.Color = SKColors.Red;

            if (Game.RageMode)
            {
                Game.EnemyPaint.Style = Number.Chance(30) ? SKPaintStyle.Stroke : SKPaintStyle.Fill;
            }

            var EnemyRect = new SKRect(Transform.X, Transform.Y, Transform.X + Transform.Width, Transform.Y + Transform.Height);
            Game.Canvas.DrawRoundRect(new SKRoundRect(EnemyRect, 4), Game.EnemyPaint);
        }

        public override void Destroy()
        {
            OnDestroy?.Invoke(this);
        }
    }
}
