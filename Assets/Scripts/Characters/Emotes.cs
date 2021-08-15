using System;
using UnityEngine;

namespace Assets.Scripts.Characters {
    [Serializable]
    public class Emotes {
        // https://i.imgur.com/Zul9DoY_d.webp?maxwidth=760&fidelity=grand
        [SerializeField]
        public RectTransform BladderCircleTransform;
        [SerializeField]
        public SpriteRenderer EmoteSpriteRenderer;
        [SerializeField] public double bladderWidthCalculationFactor1 = -0.6d;
        [SerializeField] public double bladderWidthCalculationFactor2 = 1.3d;
        [SerializeField] public double bladderWidthCalculationFactor3 = 2d;
        [SerializeField] public double bladderHeightCalculationFactor1 = 0.2;

        public Emote currentEmote = null;
        public float? remaining = null;
        private bool bladderCircleActive = false;
        public readonly Customer Customer;
        public void Update() {
            // Update emotes
            EmoteUpdate();
            // Update bladder display
            BladderDisplayUpdate();
        }
        void EmoteUpdate() {
            // Update emotes
            if ( currentEmote != null ) {
                // Abort if emote is permanent
                if ( remaining == null ) {
                    return;
                }
                // Decrease timer if the timer isnt expired
                else if ( remaining > 0f ) {
                    remaining -= Time.deltaTime;
                }
                // Stop rendering, timer elapsed
                else {
                    Emote(null);
                }
            }
        }
        void BladderDisplayUpdate() {
            // Update bladder display
            if (bladderCircleActive) {
                double width;
                double height;
                double value;
                value = Math.Max(0.15, Math.Min(1, Customer.bladder.Percentage));

                width = (Math.Sin((value + bladderWidthCalculationFactor1 ) * Math.PI) + bladderWidthCalculationFactor2) / bladderWidthCalculationFactor3;
                height = Math.Max(0.15, Customer.bladder.Percentage / 1.3 + bladderHeightCalculationFactor1);

                BladderCircleTransform.localScale = new Vector3((float)height * 80f, (float)width * 80f);
            }
        }
        public void ShowBladderCircle(bool value) {
            bladderCircleActive = value;
            BladderCircleTransform.gameObject.SetActive(value);
        }
        public void Emote(Emote emote) => Emote(emote, null);
        public void Emote(Emote emote, float? time) {
            bool emoteNull = emote == null;
            Debug.Log("Rendering " + (emoteNull ? "[NULL]" : emote.Path));
            currentEmote = emote;
            EmoteSpriteRenderer.sprite = emote?.Sprite;
            if ( time != null && emote != null ) {
                remaining = time;
            }
            else {
                remaining = null;
            }
        }

        public Emotes(Customer customer, SpriteRenderer emoteSpriteRenderer, RectTransform bladderCircleTransform) {
            Customer = customer;
            EmoteSpriteRenderer = emoteSpriteRenderer;
            BladderCircleTransform = bladderCircleTransform;
        }
    }
}
