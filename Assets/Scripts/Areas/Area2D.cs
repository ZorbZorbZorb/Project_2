using System;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class Area2D {
        [SerializeField]
        public double GridScaleX;
        public double GridScaleY;
        private Bounds bounds = new Bounds();
        public Bounds Bounds => bounds;
        private BoxCollider2D collider;
        public double LengthX => Bounds.max.x - Bounds.min.x;
        public double LengthY => Bounds.max.y - Bounds.min.y;
        public int GridPointsX => (int)Math.Floor(LengthX / GridScaleX);
        public int GridPointsY => (int)Math.Floor(LengthY / GridScaleY);

        public bool InBounds(Vector2 vector) {
            return vector.x >= Bounds.min.x && Bounds.max.x >= vector.x
                && vector.y >= Bounds.min.y && Bounds.max.y >= vector.y;
        }
        public Vector2 Clamp(Vector2 vector) {
            vector.x = Mathf.Clamp(vector.x, Bounds.min.x, Bounds.max.x);
            vector.y = Mathf.Clamp(vector.y, Bounds.min.y, Bounds.max.y);
            return vector;
        }
        public Vector2 GetGridPosition((double, double) position) {
            var x = Bounds.min.x + ( position.Item1 * (float)GridScaleX ) + ( GridScaleX / 2f );
            var y = Bounds.min.y + ( position.Item2 * (float)GridScaleY ) + ( GridScaleY / 2f );
            return new Vector2((float)x, (float)y);
        }
        public Vector2 GetGridPosition(GameSaveData.Option option) {
            var x = Bounds.min.x + ( option.X * (float)GridScaleX ) + ( GridScaleX / 2f );
            var y = Bounds.min.y + ( option.Y * (float)GridScaleY ) + ( GridScaleY / 2f );
            return new Vector2((float)x, (float)y);
        }
        public Area2D(BoxCollider2D collider) {
            bounds = new Bounds();
            UpdateArea();
            GridScaleX = 1;
            GridScaleY = 1;
        }
        public void UpdateArea() {
            collider.enabled = true;
            bounds.min = collider.bounds.min;
            bounds.max = collider.bounds.max;
            bounds.center = collider.bounds.center;
            collider.enabled = false;
        }
    }
}
