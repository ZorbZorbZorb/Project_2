using System;
using UnityEngine;

[Serializable]
public class BuildClickable : MonoBehaviour {
    [SerializeField] public TMPro.TMP_Text Text;
    [SerializeField] public Action OnClick;
    private void OnMouseDown() {
        OnClick();
    }
}