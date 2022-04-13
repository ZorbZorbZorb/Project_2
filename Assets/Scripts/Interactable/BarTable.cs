using System;
using System.Collections.Generic;

namespace Assets.Scripts.Objects {
    public class BarTable : CustomerInteractable {

        public List<Seat> Seats;

        public override InteractableType IType => InteractableType.Table;
        public override ReliefType RType => throw new NotImplementedException();
        public override bool ChangesCustomerSprite => throw new NotImplementedException();
        public override bool HidesCustomer => throw new NotImplementedException();
        public override bool CanWetHere => throw new NotImplementedException();
        public override bool CanBeSoiled => throw new NotImplementedException();
        public override string DisplayName => "Table";
    }
}
