using Assets.Scripts.Objects;
using System.Collections.Generic;
using UnityEngine;

public class Toilet : Relief {
    public override InteractableType Type => InteractableType.Toilet;
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = 0, z = 1 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = -15, z = 1 };
    public override Collections.ReliefType ReliefType => Collections.ReliefType.Toilet;
    public override bool HidesCustomer => true;
    public override string DisplayName => "Toilet";
    public override Collections.CustomerActionState StatePantsDown => Collections.CustomerActionState.ToiletPantsDown;
    public override Collections.CustomerActionState StatePeeing => Collections.CustomerActionState.ToiletPeeing;
    public override Collections.CustomerActionState StatePantsUp => Collections.CustomerActionState.ToiletPantsUp;
    public override bool CanBeSoiled => false;

    public override bool ChangesCustomerSprite => true;

    private bool doorClosed = false;
    

    
    private void Update() {
        // Open or close the stall door
        if (OccupiedBy != null && !doorClosed && OccupiedBy.AtDestination()) {
            SRenderer.sprite = Collections.spriteStallClosed;
            doorClosed = true;
        }
        else if(doorClosed && OccupiedBy == null) {
            SRenderer.sprite = Collections.spriteStallOpened;
            doorClosed = false;
        }
    }

    private void OnMouseOver() {
        if (OccupiedBy != null) {
            SRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        }
    }
    private void OnMouseExit() {
        SRenderer.color = new Color(1f, 1f, 1f, 1f);
    }

    [SerializeField] public Sprite SpritePantsDownM;
    [SerializeField] public Sprite SpritePantsUpM;
    [SerializeField] public Sprite SpritePeeingM;
    [SerializeField] public Sprite SpritePantsDownF;
    [SerializeField] public Sprite SpritePantsUpF;
    [SerializeField] public Sprite SpritePeeingF;

    private Dictionary<Collections.CustomerActionState, Sprite> SpriteLookupM = null;
    private Dictionary<Collections.CustomerActionState, Sprite> SpriteLookupF = null;

    public override Sprite GetCustomerSprite(Customer customer) {
        if ( SpriteLookupM == null ) {
            SpriteLookupM = new Dictionary<Collections.CustomerActionState, Sprite>() {
                { Collections.CustomerActionState.SinkPantsDown, SpritePantsDownM },
                { Collections.CustomerActionState.SinkPantsUp, SpritePantsUpM },
                { Collections.CustomerActionState.SinkPeeing, SpritePeeingM }
            };
            SpriteLookupF = new Dictionary<Collections.CustomerActionState, Sprite>() {
                { Collections.CustomerActionState.SinkPantsDown, SpritePantsDownF },
                { Collections.CustomerActionState.SinkPantsUp, SpritePantsUpF },
                { Collections.CustomerActionState.SinkPeeing, SpritePeeingF }
            };
        }

        if ( customer.Gender == 'm' ) {
            return SpriteLookupM[customer.ActionState];
        }
        else {
            return SpriteLookupF[customer.ActionState];
        }

    }
}

