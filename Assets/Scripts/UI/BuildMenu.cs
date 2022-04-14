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

        //[Range(0.8f, 1.2f)]
        //public float DebugHellSliderX = 1f;
        //[Range(1.5f, 2f)]
        //public float DebugHellSliderScale = 1f;

        public Transform P1;
        public Transform P2;
        public Transform P3;
        public Transform P4;
        public Transform P5;
        public Transform P6;

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
            BuildOptionsArea = new AreaBounds2D(BuildOptionsPanel.GetComponent<BoxCollider2D>(), 70, 70);

            double y = 2d;
            switch ( spot.Options.Count ) {
                case 1:
                    CreateButton(spot.Options[0], ( P3.transform.position + P4.transform.position ) / 2);
                    break;
                case 2:
                    CreateButton(spot.Options[0], P2.transform.position);
                    CreateButton(spot.Options[1], P5.transform.position);
                    break;
                case 3:
                    CreateButton(spot.Options[0], P1.transform.position);
                    CreateButton(spot.Options[1], (P3.transform.position + P4.transform.position) / 2);
                    CreateButton(spot.Options[2], P6.transform.position);
                    break;
                default:
                    throw new NotImplementedException();
            }

            //void CreateButton(LayoutOption option, double x, double y) {
            //    // Abandon all hope ye who enter this function
            //    Vector2 vector = BuildOptionsArea.GetGridPosition((x, y));
            //    GameObject instance = UnityEngine.Object.Instantiate(Prefabs.PrefabBuildButton, vector, Quaternion.identity, BuildOptionsPanel.transform);
            //    Button button = instance.GetComponent<Button>();
            //    button.transform.localScale = new Vector3(1f / 50f, 1f / 50f, 1f / 50f);
            //    button.image.sprite = spot.Alignment == Alignment.Horizontal
            //        ? Collections.HorizontalInteractableSprites[option.Type]
            //        : Collections.VerticalInteractableSprites[option.Type];
            //    button.GetComponentInChildren<TMP_Text>().text = $"${option.Cost}";
            //    button.onClick.AddListener(() => {
            //        BuildClickable.HandleSpawn(spot, option);
            //    });
            //    BuildPanelButtons.Add(button);
            //}
            void CreateButton(LayoutOption option, Vector2 vector) {
                //vector *= new Vector2(DebugHellSliderX, 1f);
                vector *= new Vector2(0.99f, 1f);
                GameObject instance = UnityEngine.Object.Instantiate(Prefabs.PrefabBuildButton, vector, Quaternion.identity, BuildOptionsPanel.transform);
                Button button = instance.GetComponent<Button>();
                //button.transform.localScale = new Vector3(1f,1f,1f) * DebugHellSliderScale;
                button.transform.localScale = new Vector3(1f, 1f, 1f) * 1.75f;
                button.image.sprite = spot.Alignment == Alignment.Horizontal
                    ? Collections.HorizontalInteractableSprites[option.Type]
                    : Collections.VerticalInteractableSprites[option.Type];
                button.GetComponentInChildren<TMP_Text>().text = $"${option.Cost}";
                button.onClick.AddListener(() => {
                    BuildClickable.HandleSpawn(spot, option);
                });
                BuildPanelButtons.Add(button);
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
            var bathroom = Bathroom.BathroomM;
            foreach ( var spot in game.Mens ) {
                // If nothing exists here
                if ( spot.Current == InteractableType.None ) {

                    // Spawn clickable
                    var positon = bathroom.Bounds.GetGridPosition(spot);
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
