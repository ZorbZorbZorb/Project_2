using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Objects {
    public class BarTable : MonoBehaviour {
        public List<Seat> Seats;
        public List<InteractableSpawnpoint> SeatSpawnpoints;
        public SpriteRenderer Renderer;

        public bool HasSpawnpoint() {
            return SeatSpawnpoints.Where(x => !x.Occupied).Any();
        }
        public Seat UnlockAndSpawnSeat(InteractableSpawnpoint point) {
            Seat seat = Instantiate(Prefabs.PrefabSeat, point.transform.position, point.transform.rotation);
            Seats.Add(seat);
            point.Occupied = true;
            GameController.GC.gameData.UnlockedPoints.Add(point.Id);
            return seat;
        }
    }
}
