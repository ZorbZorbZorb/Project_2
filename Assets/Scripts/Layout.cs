using Assets.Scripts.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class Layout {
        [Serializable]
        public class BathroomOption {
            public int X;
            public int Y;
            public List<InteractableType> Options;
            public InteractableType? Current;
            public Orientation Facing;
        }
        [Serializable]
        public class BarOption {
            public int X;
            public int Y;
            public List<InteractableType> Options;
            public InteractableType? Current;
            public Orientation Facing;
            public bool isTable;
        }
        public List<BathroomOption> Mens;
        public List<BathroomOption> Womens;
        public List<BarOption> bar;

        public void Apply() {
            List<CustomerInteractable> instances;
            Bathroom bathroom;

            // Set up BathroomM
            bathroom = Bathroom.BathroomM;
            instances = ApplyToArea(bathroom, Mens);
            instances.ForEach(x => bathroom.AddInteractable(x));
            // Add waiting spots
            AddSpot((5d, 1d), bathroom, false);
            AddSpot((4d, 1d), bathroom, false);
            // Add line spots
            AddSpot((2d, -0.5d), bathroom, true);
            AddSpot((1d, -0.5d), bathroom, true);
            AddSpot((0d, -0.5d), bathroom, true);

            // Set up BathroomF
            bathroom = Bathroom.BathroomF;
            instances = ApplyToArea(bathroom, Womens);
            instances.ForEach(x => bathroom.AddInteractable(x));
            // Add waiting spots
            AddSpot((5d, 1d), bathroom, false);
            AddSpot((4d, 1d), bathroom, false);
            // Add line spots
            AddSpot((2d, -0.5d), bathroom, true);
            AddSpot((1d, -0.5d), bathroom, true);
            AddSpot((0d, -0.5d), bathroom, true);

            // Set up bar
            ApplyToArea(Bar.Singleton, bar);

            void AddSpot((double, double) position, Bathroom bathroom, bool isLine) {
                Vector2 vector = bathroom.Area.GetGridPosition(position);
                CustomerInteractable instance = UnityEngine.Object.Instantiate(Prefabs.PrefabSpot, vector, Quaternion.identity);
                instance.Facing = Orientation.South;
                if ( isLine ) {
                    bathroom.AddLineSpot(instance as WaitingSpot);
                }
                else {
                    bathroom.AddInteractable(instance);
                }
            }
        }
        static private List<CustomerInteractable> ApplyToArea(Bathroom bathroom, List<BathroomOption> options) {
            List<CustomerInteractable> results = new List<CustomerInteractable>();
            foreach ( BathroomOption option in options ) {
                Area2D area = bathroom.Area;
                Vector2 vector = area.GetGridPosition((option.X, option.Y));
                CustomerInteractable prefab;
                if ( option.Current == null ) {
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
        static private List<CustomerInteractable> ApplyToArea(Bar bar, List<BarOption> options) {
            List<CustomerInteractable> results = new List<CustomerInteractable>();
            foreach ( var option in options ) {
                Area2D area = bar.Area;
                Vector2 vector = area.GetGridPosition((option.X, option.Y));
                CustomerInteractable prefab;
                if ( option.Current == null ) {
                    continue;
                }
                switch ( option.Current ) {
                    case InteractableType.Seat:
                        prefab = Prefabs.PrefabSeat;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                CustomerInteractable instance = UnityEngine.Object.Instantiate(prefab, vector, Quaternion.identity);
                instance.Facing = option.Facing;
                instance.Location = Location.Bar;
                results.Add(instance);
            }
            return results;
        }
        public override string ToString() {

            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        static public Layout FromJson(string json) {
            return JsonConvert.DeserializeObject<Layout>(json);
        }
    }
}
