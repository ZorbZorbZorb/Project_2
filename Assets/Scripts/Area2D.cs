using System;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class Area2D {
        [SerializeField]
        public BoxCollider2D Area;
        public double GridScaleX;
        public double GridScaleY;
        public double LengthX => Area.bounds.max.x - Area.bounds.min.x;
        public double LengthY => Area.bounds.max.y - Area.bounds.min.y;
        public int GridPointsX => (int)Math.Floor(LengthX / GridScaleX);
        public int GridPointsY => (int)Math.Floor(LengthY / GridScaleY);

        public bool InBounds(Vector2 vector) {
            return vector.x >= Area.bounds.min.x && Area.bounds.max.x >= vector.x
                && vector.y >= Area.bounds.min.y && Area.bounds.max.y >= vector.y;
        }
        public Vector2 Clamp(Vector2 vector) {
            vector.x = Mathf.Clamp(vector.x, Area.bounds.min.x, Area.bounds.max.x);
            vector.y = Mathf.Clamp(vector.y, Area.bounds.min.y, Area.bounds.max.y);
            return vector;
        }
        public Vector2 GetGridPosition((int, int) position) {

            return new Vector2(Area.bounds.min.x + (position.Item1 * (float)GridScaleX), Area.bounds.min.y + (position.Item2 * (float)GridScaleY));
        }
        public Area2D(BoxCollider2D area) {
            Area = area;
            GridScaleX = 1;
            GridScaleY = 1;
        }
    }
}
