using Assets.Scripts;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bathroom : MonoBehaviour {
    public static Bathroom Singleton = null;

    [SerializeField]
    public List<BathroomEntitySpawnpoint> Spawnpoints = new List<BathroomEntitySpawnpoint>();

    public Toilet PrefabToilet;
    public Urinal PrefabUrinal;
    public Sink PrefabSink;

    public Toilet GetToilet() {
        return Toilets.Where(x => x.OccupiedBy == null).First();
    }
    public Urinal GetUrinal() {
        return Urinals.Where(x => x.OccupiedBy == null).First();
    }

    public Sinks Sinks = new Sinks();

    public void ConstructBathroom(GameData gameData) {
        foreach ( int id in gameData.UnlockedPoints ) {
            // Lookup the point
            var spawnpoint = Spawnpoints.Where(x => x.Id == id).FirstOrDefault();
            if (spawnpoint == null) {
                Debug.LogError($"GameData requests non-existant spawnpoint '{id}' be activated!");
                continue;
            }

            // Spawn the prefab
            SpawnReliefPrefab(spawnpoint);
        }
    }
    public T SpawnReliefPrefab<T>(T prefab, BathroomEntitySpawnpoint point) where T : Relief {
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
        relief.BuildSpriteLookup();

        // Mark spawn point as used so next spawn doesn't try to use it and stack relief objects.
        point.Occupied = true;

        // Return the newly cloned monobehavior
        return relief;
    }
    public T SpawnReliefPrefab<T>(T prefab) where T : Relief {
        // Get a spawn point
        BathroomEntitySpawnpoint point = Spawnpoints
            .Where(x => x.IType == prefab.IType && !x.Occupied)
            .FirstOrDefault();
        if ( point == null ) {
            Debug.LogError($"Failed to spawn prefab {prefab.IType} because there are no points to spawn it at!");
            return null;
        }

        return SpawnReliefPrefab(prefab, point);
    }
    public Relief SpawnReliefPrefab(BathroomEntitySpawnpoint point) {
        Relief item;
        switch ( point.IType ) {
            case CustomerInteractable.InteractableType.Sink:
            item = SpawnReliefPrefab(PrefabSink, point);
            Sinks.Items.Add(item as Sink);
            break;
            case CustomerInteractable.InteractableType.Toilet:
            item = SpawnReliefPrefab(PrefabToilet, point);
            Toilets.Add(item as Toilet);
            break;
            case CustomerInteractable.InteractableType.Urinal:
            item = SpawnReliefPrefab(PrefabUrinal, point);
            Urinals.Add(item as Urinal);
            break;
            default:
            throw new NotImplementedException($"Type {point.IType} is not supported.");
        }
        point.Occupied = true;
        return item;
    }
    // These three are checked once per tick by each customer. Have them as public bools that are not calculated to save ups.
    public bool HasToiletAvailable { get; private set; }
    public bool HasUrinalAvailable { get; private set; }
    public bool HasSinkForRelief { get; private set; }

    public List<Toilet> Toilets;
    public List<Urinal> Urinals;

    // Start is called before the first frame update
    void Start() {
        Singleton = this;
    }

    // Update is called once per frame
    void Update() {
        Sinks.Update();
        HasToiletAvailable = Toilets.Where(x => x.OccupiedBy == null).Any();
        HasUrinalAvailable = Urinals.Where(x => x.OccupiedBy == null).Any();
        HasSinkForRelief = Sinks.CanUseForReliefNow();
    }
}
