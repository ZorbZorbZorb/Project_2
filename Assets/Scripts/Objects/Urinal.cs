using Assets.Scripts.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Urinal : Relief {
    public override InteractableType Type => InteractableType.Urinal;
    public override Vector3 CustomerPositionF => Sideways 
        ? transform.position + new Vector3() { x = -40, y = 25, z = 1 } 
        : transform.position + new Vector3() { x = 0, y = -5, z = -1 };
    public override Vector3 CustomerPositionM => Sideways 
        ? transform.position + new Vector3() { x = -45, y = 15, z = 1 }
        : transform.position + new Vector3() { x = 0, y = -10, z = -1 };
    public override Collections.ReliefType ReliefType => Collections.ReliefType.Urinal;
    public override bool HidesCustomer => false;
    public override string DisplayName => "Urinal";
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
        if ( SpriteLookupM == null ) {
            SpriteLookupM = new Dictionary<Collections.CustomerActionState, Sprite>() {
                { Collections.CustomerActionState.PantsDown, SpritePantsDownM },
                { Collections.CustomerActionState.PantsUp, SpritePantsUpM },
                { Collections.CustomerActionState.Peeing, SpritePeeingM }
            };
            SpriteLookupF = new Dictionary<Collections.CustomerActionState, Sprite>() {
                { Collections.CustomerActionState.PantsDown, SpritePantsDownF },
                { Collections.CustomerActionState.PantsUp, SpritePantsUpF },
                { Collections.CustomerActionState.Peeing, SpritePeeingF }
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
