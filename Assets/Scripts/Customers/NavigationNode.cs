using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Customers {
    public enum NodeUseType {
        Inner,
        Outer,
        Both
    }
    public class NavigationNode : MonoBehaviour {
        ///<summary>
        /// The first location this node connects
        /// </summary>
        [SerializeField]
        [SerializeReference]
        public NavigationPoint Point1 = new NavigationPoint();
        /// <summary>
        /// The second location this node connects
        /// </summary>
        [SerializeField]
        [SerializeReference]
        public NavigationPoint Point2 = new NavigationPoint();
        public NodeUseType NodeUseType;
        public Location[] Locations => new Location[2] { Point1.Location, Point2.Location };
        public NavigationPoint GetOther(Location source) {
            return Point1.Location == source ? Point2 : Point1;
        }
        public void Start() {
            Navigation.Add(this);
        }
    }
}
