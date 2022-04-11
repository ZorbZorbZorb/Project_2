using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Customers {
    [Serializable]
    public class Emotes {
        // https://i.imgur.com/Zul9DoY_d.webp?maxwidth=760&fidelity=grand
        [SerializeField]
        public RectTransform BladderCircleTransform;
        [SerializeField]
        public SpriteRenderer EmoteSpriteRenderer;
        [SerializeField]
        public Text BladderAmountText;
        public Canvas Canvas;
        public GameObject BladderDisplay;
        const double bladderWidthCalculationFactor1 = -0.6d;
        const double bladderWidthCalculationFactor2 = 1.2d;
        const double bladderWidthCalculationFactor3 = 1.8d;
        const double bladderHeightCalculationFactor1 = 0.2;

        public Emote currentEmote = null;
        public float? remaining = null;
        private bool bladderCircleActive = false;
        public Customer Customer;
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
                double factor;
                double multiplier;
                double scaleX = 80f;
                double scaleY = 80f;

                // Note, Code breaks down at bladder sizes over AverageMax * 2.
                factor = Customer.bladder.Max / Customer.bladder.AverageMax;
                multiplier = ( ( (factor / 2) * Math.PI ) - Math.PI / 2 ) * 0.5;
                multiplier = Math.Max(0.5, Math.Min(1.5, factor));

                value = Math.Max(0.15, Math.Min(1, Customer.bladder.Percentage));
                width = ( Math.Sin(( (value * 1.2d )+ bladderWidthCalculationFactor1 ) * 2.5) + bladderWidthCalculationFactor2 ) / bladderWidthCalculationFactor3;
                height = Math.Max(0.15, Customer.bladder.Percentage / 1.1 + bladderHeightCalculationFactor1);

                double x = height * multiplier * scaleX;
                double y = width * multiplier * scaleY;

                BladderCircleTransform.localScale = new Vector3((float)x, (float)y);

                // Update text display
                BladderAmountText.text = $"{Math.Round(Customer.bladder.Amount / 1000d, 1)}L";
            }
        }
        public void ShowBladderCircle(bool value) {
            if (bladderCircleActive != value) {
                bladderCircleActive = value;
                BladderDisplay.SetActive(value);
            }
        }
        public void Emote(Emote emote) => Emote(emote, null);
        public void Emote(Emote emote, float? time) {
            bool emoteNull = emote == null;
            //Debug.Log("Rendering " + (emoteNull ? "[NULL]" : emote.Path));
            currentEmote = emote;
            EmoteSpriteRenderer.sprite = emote?.Sprite;
            if ( time != null && emote != null ) {
                remaining = time;
            }
            else {
                remaining = null;
            }
        }

        public void Start() {
            Canvas.sortingLayerName = "UI"; 
            BladderDisplay.SetActive(false);
            bladderCircleActive = false;
        }
    }
}
