using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Customers {
    static public class Navigation {
        static private Dictionary<Location, List<NavigationNode>> nodes = new Dictionary<Location, List<NavigationNode>>();
        static public Vector3 CustomerSpawnpoint => nodes[Location.Outside]
            .First(x => x.GetOther(Location.Outside).Location == Location.Bar)
            .GetOther(Location.Bar);
        static private List<Location> getReachableLocations(Location location) {
            List<Location> result = new List<Location>();
            foreach ( NavigationNode node in nodes[location] ) {
                Location other = node.GetOther(location);
                if ( !result.Contains(other) ) {
                    result.Add(other);
                }
            }
            return result;
        }
        /// <summary>
        /// Returns vectors to reach <paramref name="destination"/> from <paramref name="source"/> 
        /// </summary>
        /// <returns>
        /// List of <see cref="Vector3"/>s to reach <paramref name="destination"/> from <paramref name="source"/> 
        /// </returns>
        static public List<Vector2> Navigate(Location source, Location destination) {
            // Return empty if source is already destination
            if ( source == destination ) {
                return new List<Vector2>();
            }

            List<NavigationNode> path = new List<NavigationNode>();
            List<Location> explored = new List<Location>() { source };
            Location current = source;

            // Find a path
            do {
                // Get all potential unexplored locations we can reach from current
                var unexploredOptions = getReachableLocations(current)
                    .Where(x => !explored.Contains(x));
                if ( unexploredOptions.Any() ) {
                    // Explore a reachable unexplored location
                    Location location = unexploredOptions.First();
                    NavigationNode node = nodes[location].First(x => x.GetOther(location) == current);
                    explored.Add(location);
                    path.Add(node);
                    current = location;
                }
                else if ( path.Count > 0 ) {
                    // Back out of an explored location
                    NavigationNode node = path.Last();
                    current = node.GetOther(current);
                    path.Remove(node);
                }
                else {
                    // Uh oh, no path found
                    Debug.LogError($"No path exists from {source} to {destination}");
                    break;
                }
            }
            while ( current != destination );

            // Return the path
            current = source;
            List<Vector2> results = new List<Vector2>();
            for ( int i = 0; i < path.Count(); i++ ) {
                NavigationNode node = path[i];
                bool isFinal = i == path.Count() - 1;
                // Always use both points if this is the final node and path length is too short
                if ( node.NodeUseType == NodeUseType.Both || ( isFinal && results.Count() < 2 ) ) {
                    AddPoints(node);
                }
                else {
                    AddPoint(node);
                }
                current = node.GetOther(current);
            }
            return results;

            void AddPoints(NavigationNode node) {
                if ( node.Point1.Location == current ) {
                    results.Add(node.Point1.Transform.position);
                    results.Add(node.Point2.Transform.position);
                }
                else {
                    results.Add(node.Point2.Transform.position);
                    results.Add(node.Point1.Transform.position);
                }
            }
            void AddPoint(NavigationNode node) {
                if (node.NodeUseType == NodeUseType.Inner) {
                    if ( node.Point1.Location == current ) {
                        results.Add(node.Point1.Transform.position);
                    }
                    else {
                        results.Add(node.Point2.Transform.position);
                    }
                }
                else {
                    if ( node.Point1.Location == current ) {
                        results.Add(node.Point2.Transform.position);
                    }
                    else {
                        results.Add(node.Point1.Transform.position);
                    }
                }
            }
        }
        static public void Add(NavigationNode node) {
            if ( nodes.ContainsKey(node.Point1.Location) ) {
                nodes[node.Point1.Location].Add(node);
            }
            else {
                nodes.Add(node.Point1.Location, new List<NavigationNode>() { node });
            }
            if ( nodes.ContainsKey(node.Point2.Location) ) {
                nodes[node.Point2.Location].Add(node);
            }
            else {
                nodes.Add(node.Point2.Location, new List<NavigationNode>() { node });
            }
        }
    }
}
