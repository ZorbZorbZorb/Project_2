using Assets.Scripts.MetaObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {
    public partial class Navigation {
        static public List<NavigationNode> Nodes { get; private set; } = new List<NavigationNode>();
        /// <summary>
        /// Returns vectors to reach <paramref name="destination"/> from <paramref name="source"/> 
        /// </summary>
        /// <returns>
        /// List of <see cref="Vector3"/>s to reach <paramref name="destination"/> from <paramref name="source"/> 
        /// </returns>
        static public List<Vector3> Navigate(Location source, Location destination) {
            List<Vector3> result = new List<Vector3>();
            if (source == destination) {
                return result;
            }
            throw new NotImplementedException();
        }
    }

}
