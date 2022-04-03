using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Objects {
    public abstract class Relief : CustomerInteractable {
        public Customer InUseBy;
        public Customer ReservedBy;
        public override Collections.Location CustomerLocation => Collections.Location.Relief;
        // Confucius says, Man whos dick is out of pants, cannot piss in pants
        public override bool CanWetHere => false;

        private void Start() {
            switch (Orientation) {
                case Orientation.North:
                    MainSRenderer.sprite = MainSprites[0];
                    break;
                case Orientation.South:
                    throw new System.NotImplementedException("Relief :: Start() -- South facing sprites are not implemented.");
                case Orientation.West:
                    MainSRenderer.sprite = MainSpritesSideways[0];
                    break;
                case Orientation.East:
                    MainSRenderer.sprite = MainSpritesSideways[0];
                    var eulerAngles = MainSRenderer.transform.localRotation.eulerAngles;
                    transform.localRotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y + 180, eulerAngles.z);
                    break;
            }
        }
    }
}
