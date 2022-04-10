using Assets.Scripts.Objects;
using UnityEngine;

public class Toilet : Relief {
    public override InteractableType IType => InteractableType.Toilet;
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = 0, z = 1 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = -15, z = 1 };
    public override ReliefType RType => ReliefType.Toilet;
    public override bool HidesCustomer => true;
    public override string DisplayName => "Toilet";
    public override bool CanBeSoiled => false;
    public override bool ChangesCustomerSprite => true;

    private bool doorClosed = false;
    private bool faded = false;
    private static Color colorFaded = new Color(1f, 1f, 1f, 0.7f);
    private static Color colorNormal = new Color(1f, 1f, 1f, 1f);

    // TODO: This doesn't need to be an on update. I was just lazy.
    private void Update() {
        // Open or close the stall door
        if ( OccupiedBy != null) {
            if ( OccupiedBy.AtDestination && !doorClosed ) {
                doorClosed = true;
                AltSRenderer.sprite = Collections.spriteStallClosed;
                MainSRenderer.sprite = Collections.spriteToiletOpened;
            }
            else if (!OccupiedBy.AtDestination && doorClosed) {
                doorClosed = false;
                AltSRenderer.sprite = Collections.spriteStallOpened;
                MainSRenderer.sprite = Collections.spriteToiletClosed;
            }
        }
        else {
            if (doorClosed) {
                doorClosed = false;
                AltSRenderer.sprite = Collections.spriteStallOpened;
                MainSRenderer.sprite = Collections.spriteToiletClosed;
            }
            // Unfade the door 
            if ( faded ) {
                AltSRenderer.color = colorNormal;
                faded = false;
            }
        }
    }
    public void OnMouseOver() {
        if (OccupiedBy != null) {
            AltSRenderer.color = colorFaded;
            faded = true;
        }
    }
    public void OnMouseExit() {
        AltSRenderer.color = colorNormal;
        faded = false;
    }
}
