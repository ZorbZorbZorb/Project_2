using Assets.Scripts.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seat : CustomerInteractable {
    public override InteractableType Type => InteractableType.Seat;
    public override string DisplayName => "seat";
    public override Vector3 CustomerPositionF => transform.position + new Vector3(0, 0, -1);
    public override Vector3 CustomerPositionM => transform.position + new Vector3(0, 0, -1);

    public override Collections.Location CustomerLocation => Collections.Location.Bar;

    public override bool HidesCustomer => true;

    public override bool CanWetHere => true;

    public override Collections.ReliefType ReliefType => Collections.ReliefType.None;

    public override bool CanBeSoiled => true;

    public SpriteRenderer Renderer;

    private void Start() {
        Renderer = gameObject.GetComponent<SpriteRenderer>();
    }

    private void Update() {
        if (OccupiedBy != null && OccupiedBy.AtDestination()) {
            Renderer.enabled = false;
            return;
        }
        else {
            Renderer.enabled = true;
        }
        if ( IsSoiled ) {
            //transform.Rotate(new Vector3(10, 0));
            Renderer.sprite = Collections.SpriteStoolWet;
        }
        else {
            Renderer.sprite = Collections.SpriteStoolNormal;
        }
    }

    public void MoveCustomerIntoSpot(Customer customer) {
        customer.StopOccupyingAll();
        if (customer.position != Collections.Location.Bar) {
            foreach (Vector3 keyframe in Collections.NavigationKeyframesFromBathroomToBar) {
                customer.MoveToVector3(keyframe);
            }
        }
        customer.MoveToVector3(customer.Gender == 'm' ? CustomerPositionM : CustomerPositionF);
        customer.Occupying = this;
        OccupiedBy = customer;
        customer.position = Collections.Location.Bar;
    }
}
