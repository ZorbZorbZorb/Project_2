using Assets.Scripts.Objects;
using UnityEngine;

public class Toilet : Relief {
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = 0, z = 1 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = -15, z = 1 };
    public override Collections.ReliefType ReliefType => Collections.ReliefType.Toilet;
    public override bool HidesCustomer => true;
    public override string DisplayName => "Toilet";
    public override Collections.CustomerActionState StatePantsDown => Collections.CustomerActionState.ToiletPantsDown;
    public override Collections.CustomerActionState StatePeeing => Collections.CustomerActionState.ToiletPeeing;
    public override Collections.CustomerActionState StatePantsUp => Collections.CustomerActionState.ToiletPantsUp;
    private bool doorClosed = false;
    private void Update() {
        // Open or close the stall door
        if (OccupiedBy != null && !doorClosed && OccupiedBy.AtDestination()) {
            SRenderer.sprite = Collections.spriteStallClosed;
            doorClosed = true;
        }
        else if(doorClosed && OccupiedBy == null) {
            SRenderer.sprite = Collections.spriteStallOpened;
            doorClosed = false;
        }
    }

    private void OnMouseOver() {
        if (OccupiedBy != null) {
            SRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        }
    }
    private void OnMouseExit() {
        SRenderer.color = new Color(1f, 1f, 1f, 1f);
    }
}

