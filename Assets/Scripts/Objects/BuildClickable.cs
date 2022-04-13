using System;
using System.Linq;
using UnityEngine;
using static Assets.Scripts.GameSaveData;

namespace Assets.Scripts.Objects {
    [Serializable]
    public class BuildClickable : MonoBehaviour {
        [SerializeField] public TMPro.TMP_Text Text;
        public SpriteRenderer SRenderer;
        public Option Option;

        private void Start() {
            // Set the text to the correct text
            Text.text = $"${Option.Cost}";

            // Change the sprite to match what could be built there
            var spriteLookup = Option.Alignment == Alignment.Horizontal
                ? Collections.HorizontalInteractableSprites
                : Collections.VerticalInteractableSprites;
            SRenderer.sprite = spriteLookup[Option.Options.First()];

            // Make it partially transparent and light green if affordable, light red if too expensive.
            if ( GameController.GC.Game.Funds >= Option.Cost ) {
                SRenderer.color = new Color(0.8f, 1f, 0.8f, 0.5f);
            }
            else {
                SRenderer.color = new Color(1f, 0.8f, 0.8f, 0.5f);
            }
        }

        private void OnMouseDown() {
            InteractableType type = Option.Options.First();

            if ( CanSpawn(type) ) {
                Spawn(type);
            }
            else {
                Debug.Log($"Can't spawn a {type} here!");
            }
        }

        public void Spawn(InteractableType type) {
            // Subtract funds
            GameController.GC.Game.Funds -= Option.Cost;
            GameController.GC.UpdateFundsDisplay();

            if ( Option.Current != InteractableType.None ) {
                throw new InvalidOperationException($"Can't spawn {type} at ({Option.X}, {Option.Y}) something already exists there");
            }
            if ( !Option.Options.Contains(type) ) {
                // Log a warning but continue anyways. Nothing will break.
                Debug.LogWarning($"{type} is not an option for option at ({Option.X}, {Option.Y})");
            }

            SpawnInteractable(Option, type);
            GameController.GC.BuildMenu.RebuildClickables();
        }
        public bool CanSpawn(InteractableType type) {
            return GameController.GC.InfiniteFunds || GameController.GC.Game.Funds >= Option.Cost;
        }

    }
}
