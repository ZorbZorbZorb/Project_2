using Assets.Scripts.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seat : CustomerInteractable {
    public override Vector3 CustomerPositionF => transform.position + new Vector3(0, 0, -1);
    public override Vector3 CustomerPositionM => transform.position + new Vector3(0, 0, -1);

    public override Collections.Location CustomerLocation => Collections.Location.Bar;

    public override bool HidesCustomer => true;

    public override bool CanWetHere => true;

    public override Collections.ReliefType ReliefType => Collections.ReliefType.None;

    public override string DisplayName => "seat";

    public void MoveCustomerIntoSpot(Customer customer) {
        customer.position = Collections.Location.Bar;
        customer.StopOccupyingAll();
        customer.Occupying = this;
        OccupiedBy = customer;
        customer.MoveToVector3(CustomerPositionF);
    }
}
