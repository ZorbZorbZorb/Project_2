using Assets.Scripts.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Urinal : Relief {
    public override InteractableType IType => InteractableType.Urinal;
    public override Vector3 CustomerPositionF { 
        get { 
            switch (Facing) {
                case Orientation.North:
                    throw new NotImplementedException();
                case Orientation.South:
                    return transform.position + new Vector3() { x = 0, y = -5, z = -1 };
                case Orientation.West:
                    return transform.position + new Vector3() { x = 40, y = 25, z = -1.1f };
                case Orientation.East:
                    return transform.position + new Vector3() { x = -40, y = 25, z = -1.1f };
                default:
                    throw new NotImplementedException();
            }
        }
    }
    public override Vector3 CustomerPositionM {
        get {
            switch ( Facing ) {
                case Orientation.North:
                    throw new NotImplementedException();
                case Orientation.South:
                    return transform.position + new Vector3() { x = 0, y = -10, z = -1 };
                case Orientation.West:
                    return transform.position + new Vector3() { x = 45, y = 15, z = -1.1f };
                case Orientation.East:
                    return transform.position + new Vector3() { x = -45, y = 15, z = -1.1f };
                default:
                    throw new NotImplementedException();
            }
        }
    }
    public override ReliefType RType => ReliefType.Urinal;
    public override bool HidesCustomer => false;
    public override string DisplayName => "Urinal";
    public override bool CanBeSoiled => false;
    public override bool ChangesCustomerSprite => true;
    public new void Start() {
        base.Start();
        switch (Facing) {
            case Orientation.West:
                transform.position += new Vector3(40, 0, 0);
                break;
            case Orientation.East:
                transform.position += new Vector3(-40, 0, 0);
                break;
        }
    }
}
