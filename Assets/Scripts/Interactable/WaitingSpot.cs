using Assets.Scripts.Areas;
using Assets.Scripts.Objects;
using UnityEngine;

public enum WaitingSpotType {
    None,
    LineGhost,
    DrinksGhost,
    Line,
    Bathroom,
    Sink
}

public class WaitingSpot : CustomerInteractable {
    public Bathroom Bathroom;
    public override InteractableType IType => InteractableType.WaitingSpot;
    public override ReliefType RType => ReliefType.None;
    public WaitingSpotType WaitingSpotType = WaitingSpotType.None;
    public override Vector3 CustomerPositionF => transform.position;
    public override Vector3 CustomerPositionM => transform.position;
    public override bool HidesCustomer => false;
    public override bool CanWetHere => true;
    public override string DisplayName => "Queuing spot";  // How Bri-ish'
    public bool Wet = false;
    public override bool CanBeSoiled => false;
    public override bool ChangesCustomerSprite => false;
}
