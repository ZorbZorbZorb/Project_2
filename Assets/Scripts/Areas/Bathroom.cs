using Assets.Scripts.Customers;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Areas {
    public class Bathroom : Area {
        public static Bathroom BathroomM;
        public static Bathroom BathroomF;

        public List<Toilet> Toilets;
        public List<Urinal> Urinals;
        public List<Sink> Sinks;

        // Change to single waiting spot, shared among all sinks
        public Line SinksLine;

        public WaitingRoom waitingRoom;
        public DoorwayQueue Line;
        public int CustomersInLine => Line.CustomerCount;
        public int CustomersWaiting => Line.CustomerCount + waitingRoom.CustomerCount;
        public bool LineIsFull => !Line.HasOpenWaitingSpot();
        public bool LineIsEmpty => Line.CustomerCount == 0;

        // Several of these are checked once per tick by each customer.
        public bool HasToiletAvailable { get; private set; }
        public bool HasUrinalAvailable { get; private set; }
        public bool HasSinkForRelief { get; private set; }
        public bool HasSinkForWash { get; private set; }
        public bool HasSinkForAny { get; private set; }
        public bool HasWaitingSpot { get; private set; }

        /// <summary>
        /// Adds the provided interactable to the room and handles rigging it up for use by customers
        /// </summary>
        /// <param name="interactable">interactable to add to this room</param>
        public override void AddInteractable(CustomerInteractable interactable) {
            interactable.Location = Location;
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
                    WaitingSpot spot = interactable as WaitingSpot;
                    spot.Bathroom = this;
                    waitingRoom.WaitingSpots.Add(spot);
                    break;
                default:
                    throw new NotImplementedException($"Type {interactable.IType} is not supported.");
            }
        }
        public void AddLineSpot(WaitingSpot spot) {
            spot.Bathroom = this;
            spot.Location = Location.Hallway;
            Line.waitingSpots.Add(spot);

            if (Line.PhantomEntrySpot != null) {
                Line.PhantomEntrySpot.transform.position = spot.transform.position;
            }
            else {
                // Set the lines phantom waiting spot
                Line.PhantomEntrySpot = Instantiate(Prefabs.PrefabSpot);
                Line.PhantomEntrySpot.MainSRenderer.enabled = false;
                Line.PhantomEntrySpot.Location = Location.Hallway;
                Line.PhantomEntrySpot.WaitingSpotType = WaitingSpotType.LineGhost;
                Line.PhantomEntrySpot.Bathroom = this;
            }
        }
        public void AddSinkLineSpot(WaitingSpot spot) {
            spot.Bathroom = this;
            spot.Location = Location;
            spot.WaitingSpotType = WaitingSpotType.Sink;
            SinksLine.Items.Add(spot);
        }
        public Toilet GetToilet() {
            return Toilets.First(x => x.OccupiedBy == null);
        }
        public Urinal GetUrinal() {
            return Urinals.First(x => x.OccupiedBy == null);
        }
        public Sink GetSink() {
            return Sinks.FirstOrDefault(x => x.OccupiedBy == null);
        }
        public bool TryEnterQueue(Customer customer) {
            customer.TimesThoughtsAboutPeeing = 0;
            if ( !Line.HasOpenWaitingSpot() ) {
                return false;
            }
            else {
                // Hotfix for girls switching to the guys line, they go to the back of the line because its to the left
                if (customer.Location == Location.Hallway) {
                    customer.MoveTo(Line.waitingSpots[0]);
                    customer.Occupying = Line.PhantomEntrySpot;
                    Line.CustomersEnteringQueue.Add(customer);
                    return true;
                }
                else {
                    customer.Occupy(Line.PhantomEntrySpot);
                    Line.CustomersEnteringQueue.Add(customer);
                    return true;
                }
            }
        }
        public new void Awake() {
            base.Awake();
            // Set singletons and references
            switch ( Location ) {
                case Location.BathroomM:
                    if ( BathroomM != null ) {
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
            Line.Bathroom = this;
            waitingRoom.Bathroom = this;
            Line.waitingSpots.ForEach(x => x.Bathroom = this);
            waitingRoom.WaitingSpots.ForEach(x => x.Bathroom = this);
            SinksLine.Items.ForEach(x => x.Bathroom = this);
        }
        void Update() {
            // Update doorway queue
            Line.Update();

            // Dequeue sinks line into sinks
            if ( SinksLine.HasAnyoneInLine() ) {
                SinksLine.Update();
                Sink sink = Sinks.FirstOrDefault(x => x.Unoccupied);
                if ( sink != null ) {
                    sink.UseForWash(SinksLine.GetNextInLine());
                }
            }

            // Update the turbo-state-machine 9000
            UpdateAvailibility();
        }
        public void UpdateAvailibility() {
            HasToiletAvailable = Toilets.Any(x => x.OccupiedBy == null);
            HasUrinalAvailable = Urinals.Any(x => x.OccupiedBy == null);
            HasSinkForRelief = !SinksLine.HasAnyoneInLine() && Sinks.Any(x => x.OccupiedBy == null);
            HasSinkForWash = HasSinkForRelief;
            HasSinkForAny = HasSinkForWash || HasSinkForRelief;
            HasWaitingSpot = waitingRoom.HasOpenWaitingSpot();
        }
    }
}
