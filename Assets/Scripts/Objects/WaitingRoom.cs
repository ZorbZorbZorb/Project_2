using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Interfaces;
using Assets.Scripts.Objects;

[Serializable]
public class WaitingRoom {
    public Bathroom Bathroom;
    public List<WaitingSpot> WaitingSpots = new List<WaitingSpot>();
    public int UID => _uid;
    private readonly int _uid = GameController.GetUid();
    public bool HasOpenWaitingSpot() {
        return WaitingSpots.Any(x => x.OccupiedBy == null);
    }
    public WaitingSpot GetNextWaitingSpot() {
        for ( int i = 0; i < WaitingSpots.Count; i++ ) {
            WaitingSpot current = WaitingSpots[i];
            if ( current.OccupiedBy == null ) {
                return current;
            }
        }
        Debug.LogError("Used GetNextWaitingSpot without checking if any spots exist. Dumbass!");
        return null;
    }
    //public void AdvanceQueue() {
    //    for ( int i = 1; i < waitingSpots.Count; i++ ) {
    //        WaitingSpot currentSpot = waitingSpots[i];
    //        WaitingSpot spotAhead = OpenPlaceAhead(currentSpot, i);
    //        if (spotAhead != null) {
    //            spotAhead.OccupiedBy = currentSpot.OccupiedBy;
    //            currentSpot.OccupiedBy = null;
    //        }
    //    }
    //    WaitingSpot OpenPlaceAhead(CustomerInteractable current, int currentIndex) {
    //        for ( int i = 0; i < currentIndex; i++ ) {
    //            WaitingSpot inspected = waitingSpots[i];
    //            if (inspected.OccupiedBy == null) {
    //                return inspected;
    //            }
    //        }
    //        return null;
    //    }
    //}
}
