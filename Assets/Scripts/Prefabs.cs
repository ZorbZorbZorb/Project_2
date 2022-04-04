using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts {
    static public class Prefabs {
        static public Customer PrefabCustomer { get; private set; }
        static public WaitingSpot PrefabSpot { get; private set; }
        static public Toilet PrefabToilet { get; private set; }
        static public Urinal PrefabUrinal { get; private set; }
        static public Sink  PrefabSink { get; private set; }
        static public Seat PrefabSeat { get; private set; }
        static public BarTable PrefabTable { get; private set; }
        static public BuildClickable PrefabClickable { get; private set; }
        static public void Load() {
            PrefabCustomer = Resources.Load<Customer>("Prefabs/Customer");
            PrefabSpot = Resources.Load<WaitingSpot>("Prefabs/Spot");
            PrefabToilet = Resources.Load<Toilet>("Prefabs/Toilet");
            PrefabUrinal = Resources.Load<Urinal>("Prefabs/Urinal");
            PrefabSink = Resources.Load<Sink>("Prefabs/Sink");
            PrefabSeat = Resources.Load<Seat>("Prefabs/Seat");
            PrefabTable = Resources.Load<BarTable>("Prefabs/Table");
            PrefabClickable = Resources.Load<BuildClickable>("Prefabs/Clickable");
        }
    }
}
