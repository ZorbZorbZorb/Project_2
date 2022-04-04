using Assets.Scripts;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class InteractableSpawnpoint : MonoBehaviour {
    public static List<InteractableSpawnpoint> Spawnpoints = new List<InteractableSpawnpoint>();

    public int Id;
    public InteractableType IType;
    public bool Sideways;
    public bool Occupied = false;
    public double Price;
    public bool StartsUnlocked = false;
    public Bathroom Bathroom;

    private void Start() {
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
    }
    private void Awake() {
        if ( !Spawnpoints.Contains(this) ) {
            Spawnpoints.Add(this);
        }
    }

    /// <summary>This is a cheat that builds everything possible when called</summary>
    public static void BuildAll() {
        foreach (var spawnpoint in Spawnpoints.Where(x => !x.Occupied )) {
            SpawnInteractablePrefab(spawnpoint);
        }
    }
    public static void Build(GameData gameData) {
        foreach ( int id in gameData.UnlockedPoints ) {
            // Lookup the point
            var spawnpoint = InteractableSpawnpoint.Spawnpoints.Where(x => x.Id == id).FirstOrDefault();
            if ( spawnpoint == null ) {
                Debug.LogError($"GameData requests non-existant spawnpoint '{id}' be activated!");
                continue;
            }
            else if ( spawnpoint.Occupied ) {
                Debug.LogError($"GameData requests activated spawnpoint '{id}' be activated?");
                continue;
            }

            // Spawn the prefab
            SpawnInteractablePrefab(spawnpoint);
        }
    }
    public static T SpawnInteractablePrefab<T>(T prefab, InteractableSpawnpoint point) where T : CustomerInteractable {
        Debug.Log($"Spawning prefab {prefab} at point {point}");

        // Duplicate prefab at the spawnpoints position
        GameObject gameObject = Instantiate(prefab.gameObject, point.transform.position, point.transform.rotation);

        // Get the monobehavior script
        T relief = gameObject.GetComponent<T>();
        if ( relief == null ) {
            Debug.LogError($"Failed to spawn prefab {prefab.IType} because expepcted monobehavior script is missing!");
            Destroy(gameObject);
            return null;
        }

        // Tell the relif it's sideways and call it's sprite lookup constructor
        relief.Facing = point.Sideways ? Orientation.East : Orientation.South;

        // Mark spawn point as used so next spawn doesn't try to use it and stack relief objects.
        point.Occupied = true;

        // Return the newly cloned monobehavior
        return relief;
    }
    public static CustomerInteractable SpawnInteractablePrefab(InteractableSpawnpoint point) {
        CustomerInteractable item;
        switch ( point.IType ) {
            case InteractableType.Sink:
                item = SpawnInteractablePrefab(point.Bathroom.PrefabSink, point);
                point.Bathroom.Sinks.Add(item as Sink);
                break;
            case InteractableType.Toilet:
                item = SpawnInteractablePrefab(point.Bathroom.PrefabToilet, point);
                point.Bathroom.Toilets.Add(item as Toilet);
                break;
            case InteractableType.Urinal:
                item = SpawnInteractablePrefab(point.Bathroom.PrefabUrinal, point);
                point.Bathroom.Urinals.Add(item as Urinal);
                break;
            case InteractableType.Seat:
                item = SpawnInteractablePrefab(GameController.GC.SeatPrefab, point);
                Bar.Singleton.Seats.Add(item as Seat);
                break;
            default:
                throw new NotImplementedException($"Type {point.IType} is not supported.");
        }
        point.Occupied = true;
        return item;
    }
}
