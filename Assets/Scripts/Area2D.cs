using System;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class Area2D {
        [SerializeField]
        public BoxCollider2D Area;
        [SerializeField]
        public (double, double) gridScale;
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
            return new Vector2(Area.bounds.min.x * (float)gridScale.Item1, Area.bounds.min.y * (float)gridScale.Item2);
        }
        public Area2D(BoxCollider2D area) {
            Area = area;
            gridScale = (1d, 1d);
        }
    }
}
