using Assets.Scripts.Customers;
using Assets.Scripts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts {
    /// <summary>
    /// Ai director class. Controls customer drinking, spawning, and other behaviors regarding game balance and
    ///   general player enjoyment.
    /// </summary>
    public class Director {

        private bool enabled;
        public bool Enabled => enabled;
        public int SkippedActions;
        public readonly CustomerManager CM;

        public Director( CustomerManager CM ) {
            this.CM = CM;
            enabled = false;
            SkippedActions = 0;
        }

        #region External Methods

        public void Enable() {
            enabled = true;
        }
        public void Disable() {
            enabled = false;
        }
        public void Act() {
            if ( SkippedActions < 2 ) {
                SkippedActions++;
                return;
            }
            else {
                CustomerSpawningLogic();
            }
        }

        #endregion

        #region Internal Methods

        private void CustomerSpawningLogic() {

            // Attempt to detect if the game is boring
            float[] BladdersInBar = CM.CustomersInBar.GetBladders();
            float[] BladdersInBathroom = CM.CustomersInBathroom.GetBladders();
            float[] BladdersInHallway = CM.CustomersInHallway.GetBladders();
            float AllHoldingAverageFullness = CM.AverageFullness;
            float BarHoldingAvgFullness = CM.CustomersInBar.AverageFullness();
            //Debug.Log($"AllHoldingAvgFullness: {Mathf.RoundToInt(AllHoldingAverageFullness * 100)} BarAvgFullness: {Mathf.RoundToInt(BarHoldingAvgFullness * 100)}");

            // Not a lot of people in the bar. Game may have just begun. Spawn more customers.
            if ( CM.Customers.Count() < 6 ) {
                SpawnNormally();
            }
            // Too few people in bathroom and hallway?
            else if ( CM.CountCustomersInBathroom + CM.CountCustomersInHallway <= 3 ) {
                // Not so fast buddy. These are drastic actions, 
                if ( SkippedActions < 2 ) {
                    SkippedActions++;
                    return;
                }

                // Lots of people in bar?
                TakeAction();

                SkippedActions = 0;
                return;
            }
            else {
                SpawnNormally();
            }

            void TakeAction() {
                float averageFullness = CM.Customers.AverageFullness();

                // Average fullness in bar is very low? We fucked up to get here.
                if ( averageFullness < 0.6f ) {
                    Debug.Log($"GM | Drastic | Average fullness too low. Game is VERY boring!");
                    // Make the most desperate customers get drinks and spawn some customers that are full?
                    var customers = CM.CustomersInBar
                            .ValidActionTargets()
                            .Where(x => x.Bladder.Fullness < 0.97f && x.Stomach.Fullness < 0.2f)
                            .OrderByDescending(x => x.Bladder.Fullness)
                            .Take(3);
                    MakeDrink(customers);

                    Debug.Log($"GM | Drastic | made {customers.Count()} drink");

                    // Spawn desperate customers who want drinks
                    int x = NumberOfCustomersToSpawn(CM.RemainingSpawns);
                    CM.CreateCustomers(x, 0.88f)
                        .ToList()
                        .ForEach(x => x.SetNext(2f, () => x.BuyDrink(), () => x.AtDestination));
                    Debug.Log($"GM | Drastic | spawned {x} desperate");
                    Debug.Log($"GM | Drastic | made {x} drink");
                }
                else if ( averageFullness < 0.8f ) {
                    Debug.Log($"GM | Drastic | Average fullness kinda low. Game is boring!");
                    // Make a few customers drink, and spawn a customer needing to go
                    var customers = CM.CustomersInBar
                            .ValidActionTargets()
                            .Where(x => x.Bladder.Fullness < 0.9f && x.Stomach.Fullness < 0.6f)
                            .TakeRandom(2);
                    MakeDrink(customers);
                    Debug.Log($"GM | Drastic | made {customers.Count()} drink");

                    if ( CM.RemainingSpawns > 0 ) {
                        CM.CreateCustomer(0.9f);
                        Debug.Log($"GM | Drastic | spawned {1} desperate");
                    }
                }
                else {
                    Debug.Log($"GM | Lots of customers full but nobodys in the bathroom.");
                    // Okay, Don't panic, everythings going to sort itself out in a sec. Should we be evil?
                    if ( BladdersInHallway.Length > 3 ) {
                        // No, too many about to lose it
                        if ( CM.RemainingSpawns > 0 ) {
                            int x = NumberOfCustomersToSpawn(CM.RemainingSpawns);
                            CM.CreateCustomers(x, Random.Range(0.35f, 0.45f));
                            Debug.Log($"GM | Spawned {x} low need customer(s)");
                        }
                        // No, also we cant do much anyways
                        else {
                        }
                    }
                    else {
                        // Chaos, Chaos!
                        int x = NumberOfCustomersToSpawn(CM.RemainingSpawns);
                        CM.CreateCustomers(x, 0.6f);
                        Debug.Log($"GM | spawned {1} full");
                    }
                }
            }
            void SpawnNormally() {
                if ( CM.RemainingSpawns <= 0 ) {
                    return;
                }
                // Few customers who are full.
                else if ( CM.CustomersAboveBladderFullness(0.7f) <= 3 ) {
                    for ( int i = 0; i < Math.Min(Random.Range(1, 3), CM.RemainingSpawns); i++ ) {
                        CM.CreateCustomer(Random.Range(0.83f, 0.89f));
                    }
                    SkippedActions = 0;
                }
                // Oh shit we spawned too many full customers
                else if ( CM.CustomersAboveBladderFullness(0.8f) > 4 ) {
                    if ( ShouldSpawnCustomerNow(++SkippedActions, CM.RemainingSpawns) ) {
                        // Try to spawn them so they'll be full after the player handles the current batch
                        var x = NumberOfCustomersToSpawn(CM.RemainingSpawns);
                        CM.CreateCustomers(x, 0.4f);
                        Debug.Log($"GM | spawned {x} desperate");

                        SkippedActions = 0;
                        return;
                    }
                }
                // Normal spawning behavior
                else if ( ShouldSpawnCustomerNow(++SkippedActions, CM.RemainingSpawns) ) {
                    var x = NumberOfCustomersToSpawn(CM.RemainingSpawns);
                    CM.CreateCustomers(x, 0.5f);
                    Debug.Log($"GM | spawned {x} desperate");
                    SkippedActions = 0;
                    return;
                }
                else {
                    SkippedActions++;
                }
            }

        }
        /// <summary>
        /// Makes the provided customers go buy a drink
        /// </summary>
        /// <param name="collection">Customers who will go get drinks</param>
        private void MakeDrink( IEnumerable<Customer> collection ) {
            var array = collection.ToArray();
            for ( int i = 0; i < array.Length; i++ ) {
                array[i].BuyDrink();
            }
        }
        private bool ShouldSpawnCustomerNow( int ticks, int remainingSpawns ) {
            if ( remainingSpawns > 15 ) {
                return ticks > 2 || Random.Range(0, 6 - ticks) == 0;
            }
            else if ( remainingSpawns > 10 ) {
                return ticks > 2 || Random.Range(0, 7 - ticks) == 0;
            }
            else {
                return ticks > 5 || Random.Range(0, 10 - ticks) == 0;
            }
        }
        private int NumberOfCustomersToSpawn( int remainingSpawns ) {
            if ( CM.RemainingSpawns <= 0 ) {
                return 0;
            }
            else if ( remainingSpawns > 10 ) {
                return Random.Range(1, 3);
            }
            else if ( remainingSpawns > 5 ) {
                return Random.Range(0, 5) == 0 ? 2 : 1;
            }
            else {
                return 1;
            }
        }

        #endregion

    }
}
