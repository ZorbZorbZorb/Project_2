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

        [Serializable]
        public class Bladder {
            public int ChanceSmall = 5;
            public int ChanceMedium = 20;
            public int ChanceLarge = 10;
            public int ChanceMassive = 2;
            [JsonIgnore]
            private int ChanceTotal => ChanceSmall + ChanceMedium + ChanceLarge + ChanceMassive;
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
