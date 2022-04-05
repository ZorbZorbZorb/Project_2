using Assets.Scripts.Interfaces;
using Assets.Scripts.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingSpot : CustomerInteractable {
    public Bathroom SpotBathroom;
    public override InteractableType IType => InteractableType.WaitingSpot;
    public override ReliefType RType => ReliefType.None;
    [Obsolete("These two need to go")]
    public override Collections.Location CustomerLocation => Collections.Location.WaitingRoom;
    [Obsolete("These two need to go")]
    public Collections.Location CustomerState = Collections.Location.WaitingRoom;
    public override Vector3 CustomerPositionF => transform.position;
    public override Vector3 CustomerPositionM => transform.position;
    public override bool HidesCustomer => false;
    public override bool CanWetHere => true;
    public override string DisplayName => "Queuing spot";  // How Bri-ish'
    public bool Wet = false;
    public override bool CanBeSoiled => false;
    public override bool ChangesCustomerSprite => false;
    public void MoveCustomerIntoSpot(Customer customer) {
        customer.StopOccupyingAll();
        if ( customer.position == Collections.Location.Bar ) {
            foreach ( Vector3 keyframe in customer.CurrentBathroom.NavigationKeyframesFromBarToBathroom ) {
                customer.MoveToVector3(keyframe);
            }
        }
        customer.MoveToVector3(CustomerPositionF);
        customer.Occupying = this;
        OccupiedBy = customer;
    }
}
