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
            Bathroom.BathroomM.Area.Area.enabled = true;
            Bathroom.BathroomF.Area.Area.enabled = true;
            Bar.Singleton.Area.Area.enabled = true;

            List<CustomerInteractable> instances;
            Bathroom bathroom;

            // Set up BathroomM
            bathroom = Bathroom.BathroomM;
            instances = ApplyToArea(bathroom, Mens);
            instances.ForEach(x => bathroom.AddInteractable(x));
            // Add waiting spots
            AddSpot((5d, 1d), bathroom, WaitingSpotType.Bathroom);
            AddSpot((4d, 1d), bathroom, WaitingSpotType.Bathroom);
            // Add line spots
            AddSpot((2d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((1d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((0d, -0.5d), bathroom, WaitingSpotType.Line);
            // Add sink spot
            AddSpot((1d, 3d), bathroom, WaitingSpotType.Sink);

            // Set up BathroomF
            bathroom = Bathroom.BathroomF;
            instances = ApplyToArea(bathroom, Womens);
            instances.ForEach(x => bathroom.AddInteractable(x));
            // Add waiting spots
            AddSpot((5d, 1d), bathroom, WaitingSpotType.Bathroom);
            AddSpot((4d, 1d), bathroom, WaitingSpotType.Bathroom);
            // Add line spots
            AddSpot((2d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((1d, -0.5d), bathroom, WaitingSpotType.Line);
            AddSpot((0d, -0.5d), bathroom, WaitingSpotType.Line);
            // Add sink spot
            AddSpot((1d, 3d), bathroom, WaitingSpotType.Sink);

            // Set up bar
            ApplyToArea(Bar.Singleton, bar);

            Bathroom.BathroomM.Area.Area.enabled = false;
            Bathroom.BathroomF.Area.Area.enabled = false;
            Bar.Singleton.Area.Area.enabled = false;

            void AddSpot((double, double) position, Bathroom bathroom, WaitingSpotType type) {
                Vector2 vector = bathroom.Area.GetGridPosition(position);
                CustomerInteractable instance = UnityEngine.Object.Instantiate(Prefabs.PrefabSpot, vector, Quaternion.identity);
                instance.Facing = Orientation.South;
                switch (type) {
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
        static private void ApplyToArea(Bar bar, List<BarOption> options) {
            // Seats handle their own setup code in Seat.Start()
            foreach ( var option in options ) {
                Area2D area = bar.Area;
                Vector2 vector = area.GetGridPosition((option.X, option.Y));
                if ( option.Current == null ) {
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
        static public Layout FromJson(string json) {
            return JsonConvert.DeserializeObject<Layout>(json);
        }
    }
}
