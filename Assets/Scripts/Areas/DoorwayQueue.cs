using Assets.Scripts.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Areas {
    [Serializable]
    public class DoorwayQueue {
        public Bathroom Bathroom;
        public List<WaitingSpot> waitingSpots = new();
        public WaitingSpot PhantomEntrySpot;
        public List<Customer> CustomersEnteringQueue = new();
        public int CustomerCount => waitingSpots.Count(x => x.Occupied) + CustomersEnteringQueue.Count();

        public void Update() {
            // Advance queue
            AdvanceQueue();
            // All canadians approaching the queue must enter the queue on arrival
            var canadians = CustomersEnteringQueue.Where(x => x.AtDestination).ToArray();
            for ( int eh = canadians.Length; eh-- > 0; ) {
                var canadian = canadians[eh];
                canadian.Occupy(GetNextWaitingSpot());
                CustomersEnteringQueue.Remove(canadian);
            }
        }
        public bool IsNextInLine(Customer customer) {
            for ( int i = 0; i < waitingSpots.Count(); i++ ) {
                if ( waitingSpots[i].OccupiedBy == customer ) {
                    return true;
                }
                else if ( waitingSpots[i].OccupiedBy == null || waitingSpots[i].OccupiedBy.IsWetting || waitingSpots[i].OccupiedBy.IsWet ) {
                    continue;
                }
                else {
                    return false;
                }
            }
            return waitingSpots[0].OccupiedBy == customer;
        }
        public bool HasOpenWaitingSpot() {
            return waitingSpots.Where(x => x.Unoccupied).Count() - CustomersEnteringQueue.Count() > 0;
        }
        public WaitingSpot GetNextWaitingSpot() {
            for ( int i = 0; i < waitingSpots.Count; i++ ) {
                WaitingSpot current = waitingSpots[i];
                if ( current.OccupiedBy == null ) {
                    return current;
                }
            }
            return null;
        }
        public void AdvanceQueue() {
            // Try to find that nasty bug where people can occupy two spots at once
            for ( int i = 0; i < waitingSpots.Count; i++ ) {
                if (waitingSpots[i].Occupied && waitingSpots[i].OccupiedBy.Occupying != waitingSpots[i]) {
                    Debug.LogError("Spot occuptation reference is incorrect!");
                    waitingSpots[i].OccupiedBy = null;
                }
            }
            // Move everyone up to the lowest place in queue
            for ( int i = 0; i++ < waitingSpots.Count - 1; ) {
                var occupant = waitingSpots[i].OccupiedBy;
                if ( occupant != null && occupant.AtDestination && !occupant.IsWet ) {
                    WaitingSpot lowest = lowestEmpty(i);
                    if ( lowest != null ) {
                        occupant.Occupy(lowest);
                    }
                }
            }
            //Returns the lowest (closest to front of line) empty (unoccupied) spot
            WaitingSpot lowestEmpty(int end) {
                for ( int i = 0; i < end; i++ ) {
                    var occupant = waitingSpots[i].OccupiedBy;
                    if ( occupant == null ) {
                        return waitingSpots[i];
                    }
                }
                return null;
            }
        }
    }
}
