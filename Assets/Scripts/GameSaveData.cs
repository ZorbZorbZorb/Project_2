using Assets.Scripts.Areas;
using Assets.Scripts.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class GameSaveData {
        [Serializable]
        public class Option {
            public double X;
            public double Y;
            public List<InteractableType> Options;
            public InteractableType Current;
            public Orientation Facing;
            public int Cost;
        }
        [Serializable]
        public class BarOption : Option {
            public bool isTable;
        }

        [HideInInspector]
        public List<Option> Mens;
        [HideInInspector]
        public List<Option> Womens;
        [HideInInspector]
        public List<BarOption> bar;

        public int Night;
        public double Funds;

        public void Apply() {

            List<CustomerInteractable> instances;
            Bathroom bathroom;

            // Set up BathroomM
            bathroom = Bathroom.BathroomM;
            instances = ApplyToArea(bathroom, Mens);
            instances.ForEach(x => bathroom.AddInteractable(x));
            // Add waiting spots
            AddSpot((3d, 1d), bathroom, WaitingSpotType.Bathroom);
            AddSpot((2d, 1d), bathroom, WaitingSpotType.Bathroom);
            // Add line spots
            AddSpot((2.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((1.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((0.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((-0.75d, -0.5d), bathroom, WaitingSpotType.Line);
            // Add sink spot
            AddSpot((1d, 2.5d), bathroom, WaitingSpotType.Sink);

            // Set up BathroomF
            bathroom = Bathroom.BathroomF;
            instances = ApplyToArea(bathroom, Womens);
            instances.ForEach(x => bathroom.AddInteractable(x));
            // Add waiting spots
            AddSpot((3d, 1d), bathroom, WaitingSpotType.Bathroom);
            AddSpot((2d, 1d), bathroom, WaitingSpotType.Bathroom);
            // Add line spots
            AddSpot((2.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((1.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((0.25d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((-0.75d, -0.5d), bathroom, WaitingSpotType.Line);
            // Add sink spot
            AddSpot((1d, 2.5d), bathroom, WaitingSpotType.Sink);

            // Set up bar
            ApplyToArea(Bar.Singleton, bar);

            void AddSpot((double, double) position, Bathroom bathroom, WaitingSpotType type) {
                Vector2 vector = bathroom.Area.GetGridPosition(position);
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
            return JsonConvert.DeserializeObject<GameSaveData>(json);
        }
        static private List<CustomerInteractable> ApplyToArea(Bathroom bathroom, List<Option> options) {
            List<CustomerInteractable> results = new List<CustomerInteractable>();
            foreach ( Option option in options ) {
                Area2D area = bathroom.Area;
                Vector2 vector = area.GetGridPosition(option);
                CustomerInteractable prefab;
                if ( option.Current == InteractableType.None ) {
                    continue;
                }
                switch ( option.Current ) {
                    case InteractableType.Sink:
                        prefab = Prefabs.PrefabSink;
                        break;
                    case InteractableType.Toilet:
                        prefab = Prefabs.PrefabToilet;
                        break;
                    case InteractableType.Urinal:
                        prefab = Prefabs.PrefabUrinal;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                CustomerInteractable instance = UnityEngine.Object.Instantiate(prefab, vector, Quaternion.identity);
                instance.Facing = option.Facing;
                instance.Location = bathroom.Location;
                results.Add(instance);
            }
            return results;
        }
        static private void ApplyToArea(Bar bar, List<BarOption> options) {
            // Seats handle their own setup code in Seat.Start()
            foreach ( var option in options ) {
                Area2D area = bar.Area;
                Vector2 vector = area.GetGridPosition(option);
                if ( option.Current == InteractableType.None ) {
                    continue;
                }
                switch ( option.Current ) {
                    case InteractableType.Seat:
                        if ( option.isTable ) {
                            BarTable prefab = Prefabs.PrefabTable;
                            BarTable instance = UnityEngine.Object.Instantiate(prefab, vector, Quaternion.identity);
                            foreach ( Seat seat in instance.Seats ) {
                                seat.Facing = option.Facing;
                                seat.Location = Location.Bar;
                                seat.SeatType = SeatType.Table;
                            }
                        }
                        else {
                            Seat prefab = Prefabs.PrefabSeat;
                            Seat instance = UnityEngine.Object.Instantiate(prefab, vector, Quaternion.identity);
                            instance.Facing = option.Facing;
                            instance.Location = Location.Bar;
                            instance.SeatType = SeatType.Counter;
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
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
