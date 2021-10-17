using Assets.Scripts.Objects;
using System.Collections.Generic;
using UnityEngine;

public class Toilet : Relief {
    public override InteractableType IType => InteractableType.Toilet;
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = 0, z = 1 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = -15, z = 1 };
    public override ReliefType RType => ReliefType.Toilet;
    public override bool HidesCustomer => true;
    public override string DisplayName => "Toilet";
    public override bool CanBeSoiled => false;

    public override bool ChangesCustomerSprite => true;

    private bool doorClosed = false;
    
    // TODO: This doesn't need to be an on update. I was just lazy.
    private void Update() {
        // Open or close the stall door
        if (OccupiedBy != null && !doorClosed && OccupiedBy.AtDestination()) {
            AltSRenderer.sprite = Collections.spriteStallClosed;
            doorClosed = true;
        }
        else if(doorClosed && OccupiedBy == null) {
            AltSRenderer.sprite = Collections.spriteStallOpened;
            doorClosed = false;
        }
    }

    private void OnMouseOver() {
        if (OccupiedBy != null) {
            AltSRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        }
    }
    private void OnMouseExit() {
        AltSRenderer.color = new Color(1f, 1f, 1f, 1f);
    }

    public override Sprite GetCustomerSprite(Customer customer) {
        if ( customer.Gender == 'm' ) {
            return SpriteLookupM[customer.ActionState];
        }
        else {
            return SpriteLookupF[customer.ActionState];
        }

    }
}

