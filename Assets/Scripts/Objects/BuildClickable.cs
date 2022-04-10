using System;
using UnityEngine;

namespace Assets.Scripts.Objects {
    [Serializable]
    public class BuildClickable : MonoBehaviour {
        [SerializeField] public TMPro.TMP_Text Text;
        [SerializeField] public Action OnClick;
        public SpriteRenderer SRenderer;
        private void OnMouseDown() {
            OnClick();
        }
    }
}
