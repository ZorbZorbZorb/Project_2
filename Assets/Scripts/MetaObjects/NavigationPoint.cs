using System;
using UnityEngine;

namespace Assets.Scripts.MetaObjects {
    [Serializable]
    public class NavigationPoint {
        [SerializeField]
        public Transform Transform;
        [SerializeField]
        public Location Location;

        [SerializeField]
        public Transform HandleAnchor;
        public Vector3 HandleA => Transform.position + HandleAnchor.transform.localPosition;
        public Vector3 HandleB => Transform.position - HandleAnchor.transform.localPosition;
        public static implicit operator Location(NavigationPoint point) => point.Location;
        public static implicit operator Vector3(NavigationPoint point) => point.Transform.position;
    }
}
