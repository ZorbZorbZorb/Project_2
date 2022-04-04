using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Assets.Scripts.Objects;
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
            public Orientation Orientation;
        }
        public Section Mens;
        public Section Womens;

        public void Apply() {
            List<CustomerInteractable> instances;
            instances = ApplyToArea(Bathroom.BathroomM.BathroomMArea, Mens.Options);
            foreach ( var item in instances ) {
                Bathroom.BathroomM.AddInteractable(item);
            }
            instances = ApplyToArea(Bathroom.BathroomF.BathroomFArea, Womens.Options);
            foreach ( var item in instances ) {
                Bathroom.BathroomF.AddInteractable(item);
            }
        }
        public override string ToString() {

            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        static public Layout FromJson(string json) {
            return JsonConvert.DeserializeObject<Layout>(json);
        }
        static private List<CustomerInteractable> ApplyToArea(Area2D area, List<Option> options) {
            List<CustomerInteractable> results = new List<CustomerInteractable>();
            foreach ( Option option in options ) {
                Vector2 position = area.GetGridPosition((option.X, option.Y));
                Vector3 vector = new Vector3(position.x, position.y);
                CustomerInteractable prefab;
                if ( option.Current == null ) {
                    continue;
                }
                switch ( option.Current ) {
                    case InteractableType.Sink:
                        prefab = Bathroom.BathroomM.PrefabSink;
                        break;
                    case InteractableType.Toilet:
                        prefab = Bathroom.BathroomM.PrefabToilet;
                        break;
                    case InteractableType.Urinal:
                        prefab = Bathroom.BathroomM.PrefabUrinal;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                CustomerInteractable instance = UnityEngine.Object.Instantiate(prefab, vector, Quaternion.identity);
                instance.Orientation = option.Orientation;
                results.Add(instance);
            }
            return results;
        }
    }
}
