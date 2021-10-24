using Assets.Scripts.Objects;
using System;
using UnityEngine;

[Serializable]
public class BathroomEntitySpawnpoint : MonoBehaviour {
    public int Id;
    public CustomerInteractable.InteractableType IType;
    public bool Sideways;
    public bool Occupied = false;
    public double Price;

    private void Start() {
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
    }
}
