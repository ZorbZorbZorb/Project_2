using Assets.Scripts.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class Layout {
        [Serializable]
        public class Section {
            public List<Option> Options;
        }
        [Serializable]
        public class Option {
            public int X;
            public int Y;
            public List<InteractableType> Options;
            public InteractableType? Current;
            public Orientation Facing;
        }
        public Section Mens;
        public Section Womens;

        public void Apply() {
            List<CustomerInteractable> instances;
            Bathroom bathroom;

            // Set up BathroomM
            bathroom = Bathroom.BathroomM;
            instances = ApplyToArea(bathroom.BathroomMArea, Mens.Options);
            instances.ForEach(x => bathroom.AddInteractable(x));
            // Add waiting spots
            AddSpot((5d, 1d), bathroom, bathroom.BathroomMArea, false);
            AddSpot((4d, 1d), bathroom, bathroom.BathroomMArea, false);

            // Add line spots
            AddSpot((2d, -0.5d), bathroom, bathroom.BathroomMArea, true);
            AddSpot((1d, -0.5d), bathroom, bathroom.BathroomMArea, true);
            AddSpot((0d, -0.5d), bathroom, bathroom.BathroomMArea, true);

            // Set up BathroomF
            bathroom = Bathroom.BathroomF;
            instances = ApplyToArea(Bathroom.BathroomF.BathroomFArea, Womens.Options);
            instances.ForEach(x => bathroom.AddInteractable(x));
            // Add waiting spots and line spots

            void AddSpot((double, double) position, Bathroom bathroom, Area2D area, bool isLine) {
                Vector2 vector = area.GetGridPosition(position);
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
        static private List<CustomerInteractable> ApplyToArea(Area2D area, List<Option> options) {
            List<CustomerInteractable> results = new List<CustomerInteractable>();
            foreach ( Option option in options ) {
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
