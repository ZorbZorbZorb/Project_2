using UnityEngine;

namespace Assets.Scripts.Objects {
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
    public enum Orientation {
        North,
        South,
        East,
        West,
    }
    public enum Alignment {
        Vertical,
        Horizontal
    }
    public abstract class CustomerInteractable : MonoBehaviour {
        // TODO: There has to be a better way than having two enums with intersecting values...
        public abstract InteractableType IType { get; }
        public abstract ReliefType RType { get; }
        public virtual Vector3 CustomerPositionF { get; }
        public virtual Vector3 CustomerPositionM { get; }
        public Orientation Orientation { get; set; }
        public Alignment Alignment => Orientation == Orientation.North || Orientation == Orientation.South 
            ? Alignment.Vertical 
            : Alignment.Horizontal;
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
        public bool Unoccupied => OccupiedBy == null;

        public SpriteRenderer MainSRenderer;
        public SpriteRenderer AltSRenderer;
        public Sprite[] MainSprites;
        public Sprite[] MainSpritesSideways;
        public Sprite[] AltSprites;
        public Sprite[] AltSpritesSideways;
    }
}
