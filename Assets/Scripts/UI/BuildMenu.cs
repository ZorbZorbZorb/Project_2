using Assets.Scripts.Areas;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using TMPro;
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

        public GameObject BuildOptionsPanel;
        public AreaBounds2D BuildOptionsArea;

        private bool enabled = false;
        public bool Enabled {
            get => enabled;
            set {
                if ( value ) {
                    Start();
                }
                else {
                    End();
                }
            }
        }

        private List<BuildClickable> clickables = new List<BuildClickable>();

        private List<Button> BuildPanelButtons = new List<Button>();

        public void Start() {
            enabled = true;
            Canvas.SetActive(true);
            RebuildClickables();
        }
        public void End() {
            // Destory the clickables we created
            ClearClickables();

            // Close the menu
            enabled = false;
            Canvas.SetActive(false);
        }

        public void OpenBuildOptionsMenu(LayoutSpot spot) {
            BuildPanelButtons.ForEach(x => UnityEngine.Object.Destroy(x.gameObject));
            BuildPanelButtons.Clear();
            BuildOptionsPanel.SetActive(true);
            // Calculate the build options panel's grid
            BuildOptionsArea = new AreaBounds2D(BuildOptionsPanel.GetComponent<BoxCollider2D>(), 50, 50);

            double y = 1d;
            switch ( spot.Options.Count ) {
                case 1:
                    CreateButton(spot.Options[0], 4, y);
                    break;
                case 2:
                    CreateButton(spot.Options[0], 1, y);
                    CreateButton(spot.Options[1], 5, y);
                    break;
                case 3:
                    CreateButton(spot.Options[0], 0, y);
                    CreateButton(spot.Options[1], 3, y);
                    CreateButton(spot.Options[2], 6, y);
                    break;
                default:
                    throw new NotImplementedException();
            }

            GameObject CreateButton(LayoutOption option, double x, double y) {
                // Abandon all hope ye who enter this function
                Vector2 vector = BuildOptionsArea.GetGridPosition((x, y));
                GameObject instance = UnityEngine.Object.Instantiate(Prefabs.PrefabBuildButton, vector, Quaternion.identity, BuildOptionsPanel.transform);
                Button button = instance.GetComponent<Button>();
                button.transform.localScale *= 2;
                button.image.sprite = spot.Alignment == Alignment.Horizontal
                    ? Collections.HorizontalInteractableSprites[option.Type]
                    : Collections.VerticalInteractableSprites[option.Type];
                button.GetComponentInChildren<TMP_Text>().text = $"${option.Cost}";
                button.onClick.AddListener(() => {
                    BuildClickable.HandleSpawn(spot, option);
                });
                BuildPanelButtons.Add(button);
                return instance;
            }
        }
        public void CloseBuildOptionsMenu() {
            BuildPanelButtons.ForEach(x => UnityEngine.Object.Destroy(x.gameObject));
            BuildPanelButtons.Clear();
            BuildOptionsPanel.SetActive(false);
        }

        public void RebuildClickables() {
            // Destory the clickables we created
            ClearClickables();

            GameSaveData game = GameController.GC.Game;
            BuildAreaClickables(game.Mens);
            BuildAreaClickables(game.Womens);
            BuildAreaClickables(game.bar);
        }
        void BuildAreaClickables(IEnumerable<LayoutSpot> spots) {
            foreach ( var spot in spots ) {
                // If nothing exists here
                if ( spot.Current == InteractableType.None ) {

                    // Spawn clickable
                    var positon = spot.Area.Bounds.GetGridPosition(spot);
                    BuildClickable clickable = UnityEngine.Object.Instantiate(Prefabs.PrefabClickable, positon, Quaternion.identity);
                    clickable.Spot = spot;

                    // Add to list of tracked clickables for future teardown
                    clickables.Add(clickable);
                }
            }
        }
        public void ClearClickables() {
            clickables.ForEach(x => UnityEngine.Object.Destroy(x.gameObject));
            clickables.Clear();
        }
        public void SetUpButtons() {
            MainMenuButton.onClick.AddListener(GameController.GC.GoToMainMenu);
            StartButton.onClick.AddListener(GameController.GC.EndBuildMode);
        }
    }
}
