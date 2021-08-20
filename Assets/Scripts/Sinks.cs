using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Objects;

[Serializable]
public class Sinks {
    [SerializeField]
    public Line Line;
    [SerializeField]
    public List<Sink> Items = new List<Sink>();

    public void Update() {
        if (Line.Any()) {
            Line.Update();
            if (AnyUnoccupiedSink()) {
                Sink sink = FirstUnoccupiedSink();
                Customer customer = Line.GetNextInLine();
                customer.UseInteractable(sink);
                sink.Use(customer);
            }
        }
    }
    public bool AllSinksBeingPeedIn() {
        return Items.Where(x => x.OccupiedBy != null && x.OccupiedBy.IsRelievingSelf).Count() == Items.Count();
    }
    public bool CanUseForReliefNow() {
        return !( Items.Where(x => x.OccupiedBy != null).Any() || Line.Any() );
    }
    public bool AnyUnoccupiedSink() {
        return Items.Where(x => x.OccupiedBy == null).Any();
    }
    public Sink FirstUnoccupiedSink() {
        return Items.Where(x => x.OccupiedBy == null).First();
    }
    public bool HasOpenWaitingSpot() {
        return Line.HasOpenWaitingSpot();
    }
    public void EnterLine(Customer customer) {
        if (customer.Occupying.Type == CustomerInteractable.InteractableType.Sink) {
            Sink sink = (Sink)customer.Occupying;
            customer.UseInteractable(sink);
            sink.Use(customer);
        }
        else {
            customer.Occupying.OccupiedBy = null;
            customer.Occupying = null;
        }

        if (!Line.Any() && AnyUnoccupiedSink()) {
            Sink sink = FirstUnoccupiedSink();
            customer.UseInteractable(sink);
            sink.Use(customer);
        }
        else {
            WaitingSpot spot = Line.GetNextWaitingSpot();
            customer.UseInteractable(spot);
        }
    }
}
