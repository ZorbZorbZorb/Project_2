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

    public DoorwayQueue doorwayQueue;
    public WaitingRoom waitingRoom;

    public List<Vector3> NavigationKeyframesFromBarToBathroom;
    public List<Vector3> NavigationKeyframesFromBathroomToBar;
    public List<NavigationKeyframe> navigationKeyframesFromBarToBathroom;
    public List<NavigationKeyframe> navigationKeyframesFromBathroomToBar;

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
        // Get the transform position for the navigation keyframes
        navigationKeyframesFromBarToBathroom.ForEach(x => NavigationKeyframesFromBarToBathroom.Add(x.transform.position));
        navigationKeyframesFromBathroomToBar.ForEach(x => NavigationKeyframesFromBathroomToBar.Add(x.transform.position));

        // Set doorway queue, waiting room, spawnpoint, and waiting spots bathroom ref
        doorwayQueue.Bathroom = this;
        waitingRoom.Bathroom = this;
        doorwayQueue.waitingSpots.ForEach(x => x.SpotBathroom = this);
        waitingRoom.WaitingSpots.ForEach(x => x.SpotBathroom = this);
        SinksLine.Items.ForEach(x => x.SpotBathroom = this);
        // I've messed this up in the past, so drop an error in the console if I forget to change the list size for spawnpoints
        if (Spawnpoints.Any(x => x == null)) {
            Debug.LogError("Spawnpoint was null. Is list size too large or was spawnpoint deleted?");
        }
        Spawnpoints.Where(x => x != null)
            .ToList()
            .ForEach(x => x.Bathroom = this);

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
        doorwayQueue.Update();

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
