using Assets.Scripts;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bathroom : MonoBehaviour {
    // I'm just going to follow K.I.S.S. and use two singletons for this.
    public bool ThisBathroomIsMensRoom = true;
    public static Bathroom BathroomM;
    public static Bathroom BathroomF;

    public Toilet PrefabToilet;
    public Urinal PrefabUrinal;
    public Sink PrefabSink;

    public List<InteractableSpawnpoint> Spawnpoints;

    public List<Toilet> Toilets;
    public List<Urinal> Urinals;
    public List<Sink> Sinks;

    public Line SinksLine;

    public DoorwayQueue DoorwayQueue;
    public WaitingRoom WaitingRoom;

    // Several of these are checked once per tick by each customer.
    public bool HasToiletAvailable { get; private set; }
    public bool HasUrinalAvailable { get; private set; }
    public bool HasSinkForRelief { get; private set; }
    public bool HasSinkForWash { get; private set; }
    public bool HasSinkForAny { get; private set; }

    public Toilet GetToilet() {
        return Toilets.First(x => x.OccupiedBy == null);
    }
    public Urinal GetUrinal() {
        return Urinals.First(x => x.OccupiedBy == null);
    }
    public Sink GetSink() {
        return Sinks.First(x => x.OccupiedBy == null);
    }
    public void EnterSinkQueue(Customer customer) {
        if ( customer.Occupying.IType == CustomerInteractable.InteractableType.Sink ) {
            Sink sink = (Sink)customer.Occupying;
            customer.UseInteractable(sink);
            sink.UseForWash(customer);
            return;
        }
        else {
            customer.Occupying.OccupiedBy = null;
            customer.Occupying = null;
        }

        if ( !SinksLine.HasAnyoneInLine() && HasSinkForAny ) {
            Sink sink = GetSink();
            sink.UseForWash(customer);
        }
        else {
            WaitingSpot spot = SinksLine.GetNextWaitingSpot();
            customer.UseInteractable(spot);
        }
    }

    void Awake() {
        DoorwayQueue.Bathroom = this;
        WaitingRoom.Bathroom = this;
        Spawnpoints.ForEach(x => x.Bathroom = this);

        // If this is the first bathroom to initialize, set both singletons to this (incase unisex)
        if ( BathroomM == null && BathroomF == null ) {
            BathroomM = this;
            BathroomF = this;
        }
        else {
            if ( ThisBathroomIsMensRoom ) {
                BathroomM = this;
            }
            else {
                BathroomF = this;
            }
        }
    }
    void Update() {

        // Update doorway queue
        DoorwayQueue.Update();

        // Dequeue sinks line into sinks
        if ( SinksLine.HasAnyoneInLine() ) {
            SinksLine.Update();
            Sink sink = Sinks.FirstOrDefault(x => x.Unoccupied);
            if ( sink != null) {
                Customer customer = SinksLine.GetNextInLine();
                sink.UseForWash(customer);
            }
        }

        // Update the turbo-state-machine 9000
        HasToiletAvailable = Toilets.Any(x => x.OccupiedBy == null);
        HasUrinalAvailable = Urinals.Any(x => x.OccupiedBy == null);
        HasSinkForRelief = !SinksLine.HasAnyoneInLine() && Sinks.Any(x => x.OccupiedBy == null);
        HasSinkForWash = HasSinkForRelief;
        HasSinkForAny = HasSinkForWash || HasSinkForRelief;

    }
}
