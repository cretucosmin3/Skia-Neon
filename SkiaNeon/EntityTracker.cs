using QuadTrees.QTreeRectF;
using System.Drawing;

namespace SkiaNeon
{
	public class EntityTracker : IRectFQuadStorable
	{
		private Transform transform;
		public TrackedEntity entity;
		RectangleF IRectFQuadStorable.Rect => transform.Rect;

		public EntityTracker(ref Transform t, TrackedEntity e)
		{
			entity = e;
			transform = t;
		}
	}
}
