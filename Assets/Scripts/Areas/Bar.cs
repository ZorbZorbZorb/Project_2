using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Assets.Scripts.Objects;
using Assets.Scripts;
using Assets.Scripts.Areas;
using System;

public class Bar : Area {

    public static Bar Singleton = null;
    
    [SerializeField]
    public static double DrinkCost;
    [SerializeField]
    public static double DrinkAmount;
    public WaitingSpot DrinkPhantomWaitingSpot = null;

    [SerializeField] public List<Seat> Seats = new List<Seat>();

    public Seat GetRandomOpenSeat() {
        var x = Seats.Where(x => x.OccupiedBy == null && !x.IsSoiled).ToArray();
        return x[Random.Range(0, x.Count())];
    }

    public override void AddInteractable(CustomerInteractable interactable) {
        if (interactable is Seat seat) {
            seat.SeatType = SeatType.Counter;
            Seats.Add(seat);
        }
        else if (interactable is BarTable table) {
            table.Location = Location;
            foreach ( Seat tableSeat in table.Seats ) {
                tableSeat.SeatType = SeatType.Table;
                tableSeat.Facing = table.Facing;
                tableSeat.Location = table.Location;
                Seats.Add(tableSeat);
            }
        }
        else if (interactable is WaitingSpot spot) {
            if (DrinkPhantomWaitingSpot != null) {
                throw new InvalidOperationException("Phantom drinks spot already set");
            }
            spot.WaitingSpotType = WaitingSpotType.DrinksGhost;
            spot.MainSRenderer.enabled = false;
            DrinkPhantomWaitingSpot = spot;
        }
        else {
            throw new NotImplementedException($"Type {interactable.IType} is not supported.");
        }
    }

    public new void Awake() {
        base.Awake();
        Seats = new List<Seat>();
        Location = Location.Bar;
        Singleton = this;
        DrinkCost = 5d;
        DrinkAmount = 360d;
    }

}
