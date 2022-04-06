using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {
    [Serializable]
    public class Line {
        [SerializeField]
        public List<WaitingSpot> Items = new List<WaitingSpot>();

        public void Update() {
            AdvanceQueue();
        }

        public int UID => _uid;
        private readonly int _uid = GameController.GetUid();

        public bool HasAnyoneInLine() {
            return Items.Any(x => x.OccupiedBy != null);
        }
        public bool IsNextInLine(Customer customer) {
            for ( int i = 0; i < Items.Count(); i++ ) {
                if ( Items[i].OccupiedBy == customer ) {
                    return true;
                }
                else if ( Items[i].OccupiedBy == null || Items[i].OccupiedBy.IsWetting || Items[i].OccupiedBy.IsWet ) {
                    continue;
                }
                else {
                    return false;
                }
            }
            return Items[0].OccupiedBy == customer;
        }
        public Customer GetNextInLine() {
            for ( int i = 0; i < Items.Count(); i++ ) {
                var item = Items[i];
                if ( item.OccupiedBy != null && !item.OccupiedBy.IsWetting && !item.OccupiedBy.IsWet ) {
                    return Items[i].OccupiedBy;
                }
            }
            return null;
        }

        public bool HasOpenWaitingSpot() {
            return Items.Where(x => x.OccupiedBy == null).Any();
        }

        public WaitingSpot GetNextWaitingSpot() {
            for ( int i = 0; i < Items.Count; i++ ) {
                WaitingSpot current = Items[i];
                if ( current.OccupiedBy == null ) {
                    return current;
                }
            }
            Debug.LogError("Used GetNextWaitingSpot without checking if any spots exist. Dumbass!");
            return null;
        }

        public void AdvanceQueue() {
            for ( int i = 1; i < Items.Count(); i++ ) {
                // If spot ahead is empty
                if ( Items[i - 1].OccupiedBy == null ) {
                    // If current spot is empty
                    if ( Items[i].OccupiedBy == null ) {
                        for ( int j = i + 1; j < Items.Count(); j++ ) {
                            if ( Items[j].OccupiedBy != null ) {
                                // Move customer in spot j to spot i - 1
                                Items[j].OccupiedBy.Occupy(Items[i - 1]);
                                Items[j].OccupiedBy = null;
                                // And break loop j ffs
                                break;
                            }
                        }
                    }
                    else {
                        // No move if customer is piss pant)
                        if ( Items[i].OccupiedBy.IsWet ) {
                            continue;
                        }

                        // Move customer in spot i to spot i - 1
                        Items[i].OccupiedBy.Occupy(Items[i - 1]);
                        Items[i].OccupiedBy = null;
                    }
                }
            }
        }
    }
}
