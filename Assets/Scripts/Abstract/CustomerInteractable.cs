using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Objects {
    public abstract class CustomerInteractable : MonoBehaviour {
        // TODO: There has to be a better way than having two enums with intersecting values...
        public enum InteractableType {
            WaitingSpot,
            Seat,
            Toilet,
            Urinal,
            Ourinal,
            Sink
        };
        public enum ReliefType {
            None,
            Toilet,
            Urinal,
            Sink,
            Towel,
        }
        public abstract InteractableType IType { get; }
        public abstract ReliefType RType { get; }
        public virtual Vector3 CustomerPositionF { get; }
        public virtual Vector3 CustomerPositionM { get; }
        [SerializeField] public bool Sideways = false;
        public abstract Collections.Location CustomerLocation { get; }
        public Customer OccupiedBy;
        public abstract bool ChangesCustomerSprite { get; }
        public abstract bool HidesCustomer { get; }
        public abstract bool CanWetHere { get; }
        public abstract bool CanBeSoiled { get; }
        public bool IsSoiled { get; set; }
        public int UID { get => uid; }
        private readonly int uid = GameController.GetUid();
        public abstract string DisplayName { get; }

        [SerializeField]
        public SpriteRenderer MainSRenderer;
        public SpriteRenderer AltSRenderer;
        public Sprite[] MainSprites;
        public Sprite[] MainSpritesSideways;
        public Sprite[] AltSprites;
        public Sprite[] AltSpritesSideways;
    }
}
