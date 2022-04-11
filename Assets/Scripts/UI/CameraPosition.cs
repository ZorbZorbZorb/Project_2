using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.UI {
    internal class CameraPosition {
        public static List<CameraPosition> CameraPositions { get; set; }
        public int Zoom;
        public Vector2 Pan;
        public Dictionary<Orientation, CameraPosition> Links = new Dictionary<Orientation, CameraPosition>();
        CameraPosition(int zoom, Vector2 pan) { 
            Zoom = zoom;
            Pan = pan;
            CameraPositions.Add(this);
        }
    }
}
