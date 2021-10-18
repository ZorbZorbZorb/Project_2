using Assets.Scripts.Objects;
using System;
using UnityEngine;

[Serializable]
public class BathroomEntitySpawnpoint : MonoBehaviour {
    public CustomerInteractable.InteractableType IType;
    public bool Sideways;
    public bool Occupied = false;
}
