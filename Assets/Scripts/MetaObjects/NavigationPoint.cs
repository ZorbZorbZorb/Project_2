using System;
using UnityEngine;

namespace Assets.Scripts.MetaObjects {
    [Serializable]
    public class NavigationPoint {
        [SerializeField]
        public Transform Transform { get; set; }
        [SerializeField]
        public Location Location { get; set; }
    }
}
