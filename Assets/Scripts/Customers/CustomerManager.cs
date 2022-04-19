using Assets.Scripts.Areas;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Customers {
    [Serializable]
    public class CustomerManager {

        public int CountSeats => Bar.Singleton.Seats.Count;
        public int CountWorkingSeats => Bar.Singleton.Seats
            .Count(x => !x.IsSoiled);
        public int CountBrokenSeats => Bar.Singleton.Seats
            .Count(x => x.IsSoiled);
        public int CountEmptyWorkingSeats => Bar.Singleton.Seats
            .Count(x => !x.IsSoiled && x.OccupiedBy == null);
        public int RemainingSpawns => CountWorkingSeats - Customers.Count(x => x.CurrentAction != CustomerAction.Leaving);
        public float AverageFullness => CustomersHolding
            .Select(x => x.Bladder.Fullness)
            .Average();
        public int CountCustomersHolding => Customers
            .Count(x => x.CurrentAction != CustomerAction.Leaving && x.CurrentAction != CustomerAction.Wetting && x.CurrentAction != CustomerAction.Peeing);
        public IEnumerable<Customer> CustomersHolding => Customers
            .Where(x => x.CurrentAction != CustomerAction.Leaving && x.CurrentAction != CustomerAction.Wetting && x.CurrentAction != CustomerAction.Peeing);
        public int CountCustomersInBathroom => Customers
            .Count(x => x.Location == Location.BathroomM || x.Location == Location.BathroomF);
        public IEnumerable<Customer> CustomersInBathroom => Customers
            .Where(x => x.Location == Location.BathroomM || x.Location == Location.BathroomF);
        public int CountCustomersInHallway => Customers
            .Count(x => x.Location == Location.Hallway);
        public IEnumerable<Customer> CustomersInHallway => Customers
            .Where(x => x.Location == Location.Hallway);
        public int CountCustomersInBar => Customers
            .Count(x => x.Location == Location.Bar || x.Location == Location.Hallway);
        public IEnumerable<Customer> CustomersInBar => Customers
            .Where(x => x.Location == Location.Bar || x.Location == Location.Hallway);

        public int CustomersBelowBladderFullness( float fullness ) {
            return Customers.Count(x => x.Bladder.Fullness <= fullness);
        }
        public int CustomersAboveBladderFullness( float fullness ) {
            return Customers.Count(x => x.Bladder.Fullness >= fullness);
        }

        public List<Customer> Customers = new();
        public GameObject CustomersHolder;

        public Customer CreateCustomer( float fullness ) {
            Customer newCustomer = UnityEngine.Object.Instantiate(Prefabs.PrefabCustomer, Navigation.CustomerSpawnpoint, Quaternion.identity);
            newCustomer.transform.SetParent(CustomersHolder.transform, true);
            newCustomer.Location = Location.Outside;
            newCustomer.Gender = Random.Range(0, 2) == 0 ? 'm' : 'f';
            Customers.Add(newCustomer);
            newCustomer.SetupCustomer(fullness);
            //Debug.Log($"Created | state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}", newCustomer);
            newCustomer.Active = true;
            bool enteredDoorway = false;

            // Enter bathroom or bar on spawn
            Bathroom bathroom = newCustomer.GetBathroomWillingToEnter();
            if ( bathroom != null ) {
                enteredDoorway = newCustomer.GetInLine(bathroom);
            }
            if ( !enteredDoorway ) {
                Seat seat = Bar.Singleton.GetRandomOpenSeat();
                newCustomer.Occupy(seat);
            }

            return newCustomer;
        }
        public Customer[] CreateCustomers(int numberToCreate, float fullness) {
            Customer[] created = new Customer[numberToCreate];
            for ( int i = 0; i < numberToCreate; i++ ) {
                created[i] = CreateCustomer(fullness);
            }
            return created;
        }

        public void RemoveCustomer( Customer customer ) {
            customer.StopOccupyingAll();
            Customers.Remove(customer);
            UnityEngine.Object.Destroy(customer.gameObject);
        }
    }
}
