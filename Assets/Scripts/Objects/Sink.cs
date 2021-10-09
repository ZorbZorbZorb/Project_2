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
    public override Collections.CustomerActionState StatePantsDown => Collections.CustomerActionState.SinkPantsDown;
    public override Collections.CustomerActionState StatePeeing => Collections.CustomerActionState.SinkPeeing;
    public override Collections.CustomerActionState StatePantsUp => Collections.CustomerActionState.SinkPantsUp;
    public override bool CanBeSoiled => false;

    public override bool ChangesCustomerSprite => true;

    [SerializeField] public Sprite SpritePantsDownM;
    [SerializeField] public Sprite SpritePantsUpM;
    [SerializeField] public Sprite SpritePeeingM;
    [SerializeField] public Sprite SpritePantsDownF;
    [SerializeField] public Sprite SpritePantsUpF;
    [SerializeField] public Sprite SpritePeeingF;

    private Dictionary<Collections.CustomerActionState, Sprite> SpriteLookupM = null;
    private Dictionary<Collections.CustomerActionState, Sprite> SpriteLookupF = null;

    public override Sprite GetCustomerSprite(Customer customer) {
        if (SpriteLookupM == null) {
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

    public void Use(Customer customer) {
        customer.ActionState = Collections.CustomerActionState.SinkWashingHands;
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
