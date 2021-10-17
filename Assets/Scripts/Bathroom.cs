using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using Assets.Scripts.Objects;

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
        for ( int i = 0; i < gameData.bathroomToilets; i++ ) {
            Toilet newToilet = SpawnRelifPrefab(PrefabToilet);
            Toilets.Add(newToilet);
        }

        for ( int i = 0; i < gameData.bathroomUrinals; i++ ) {
            Urinal newUrinal = SpawnRelifPrefab(PrefabUrinal);
            Urinals.Add(newUrinal);
        }
    }
    private T SpawnRelifPrefab<T>(T prefab) where T:Relief {
        // Get a spawn point
        BathroomEntitySpawnpoint point = Spawnpoints
            .Where(x => x.IType == prefab.IType && !x.used)
            .FirstOrDefault();
        if ( point == null ) {
            Debug.LogError($"Failed to spawn prefab {prefab.IType} because there are no points to spawn it at!");
            return null;
        }

        // Duplicate prefab at the spawnpoints position
        GameObject gameObject = Instantiate(prefab.gameObject, point.transform.position, point.transform.rotation);

        // Get the monobehavior script
        T relief = gameObject.GetComponent<T>();
        if (relief == null) {
            Debug.LogError($"Failed to spawn prefab {prefab.IType} because expepcted monobehavior script is missing!");
            Destroy(gameObject);
            return null;
        }

        // Tell the relif it's sideways and call it's sprite lookup constructor
        relief.Sideways = point.Sideways;
        relief.BuildSpriteLookup();

        // Mark spawn point as used so next spawn doesn't try to use it and stack relief objects.
        point.used = true;

        // Return the newly cloned monobehavior
        return relief;
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
