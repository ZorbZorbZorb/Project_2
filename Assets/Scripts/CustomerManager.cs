using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class GameController : MonoBehaviour {
    [Serializable]
    public class CustomerManager {
        public int MaxCustomers;
        public List<Customer> Customers = new List<Customer>();
        public IEnumerable<Customer> CustomersInBathroom => Customers
            .Where(x => x.Location == Location.BathroomM || x.Location == Location.BathroomF);
        public IEnumerable<Customer> CustomersInBar => Customers
            .Where(x => x.Location == Location.Bar || x.Location == Location.Hallway);
        public bool AtCapacity => Customers.Count >= MaxCustomers;
        public Customer CreateCustomer(bool desperate) {
            Customer newCustomer = Instantiate(Prefabs.PrefabCustomer, Assets.Scripts.Navigation.CustomerSpawnpoint, Quaternion.identity);
            newCustomer.Location = Location.Outside;
            newCustomer.Gender = Random.Range(0, 2) == 0 ? 'm' : 'f';
            Customers.Add(newCustomer);
            if ( desperate ) {
                newCustomer.SetupCustomer(90, 100);
            }
            else {
                newCustomer.SetupCustomer(30, 90);
            }
            Debug.Log($"Customer {newCustomer.UID} created. state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}");
            newCustomer.Active = true;
            bool enteredDoorway = false;
            if ( newCustomer.WantsToEnterBathroom() &&
                ( newCustomer.DesperationState == Collections.CustomerDesperationState.State3 ||
                newCustomer.DesperationState == Collections.CustomerDesperationState.State4 ||
                newCustomer.DesperationState == Collections.CustomerDesperationState.State5 ) ) {

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
            Debug.Log($"Deleted customer {customer.UID}");
            customer.StopOccupyingAll();
            Customers.Remove(customer);
            Destroy(customer.gameObject);
        }
    }
}
