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

        public Bladder BladderSettings = new();
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

            public float SmallHoldPower = 60f;
            public float MediumHoldPower = 90f;
            public float LargeHoldPower = 130f;
            public float MassiveHoldPower = 200f;

            // Default values for customers's bladders.
            public float DefaultFillRate = 2f;
            public float DefaultDrainRate = 25f;
            public float DefaultStartingFullness = 0.5f;
            public float DefaultHoldingPower = 130f;
            public float DefaultHoldingPowerReserveM = 15f;
            public float DefaultHoldingPowerReserveF = 10f;
            public float DefaultPinchOffTime = 2.5f;

            [JsonIgnore]
            public int ChanceTotal => ChanceSmall + ChanceMedium + ChanceLarge + ChanceMassive;
        }

        // Chances each bladder size will get up to use the restroom each second. 1/x chance each second
        public int SmallUsesBathroomStage1 = 10;
        public int SmallUsesBathroomStage2 = 3;
        public int SmallUsesBathroomStage3 = 2;
        // Medium
        public int MediumUsesBathroomStage1 = 30;
        public int MediumUsesBathroomStage2 = 10;
        public int MediumUsesBathroomStage3 = 5;
        // Large
        public int LargeUsesBathroomStage1 = 0;
        public int LargeUsesBathroomStage2 = 20;
        public int LargeUsesBathroomStage3 = 10;
        // Massive
        public int MassiveUsesBathroomStage1 = 0;
        public int MassiveUsesBathroomStage2 = 0;
        public int MassiveUsesBathroomStage3 = 20;

        // V2
        public float SmallUsesBathroom = 50f;
        public float MediumUsesBathroom = 65f;
        public float LargeUsesBathroom = 75f;
        public float MassiveUsesBathroom = 85f;

        public float PantsDownTime = 2f;
        public float PantsUpTime = 1f;

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
