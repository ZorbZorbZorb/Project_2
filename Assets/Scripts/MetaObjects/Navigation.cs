using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.MetaObjects {
    /// <summary>
    /// Acts as a point in space that tells customers where to first go when 
    ///   trying to reach another destination.
    /// <para>
    /// Acts as a reference to it's current Vector3, 
    ///   Where it is considered being, and where it connects to
    /// </para>
    /// </summary>
    internal class Navigation : MonoBehaviour {
        static public List<Navigation> Nodes { get; private set; } = new List<Navigation>();
        //static public Dictionary<Location, Dictionary<Location, Vector3>> Links { get; private set; }
        /// <summary>
        /// Returns intermediate vectors between the <paramref name="source"/> 
        ///   and <paramref name="destination"/>
        /// </summary>
        /// <returns>
        /// List of <see cref="Vector3"/>s between <paramref name="source"/> and 
        /// <paramref name="destination"/>
        /// </returns>
        static public IEnumerable<Vector3> Navigate(Location source, Location destination) {
            Location current = source;
            List<Location> path = new List<Location> () { 
                source 
            };
            List<Location> explored = new List<Location>() {
                source
            };
            do {
                // Does current position contain any nodes?
                if ( Nodes.Any(x => x.From == current ) ) {
                    var nodes = Nodes.Where( x=> x.From == current) ;
                    // If final leg of journey
                    if ( nodes.Any( x => x.To == destination )) {
                        Navigation node = nodes.First(x => x.To == destination);
                        yield return node.transform.position;
                        // Add sister node if required and exists
                        if (node.UseBothNodes) {
                            yield return GetSiblingNode(node).transform.position;
                        }
                    }
                    // Else crawl down tree of nodes to find destination
                    else {
                        foreach ( var location in nodes.Keys ) {
                            if ( ReachableLocations(ref explored, destination).Contains(destination) {
                                explored = new List<Location>();
                                explored.AddRange(path);
                                path.Add()
                            }
                        }
                    }
                }
                else {
                    Debug.LogWarning($"Missing navigation link from {source} to {destination}");
                    current = destination;
                }
            } 
            while ( current != destination );

            // Crawls down the Links list to find all locations reachable from the current location
            //   Pays no creedance to path length, but will not move in circles if I decide to add
            //   circular pathing... for whatever reason I would do that. Circle bar! Circle bar!
            List<Location> ReachableLocations (ref List<Location> explored, Location current) {
                explored.Add(current);
                if (!Links.ContainsKey(current)) {
                    return explored;
                }
                else {
                    foreach(var location in Links[current].Keys) {
                        if (!explored.Contains(location)) {
                            ReachableLocations(ref explored, location);
                        }
                    }
                }
                return explored;
            }
            Navigation GetSiblingNode(Navigation node) {
                return Nodes.First(x => x.From == node.To && x.To == node.From);
            }
        }

        /// <summary>
        /// Were this node is located
        /// </summary>
        public Location From { get; private set; }
        /// <summary>
        /// Where this node is connecting to
        /// </summary>
        public Location To { get; private set; }
        /// <summary>
        /// Should navigation including this node search for a sibling node and include it?
        /// </summary>
        public bool UseBothNodes { get; private set; }

        public void Start() {
            Nodes.Add(this); 
            //if ( Links.ContainsKey(From) ) {
            //    Links[From].Add(To, transform.position);
            //}
            //else {
            //    Links.Add(From, new Dictionary<Location, Vector3>() {
            //        { To, transform.position }
            //    });
            //}
        }
    }
}
