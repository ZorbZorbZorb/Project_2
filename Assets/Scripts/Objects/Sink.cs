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
    public override bool CanBeSoiled => false;

    public override bool ChangesCustomerSprite => true;

    [SerializeField] public Sprite SpritePantsDownM;
    [SerializeField] public Sprite SpritePantsUpM;
    [SerializeField] public Sprite SpritePeeingM;
    [SerializeField] public Sprite SpriteWashM;
    [SerializeField] public Sprite SpritePantsDownF;
    [SerializeField] public Sprite SpritePantsUpF;
    [SerializeField] public Sprite SpritePeeingF;
    [SerializeField] public Sprite SpriteWashF;

    private Dictionary<Collections.CustomerActionState, Sprite> SpriteLookupM = null;
    private Dictionary<Collections.CustomerActionState, Sprite> SpriteLookupF = null;

    public override Sprite GetCustomerSprite(Customer customer) {
        if (SpriteLookupM == null) {
            SpriteLookupM = new Dictionary<Collections.CustomerActionState, Sprite>() {
                { Collections.CustomerActionState.PantsDown, SpritePantsDownM },
                { Collections.CustomerActionState.PantsUp, SpritePantsUpM },
                { Collections.CustomerActionState.Peeing, SpritePeeingM },
                { Collections.CustomerActionState.WashingHands, SpriteWashM}
            };
            SpriteLookupF = new Dictionary<Collections.CustomerActionState, Sprite>() {
                { Collections.CustomerActionState.PantsDown, SpritePantsDownF },
                { Collections.CustomerActionState.PantsUp, SpritePantsUpF },
                { Collections.CustomerActionState.Peeing, SpritePeeingF },
                { Collections.CustomerActionState.WashingHands, SpriteWashF}
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
        customer.ActionState = Collections.CustomerActionState.WashingHands;
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
