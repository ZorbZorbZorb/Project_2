using Assets.Scripts.Customers;
using Assets.Scripts.Objects;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class Sink : Relief {
    public override InteractableType IType => InteractableType.Sink;
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = 10, z = -1 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = -5, z = -1 };
    public override ReliefType RType => ReliefType.Sink;
    public override bool HidesCustomer => false;
    public override string DisplayName => "Sink";
    public override bool CanBeSoiled => false;
    public override bool ChangesCustomerSprite => true;
    public void UseForWash(Customer customer) {
        customer.Occupy(this);

        customer.ActionState = CustomerActionState.WashingHands;

        customer.NextDelay = 10f;
        customer.Next = () => {
            if (customer.IsWet) {
                customer.ActionState = CustomerActionState.None;
                customer.Leave();
            }
            else {
                customer.ActionState = CustomerActionState.None;
                customer.EnterBar();
            }
        };
    }
}
