using Assets.Scripts.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Areas {
    [Serializable]
    public class DoorwayQueue {
        public Bathroom Bathroom;
        public List<WaitingSpot> waitingSpots = new List<WaitingSpot>();

        public void Update() {
            // Advance queue
            AdvanceQueue();
        }

        public int UID => _uid;
        private readonly int _uid = GameController.GetUid();


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
            return waitingSpots.Any(x => x.Unoccupied);
        }

        public WaitingSpot GetNextWaitingSpot() {
            for ( int i = 0; i < waitingSpots.Count; i++ ) {
                WaitingSpot current = waitingSpots[i];
                if ( current.OccupiedBy == null ) {
                    return current;
                }
            }
            Debug.LogError("Used GetNextWaitingSpot without checking if any spots exist. Dumbass!");
            return null;
        }

        public void AdvanceQueue() {
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
