using Assets.Scripts.Interfaces;
using Assets.Scripts.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            if (waitingSpots[i].OccupiedBy == customer) {
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
        for ( int i = 1; i < waitingSpots.Count(); i++ ) {
            // If spot ahead is empty
            if ( waitingSpots[i - 1].OccupiedBy == null ) {
                // If current spot is empty
                if ( waitingSpots[i].OccupiedBy == null ) {
                    for ( int j = i + 1; j < waitingSpots.Count(); j++ ) {
                        if ( waitingSpots[j].OccupiedBy != null ) {
                            // Move customer in spot j to spot i - 1
                            waitingSpots[j].OccupiedBy.Occupy(waitingSpots[i - 1]);
                            waitingSpots[j].OccupiedBy = null;
                            // And break loop j ffs
                            break;
                        }
                    }
                }
                else {
                    // No move if customer is piss pant)
                    if ( waitingSpots[i].OccupiedBy.IsWet ) {
                        continue;
                    }

                    // Move customer in spot i to spot i - 1
                    waitingSpots[i].OccupiedBy.Occupy(waitingSpots[i - 1]);
                    waitingSpots[i].OccupiedBy = null;
                }
            }
        }
    }
}
