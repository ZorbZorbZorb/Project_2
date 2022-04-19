using Assets.Scripts.Customers;
using Assets.Scripts.Objects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts;

public static class Collections {
    public static Sprite spriteToilet = Resources.Load<Sprite>("Sprites/Entities/Toilet");
    public static Sprite spriteToiletOpened = Resources.Load<Sprite>("Sprites/Entities/Toilet_Open");
    public static Sprite spriteToiletClosed = Resources.Load<Sprite>("Sprites/Entities/Toilet");
    public static Sprite spriteUrinal = Resources.Load<Sprite>("Sprites/Entities/Urinal");
    public static Sprite spriteUrinalSideways = Resources.Load<Sprite>("Sprites/Entities/Urinal_Side");
    public static Sprite spriteSink = Resources.Load<Sprite>("Sprites/Entities/Sink");

    public static Sprite spriteStallClosed = Resources.Load<Sprite>("Sprites/Entities/Stall_closed");
    public static Sprite spriteStallOpened = Resources.Load<Sprite>("Sprites/Entities/Stall_opened");

    public static Sprite spriteTable = Resources.Load<Sprite>("Sprites/Entities/Table");

    public static Sprite SpriteStoolNormal = Resources.Load<Sprite>("Sprites/Entities/Stool_normal");
    public static Sprite SpriteStoolWet = Resources.Load<Sprite>("Sprites/Entities/Stool_wet");

    public static Dictionary<CustomerAction, Sprite> BubbleSpriteLookupF = 
        BubbleSpriteLookupF = new Dictionary<CustomerAction, Sprite>() {
            //{ CustomerAction.Wetting, Resources.Load<Sprite>("Sprites/Bubbles/bubble_pee_stream") },
            //{ CustomerAction.Peeing, Resources.Load<Sprite>("Sprites/Bubbles/bubble_pee_stream") },
            { CustomerAction.PantsDown, Resources.Load<Sprite>("Sprites/Bubbles/bubble_zipper_down") },
            { CustomerAction.PantsUp, Resources.Load<Sprite>("Sprites/Bubbles/bubble_zipper_up") },
        };
    public static Dictionary<CustomerAction, Sprite> BubbleSpriteLookupM = 
        BubbleSpriteLookupM = new Dictionary<CustomerAction, Sprite>() {
            //{ CustomerAction.Wetting, Resources.Load<Sprite>("Sprites/Bubbles/bubble_pee_stream") },
            //{ CustomerAction.Peeing, Resources.Load<Sprite>("Sprites/Bubbles/bubble_pee_stream") },
            { CustomerAction.PantsDown, Resources.Load<Sprite>("Sprites/Bubbles/bubble_zipper_down") },
            { CustomerAction.PantsUp, Resources.Load<Sprite>("Sprites/Bubbles/bubble_zipper_up") },
        };

    public static Dictionary<char, Dictionary<CustomerAction, Sprite>> GenderedBubbleSpriteLookup = 
        new Dictionary<char, Dictionary<CustomerAction, Sprite>>() {
            {'f', BubbleSpriteLookupF },
            {'m', BubbleSpriteLookupM }
    };

    public static CustomerAction[] ValidBubbleActionStatesF = BubbleSpriteLookupF.Keys.ToArray();
    public static CustomerAction[] ValidBubbleActionStatesM = BubbleSpriteLookupM.Keys.ToArray();

    public static readonly Dictionary<InteractableType, Sprite> VerticalInteractableSprites =
        new Dictionary<InteractableType, Sprite>() {
            { InteractableType.Sink, spriteSink },
            { InteractableType.Toilet, spriteToiletClosed },
            { InteractableType.Seat, SpriteStoolNormal },
            { InteractableType.Urinal, spriteUrinal },
            { InteractableType.Table, spriteTable }
    };
    public static readonly Dictionary<InteractableType, Sprite> HorizontalInteractableSprites =
        new Dictionary<InteractableType, Sprite>() {
            { InteractableType.Urinal, spriteUrinalSideways },
    };

    public static Sprite GetBubbleSprite(Customer customer) {
        CustomerAction[] ValidBubbleActionStates = customer.Gender == 'm' ? ValidBubbleActionStatesM : ValidBubbleActionStatesF;
        if ( !ValidBubbleActionStates.Contains(customer.CurrentAction) ) {
            return null;
        }

        // Bubble displays early if they are about to wet themselves
        if ( customer.DesperationState == CustomerDesperationState.State5 || customer.AtDestination ) {
            return GenderedBubbleSpriteLookup[customer.Gender][customer.CurrentAction];
        }

        return null;
    }
}
