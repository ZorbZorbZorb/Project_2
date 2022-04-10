using Assets.Scripts.Customers;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Objects {
    public abstract class Relief : CustomerInteractable {
        public Customer InUseBy;
        public Customer ReservedBy;
        // Confucius says, Man whos dick is out of pants, cannot piss in pants
        public override bool CanWetHere => false;

        public void Start() {
            switch (Facing) {
                case Orientation.North:
                    throw new System.NotImplementedException("Relief :: Start() -- North facing sprites are not implemented.");
                case Orientation.South:
                    MainSRenderer.sprite = MainSprites[0];
                    break;
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
