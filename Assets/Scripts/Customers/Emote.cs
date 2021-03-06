using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Customers {
    public class Emote {

        #region Static
        public static Emote PeeStrong;
        public static Emote PeeMedium;
        public static Emote PeeWeak;
        public static Emote PantsDown;
        public static Emote PantsUp;
        public static Emote StruggleStop;

        public static Emote[] PeeStreamEmotes;

        public static Emote GetPeeStreamEmote(double BladderPercentage) {
            // I mean, this is kinda a physical thing not a mental thing so percentage should be the best thing to use.
            int index = (int)( Math.Round(( BladderPercentage + 0.1d ) * PeeStreamEmotes.Length) );
            index = Math.Max(index, 0);
            index = Math.Min(index, PeeStreamEmotes.Length - 1);
            return PeeStreamEmotes[index];
        }
        #endregion

        #region Instance
        public Sprite Sprite;
        public string Path;

        public Emote(string spritePath) {
            Path = spritePath;
            Sprite = Resources.Load<Sprite>(spritePath);
            if ( Sprite == null ) {
                throw new System.IO.FileNotFoundException(spritePath);
            }
        }
        #endregion
    }
}