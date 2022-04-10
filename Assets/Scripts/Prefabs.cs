using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts {
    static public class Prefabs {
        static public Customer PrefabCustomer { get; } = Resources.Load<Customer>("Prefabs/Customer");
        static public WaitingSpot PrefabSpot { get; } = Resources.Load<WaitingSpot>("Prefabs/Spot");
        static public Toilet PrefabToilet { get; } = Resources.Load<Toilet>("Prefabs/Toilet");
        static public Urinal PrefabUrinal { get; } = Resources.Load<Urinal>("Prefabs/Urinal");
        static public Sink PrefabSink { get; } = Resources.Load<Sink>("Prefabs/Sink");
        static public Seat PrefabSeat { get; } = Resources.Load<Seat>("Prefabs/Seat");
        static public BarTable PrefabTable { get; } = Resources.Load<BarTable>("Prefabs/Table");
        static public BuildClickable PrefabClickable { get; } = Resources.Load<BuildClickable>("Prefabs/Clickable");
    }
}
