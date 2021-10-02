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
        public Text CenterText;
        [SerializeField]
        public Text HintText;
        [SerializeField]
        public Text BoldText;
        [SerializeField]
        public Button RestartButton;
        [SerializeField]
        public Button MainMenuButton;

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
        public void SetUpButtons() {
            RestartButton.onClick.AddListener(GameController.controller.RestartCurrentNight);
            MainMenuButton.onClick.AddListener(GameController.controller.GoToMainMenu);
        }

        public PauseMenu() {
        }
    }
}
