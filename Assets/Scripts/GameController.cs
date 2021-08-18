using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour {
    // Unique Id System
    static private int uid = 0;
    static public int GetUid() => uid++;

    [SerializeField]
    public float runTime = 0f;
    [SerializeField]
    public int timeIncrementsElapsed = 0;
    [SerializeField]
    public DateTime barTime;
    [SerializeField]
    public Text barTimeDisplay;
    [SerializeField]
    public int AdvanceBarTimeEveryXSeconds;
    [SerializeField]
    public int AdvanceBarTimeByXMinutes;

    public Customer templateCustomer;

    public static List<Customer> customers = new List<Customer>();
    public int ticksSinceLastSpawn = 0;
    public int maxCustomers = 14;

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
        maxCustomers = Bar.Singleton.Seats.Length;

        barTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 21, 0, 0);

        Customer firstCustomer = CreateCustomer();
        firstCustomer.Active = true;
    }

    // Think only once a second for better game performance.
    float timeAcc = 0f;
    void Update() {
        runTime += Time.deltaTime;
        timeAcc += Time.deltaTime;
        if ( timeAcc >= 1 ) {
            timeAcc -= 1;
            DespawnCustomerOutside();
            Think();
        }

        barTimeDisplay.text = barTime.ToString("hh:mm tt");
    }
    // Temp method that despawns customers in the bar that dont need to go or have peed themselves
    private void DespawnCustomerOutside() {
        Customer[] targets = customers.Where(x => x.position == Collections.Location.Outside && x.AtDestination() && x.Active).ToArray();
        foreach(Customer target in targets) {
            RemoveCustomer(target);
        }
    }
    private int CustomersInBarNotDesperate() {
        return customers
            .Where(x => x.DesperationState == Collections.CustomerDesperationState.State0 || x.DesperationState == Collections.CustomerDesperationState.State1)
            .Count();
    }
    private int CustomersInBarDesperate() {
        return customers
            .Where(x => 
            x.DesperationState == Collections.CustomerDesperationState.State3 || 
                x.DesperationState != Collections.CustomerDesperationState.State4)
            .Count();
    }
    private int GetCustomersAboutToWetCount() {
        return customers
            .Where(x =>
                x.bladder.Percentage > 1d ||
                x.bladder.LosingControl)
            .Count();
    }
    private int GetReliefAvailableCount() {
        return Bathroom.bathroom.Toilets.Count() + Bathroom.bathroom.Urinals.Count() + Bathroom.bathroom.Sinks.Items.Count();
    }
    private int GetCustomersPeeingCOunt() {
        return customers.Where(x => x.bladder.Emptying).Count();
    }

    // Thinks about what should happen next, spawning customers
    private void Think() {
        // Customer spawning
        if ( customers.Count < maxCustomers ) {
            ticksSinceLastSpawn++;
            bool spawnNow = Random.Range(0, 6) == 0;
            if ((spawnNow && ticksSinceLastSpawn > 1) || ticksSinceLastSpawn > 6) {
                ticksSinceLastSpawn = 0;
                //int customersDesperateCount = CustomersInBarDesperate();
                //int customersNotDesperateCount = CustomersInBarNotDesperate();
                int reliefCount = GetReliefAvailableCount();
                int aboutToWetCount = GetCustomersAboutToWetCount();
                int customersPeeing = GetCustomersPeeingCOunt();
                //if (((customersNotDesperateCount / 2) > customersDesperateCount) && (aboutToWetCount < reliefCount)) {
                if (aboutToWetCount + customersPeeing <= reliefCount) {
                    Customer customer = SpawnCustomerInBar(desperate: true);
                }
                else {
                    Customer customer = SpawnCustomerInBar(desperate: false);
                }
            }
        }

        //if ( customers.Count < 10 ) {
        //    if ( ticksSinceLastSpawn < 2) {
        //        return;
        //    }
        //    int random = Random.Range(0, 6);
        //    if ( random == 1 || ticksSinceLastSpawn > 10 ) {
        //        Customer customer = CreateCustomer();
        //        customer.Active = true;
        //        ticksSinceLastSpawn = 0;
        //    }
        //}

        // Update the bar time
        if ( Math.Floor( runTime / AdvanceBarTimeEveryXSeconds) > timeIncrementsElapsed ) {
            timeIncrementsElapsed++;
            barTime = barTime.AddMinutes(AdvanceBarTimeByXMinutes);
        }
        barTimeDisplay.text = barTime.ToString("hh:mm tt");
    }

    // Closes any open menu
    public void CloseOpenMenus() {
        // Close customer menus
        customers.Where(x => x.Menu.enabled == true).ToList().ForEach(x => x.MenuClose());
    }

    public Customer SpawnCustomerInBar(double metric) {
        throw new NotImplementedException();
    }
    public Customer SpawnCustomerInBar(bool desperate) {
        Customer newCustomer = Instantiate(templateCustomer);
        newCustomer.Gender = Random.Range(0, 3) == 0 ? 'm' : 'f';
        customers.Add(newCustomer);
        if (desperate) {
            newCustomer.SetupCustomer(80, 100);
        }
        else {
            newCustomer.SetupCustomer(30, 92);
        }
        Debug.Log($"Customer {newCustomer.UID} created. state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}");
        newCustomer.Active = true;
        bool enteredDoorway = false;
        if ( newCustomer.FeelsNeedToGo &&
            newCustomer.DesperationState == Collections.CustomerDesperationState.State3 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State4 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State5 ) {

            enteredDoorway = newCustomer.EnterDoorway();
        }
        // Else sit right down at the bar and wait
        if ( !enteredDoorway ) {
            Seat seat = Bar.Singleton.GetOpenSeat();
            seat.MoveCustomerIntoSpot(newCustomer);
        }

        return newCustomer;
    }

    public Customer CreateCustomer() {
        Customer newCustomer = Instantiate(templateCustomer);
        newCustomer.Gender = Random.Range(0,3) == 0 ? 'm' : 'f';
        customers.Add(newCustomer);

        // Customer count changes range of bladder fullness for next customer to enter
        int min = 35;
        int max = 105;
        if (customers.Count() > 8) {
            min = 10;
            max = 80;
        }
        if (customers.Count() == 1) {
            min = 85;
            max = 98;
        }
        //Debug.LogWarning($"{min}, {max}");

        newCustomer.SetupCustomer(min, max);
        Debug.Log($"Customer {newCustomer.UID} created. state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}");
        newCustomer.Active = true;

        // If customer enters bar and needs to go badly try to enter bathroom right away
        bool enteredDoorway = false;
        //Debug.Log($"Customer {newCustomer.UID} {( newCustomer.FeelsNeedToGo ? "does" : "does-not" )} need to go and is at state {newCustomer.DesperationState}");
        //Debug.Log($"{newCustomer.bladder.FeltNeed}");
        if (newCustomer.FeelsNeedToGo &&
            newCustomer.DesperationState == Collections.CustomerDesperationState.State3 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State4 || 
            newCustomer.DesperationState == Collections.CustomerDesperationState.State5) {

            enteredDoorway = newCustomer.EnterDoorway();
        }

        // Else sit right down at the bar and wait
        if (!enteredDoorway) { 
            Seat seat = Bar.Singleton.GetOpenSeat();
            seat.MoveCustomerIntoSpot(newCustomer);
        }

        return newCustomer;
    }
    public void RemoveCustomer(Customer customer) {
        Debug.LogWarning($"Deleted customer {customer.UID}");
        customer.StopOccupyingAll();
        customers.Remove(customer);
        Destroy(customer.gameObject);
    }

}
