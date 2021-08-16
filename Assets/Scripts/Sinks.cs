using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Sinks {
    [SerializeField]
    public List<Sink> Items;
    [SerializeField]
    public List<WaitingSpot> Queue;
    public bool AnyUnoccupied() {
        return Items.Where(x => x.OccupiedBy == null).Any();
    }
    public Sink FirstUnoccupied() {
        return Items.Where(x => x.OccupiedBy == null).First();
    }
}
