using Assets.Scripts.Objects;
using System;
using UnityEngine;
using Assets.Scripts;

public enum SeatType {
    None,
    Counter,
    Table
}

public class Seat : CustomerInteractable {
    public override InteractableType IType => InteractableType.Seat;
    public override string DisplayName => "seat";
    public override Vector3 CustomerPositionF => transform.position + new Vector3(0, 0);
    public override Vector3 CustomerPositionM => transform.position + new Vector3(0, 0);
    public override bool HidesCustomer => true;
    public override bool CanWetHere => true;
    public override ReliefType RType => ReliefType.None;
    public override bool CanBeSoiled => true;
    public override bool ChangesCustomerSprite => true;
    
    public SeatType SeatType;

    private void Update() {
        if ( OccupiedBy != null && OccupiedBy.AtDestination ) {
            MainSRenderer.enabled = false;
            return;
        }
        else {
            MainSRenderer.enabled = true;
        }
        if ( IsSoiled ) {
            MainSRenderer.sprite = Collections.SpriteStoolWet;
        }
        else {
            MainSRenderer.sprite = Collections.SpriteStoolNormal;
        }
    }
}
