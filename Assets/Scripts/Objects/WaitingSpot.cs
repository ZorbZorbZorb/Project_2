using Assets.Scripts.Interfaces;
using Assets.Scripts.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingSpot : CustomerInteractable {
    public override Collections.Location CustomerLocation => Collections.Location.WaitingRoom;
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = 100, z = 0 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = 100, z = 0 };
    public override bool HidesCustomer => false;
    public override bool CanWetHere => true;
    public override Collections.IReliefType ReliefType => Collections.IReliefType.None;
    public override string DisplayName => "Queuing spot";  // How Bri-ish'
    public bool Wet = false;
    public Collections.Location CustomerState = Collections.Location.WaitingRoom;

    public void MoveCustomerIntoSpot(Customer customer) {
        customer.StopOccupyingAll();
        customer.Occupying = this;
        OccupiedBy = customer;
        customer.MoveToVector3(CustomerPositionF);
    }
}
