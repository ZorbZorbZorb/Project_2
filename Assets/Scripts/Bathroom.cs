using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bathroom : MonoBehaviour {
    public static Bathroom bathroom = null;
    public Toilet GetToilet() {
        return Toilets.Where(x => x.OccupiedBy == null).First();
    }
    public Urinal GetUrinal() {
        return Urinals.Where(x => x.OccupiedBy == null).First();
    }

    public Sinks Sinks;
    
    // These three are checked once per tick by each customer. Have them as public bools that are not calculated to save ups.
    public bool HasToiletAvailable { get; private set; }
    public bool HasUrinalAvailable { get; private set; }
    public bool HasSinkAvailable { get; private set; }

    public List<Toilet> Toilets;
    public List<Urinal> Urinals;

    // Start is called before the first frame update
    void Start()
    {
        bathroom = this;
    }

    // Update is called once per frame
    void Update() {
        HasToiletAvailable = Toilets.Where(x => x.OccupiedBy == null).Any();
        HasUrinalAvailable = Urinals.Where(x => x.OccupiedBy == null).Any();
        HasSinkAvailable = Sinks.AnyUnoccupied();
    }
}
