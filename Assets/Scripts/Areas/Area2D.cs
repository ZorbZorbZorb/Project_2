using System;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class Area2D {
        [SerializeField]
        public double GridScaleX;
        public double GridScaleY;
        private Bounds? bounds = null;
        public Bounds Bounds => bounds.HasValue
            ? (Bounds)bounds 
            : throw new InvalidOperationException();
        private BoxCollider2D collider { get; set; } = null;
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
        public Area2D(BoxCollider2D collider, int GridScaleX, int GridScaleY) {
            this.collider = collider;
            this.GridScaleX = GridScaleX;
            this.GridScaleY = GridScaleY;
            UpdateArea();
        }
        public void UpdateArea() {
            collider.enabled = true;
            bounds = new Bounds(collider.bounds.center, collider.bounds.size);
            collider.enabled = false;
        }
        public void SetCollider(BoxCollider2D collider) {
            this.collider = collider;
            UpdateArea();
        }
    }
}
