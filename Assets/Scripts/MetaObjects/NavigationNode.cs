using UnityEngine;

namespace Assets.Scripts.MetaObjects {
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
        /// <summary>
        /// Should navigation including this node use both the in and out point?
        /// </summary>
        [SerializeField]
        public bool UseBothPositions;
        public void Start() {
            Navigation.Nodes.Add(this);
        }
    }
}
