using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Areas;

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
}
