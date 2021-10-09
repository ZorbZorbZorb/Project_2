using UnityEngine;

namespace Assets.Scripts.Objects {
    public abstract class Relief : CustomerInteractable {
        public Customer InUseBy;
        public Customer ReservedBy;
        public override Collections.Location CustomerLocation => Collections.Location.Relief;
        // Confucius says, Man whos dick is out of pants, cannot piss in pants
        public override bool CanWetHere => false;
    }
}
