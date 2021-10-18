using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Objects;

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

        [SerializeField] public GameObject ClickablePrefab;
        private List<GameObject> clickables = new List<GameObject>();

        public void Open() {
            enabled = true;
            Canvas.SetActive(true);

            // Place some clickables to buy things.
            var spawnpoints = Bathroom.Singleton.Spawnpoints
                .Where(x => !x.Occupied);
            foreach(var point in spawnpoints) {

                // Make a new clickable
                GameObject clickable = GameObject.Instantiate(ClickablePrefab, point.transform.position, point.transform.rotation);
                SpriteRenderer renderer = clickable.GetComponent<SpriteRenderer>();
                
                // Change the sprite to match what could be built there
                switch (point.IType) {
                    case CustomerInteractable.InteractableType.Sink:
                    renderer.sprite = Collections.spriteSink;
                    break;
                    case CustomerInteractable.InteractableType.Toilet:
                    renderer.sprite = Collections.spriteToilet;
                    break;
                    case CustomerInteractable.InteractableType.Urinal:
                    renderer.sprite = point.Sideways ? Collections.spriteUrinalSideways : Collections.spriteUrinal;
                    break;
                    default:
                    Debug.LogError($"Unsupported spawnpoint type {point.IType}");
                    break;
                }

                // Make it partially transparent and light blue
                renderer.color = new Color(0.9f, 0.9f, 1f, 0.6f);

                // Set up the click action
                clickable.GetComponent<ClickableObject>().OnClick = () => {
                    Bathroom.Singleton.SpawnReliefPrefab(point);
                    UnityEngine.Object.Destroy(clickable);
                };

                // Add to list of tracked clickables for future teardown
                clickables.Add(clickable);
            }
        }
        public void Close() {
            // Destory the clickables we created
            clickables.ForEach(x => UnityEngine.Object.Destroy(x));
            
            // Close the menu
            enabled = false;
            Canvas.SetActive(false);
        }

        public void SetUpButtons() {
            MainMenuButton.onClick.AddListener(GameController.GC.GoToMainMenu);
            StartButton.onClick.AddListener(GameController.GC.EndBuildMode);
        }

        public BuildMenu() {
        }
    }
}
