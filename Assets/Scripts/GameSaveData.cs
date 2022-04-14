using Assets.Scripts.Areas;
using Assets.Scripts.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public partial class GameSaveData {

        [HideInInspector]
        public List<LayoutSpot> Mens;
        [HideInInspector]
        public List<LayoutSpot> Womens;
        [HideInInspector]
        public List<LayoutSpot> bar;

        [JsonIgnore]
        public IEnumerable<LayoutSpot> All => Mens.Union(Womens).Union(bar);

        public int Night;
        public double Funds;

        public void Apply() {
            Bathroom bathroom;

            // Set up BathroomM
            bathroom = Bathroom.BathroomM;
            ApplyToArea(bathroom, Mens);
            // Add waiting spots
            AddSpot((3d, 1d), bathroom, WaitingSpotType.Bathroom);
            AddSpot((2d, 1d), bathroom, WaitingSpotType.Bathroom);
            // Add line spots
            AddSpot((2.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((1.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((0.25d, -0.5d), bathroom, WaitingSpotType.Line);
            //AddSpot((-0.75d, -0.5d), bathroom, WaitingSpotType.Line);
            // Add sink spot
            AddSpot((1d, 2.5d), bathroom, WaitingSpotType.Sink);

            // Set up BathroomF
            bathroom = Bathroom.BathroomF;
            ApplyToArea(bathroom, Womens);
            // Add waiting spots
            AddSpot((3d, 1d), bathroom, WaitingSpotType.Bathroom);
            AddSpot((2d, 1d), bathroom, WaitingSpotType.Bathroom);
            // Add line spots
            AddSpot((2.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((1.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((0.25d, -0.5d), bathroom, WaitingSpotType.Line);
            //AddSpot((-0.75d, -0.5d), bathroom, WaitingSpotType.Line);
            // Add sink spot
            AddSpot((1d, 2.5d), bathroom, WaitingSpotType.Sink);

            // Set up bar
            ApplyToArea(Bar.Singleton, bar);

            void AddSpot((double, double) position, Bathroom bathroom, WaitingSpotType type) {
                Vector2 vector = bathroom.Bounds.GetGridPosition(position);
                CustomerInteractable instance = UnityEngine.Object.Instantiate(Prefabs.PrefabSpot, vector, Quaternion.identity);
                instance.Facing = Orientation.South;
                switch ( type ) {
                    case WaitingSpotType.Line:
                        bathroom.AddLineSpot(instance as WaitingSpot);
                        break;
                    case WaitingSpotType.Bathroom:
                        bathroom.AddInteractable(instance);
                        break;
                    case WaitingSpotType.Sink:
                        bathroom.AddSinkLineSpot(instance as WaitingSpot);
                        break;
                    default:
                        throw new NotImplementedException("Waiting spot not initialized correctly");
                }
            }
        }
        static public GameSaveData FromJson(string json) {
            var result = JsonConvert.DeserializeObject<GameSaveData>(json);

            // Set layout spot references
            foreach ( LayoutSpot spot in result.All ) {
                foreach ( LayoutOption option in spot.Options) {
                    option.LayoutSpot = spot;
                }
            }

            return result;
        }
        /// <summary>
        /// Called once per save data load to apply the existing layout to the scene.
        /// </summary>
        /// <typeparam name="T">Area type. Should be implicit.</typeparam>
        /// <param name="area">Area to apply these layout options to</param>
        /// <param name="spots">Layout spots to apply</param>
        static private void ApplyToArea(Area area, List<LayoutSpot> spots) {
            foreach ( LayoutSpot option in spots ) {
                // Set the options area reference
                option.Area = area;

                if ( option.Current != InteractableType.None ) {
                    SpawnInteractable(option, option.Current);
                }
            }
        }
        public static CustomerInteractable SpawnInteractable(LayoutSpot spot, InteractableType type) {
            spot.Current = type;
            CustomerInteractable prefab = Prefabs.InteractablePrefabs[type];
            Vector2 vector = spot.Area.Bounds.GetGridPosition(spot);
            CustomerInteractable instance = UnityEngine.Object.Instantiate(prefab, vector, Quaternion.identity);
            instance.Facing = spot.Facing;
            instance.Location = spot.Area.Location;
            spot.Area.AddInteractable(instance);
            return instance;
        }
        public override string ToString() {

            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        private static string GetSavePath(int slotNumber) => Path.Combine(Application.persistentDataPath, $"/{slotNumber}.json");
        public static bool Exists(int slotNumber) => File.Exists(GetSavePath(slotNumber));
        public static GameSaveData ImportDefault() {
            string json = Resources.Load<TextAsset>(@"Configs\layoutDefault").text;
            return FromJson(json);
        }
        public static GameSaveData Import(int slotNumber) {
            string path = GetSavePath(slotNumber);
            FileStream fs = null;
            StreamReader reader = null;
            string json;
            try {
                using ( fs = new FileStream(path, FileMode.OpenOrCreate) ) {
                    using ( reader = new StreamReader(fs) ) {
                        json = reader.ReadToEnd();
                    }
                }
            }
            catch ( IOException e ) {
                Debug.LogError($"Save import failed for path '{path}'");
                throw e;
            }
            finally {
                reader?.Close();
                fs?.Close();
            }

            try {
                GameSaveData layout = FromJson(json);
                Debug.Log($"Save imported from slot {slotNumber}");
                return layout;
            }
            catch ( Exception e ) {
                Debug.LogError($"Save import failed for slot {slotNumber}.\r\nSave json may be corrupt.\r\nError: {e.Message}");
                throw e;
            }
        }
        public void Export(int slotNumber) {
            string path = GetSavePath(slotNumber);
            FileStream fs = null;
            StreamWriter writer = null;
            string json = ToString();
            try {
                File.WriteAllText(path, json);
                using ( fs = new FileStream(path, FileMode.Create) ) {
                    using ( writer = new StreamWriter(fs) ) {
                        writer.Write(json);
                    }
                }
                Debug.Log($"Save exported to slot {slotNumber}");
            }
            catch ( IOException e ) {
                Debug.LogError($"Save export failed for path '{path}'");
                throw e;
            }
            finally {
                writer?.Close();
                fs?.Close();
            }
        }
    }
}
