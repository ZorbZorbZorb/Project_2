using UnityEngine;

namespace Assets.Scripts.MetaObjects {
    public partial class NavigationNode : Behaviour {
        ///<summary>
        /// The first location this node connects
        /// </summary>
        [SerializeField]
        public NavigationPoint Point1 { get; private set; }
        /// <summary>
        /// The second location this node connects
        /// </summary>
        [SerializeField]
        public NavigationPoint Point2 { get; private set; }
        /// <summary>
        /// Should navigation including this node use both the in and out point?
        /// </summary>
        [SerializeField]
        public bool UseBothPositions { get; private set; }
        public void Start() {
            Navigation.Nodes.Add(this);
        }
    }
}
