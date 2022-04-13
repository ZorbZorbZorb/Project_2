using Assets.Scripts.Objects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.UI {
    public class CameraPosition : MonoBehaviour {
        public class CameraInstruction {
            public int Zoom;
            public Vector2 Pan;
            public CameraInstruction(int zoom, Vector2 pan) {
                Zoom = zoom;
                Pan = pan;
            }
            public CameraInstruction(Camera camera) {
                Pan = camera.transform.position;
                Zoom = (int)camera.orthographicSize;
            }
        }
        private struct RelativePosition {
            public float Distance;
            public float Angle;
            public RelativePosition(float distance, float angle) {
                Distance = distance;
                Angle = angle;
            }
            public RelativePosition(Vector2 p0, Vector2 p1) {
                Distance = Vector2.Distance(p0, p1);
                Angle = Get360Angle(p0, p1);
            }
        }
        private static List<CameraInstruction> instructions { get; set; } = new List<CameraInstruction>();
        private void Awake() {
            var camera = GetComponent<Camera>();
            AddPosition(camera.transform.position, (int)camera.orthographicSize);
            if ( Debug.isDebugBuild ) {
                gameObject.SetActive(false);
            }
            else {
                Destroy(gameObject);
            }
        }

        public static float Get360Angle(Vector2 p0, Vector2 p1) {
            // Returns p0's angle from p1 where 0 is north, 90 is right and returned angle is between 0f and 359.99f
            return 180f + Vector2.SignedAngle(p1 - p0, Vector2.up);
        }
        public static bool WithinAngle(Orientation orientation, float angle, float tolerance) {
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
        private static void DrawAngleLines(Vector2 p0, Orientation orientation, float tolerance, Color color) {
            float a0, a1;
            switch ( orientation ) {
                case Orientation.South:
                    a0 = ( 180f - tolerance );
                    a1 = ( 180f + tolerance );
                    break;
                case Orientation.West:
                    a0 = ( 270f - tolerance );
                    a1 = ( 270f + tolerance );
                    break;
                case Orientation.North:
                    a0 = ( -tolerance );
                    a1 = tolerance;
                    break;
                default:
                    a0 = ( 90 - tolerance );
                    a1 = ( 90f + tolerance );
                    break;
            }
            a0 = (90f + a0) * Mathf.Rad2Deg;
            a1 = (90f + a1) * Mathf.Rad2Deg;
            Vector2 p1 = new Vector2(-Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 p2 = new Vector2(-Mathf.Cos(a1), Mathf.Sin(a1));
            Debug.DrawLine(p0, p0 + (p1 * 600f), color, 1f);
            Debug.DrawLine(p0, p0 + (p2 * 600f), color, 1f);
        }
        public static bool WithinAngle(Vector2 p0, Vector2 p1, Orientation orientation, float tolerance) {
            return WithinAngle(orientation, Get360Angle(p0, p1), tolerance);
        }

        public static void AddPosition(Vector2 pan, int zoom) {
            instructions.Add(new CameraInstruction(zoom, pan));
        }

        private static List<(RelativePosition position, byte index)> positions = new List<(RelativePosition position, byte index)>();
        public static void UpdatePositions(Vector2 current) {
            positions = new List<(RelativePosition position, byte index)>();
            for ( byte i = 0; i < instructions.Count; i++ ) {
                var relativePosition = new RelativePosition(current, instructions[i].Pan);
                if ( relativePosition.Distance < 200f ) {
                    continue;
                }
                positions.Add((relativePosition, i));
            }
        }
        /// <summary>
        /// Trys to navigate the camera to the next closest point in the given <paramref name="orientation"/>
        /// </summary>
        /// <param name="orientation">WASD Key that was pressed, translated into <see cref="Orientation"/></param>
        /// <param name="current">Pass pan intent instead of current pan</param>
        public static CameraInstruction Navigate(Orientation orientation, Vector2 current) {

            if ( GameController.GC.DrawPaths ) {
                for ( int i = 0; i < positions.Count; i++ ) {
                    float angle = Get360Angle(current, instructions[i].Pan);
                    if ( WithinAngle(orientation, angle, GameController.GC.CameraTolerangeTight) ) {
                        Debug.DrawLine(current, instructions[i].Pan, Color.green, 2f);
                    }
                    else if ( WithinAngle(orientation, angle, GameController.GC.CameraTolerangeMid) ) {
                        Debug.DrawLine(current, instructions[i].Pan, Color.yellow, 2f);
                    }
                    else if ( WithinAngle(orientation, angle, GameController.GC.CameraTolerangeLoose) ) {
                        Debug.DrawLine(current, instructions[i].Pan, Color.red, 2f);
                    }
                    else {
                        Debug.DrawLine(current, instructions[i].Pan, Color.black, 2f);
                    }
                }
                DrawAngleLines(current, orientation, GameController.GC.CameraTolerangeLoose, new Color(0.8f,0.8f,1f));
                DrawAngleLines(current, orientation, GameController.GC.CameraTolerangeMid, new Color(0.5f, 0.5f, 1f));
                DrawAngleLines(current, orientation, GameController.GC.CameraTolerangeTight, new Color(0.2f, 0.2f, 1f));
            }

            var angleTight = positions.Where(x => WithinAngle(orientation, x.position.Angle, GameController.GC.CameraTolerangeTight));
            if ( angleTight.Any() ) {
                CameraInstruction instruction = GetClosest(angleTight);
                Navigate(instruction);
                return instruction;
            }
            else {
                var angleMid = positions.Where(x => WithinAngle(orientation, x.position.Angle, GameController.GC.CameraTolerangeMid));
                if ( angleMid.Any() ) {
                    CameraInstruction instruction = GetClosest(angleMid);
                    Navigate(instruction);
                    return instruction;
                }
                else {
                    var angleLoose = positions.Where(x => WithinAngle(orientation, x.position.Angle, GameController.GC.CameraTolerangeLoose));
                    if ( angleLoose.Any() ) {
                        CameraInstruction instruction = GetClosest(angleLoose);
                        Navigate(instruction);
                        return instruction;
                    }
                }
            }
            return null;

            static CameraInstruction GetClosest(IEnumerable<(RelativePosition position, byte index)> distances) {
                var lowest = distances.ElementAt(0);
                for ( int i = 1; i < distances.Count(); i++ ) {
                    var next = distances.ElementAt(i);
                    if ( lowest.position.Distance > next.position.Distance ) {
                        lowest = next;
                    }
                }
                return instructions[lowest.index];
            }

        }
        public static bool HasPosition(Orientation orientation) {
            return positions.Any(x => WithinAngle(orientation, x.position.Angle, GameController.GC.CameraTolerangeLoose));
        }

        private static void Navigate(CameraInstruction instruction) {
            GameController.FC.PanTo(instruction.Pan);
            GameController.FC.ZoomTo(instruction.Zoom);
        }

        public static float MinDistanceToNavigate = 100f;
        public static float MaxDistanceToKeypress = 50f;

    }
}
