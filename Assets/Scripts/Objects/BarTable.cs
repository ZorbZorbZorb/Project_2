using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            GameObject gameObject = Instantiate(GameController.GC.SeatPrefab.gameObject, point.transform.position, point.transform.rotation);
            Seat seat = gameObject.GetComponent<Seat>();
            Bar.Singleton.Seats.Add(seat);
            point.Occupied = true;
            GameController.GC.gameData.UnlockedPoints.Add(point.Id);
            return seat;
        }
    }
}
