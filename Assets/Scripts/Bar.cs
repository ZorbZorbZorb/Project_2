using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Bar : MonoBehaviour {
    public static Bar Singleton = null;

    [SerializeField]
    public static double DrinkCost = 5d;
    [SerializeField]
    public static double DrinkAmount = 360d;

    [SerializeField]
    public Seat[] Seats;

    public Seat GetOpenSeat() {
        var x = Seats.Where(x => x.OccupiedBy == null).ToArray();
        return x[Random.Range(0, x.Count())];
    }

    private void Awake() {
        Singleton = this;
        Customer.Bar = this;
    }

}
