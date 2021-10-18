using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ClickableObject : MonoBehaviour {
    [SerializeField] public Action OnClick;
    private void OnMouseDown() {
        OnClick();
    }
}
