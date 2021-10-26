using Assets.Scripts;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class InteractableSpawnpoint : MonoBehaviour {
    [SerializeField] public static List<InteractableSpawnpoint> Spawnpoints = new List<InteractableSpawnpoint>();

    public int Id;
    public CustomerInteractable.InteractableType IType;
    public bool Sideways;
    public bool Occupied = false;
    public double Price;

    private void Start() {
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
    }
    private void Awake() {
        if ( !Spawnpoints.Contains(this) ) {
            Spawnpoints.Add(this);
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
        relief.Sideways = point.Sideways;

        // Mark spawn point as used so next spawn doesn't try to use it and stack relief objects.
        point.Occupied = true;

        // Return the newly cloned monobehavior
        return relief;
    }
    public static CustomerInteractable SpawnInteractablePrefab(InteractableSpawnpoint point) {
        CustomerInteractable item;
        switch ( point.IType ) {
            case CustomerInteractable.InteractableType.Sink:
                item = SpawnInteractablePrefab(Bathroom.Singleton.PrefabSink, point);
                Bathroom.Singleton.Sinks.Items.Add(item as Sink);
                break;
            case CustomerInteractable.InteractableType.Toilet:
                item = SpawnInteractablePrefab(Bathroom.Singleton.PrefabToilet, point);
                Bathroom.Singleton.Toilets.Add(item as Toilet);
                break;
            case CustomerInteractable.InteractableType.Urinal:
                item = SpawnInteractablePrefab(Bathroom.Singleton.PrefabUrinal, point);
                Bathroom.Singleton.Urinals.Add(item as Urinal);
                break;
            case CustomerInteractable.InteractableType.Seat:
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
