using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Assets.Scripts.GameSaveData;

namespace Assets.Scripts.Objects {
    [Serializable]
    public class BuildClickable : MonoBehaviour {
        public Image SRenderer;
        public SpriteRenderer AltSRenderer;
        public TMPro.TMP_Text Text;
        public LayoutSpot Spot;

        private void Start() {
            // Change the sprite to match what could be built there
            var spriteLookup = Spot.Alignment == Alignment.Horizontal
                ? Collections.HorizontalInteractableSprites
                : Collections.VerticalInteractableSprites;

            //// Make the graphic green if everything is affordable, yellow if some thing, red if none
            //var Affordables = Spot.Options.Select(x => GameController.GC.Game.Funds >= x.Cost);
            //if ( Affordables.All(x => x)) {
            //    SRenderer.color = new Color(0.8f, 1f, 0.8f, 0.5f);
            //}
            //else if ( Affordables.All(x => x) ) {
            //    SRenderer.color = new Color(0.95f, 0.95f, 0.8f, 0.5f);
            //}
            //else {
            //    SRenderer.color = new Color(1f, 0.8f, 0.8f, 0.5f);
            //}

            // If only one option, show graphic
            if (Spot.Options.Count == 1) {
                AltSRenderer.sprite = spriteLookup[Spot.Options[0].Type];
                Text.text = $"${Spot.Options[0].Cost}";
            }
            else {
                Text.enabled = false;
            }
        }
        private void OnMouseDown() {
            if (Spot.Options.Count > 1) {
                GameController.GC.BuildMenu.OpenBuildOptionsMenu(Spot);
            }
            else {
                HandleSpawn(Spot, Spot.Options[0]);
            }
        }

        public static void HandleSpawn(LayoutSpot spot, LayoutOption option) {
            if ( CanSpawn(option) ) {
                Spawn(spot, option);
            }
            else {
                Debug.Log($"Can't spawn a {option.Type} here!");
            }
            GameController.GC.BuildMenu.CloseBuildOptionsMenu();
        }
        public static void Spawn(LayoutSpot spot, LayoutOption option) {
            // Subtract funds
            GameController.GC.Game.Funds -= option.Cost;
            GameController.GC.UpdateFundsDisplay();

            if ( spot.Current != InteractableType.None ) {
                throw new InvalidOperationException($"Can't spawn at ({spot.X}, {spot.Y}) something already exists there");
            }

            SpawnInteractable(spot, option.Type);
            GameController.GC.BuildMenu.RebuildClickables();
        }
        public static bool CanSpawn(LayoutOption option) {
            return GameController.GC.InfiniteFunds || GameController.GC.Game.Funds >= option.Cost;
        }

    }
}
