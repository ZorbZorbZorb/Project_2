using Assets.Scripts.Objects;
using UnityEngine;

public class Sink : Relief {
    public Sinks Sinks;
    public override InteractableType Type => InteractableType.Sink;
    public override Vector3 CustomerPositionF => transform.position + new Vector3() { x = 0, y = 10, z = -1 };
    public override Vector3 CustomerPositionM => transform.position + new Vector3() { x = 0, y = -5, z = -1 };
    public override Collections.ReliefType ReliefType => Collections.ReliefType.Sink;
    public override bool HidesCustomer => false;
    public override string DisplayName => "Sink";
    public override Collections.CustomerActionState StatePantsDown => Collections.CustomerActionState.SinkPantsDown;
    public override Collections.CustomerActionState StatePeeing => Collections.CustomerActionState.SinkPeeing;
    public override Collections.CustomerActionState StatePantsUp => Collections.CustomerActionState.SinkPantsUp;
    public void Use(Customer customer) {
        customer.ActionState = Collections.CustomerActionState.SinkPantsUp;
        customer.NextDelay = 6f;
        customer.Next = () => {
            customer.ActionState = Collections.CustomerActionState.None;
            customer.EnterBar();
        };
    }
}
