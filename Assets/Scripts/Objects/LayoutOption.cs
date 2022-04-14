using Assets.Scripts.Areas;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Objects {
    [Serializable]
    public class LayoutOption {
        public int Cost;
        public InteractableType Type;
        //public Action SpawnAction => () => { BuildClickable.HandleSpawn(spot, option); };
        // Condition for build, such as achievmenet should be in this class?

    }
}
