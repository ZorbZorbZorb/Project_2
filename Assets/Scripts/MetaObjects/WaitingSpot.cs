using Assets.Scripts;
using Assets.Scripts.Objects;
using UnityEngine;

public class WaitingSpot : CustomerInteractable {
    public Bathroom Bathroom;
    public override InteractableType IType => InteractableType.WaitingSpot;
    public override ReliefType RType => ReliefType.None;
    public override Vector3 CustomerPositionF => transform.position;
    public override Vector3 CustomerPositionM => transform.position;
    public override bool HidesCustomer => false;
    public override bool CanWetHere => true;
    public override string DisplayName => "Queuing spot";  // How Bri-ish'
    public bool Wet = false;
    public override bool CanBeSoiled => false;
    public override bool ChangesCustomerSprite => false;
}
