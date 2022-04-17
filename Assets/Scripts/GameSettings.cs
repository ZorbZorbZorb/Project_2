using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Assets.Scripts {
    /// <summary>
    /// GameSettings's purpose is to store values that affect the games balance.
    /// <para>Game graphics settings or layout/save data should not be stored here</para>
    /// </summary>
    [Serializable]
    public class GameSettings{
        public static GameSettings Current = new();

        public const string GAME_SETTINGS_PATH = @"Configs\gameSettings";

        public Bladder BladderSettings = new Bladder();
        [Serializable]
        public class Bladder {
            // This is the weighted distribution table for spawning customers. Think about it as if it was a bag 
            //   of marbles. The bag is filled with x of each marble. You reach in and choose a marble at random.
            public int ChanceSmall = 2;
            public int ChanceMedium = 10;
            public int ChanceLarge = 6;
            public int ChanceMassive = 1;

            // These sizes are the minimuim and maximum values for each bladder size
            public float SizeMinSmall = 500f;
            public float SizeMaxSmall = 600f;
            public float SizeMinMedium = 750f;
            public float SizeMaxMedium = 950f;
            public float SizeMinLarge = 1100f;
            public float SizeMaxLarge = 1300f;
            public float SizeMinMassive = 1500f;
            public float SizeMaxMassive = 1900f;

            // Default values for customers's bladders.
            public float DefaultControlRemaining = 130f;
            public float DefaultFillRate = 1f;
            public float DefaultDrainRate = 30f;
            public float DefaultLossOfControlTimeM = 16f;
            public float DefaultLossOfControlTimeF = 8f;
            public float DefaultStartingFullness = 0.5f;

            [JsonIgnore]
            public int ChanceTotal => ChanceSmall + ChanceMedium + ChanceLarge + ChanceMassive;
        }

        public static GameSettings Load(string path) {
            string json = Resources.Load<TextAsset>(path).text;
            try {
                return JsonConvert.DeserializeObject<GameSettings>(json);
            }
            catch (Exception e) {
                Debug.LogError("Error parsing JSON");
                throw e;
            }
        }

    }
}
