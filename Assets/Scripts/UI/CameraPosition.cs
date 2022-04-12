using Assets.Scripts.Objects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.UI {
    internal class CameraPosition {
        public static List<CameraPosition> CameraPositions { get; set; } = new List<CameraPosition>();
        public int Zoom;
        public Vector2 Pan;
        public static float MinDistanceToNavigate = 100f;
        public Dictionary<Orientation, CameraPosition> Links = new Dictionary<Orientation, CameraPosition>();
        public CameraPosition(Vector2 pan, int zoom) {
            Zoom = zoom;
            Pan = pan;
            CameraPositions.Add(this);
        }

        /// <summary>
        /// Trys to navigate the camera to the next closest point in the given <paramref name="orientation"/>
        /// </summary>
        /// <param name="orientation">WASD Key that was pressed</param>
        /// <param name="current">Pass pan intent instead of current pan</param>
        /// <returns></returns>
        public static void Navigate(Orientation orientation, Vector2 current) {
            Dictionary<CameraPosition, float> distances = new Dictionary<CameraPosition, float>();
            for ( int i = 0; i < CameraPositions.Count; i++ ) {
                var item = CameraPositions[i];
                if ( WithinAngle(current, item.Pan, orientation) ) {
                    var distance = Vector2.Distance(current, item.Pan);
                    if (distance > MinDistanceToNavigate ) {
                        distances.Add(item, distance);
                        if ( GameController.GC.DrawPaths )
                        Debug.DrawLine(current, item.Pan, Color.green, 1f);
                    }
                }
            }
            if (distances.Any()) {
                var lowest = distances.ElementAt(0);
                for ( int i = 1; i < distances.Count(); i++ ) {
                    var next = distances.ElementAt(i);
                    if (lowest.Value > next.Value) {
                        lowest = next;
                    }
                }
                GameController.FC.PanTo(lowest.Key.Pan);
                GameController.FC.ZoomTo(lowest.Key.Zoom);
            }
        }
        public static float Get360Angle(Vector2 p0, Vector2 p1) {
            // Returns p0's angle from p1 where 0 is north, 90 is right and returned angle is between 0f and 359.99f
            return 180f + Vector2.SignedAngle(p1 - p0, Vector2.up);
        }
        public static bool WithinAngle(Orientation orientation, float angle, float tolerance = 60f) {
            switch ( orientation ) {
                case Orientation.South:
                    return angle < tolerance || angle > 360f - tolerance;
                case Orientation.West:
                    return angle > 90 - tolerance && angle < 90f + tolerance;
                case Orientation.North:
                    return angle > 180f - tolerance && angle < 180f + tolerance;
                case Orientation.East:
                    return angle > 270f - tolerance && angle < 270f + tolerance;
                // default is impossible unless we invent a cool new cardinal direction, and if we do it better be called Weast.
                default:
                    throw new System.ArgumentOutOfRangeException("GOD FUCKING damnit KRIS where the FUCK are we!?");
            }
        }
        public static bool WithinAngle(Vector2 p0, Vector2 p1, Orientation orientation, float tolerance = 60f) {
            return WithinAngle(orientation, Get360Angle(p0, p1), tolerance);
        }
    }
}
