using Assets.Scripts.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Urinal : Relief {
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = -5, z = -1 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = -10, z = -1 };
    public override Collections.ReliefType ReliefType => Collections.ReliefType.Urinal;
    public override bool HidesCustomer => false;
    public override string DisplayName => "Urinal";
    public override Collections.CustomerActionState StatePantsDown => Collections.CustomerActionState.UrinalPantsDown;
    public override Collections.CustomerActionState StatePeeing => Collections.CustomerActionState.UrinalPeeing;
    public override Collections.CustomerActionState StatePantsUp => Collections.CustomerActionState.UrinalPantsUp;
}
