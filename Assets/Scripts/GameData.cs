using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class GameData {
        // https://www.youtube.com/watch?v=XOjd_qU2Ido
        public static void Export(int saveNumber, GameData data) {
            string path = Application.persistentDataPath + $"/{saveNumber}.save";
            FileStream stream;
            try {
                stream = new FileStream(path, FileMode.OpenOrCreate);
            }
            catch {
                Debug.LogError($"Failed to open or create a file at '{path}'");
                return;
            }

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);
            stream.Close();
            Debug.Log($"Saved game data to '{path}'");
        }
        public static GameData Import(int saveNumber) {
            string path = Application.persistentDataPath + $"/{saveNumber}.save";
            FileStream stream;
            try {
                stream = new FileStream(path, FileMode.Open);
            }
            catch {
                Debug.LogError($"Failed to open game data file at '{path}'");
                return null;
            }

            BinaryFormatter formatter = new BinaryFormatter();
            try {
                GameData data = (GameData)formatter.Deserialize(stream);
                stream.Close();
                Debug.Log($"Loaded game data from '{path}'");
                return data;
            }
            catch {
                Debug.LogError($"Failed to deserialize game data from '{path}'");
                stream.Close();
                return null;
            }
        }

        public int night;
        public int wettings;
        public double funds;

        public int bathroomToilets;
        public int bathroomUrinals;
        public int bathroomSinks;

        public bool barHasBar;
        public int barStools;
        public int barSeats;

        public GameData() {
            barHasBar = true;
            barStools = 6;
            barSeats = -1;
            bathroomSinks = -1;
            bathroomToilets = -1;
            bathroomUrinals = -1;
            night = 1;
            wettings = 0;
            funds = 0d;
        }
    }
}
