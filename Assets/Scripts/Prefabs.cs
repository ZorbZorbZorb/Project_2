using Assets.Scripts.Customers;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts {
    static public class Prefabs {
        static public Customer PrefabCustomer = Resources.Load<Customer>("Prefabs/Customer");
        static public WaitingSpot PrefabSpot = Resources.Load<WaitingSpot>("Prefabs/Spot");
        static public Toilet PrefabToilet = Resources.Load<Toilet>("Prefabs/Toilet");
        static public Urinal PrefabUrinal = Resources.Load<Urinal>("Prefabs/Urinal");
        static public Sink PrefabSink = Resources.Load<Sink>("Prefabs/Sink");
        static public Seat PrefabSeat = Resources.Load<Seat>("Prefabs/Seat");
        static public BarTable PrefabTable = Resources.Load<BarTable>("Prefabs/Table");
        static public BuildClickable PrefabClickable = Resources.Load<BuildClickable>("Prefabs/Clickable");
        static public BuildButton PrefabBuildButton = Resources.Load<BuildButton>("Prefabs/BuildButton");

        static public Dictionary<InteractableType, CustomerInteractable> InteractablePrefabs = 
            new Dictionary<InteractableType, CustomerInteractable>() {
                { InteractableType.Sink, PrefabSink },
                { InteractableType.Toilet, PrefabToilet },
                { InteractableType.Seat, PrefabSeat },
                { InteractableType.WaitingSpot, PrefabSpot },
                { InteractableType.Urinal, PrefabUrinal },
                { InteractableType.Table, PrefabTable }
        };
    }
}
