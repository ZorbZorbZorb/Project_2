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

        private void RebuildClickables() {
            // Destory the clickables we created
            clickables.ForEach(x => UnityEngine.Object.Destroy(x));

            GameSaveData game = GameController.GC.Game;
            var bathroom = Bathroom.BathroomM;
            foreach ( GameSaveData.Option option in game.Mens ) {
                // If nothing exists here
                if ( option.Current == InteractableType.None ) {
                    // Spawn clickable
                    var area = bathroom.Area;
                    var positon = area.GetGridPosition(option);
                    BuildClickable clickable = UnityEngine.Object.Instantiate(Prefabs.PrefabClickable, positon, Quaternion.identity);
                    // Set the text to the correct text
                    clickable.Text.text = $"${option.Cost}";

                    // Change the sprite to match what could be built there
                    #warning only supports first type, change me!
                    InteractableType type = option.Options.First();
                    switch ( type ) {
                        case InteractableType.Sink:
                            clickable.SRenderer.sprite = Collections.spriteSink;
                            break;
                        case InteractableType.Toilet:
                            clickable.SRenderer.sprite = Collections.spriteToilet;
                            break;
                        case InteractableType.Urinal:
                            clickable.SRenderer.sprite = Collections.spriteUrinal;
                            break;
                        case InteractableType.Seat:
                            clickable.SRenderer.sprite = Collections.SpriteStoolNormal;
                            break;
                        default:
                            Debug.LogError($"Unsupported spawnpoint type {type}");
                            break;
                    }

                    // Make it partially transparent and light green if affordable, light red if too expensive.
                    if ( GameController.GC.Game.Funds >= option.Cost ) {
                        clickable.SRenderer.color = new Color(0.8f, 1f, 0.8f, 0.5f);
                    }
                    else {
                        clickable.SRenderer.color = new Color(1f, 0.8f, 0.8f, 0.5f);
                    }

                    // Set up the click action
                    clickable.OnClick = () => {
                        if ( GameController.GC.InfiniteFunds || GameController.GC.Game.Funds >= option.Cost ) {
                            // Subtract funds
                            GameController.GC.Game.Funds -= option.Cost;
                            GameController.GC.UpdateFundsDisplay();
                            // Spawn prefab
                            Vector2 vector = bathroom.Area.GetGridPosition(option);
                            CustomerInteractable prefab;
                            switch ( option.Options.First() ) {
                                case InteractableType.Sink:
                                    prefab = Prefabs.PrefabSink;
                                    break;
                                case InteractableType.Toilet:
                                    prefab = Prefabs.PrefabToilet;
                                    break;
                                case InteractableType.Urinal:
                                    prefab = Prefabs.PrefabUrinal;
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            var instance = UnityEngine.Object.Instantiate(prefab, vector, Quaternion.identity);
                            // Initialize instance
                            instance.Facing = option.Facing;
                            instance.Location = bathroom.Location;
                            // Set the new instance to the current in the option
                            option.Current = instance.IType;
                            // Rebuild all of the clickables
                            RebuildClickables();
                        }
                    };

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
