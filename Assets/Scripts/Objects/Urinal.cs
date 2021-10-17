using Assets.Scripts.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Urinal : Relief {
    public override InteractableType IType => InteractableType.Urinal;
    public override Vector3 CustomerPositionF => Sideways 
        ? transform.position + new Vector3() { x = -40, y = 25, z = 1 } 
        : transform.position + new Vector3() { x = 0, y = -5, z = -1 };
    public override Vector3 CustomerPositionM => Sideways 
        ? transform.position + new Vector3() { x = -45, y = 15, z = 1 }
        : transform.position + new Vector3() { x = 0, y = -10, z = -1 };
    public override ReliefType RType => ReliefType.Urinal;
    public override bool HidesCustomer => false;
    public override string DisplayName => "Urinal";
    public override bool CanBeSoiled => false;

    public override bool ChangesCustomerSprite => true;

    public override Sprite GetCustomerSprite(Customer customer) {
        if ( customer.Gender == 'm' ) {
            return SpriteLookupM[customer.ActionState];
        }
        else {
            return SpriteLookupF[customer.ActionState];
        }

    }
}
