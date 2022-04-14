using Assets.Scripts.Areas;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Objects {
    [Serializable]
    public class LayoutSpot {
        public double X;
        public double Y;
        public List<LayoutOption> Options;
        public Orientation Facing;

        // Here is where I could put a "Depends-On-Spot", where you can't build here
        //   until you build a nearby or dependent spot first! That could be cool.

        [JsonIgnore]
        public Alignment Alignment => Facing == Orientation.North || Facing == Orientation.South
        ? Alignment.Vertical
        : Alignment.Horizontal;

        public InteractableType Current;

        /// <summary>
        /// The <see cref="Area"/> this <see cref="LayoutOption"/> is bound to.
        /// <para>No reference is set until <see cref="Apply"/> is called.</para>
        /// </summary>
        [NonSerialized, JsonIgnore]
        public Area Area;
        /// <summary>
        /// The current <see cref="CustomerInteractable"/> instance for this <see cref="LayoutOption"/>.
        /// <para>No reference is set until <see cref="Apply"/> is called.</para>
        /// </summary>
        [NonSerialized, JsonIgnore]
        public CustomerInteractable Interactable;
    }
}
