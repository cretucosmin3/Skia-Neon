using System;
using System.Collections.Generic;
using System.Drawing;

namespace SkiaNeon
{
    public class Transform
    {
        public RectangleF Rect;
        public float X { get => Rect.X; set => Rect.X = value; }
        public float Y { get => Rect.Y; set => Rect.Y = value; }
        public float Width { get => Rect.Width; set => Rect.Width = value; }
        public float Height { get => Rect.Height; set => Rect.Height = value; }

        public Transform(float x, float y, float size, float size1)
        {
            Rect = new RectangleF(x, y, size, size);
        }
    }

    public class TrackedEntity
    {
        public readonly Transform Transform = new Transform(0, 0, 0, 0);
        public EntityType Type = EntityType.Enemy;
        public EntityTracker Tracker;

        public virtual event Destroy OnDestroy;
        public virtual void Destroy()
        {
            OnDestroy?.Invoke(this);
        }
    }

    public enum EntityType
    {
        Player,
        Enemy
    }

    public delegate void Destroy(TrackedEntity e);
}
