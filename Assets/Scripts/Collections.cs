using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static Assets.Scripts.Objects.CustomerInteractable;
using Assets.Scripts.Characters;

public class Collections : MonoBehaviour {
    static public Collections collections = null;

    private void Awake() {
        collections = this;

        // Create customer sprite marshals
        CustomerSpriteController.Controller = new Dictionary<char, CustomerSpriteController>();
        CustomerSpriteController.NewController('m', "Sprites/People/m");
        CustomerSpriteController.NewController('f', "Sprites/People/f");

        Emote.PeeStrong =       new Emote("Sprites/Bubbles/Stream_3");
        Emote.PeeMedium =       new Emote("Sprites/Bubbles/Stream_2");
        Emote.PeeWeak =         new Emote("Sprites/Bubbles/Stream_1");
        Emote.PantsDown =       new Emote("Sprites/Bubbles/bubble_zipper_down");
        Emote.PantsUp =         new Emote("Sprites/Bubbles/bubble_zipper_up");
        Emote.StruggleStop =    new Emote("Sprites/Bubbles/bubble_struggle_stop");
        Emote.PeeStreamEmotes = new Emote[] { Emote.PeeWeak, Emote.PeeWeak, Emote.PeeWeak, Emote.PeeMedium, Emote.PeeMedium, Emote.PeeStrong, Emote.PeeStrong };
        // maybe add a null to the start so it looks like they took a second between peeing and pants up?
        //   No. Do this using next action chaining.

        spriteToilet = Resources.Load<Sprite>("Sprites/Entities/Toilet");
        spriteUrinal = Resources.Load<Sprite>("Sprites/Entities/Urinal");
        spriteUrinalSideways = Resources.Load<Sprite>("Sprites/Entities/Urinal_Side");
        spriteSink = Resources.Load<Sprite>("Sprites/Entities/Sink");

        spriteStallClosed = Resources.Load<Sprite>("Sprites/Entities/Stall_closed");
        spriteStallOpened = Resources.Load<Sprite>("Sprites/Entities/Stall_opened");

        BubbleSpriteLookupM = new Dictionary<CustomerActionState, Sprite>() {
            { CustomerActionState.Wetting, Resources.Load<Sprite>("Sprites/Bubbles/bubble_pee_stream") },
            { CustomerActionState.Peeing, Resources.Load<Sprite>("Sprites/Bubbles/bubble_pee_stream") },
            { CustomerActionState.PantsDown, Resources.Load<Sprite>("Sprites/Bubbles/bubble_zipper_down") },
            { CustomerActionState.PantsUp, Resources.Load<Sprite>("Sprites/Bubbles/bubble_zipper_up") },
        };
        BubbleSpriteLookupF = new Dictionary<CustomerActionState, Sprite>() {
            { CustomerActionState.Wetting, Resources.Load<Sprite>("Sprites/Bubbles/bubble_pee_stream") },
            { CustomerActionState.Peeing, Resources.Load<Sprite>("Sprites/Bubbles/bubble_pee_stream") },
            { CustomerActionState.PantsDown, Resources.Load<Sprite>("Sprites/Bubbles/bubble_zipper_down") },
            { CustomerActionState.PantsUp, Resources.Load<Sprite>("Sprites/Bubbles/bubble_zipper_up") },
        };

        SpriteStoolNormal = Resources.Load<Sprite>("Sprites/Entities/Stool_normal");
        SpriteStoolWet = Resources.Load<Sprite>("Sprites/Entities/Stool_wet");

        // Are ya pissin son?
        ValidBubbleActionStatesF = BubbleSpriteLookupF.Keys.ToArray();
        ValidBubbleActionStatesM = BubbleSpriteLookupM.Keys.ToArray();

        GenderedBubbleSpriteLookup = new Dictionary<char, Dictionary<CustomerActionState, Sprite>>() {
            {'f', BubbleSpriteLookupF },
            {'m', BubbleSpriteLookupM }
        };
    }
    public enum BladderControlState {
        Normal,
        LosingControl,
        Wetting,
        Emptying,
    }
    public enum CustomerDesperationState {
        State0,  // No desperation
        State1,  // Desperation1
        State2,  // Desperation2
        State3,  // Desperation3
        State4,  // Losing Control
        State5,  // Wetting
        State6,  // Wet
    }
    public enum CustomerActionState {
        None,
        PantsDown,
        PantsUp,
        Peeing,
        WashingHands,
        Wetting
    }

    public static readonly Vector3[] NavigationKeyframesFromBarToBathroom = {
        new Vector3(80,-300,0),
        new Vector3(80,-500,0)
    };
    public static readonly Vector3[] NavigationKeyframesFromBathroomToBar = {
        new Vector3(-160,-500,0),
        new Vector3(80,-500,0),
        new Vector3(80,-300,0)
    };

    public static Sprite spriteToilet;
    public static Sprite spriteUrinal;
    public static Sprite spriteUrinalSideways;
    public static Sprite spriteSink;

    public static Sprite spriteStallClosed;
    public static Sprite spriteStallOpened;

    public static Sprite SpriteStoolNormal;
    public static Sprite SpriteStoolWet;

    public static Dictionary<char, Dictionary<CustomerActionState, Sprite>> GenderedBubbleSpriteLookup;

    public static Dictionary<CustomerActionState, Sprite> BubbleSpriteLookupF;
    public static Dictionary<CustomerActionState, Sprite> BubbleSpriteLookupM;

    public static CustomerActionState[] ValidBubbleActionStatesF;
    public static CustomerActionState[] ValidBubbleActionStatesM;

    public enum Location {
        Outside,
        Bar,
        Doorway,
        WaitingRoom,
        Relief
    }

    static readonly public Vector3 OffScreenTop = new Vector3() {
        x = 600,
        y = 750,
        z = 0
    };

    public static Sprite GetBubbleSprite(Customer customer) {
        CustomerActionState[] ValidBubbleActionStates = customer.Gender == 'm' ? ValidBubbleActionStatesM : ValidBubbleActionStatesF;
        if (! ValidBubbleActionStates.Contains(customer.ActionState) ) {
            return null;
        }

        // Bubble displays early if they are about to wet themselves
        if (customer.DesperationState == CustomerDesperationState.State5 || customer.AtDestination()) {
            return GenderedBubbleSpriteLookup[customer.Gender][customer.ActionState];
        }

        return null;
    }
}
