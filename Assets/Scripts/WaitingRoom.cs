using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Interfaces;
using Assets.Scripts.Objects;

public class WaitingRoom : MonoBehaviour
{
    public static WaitingRoom waitingRoom = null;
    public List<WaitingSpot> waitingSpots = new List<WaitingSpot>();

    // Start is called before the first frame update
    void Start() {
        if (waitingRoom == null) {
            waitingRoom = this;
        }
        else {
            throw new Exception("Only one waiting room can exist");
        }
    }

    // Update is called once per frame
    void Update() {
    }

    public int UID => _uid;
    private readonly int _uid = GameController.GetUid();

    public bool HasOpenWaitingSpot() {
        return waitingSpots.Where(x => x.OccupiedBy == null).Any();
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
        for ( int i = 1; i < waitingSpots.Count; i++ ) {
            WaitingSpot currentSpot = waitingSpots[i];
            WaitingSpot spotAhead = OpenPlaceAhead(currentSpot, i);
            if (spotAhead != null) {
                spotAhead.OccupiedBy = currentSpot.OccupiedBy;
                currentSpot.OccupiedBy = null;
            }
        }

        WaitingSpot OpenPlaceAhead(CustomerInteractable current, int currentIndex) {
            for ( int i = 0; i < currentIndex; i++ ) {
                WaitingSpot inspected = waitingSpots[i];
                if (inspected.OccupiedBy == null) {
                    return inspected;
                }
            }
            return null;
        }
    }
}
