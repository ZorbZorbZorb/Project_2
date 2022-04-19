using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Customers {
    [Serializable]
    public class Emotes {
        public SpriteRenderer EmoteSpriteRenderer;

        public GameObject BladderDisplay;
        public Canvas Canvas;
        public Text BladderAmountText;
        public SpriteRenderer SpriteBladderTop;
        public SpriteRenderer SpriteBladderBottom;
        public RectTransform BladderCircleTransform;

        // Used to make the bladder display larger or smaller.
        public double AverageMax = 700;

        const double bladderWidthCalculationFactor1 = -0.6d;
        const double bladderWidthCalculationFactor2 = 1.2d;
        const double bladderWidthCalculationFactor3 = 1.8d;
        const double bladderHeightCalculationFactor1 = 0.2;

        private Color bladderColorNormal;

        public Emote currentEmote = null;
        public float? remaining = null;
        private bool bladderCircleActive = false;
        public Customer Customer;
        private bool flipped = false;
        public void Update() {
            // Update emotes
            EmoteUpdate();
            // Update bladder display
            BladderDisplayUpdate();
            // Flip emotes if need be
            if ( Customer.Occupying != null && Customer.Occupying.Facing == Objects.Orientation.East ) {
                if ( !flipped ) {
                    FlipEmoteX();
                }
            }
            else if ( flipped ) {
                FlipEmoteX();
            }

            void FlipEmoteX() {
                flipped = !flipped;
                //BladderDisplay.transform.localPosition *= new Vector2(-1, 1);
                EmoteSpriteRenderer.transform.localPosition *= new Vector2(-1, 1);
                EmoteSpriteRenderer.flipX = flipped;
                //var eulerAngles = BladderDisplay.transform.localRotation.eulerAngles;
                //EmoteSpriteRenderer.transform.localRotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y + 180, eulerAngles.z);
            }
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
            if ( bladderCircleActive ) {
                double width;
                double height;
                double value;
                double factor;
                double scaleX = 160f;
                double scaleY = 160f;

                // Note, Code breaks down at bladder sizes over AverageMax * 2.
                factor = Customer.Bladder.Max / AverageMax;
                double multiplier = ( ( ( factor / 2 ) * Math.PI ) - Math.PI / 2 ) * 0.5;
                multiplier = Math.Max(0.5, Math.Min(1.5, multiplier));

                value = Math.Max(0.15, Math.Min(1, Customer.Bladder.Fullness));
                width = ( Math.Sin(( ( value * 1.2d ) + bladderWidthCalculationFactor1 ) * 2.5) + bladderWidthCalculationFactor2 ) / bladderWidthCalculationFactor3;
                height = Math.Max(0.15, Customer.Bladder.Fullness / 1.1 + bladderHeightCalculationFactor1);

                // Update the size
                double x = height * multiplier * scaleX;
                double y = width * multiplier * scaleY;
                BladderCircleTransform.localScale = new Vector3((float)x, (float)y);

                // Update text display
                BladderAmountText.text = ((int)Customer.Bladder.Amount).ToString();

                // Fade the display if too low amount
                if ( Customer.Bladder.Amount < 250 ) {
                    Color colorText = new Color(1f, 1f, 1f, 0f);
                    Color color = new Color(SpriteBladderTop.color.r, SpriteBladderTop.color.g, SpriteBladderTop.color.g, 0f);
                    BladderAmountText.color = colorText;
                    SpriteBladderTop.color = color;
                    SpriteBladderBottom.color = color;
                }
                else if ( Customer.Bladder.Amount < 300 ) {
                    float a = ( (float)Customer.Bladder.Amount - 250f ) / 50f;
                    Color colorText = new Color(0f, 0f, 0f, Mathf.Clamp(a, 0f, 1f));
                    Color color = bladderColorNormal * new Vector4(1f, 1f, 1f, Mathf.Clamp(a, 0f, 1f));
                    BladderAmountText.color = colorText;
                    SpriteBladderTop.color = color;
                    SpriteBladderBottom.color = color;
                }
                else {
                    SpriteBladderTop.color = bladderColorNormal;
                    SpriteBladderBottom.color = bladderColorNormal;
                    BladderAmountText.color = new Color(0f, 0f, 0f, 1f);
                }
            }
        }
        public void ShowBladderCircle(bool value) {
            if ( bladderCircleActive != value ) {
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
            bladderColorNormal = SpriteBladderTop.color;
            BladderDisplay.SetActive(false);
            bladderCircleActive = false;
            Canvas.sortingLayerName = "AboveBlockSight";
        }
    }
}
