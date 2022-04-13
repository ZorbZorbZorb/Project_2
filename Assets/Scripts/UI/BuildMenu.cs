using Assets.Scripts.Areas;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    [Serializable]
    public class BuildMenu {
        [SerializeField]
        public GameObject Canvas;

        [SerializeField]
        public Button MainMenuButton;
        [SerializeField]
        public Button StartButton;

        private bool enabled = false;
        public bool Enabled {
            get => enabled;
            set {
                if ( value ) {
                    Open();
                }
                else {
                    Close();
                }
            }
        }

        private List<BuildClickable> clickables = new List<BuildClickable>();

        public void Open() {
            enabled = true;
            Canvas.SetActive(true);
            RebuildClickables();
        }
        public void Close() {
            // Destory the clickables we created
            clickables.ForEach(x => UnityEngine.Object.Destroy(x));

            // Close the menu
            enabled = false;
            Canvas.SetActive(false);
        }

        public void RebuildClickables() {
            // Destory the clickables we created
            clickables.ForEach(x => UnityEngine.Object.Destroy(x.gameObject));
            clickables.Clear();

            GameSaveData game = GameController.GC.Game;
            var bathroom = Bathroom.BathroomM;
            foreach ( GameSaveData.Option option in game.Mens ) {
                // If nothing exists here
                if ( option.Current == InteractableType.None ) {
                    // Spawn clickable
                    var positon = bathroom.Bounds.GetGridPosition(option);
                    BuildClickable clickable = UnityEngine.Object.Instantiate(Prefabs.PrefabClickable, positon, Quaternion.identity);
                    clickable.Option = option;

                    // Add to list of tracked clickables for future teardown
                    clickables.Add(clickable);
                }
            }
        }
        public void SetUpButtons() {
            MainMenuButton.onClick.AddListener(GameController.GC.GoToMainMenu);
            StartButton.onClick.AddListener(GameController.GC.EndBuildMode);
        }

        public BuildMenu() {
        }
    }
}
