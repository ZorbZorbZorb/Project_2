﻿using Assets.Scripts.MetaObjects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {
    static public class Navigation {
        static private Dictionary<Location, List<NavigationNode>> nodes = new Dictionary<Location, List<NavigationNode>>();
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
        static public List<Vector3> Navigate(Location source, Location destination) {
            // Return empty if source is already destination
            if ( source == destination ) {
                return new List<Vector3>();
            }

            List<NavigationNode> path = new List<NavigationNode>();
            List<Location> explored = new List<Location>() { source };
            Location current = source;

            // Find a path
            do {
                // Get all potential unexplored locations we can reach from current
                var unexploredOptions = getReachableLocations(current)
                    .Where(x => !explored.Contains(x));
                if (unexploredOptions.Any()) {
                    // Explore a reachable unexplored location
                    Location location = unexploredOptions.First();
                    NavigationNode node = nodes[location].First(x => x.GetOther(location) == current);
                    explored.Add(location);
                    path.Add(node);
                    current = location;
                }
                else if (path.Count > 0) {
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
            List<Vector3> results = new List<Vector3>();
            foreach ( NavigationNode node in path ) {
                if (node.Point1.Location == current) {
                    results.Add(node.Point1.Transform.position);
                    results.Add(node.Point2.Transform.position);
                }
                else {
                    results.Add(node.Point2.Transform.position);
                    results.Add(node.Point1.Transform.position);
                }
                current = node.GetOther(current);
            }
            return results;
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