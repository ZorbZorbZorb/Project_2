using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts {
    ///<summary>
    ///  A vertex path is a collection of points (vertices) that lie along a bezier path.
    ///  This allows one to do things like move at a constant speed along the path,
    ///  which is not possible with a bezier path directly due to how they're constructed mathematically.
    ///  
    ///  This class also provides methods for getting the position along the path at a certain distance or time
    ///  (where time = 0 is the start of the path, and time = 1 is the end of the path).
    ///  Other info about the path (tangents, normals, rotation) can also be retrieved in this manner.
    ///  
    ///  <para>
    ///    This is almost entirely Sebastion Lagues code that I stole and modified to be more performant 
    ///    for 2D applications.
    ///  </para>
    ///</summary>    
    public class VertexPath2 {
        
        #region Fields

        public readonly PathSpace space;
        public readonly bool isClosedLoop;
        public readonly Vector2[] localPoints;
        public readonly Vector2[] localTangents;

        /// Percentage along the path at each vertex (0 being start of path, and 1 being the end)
        public readonly float[] times;
        /// Total distance between the vertices of the polyline
        public readonly float length;
        /// Total distance from the first vertex up to each vertex in the polyline
        public readonly float[] cumulativeLengthAtEachVertex;
        /// Bounding box of the path
        public readonly Bounds bounds;

        // Default values and constants:    
        const float accuracy = 0.02f; // A scalar for how many times bezier path is divided when determining vertex positions
        const float minVertexSpacing = 1f;

        Transform transform;

        public static readonly Dictionary<Vector2[], VertexPath2> PathCache = new Dictionary<Vector2[], VertexPath2>();

        #endregion

        #region Constructors

        /// <summary> Splits bezier path into array of vertices along the path.</summary>
        ///<param name="maxAngleError">How much can the angle of the path change before a vertex is added. This allows fewer vertices to be generated in straighter sections.</param>
        ///<param name="minVertexDst">Vertices won't be added closer together than this distance, regardless of angle error.</param>
        public VertexPath2(BezierPath bezierPath, Transform transform, float maxAngleError = 0.3f, float minVertexDst = 0) :
            this(bezierPath, VertexPath2Utility.SplitBezierPathByAngleError(bezierPath, maxAngleError, minVertexDst, VertexPath2.accuracy), transform) { }

        /// <summary> Splits bezier path into array of vertices along the path.</summary>
        ///<param name="maxAngleError">How much can the angle of the path change before a vertex is added. This allows fewer vertices to be generated in straighter sections.</param>
        ///<param name="minVertexDst">Vertices won't be added closer together than this distance, regardless of angle error.</param>
        ///<param name="accuracy">Higher value means the change in angle is checked more frequently.</param>
        public VertexPath2(BezierPath bezierPath, Transform transform, float vertexSpacing) :
            this(bezierPath, VertexPath2Utility.SplitBezierPathEvenly(bezierPath, Mathf.Max(vertexSpacing, minVertexSpacing), VertexPath2.accuracy), transform) { }

        /// Internal contructor
        VertexPath2(BezierPath bezierPath, VertexPath2Utility.PathSplitData pathSplitData, Transform transform) {
            this.transform = transform;
            space = bezierPath.Space;
            isClosedLoop = bezierPath.IsClosed;
            int numVerts = pathSplitData.vertices.Count;
            length = pathSplitData.cumulativeLength[numVerts - 1];

            localPoints = new Vector2[numVerts];
            localTangents = new Vector2[numVerts];
            cumulativeLengthAtEachVertex = new float[numVerts];
            times = new float[numVerts];

            // Loop through the data and assign to arrays.
            for ( int i = 0; i < localPoints.Length; i++ ) {
                localPoints[i] = pathSplitData.vertices[i];
                localTangents[i] = pathSplitData.tangents[i];
                cumulativeLengthAtEachVertex[i] = pathSplitData.cumulativeLength[i];
                times[i] = cumulativeLengthAtEachVertex[i] / length;
            }
        }

        #endregion

        #region Public methods and accessors

        public void UpdateTransform(Transform transform) {
            this.transform = transform;
        }
        public int NumPoints {
            get {
                return localPoints.Length;
            }
        }

        public Vector2 GetTangent(int index) {
            return MathUtility.TransformDirection(localTangents[index], transform, space);
        }

        public Vector2 GetPoint(int index) {
            return MathUtility.TransformPoint(localPoints[index], transform, space);
        }

        /// Gets point on path based on distance travelled.
        public Vector2 GetPointAtDistance(float dst, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            float t = dst / length;
            return GetPointAtTime(t, endOfPathInstruction);
        }

        /// Gets forward direction on path based on distance travelled.
        public Vector2 GetDirectionAtDistance(float dst, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            float t = dst / length;
            return GetDirection(t, endOfPathInstruction);
        }

        /// Gets point on path based on 'time' (where 0 is start, and 1 is end of path).
        public Vector2 GetPointAtTime(float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            var data = CalculatePercentOnPathData(t, endOfPathInstruction);
            return Vector2.Lerp(GetPoint(data.previousIndex), GetPoint(data.nextIndex), data.percentBetweenIndices);
        }

        /// Gets forward direction on path based on 'time' (where 0 is start, and 1 is end of path).
        public Vector2 GetDirection(float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            var data = CalculatePercentOnPathData(t, endOfPathInstruction);
            Vector2 dir = Vector2.Lerp(localTangents[data.previousIndex], localTangents[data.nextIndex], data.percentBetweenIndices);
            return MathUtility.TransformDirection(dir, transform, space);
        }

        /// Finds the closest point on the path from any point in the world
        public Vector2 GetClosestPointOnPath(Vector2 worldPoint) {
            TimeOnPathData data = CalculateClosestPointOnPathData(worldPoint);
            return Vector2.Lerp(GetPoint(data.previousIndex), GetPoint(data.nextIndex), data.percentBetweenIndices);
        }

        /// Finds the 'time' (0=start of path, 1=end of path) along the path that is closest to the given point
        public float GetClosestTimeOnPath(Vector2 worldPoint) {
            TimeOnPathData data = CalculateClosestPointOnPathData(worldPoint);
            return Mathf.Lerp(times[data.previousIndex], times[data.nextIndex], data.percentBetweenIndices);
        }

        /// Finds the distance along the path that is closest to the given point
        public float GetClosestDistanceAlongPath(Vector2 worldPoint) {
            TimeOnPathData data = CalculateClosestPointOnPathData(worldPoint);
            return Mathf.Lerp(cumulativeLengthAtEachVertex[data.previousIndex], cumulativeLengthAtEachVertex[data.nextIndex], data.percentBetweenIndices);
        }

        #endregion

        #region Internal methods

        /// For a given value 't' between 0 and 1, calculate the indices of the two vertices before and after t. 
        /// Also calculate how far t is between those two vertices as a percentage between 0 and 1.
        TimeOnPathData CalculatePercentOnPathData(float t, EndOfPathInstruction endOfPathInstruction) {
            // Constrain t based on the end of path instruction
            switch ( endOfPathInstruction ) {
                case EndOfPathInstruction.Loop:
                    // If t is negative, make it the equivalent value between 0 and 1
                    if ( t < 0 ) {
                        t += Mathf.CeilToInt(Mathf.Abs(t));
                    }
                    t %= 1;
                    break;
                case EndOfPathInstruction.Reverse:
                    t = Mathf.PingPong(t, 1);
                    break;
                case EndOfPathInstruction.Stop:
                    t = Mathf.Clamp01(t);
                    break;
            }

            int prevIndex = 0;
            int nextIndex = NumPoints - 1;
            int i = Mathf.RoundToInt(t * ( NumPoints - 1 )); // starting guess

            // Starts by looking at middle vertex and determines if t lies to the left or to the right of that vertex.
            // Continues dividing in half until closest surrounding vertices have been found.
            while ( true ) {
                // t lies to left
                if ( t <= times[i] ) {
                    nextIndex = i;
                }
                // t lies to right
                else {
                    prevIndex = i;
                }
                i = ( nextIndex + prevIndex ) / 2;

                if ( nextIndex - prevIndex <= 1 ) {
                    break;
                }
            }

            float abPercent = Mathf.InverseLerp(times[prevIndex], times[nextIndex], t);
            return new TimeOnPathData(prevIndex, nextIndex, abPercent);
        }

        /// Calculate time data for closest point on the path from given world point
        TimeOnPathData CalculateClosestPointOnPathData(Vector2 worldPoint) {
            float minSqrDst = float.MaxValue;
            Vector2 closestPoint = Vector2.zero;
            int closestSegmentIndexA = 0;
            int closestSegmentIndexB = 0;

            for ( int i = 0; i < localPoints.Length; i++ ) {
                int nextI = i + 1;
                if ( nextI >= localPoints.Length ) {
                    if ( isClosedLoop ) {
                        nextI %= localPoints.Length;
                    }
                    else {
                        break;
                    }
                }

                Vector2 closestPointOnSegment = MathUtility.ClosestPointOnLineSegment(worldPoint, GetPoint(i), GetPoint(nextI));
                float sqrDst = ( worldPoint - closestPointOnSegment ).sqrMagnitude;
                if ( sqrDst < minSqrDst ) {
                    minSqrDst = sqrDst;
                    closestPoint = closestPointOnSegment;
                    closestSegmentIndexA = i;
                    closestSegmentIndexB = nextI;
                }

            }
            float closestSegmentLength = ( GetPoint(closestSegmentIndexA) - GetPoint(closestSegmentIndexB) ).magnitude;
            float t = ( closestPoint - GetPoint(closestSegmentIndexA) ).magnitude / closestSegmentLength;
            return new TimeOnPathData(closestSegmentIndexA, closestSegmentIndexB, t);
        }

        public struct TimeOnPathData {
            public readonly int previousIndex;
            public readonly int nextIndex;
            public readonly float percentBetweenIndices;

            public TimeOnPathData(int prev, int next, float percentBetweenIndices) {
                this.previousIndex = prev;
                this.nextIndex = next;
                this.percentBetweenIndices = percentBetweenIndices;
            }
        }

        #endregion

    }
}
