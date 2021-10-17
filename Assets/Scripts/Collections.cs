using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static Assets.Scripts.Objects.CustomerInteractable;

public class Collections : MonoBehaviour {
    static public Collections collections = null;

    private void Awake() {
        collections = this;

        Emote.PeeStrong =       new Emote("Sprites/Bubbles/Stream_3");
        Emote.PeeMedium =       new Emote("Sprites/Bubbles/Stream_2");
        Emote.PeeWeak =         new Emote("Sprites/Bubbles/Stream_1");
        Emote.PantsDown =       new Emote("Sprites/Bubbles/bubble_zipper_down");
        Emote.PantsUp =         new Emote("Sprites/Bubbles/bubble_zipper_up");
        Emote.StruggleStop =    new Emote("Sprites/Bubbles/bubble_struggle_stop");
        Emote.PeeStreamEmotes = new Emote[] { Emote.PeeWeak, Emote.PeeWeak, Emote.PeeWeak, Emote.PeeMedium, Emote.PeeMedium, Emote.PeeStrong, Emote.PeeStrong };
        // maybe add a null to the start so it looks like they took a second between peeing and pants up?

        spriteStallClosed = Resources.Load<Sprite>("Sprites/Entities/Stall_closed");
        spriteStallOpened = Resources.Load<Sprite>("Sprites/Entities/Stall_opened");

        DesperationSpriteLookupF = new Dictionary<CustomerDesperationState, Sprite>() {
            { CustomerDesperationState.State0, Resources.Load<Sprite>("Sprites/People/f/desp_state_0") },
            { CustomerDesperationState.State1, Resources.Load<Sprite>("Sprites/People/f/desp_state_1") },
            { CustomerDesperationState.State2, Resources.Load<Sprite>("Sprites/People/f/desp_state_2") },
            { CustomerDesperationState.State3, Resources.Load<Sprite>("Sprites/People/f/desp_state_3") },
            { CustomerDesperationState.State4, Resources.Load<Sprite>("Sprites/People/f/desp_state_4") },
            { CustomerDesperationState.State5, Resources.Load<Sprite>("Sprites/People/f/desp_state_5") },
            { CustomerDesperationState.State6, Resources.Load<Sprite>("Sprites/People/f/desp_state_6") }
        };
        DesperationSpriteLookupM = new Dictionary<CustomerDesperationState, Sprite>() {
            { CustomerDesperationState.State0, Resources.Load<Sprite>("Sprites/People/m/desp_state_0") },
            { CustomerDesperationState.State1, Resources.Load<Sprite>("Sprites/People/m/desp_state_1") },
            { CustomerDesperationState.State2, Resources.Load<Sprite>("Sprites/People/m/desp_state_2") },
            { CustomerDesperationState.State3, Resources.Load<Sprite>("Sprites/People/m/desp_state_3") },
            { CustomerDesperationState.State4, Resources.Load<Sprite>("Sprites/People/m/desp_state_4") },
            { CustomerDesperationState.State5, Resources.Load<Sprite>("Sprites/People/m/desp_state_5") },
            { CustomerDesperationState.State6, Resources.Load<Sprite>("Sprites/People/m/desp_state_6") }
        };

        DesperationSeatSpriteLookupF = new Dictionary<CustomerDesperationState, Sprite>() {
            { CustomerDesperationState.State0, Resources.Load<Sprite>("Sprites/People/Bar/f/DespStateStool_0") },
            { CustomerDesperationState.State1, Resources.Load<Sprite>("Sprites/People/Bar/f/DespStateStool_1") },
            { CustomerDesperationState.State2, Resources.Load<Sprite>("Sprites/People/Bar/f/DespStateStool_2") },
            { CustomerDesperationState.State3, Resources.Load<Sprite>("Sprites/People/Bar/f/DespStateStool_3") },
            { CustomerDesperationState.State4, Resources.Load<Sprite>("Sprites/People/Bar/f/DespStateStool_4") },
            { CustomerDesperationState.State5, Resources.Load<Sprite>("Sprites/People/Bar/f/DespStateStool_5") },
            { CustomerDesperationState.State6, Resources.Load<Sprite>("Sprites/People/Bar/f/DespStateStool_6") }
        };
        DesperationSeatSpriteLookupM = new Dictionary<CustomerDesperationState, Sprite>() {
            { CustomerDesperationState.State0, Resources.Load<Sprite>("Sprites/People/Bar/m/DespStateStool_0") },
            { CustomerDesperationState.State1, Resources.Load<Sprite>("Sprites/People/Bar/m/DespStateStool_1") },
            { CustomerDesperationState.State2, Resources.Load<Sprite>("Sprites/People/Bar/m/DespStateStool_2") },
            { CustomerDesperationState.State3, Resources.Load<Sprite>("Sprites/People/Bar/m/DespStateStool_3") },
            { CustomerDesperationState.State4, Resources.Load<Sprite>("Sprites/People/Bar/m/DespStateStool_4") },
            { CustomerDesperationState.State5, Resources.Load<Sprite>("Sprites/People/Bar/m/DespStateStool_5") },
            { CustomerDesperationState.State6, Resources.Load<Sprite>("Sprites/People/Bar/m/DespStateStool_6") }
        };

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

        GenderedActionSpriteLookup = new Dictionary<char, Dictionary<CustomerActionState, Sprite>>() {
            {'f', ActionSpriteLookupF },
            {'m', ActionSpriteLookupM }
        };
        GenderedDesperationSpriteLookup = new Dictionary<char, Dictionary<CustomerDesperationState, Sprite>>() {
            {'f', DesperationSpriteLookupF },
            {'m', DesperationSpriteLookupM }
        };
        GenderedBubbleSpriteLookup = new Dictionary<char, Dictionary<CustomerActionState, Sprite>>() {
            {'f', BubbleSpriteLookupF },
            {'m', BubbleSpriteLookupM }
        };
        GenderedSeatDesperationSpriteLookup = new Dictionary<char, Dictionary<CustomerDesperationState, Sprite>>() {
            {'m', DesperationSeatSpriteLookupM },
            {'f', DesperationSeatSpriteLookupF }
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

    public static Sprite spriteStallClosed;
    public static Sprite spriteStallOpened;

    public static Sprite SpriteStoolNormal;
    public static Sprite SpriteStoolWet;

    public static Dictionary<char, Dictionary<CustomerActionState, Sprite>> GenderedBubbleSpriteLookup;
    public static Dictionary<char, Dictionary<CustomerActionState, Sprite>> GenderedActionSpriteLookup;
    public static Dictionary<char, Dictionary<CustomerDesperationState, Sprite>> GenderedDesperationSpriteLookup;
    public static Dictionary<char, Dictionary<CustomerDesperationState, Sprite>> GenderedSeatDesperationSpriteLookup;

    public static Dictionary<CustomerDesperationState, Sprite> DesperationSpriteLookupF;
    public static Dictionary<CustomerDesperationState, Sprite> DesperationSpriteLookupM;
    public static Dictionary<CustomerDesperationState, Sprite> DesperationSeatSpriteLookupF;
    public static Dictionary<CustomerDesperationState, Sprite> DesperationSeatSpriteLookupM;
    public static Dictionary<CustomerActionState, Sprite> ActionSpriteLookupF;
    public static Dictionary<CustomerActionState, Sprite> ActionSpriteLookupM;
    public static Dictionary<CustomerActionState, Sprite> BubbleSpriteLookupF;
    public static Dictionary<CustomerActionState, Sprite> BubbleSpriteLookupM;
    public static CustomerActionState[] ValidSpriteActionStatesF;
    public static CustomerActionState[] ValidBubbleActionStatesF;
    public static CustomerActionState[] ValidSpriteActionStatesM;
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
