using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Assets.Scripts.Objects;
using Assets.Scripts;

public class Bar : MonoBehaviour {
    public static Bar Singleton = null;
    
    [SerializeField]
    public Area2D Area;

    [SerializeField]
    public static double DrinkCost;
    [SerializeField]
    public static double DrinkAmount;

    [SerializeField] public List<Seat> Seats;

    public Seat GetOpenSeat() {
        var x = Seats.Where(x => x.OccupiedBy == null && !x.IsSoiled).ToArray();
        return x[Random.Range(0, x.Count())];
    }

    private void Awake() {
        DrinkCost = 5d;
        DrinkAmount = 360d;
        Singleton = this;
    }

}
