using System;
using UnityEngine;

[Serializable]
public class BathroomEntitySpawnpoint : MonoBehaviour {
    public enum Type {
        Toilet, 
        Sink, 
        Urinal
    }
    [SerializeField]
    public Type type;
    [SerializeField]
    public bool sideways; 
}
