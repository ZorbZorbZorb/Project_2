using Assets.Scripts.Objects;
using System.Collections.Generic;
using UnityEngine;

public class Sink : Relief {
    public Sinks Sinks;
    public override InteractableType Type => InteractableType.Sink;
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = 10, z = -1 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = -5, z = -1 };
    public override Collections.ReliefType ReliefType => Collections.ReliefType.Sink;
    public override bool HidesCustomer => false;
    public override string DisplayName => "Sink";
    public override bool CanBeSoiled => false;

    public override bool ChangesCustomerSprite => true;

    public override Sprite GetCustomerSprite(Customer customer) {
        if (SpriteLookupM == null) {
            BuildSpriteLookup();
        }

        if ( customer.Gender == 'm' ) {
            return SpriteLookupM[customer.ActionState];
        }
        else {
            return SpriteLookupF[customer.ActionState];
        }
        
    }

    public void Use(Customer customer) {
        customer.ActionState = Collections.CustomerActionState.WashingHands;
        customer.NextDelay = 6f;
        customer.Next = () => {
            if (customer.IsWet) {
                customer.ActionState = Collections.CustomerActionState.None;
                customer.Leave();
            }
            else {
                customer.ActionState = Collections.CustomerActionState.None;
                customer.EnterBar();
            }
        };
    }
}
