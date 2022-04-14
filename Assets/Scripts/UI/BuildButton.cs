using Assets.Scripts.Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI {
    public class BuildButton : MonoBehaviour {
        public Button Button;
        public Image Image;
        public TMP_Text Text;
        /// <summary>Reference back to the build clickable that created this option</summary>
        public BuildClickable BuildClickable;

        // Change parent sprite
        public void OnMouseEnter() {
            BuildClickable.SRenderer.sprite = Image.sprite;
        }
        // Reset parent sprite
        public void OnMouseExit() {
            BuildClickable.SRenderer.sprite = Image.sprite;
        }
        // Reset parent sprite
        public void OnDestroy() {
            
        }
    }
}
