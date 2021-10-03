using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bathroom : MonoBehaviour {
    public static Bathroom Singleton = null;
    public Toilet GetToilet() {
        return Toilets.Where(x => x.OccupiedBy == null).First();
    }
    public Urinal GetUrinal() {
        return Urinals.Where(x => x.OccupiedBy == null).First();
    }

    [SerializeField]
    public Sinks Sinks = new Sinks();
    
    // These three are checked once per tick by each customer. Have them as public bools that are not calculated to save ups.
    public bool HasToiletAvailable { get; private set; }
    public bool HasUrinalAvailable { get; private set; }
    public bool HasSinkForRelief { get; private set; }

    public List<Toilet> Toilets;
    public List<Urinal> Urinals;

    // Start is called before the first frame update
    void Start() {
        Singleton = this;
    }

    // Update is called once per frame
    void Update() {
        Sinks.Update();
        HasToiletAvailable = Toilets.Where(x => x.OccupiedBy == null).Any();
        HasUrinalAvailable = Urinals.Where(x => x.OccupiedBy == null).Any();
        HasSinkForRelief = Sinks.CanUseForReliefNow();
    }
}
