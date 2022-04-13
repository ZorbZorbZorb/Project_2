using Assets.Scripts.Areas;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Customers {
    [Serializable]
    public class CustomerManager {
        public int MaxCustomers;
        public List<Customer> Customers = new List<Customer>();
        public IEnumerable<Customer> CustomersInBathroom => Customers
            .Where(x => x.Location == Location.BathroomM || x.Location == Location.BathroomF);
        public IEnumerable<Customer> CustomersInBar => Customers
            .Where(x => x.Location == Location.Bar || x.Location == Location.Hallway);
        public bool AtCapacity => Customers.Count >= MaxCustomers;
        public GameObject CustomersHolder;
        public Customer CreateCustomer(bool desperate) {
            Customer newCustomer = UnityEngine.Object.Instantiate(Prefabs.PrefabCustomer, Navigation.CustomerSpawnpoint, Quaternion.identity);
            newCustomer.transform.SetParent(CustomersHolder.transform, true);
            newCustomer.Location = Location.Outside;
            newCustomer.Gender = Random.Range(0, 2) == 0 ? 'm' : 'f';
            Customers.Add(newCustomer);
            if ( desperate ) {
                newCustomer.SetupCustomer(90, 100);
            }
            else {
                newCustomer.SetupCustomer(30, 90);
            }
            //Debug.Log($"Created | state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}", newCustomer);
            newCustomer.Active = true;
            bool enteredDoorway = false;
            if ( newCustomer.WantsToEnterBathroom() &&
                ( newCustomer.DesperationState == CustomerDesperationState.State3 ||
                newCustomer.DesperationState == CustomerDesperationState.State4 ||
                newCustomer.DesperationState == CustomerDesperationState.State5 ) ) {

                var bathroom = newCustomer.Gender == 'm' ? Bathroom.BathroomM : Bathroom.BathroomF;
                enteredDoorway = newCustomer.GetInLine(bathroom);
            }
            // Else sit right down at the bar and wait
            if ( !enteredDoorway ) {
                Seat seat = Bar.Singleton.GetOpenSeat();
                newCustomer.Occupy(seat);
            }

            return newCustomer;
        }
        public void RemoveCustomer(Customer customer) {
            customer.StopOccupyingAll();
            Customers.Remove(customer);
            UnityEngine.Object.Destroy(customer.gameObject);
        }
    }
}
