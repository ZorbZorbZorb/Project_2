using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Areas {
    public abstract class Area : MonoBehaviour {
        public AreaBounds2D Bounds;
        public Location Location;

        public abstract void AddInteractable(CustomerInteractable interactable);

        public Vector2 Center => Bounds.Center;

        public void Awake() {
            Bounds = new AreaBounds2D(GetComponent <BoxCollider2D>(), 140, 140);
        }

    }
}
