using Assets.Scripts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects {
    public abstract class CustomerInteractable : MonoBehaviour {
        public enum InteractableType {
            WaitingSpot,
            Seat,
            Toilet,
            Urinal,
            Ourinal,
            Sink
        };
        public abstract InteractableType Type { get; }
        public abstract Vector3 CustomerPositionF { get; }
        public abstract Vector3 CustomerPositionM { get; }
        public abstract Collections.Location CustomerLocation { get; }
        public Customer OccupiedBy;
        public abstract bool HidesCustomer { get; }
        public abstract bool CanWetHere { get; }
        public bool IsWet { get; set; }
        public abstract Collections.ReliefType ReliefType { get; }
        public int UID { get => uid; }
        private readonly int uid = GameController.GetUid();
        public abstract string DisplayName { get; }

        [SerializeField]
        public SpriteRenderer SRenderer;
    }
}
