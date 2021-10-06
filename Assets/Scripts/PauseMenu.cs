using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    [Serializable]
    public class PauseMenu {
        [SerializeField]
        public GameObject Canvas;

        [SerializeField]
        public SpriteRenderer Overlay;

        [SerializeField]
        public Text CenterText;
        [SerializeField]
        public Text HintText;
        [SerializeField]
        public Text BoldText;
        [SerializeField]
        public Button RestartButton;
        [SerializeField]
        public Button MainMenuButton;
        [SerializeField]
        public Button ContinueButton;

        private string centerTextStringDefault = "Paused";
        private string hintTextStringDefault = "(Press Esc to Resume)";
        private string BoldTextStringDefault = "Game Over\r\n\r\nToo many people wet\r\nthemselves in the bar\r\n";

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

        public void Open() {
            enabled = true;
            Canvas.SetActive(true);
        }
        public void Close() {
            enabled = false;
            Canvas.SetActive(false);
        }
        public void SwitchToBoldTextDisplay() {
            CenterText.gameObject.SetActive(false);
            HintText.gameObject.SetActive(false);
            BoldText.gameObject.SetActive(true);
        }
        public void SwitchToCenterTextDisplay() {
            CenterText.gameObject.SetActive(true);
            HintText.gameObject.SetActive(true);
            BoldText.gameObject.SetActive(false);
        }
        public void SetBoldTextDisplay(string text) {
            BoldText.text = text;
        }
        public void SetCenterTextDisplay(string text, string hint) {
            CenterText.text = text;
            HintText.text = hint;
        }
        public void EnableContinueButton(bool value) {
            ContinueButton.gameObject.SetActive(value);
        }
        public void SetUpButtons() {
            RestartButton.onClick.AddListener(GameController.controller.RestartCurrentNight);
            MainMenuButton.onClick.AddListener(GameController.controller.GoToMainMenu);
            ContinueButton.onClick.AddListener(GameController.controller.ContinueToNextNight);

            EnableContinueButton(false);
        }
        public void FadeOverlayToBlack() {
            float rate = 0.3f * Time.unscaledDeltaTime;
            Color current = Overlay.color;
            current.r = Math.Max(current.r - rate, 0f);
            current.g = Math.Max(current.g - rate, 0f);
            current.b = Math.Max(current.b - rate, 0f);
            current.a = Math.Min(current.a + rate, 1f);
            Overlay.color = current;
        }
        public bool FadeOverlayComplete() {
            return Overlay.color.r == 0f && Overlay.color.a == 255f;
        }

        public PauseMenu() {
        }
    }
}
