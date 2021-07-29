using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Emote {

    #region Static
    public static Emote PeeStrong;
    public static Emote PeeMedium;
    public static Emote PeeWeak;
    public static Emote PantsDown;
    public static Emote PantsUp;

    public static Emote[] PeeStreamEmotes = new Emote[] {PeeWeak, PeeMedium, PeeStrong};

    public static Emote GetPeeStreamEmote(double BladderPercentage) {
        // I mean, this is kinda a physical thing not a mental thing so percentage should be the best thing to use.
        return PeeStreamEmotes[(int)(Math.Ceiling(BladderPercentage * PeeStreamEmotes.Length)) - 1];
    }

    public static void LoadResources() {
        PeeStrong = new Emote("Sprites/Bubbles/Stream_3");
        PeeMedium = new Emote("Sprites/Bubbles/Stream_2");
        PeeWeak = new Emote("Sprites/Bubbles/Stream_1");
        PantsDown = new Emote("Sprites/Bubbles/bubble_zipper_down");
        PantsUp = new Emote("Sprites/Bubbles/bubble_ziper_up");
    }
    #endregion

    #region Instance
    public Sprite Sprite {
        get;
        private set;
    }

    public Emote(string spritePath) {
        Sprite = Resources.Load<Sprite>(spritePath);
    }
    #endregion
}
