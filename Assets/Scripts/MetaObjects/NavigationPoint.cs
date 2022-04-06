using System;
using UnityEngine;

namespace Assets.Scripts.MetaObjects {
    [Serializable]
    public class NavigationPoint {
        [SerializeField]
        public Transform Transform;
        [SerializeField]
        public Location Location;
    }
}
