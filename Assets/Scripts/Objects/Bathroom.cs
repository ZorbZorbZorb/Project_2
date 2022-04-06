using Assets.Scripts;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bathroom : MonoBehaviour {
    public static Bathroom BathroomM;
    public static Bathroom BathroomF;
    public Area2D Area;

    public List<InteractableSpawnpoint> Spawnpoints;

    public List<Toilet> Toilets;
    public List<Urinal> Urinals;
    public List<Sink> Sinks;

    public Line SinksLine;

    public DoorwayQueue doorwayQueue;
    public WaitingRoom waitingRoom;

    public Location Location;

    // Several of these are checked once per tick by each customer.
    public bool HasToiletAvailable { get; private set; }
    public bool HasUrinalAvailable { get; private set; }
    public bool HasSinkForRelief { get; private set; }
    public bool HasSinkForWash { get; private set; }
    public bool HasSinkForAny { get; private set; }

    /// <summary>
    /// Adds the provided interactable to the room and handles rigging it up for use by customers
    /// </summary>
    /// <param name="interactable">interactable to add to this room</param>
    public void AddInteractable(CustomerInteractable interactable) {
        switch ( interactable.IType ) {
            case InteractableType.Sink:
                Sinks.Add(interactable as Sink);
                break;
            case InteractableType.Toilet:
                Toilets.Add(interactable as Toilet);
                break;
            case InteractableType.Urinal:
                Urinals.Add(interactable as Urinal);
                break;
            case InteractableType.WaitingSpot:
                waitingRoom.WaitingSpots.Add(interactable as WaitingSpot);
                break;
            default:
                throw new NotImplementedException($"Type {interactable.IType} is not supported.");
        }
    }
    public void AddLineSpot(WaitingSpot spot) {
        doorwayQueue.waitingSpots.Add(spot);
    }
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
        if ( customer.Occupying.IType == InteractableType.Sink ) {
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

        switch (Location) {
            case Location.BathroomM:
                if (BathroomM != null) {
                    throw new InvalidOperationException($"BathroomM was already set. Second Bathroom trying to set.");
                }
                BathroomM = this;
                break;
            case Location.BathroomF:
                if ( BathroomF != null ) {
                    throw new InvalidOperationException($"BathroomM was already set. Second Bathroom trying to set.");
                }
                BathroomF = this;
                break;
            default:
                throw new InvalidOperationException($"Bathroom was set to unexpected location {Location}");
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
