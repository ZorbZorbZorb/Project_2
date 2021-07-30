using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour {
    // Unique Id System
    static private int uid = 0;
    static public int GetUid() => uid++;

    public Customer templateCustomer;

    public static List<Customer> customers = new List<Customer>();
    public int ticksSinceLastSpawn = 0;
    public int maxCustomers = 11;

    [SerializeField]
    public WaitingRoom waitingRoom;
    [SerializeField]
    public DoorwayQueue doorwayQueue;

    public static GameController controller = null;

    void Start() {
        if ( controller != null ) {
            throw new InvalidOperationException("Only one game controller may exist");
        }
        controller = this;
        Customer firstCustomer = CreateCustomer();
        firstCustomer.Active = true;
    }

    // Think once a second. Performance optimization because Unity poo-poo.
    float timeAcc = 0f;
    void Update() {
        timeAcc += Time.deltaTime;
        if ( timeAcc >= 1 ) {
            timeAcc -= 1;
            DespawnCustomerInBar();
            Think();
        }
    }
    // Temp method that despawns customers in the bar that dont need to go or have peed themselves
    private void DespawnCustomerInBar() {
        Customer[] targets = customers.Where(x => x.position == Collections.Location.Bar && x.AtDestination() && x.bladder.Percentage < 0.15d && x.Active).ToArray();
        foreach(Customer target in targets) {
            RemoveCustomer(target);
        }
    }

    // Thinks about what should happen next, spawning customers
    private void Think() {
        ticksSinceLastSpawn++;
        // Customer spawning
        if ( customers.Count < 10 ) {
            if ( ticksSinceLastSpawn < 2) {
                return;
            }
            int random = Random.Range(0, 6);
            if ( random == 1 || ticksSinceLastSpawn > 10 ) {
                Customer customer = CreateCustomer();
                customer.Active = true;
                ticksSinceLastSpawn = 0;
            }
        }
    }

    // Closes any open menu
    public void CloseOpenMenus() {
        // Close customer menus
        customers.Where(x => x.Menu.enabled == true).ToList().ForEach(x => x.MenuClose());
    }

    public Customer SpawnCustomerInBar(double metric) {
        throw new NotImplementedException();
    }

    public Customer CreateCustomer() {
        Customer newCustomer = Instantiate(templateCustomer);
        newCustomer.Gender = Random.Range(0,3) == 0 ? 'm' : 'f';
        // Logging
        string logString2 = $"Customer {newCustomer.UID} created. bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}";
        Debug.Log(logString2);
        customers.Add(newCustomer);

        // Customer count changes range of bladder fullness for next customer to enter
        int wiggleRoom = 20;
        int min = 50 / ( customers.Count() + 1 );
        min = Random.Range(min - wiggleRoom, min + wiggleRoom);
        int max = 90 / ( (int)Math.Floor(customers.Count() / 2d) + 1 );
        max = Random.Range(max - wiggleRoom, max + wiggleRoom);

        newCustomer.SetupCustomer(min, max);
        newCustomer.AnnounceStateChange();
        newCustomer.Active = true;
        Seat seat = Bar.Singleton.GetOpenSeat();
        seat.MoveCustomerIntoSpot(newCustomer);
        return newCustomer;
    }
    public void RemoveCustomer(Customer customer) {
        Debug.LogWarning($"Deleted customer {customer.UID}");
        customer.StopOccupyingAll();
        customers.Remove(customer);
        Destroy(customer.gameObject);
    }

}
