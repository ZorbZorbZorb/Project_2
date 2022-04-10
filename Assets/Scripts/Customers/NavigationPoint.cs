using System;
using UnityEngine;

namespace Assets.Scripts.Customers {
    [Serializable]
    public class NavigationPoint {
        [SerializeField]
        public Transform Transform;
        [SerializeField]
        public Location Location;
        public static implicit operator Location(NavigationPoint point) => point.Location;
        public static implicit operator Vector3(NavigationPoint point) => point.Transform.position;
    }
}
