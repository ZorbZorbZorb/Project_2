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
        [SerializeField] public double ws = -0.6d;
        [SerializeField] public double wa = 1.3d;
        [SerializeField] public double wd = 2d;
        [SerializeField] public double hp = 0.2;

        public Emote currentEmote = null;
        public float? remaining = null;
        private bool bladderCircleActive = true;
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
            if ( true || bladderCircleActive ) {
                double width;
                double height;
                double value;
                value = Math.Max(0.15, Math.Min(1, Customer.bladder.Percentage));

                width = (Math.Sin((value + ws ) * Math.PI) + wa) / wd;
                height = Math.Max(0.15, Customer.bladder.Percentage / 1.3 + hp);

                BladderCircleTransform.localScale = new Vector3((float)height * 80f, (float)width * 80f);

                /*
                value = Math.Max(0.1, Math.Min(1, Customer.bladder.Percentage));

                width = (value - ( Math.PI / 6d )) * Math.PI;
                width = Math.Sin(width);
                width = ( width + 1.5d ) / 2d;
                width = Math.Min(1.2, width);

                height = Math.Max(0.1, Customer.bladder.Percentage * 1.5);

                BladderCircleTransform.localScale = new Vector3((float)width * 60f, (float)height * 60f);
                 */

                //double value = Customer.bladder.Percentage * 80;
                //if ( Customer.bladder.Percentage > 0.66 ) {
                //    BladderCircleTransform.localScale = new Vector3((float)Customer.bladder.Percentage * 80f, (float)Customer.bladder.Percentage * 95f, 1);
                //}
                //else if ( Customer.bladder.Percentage > 0.33 ) {
                //    BladderCircleTransform.localScale = new Vector3((float)Customer.bladder.Percentage * 80f, (float)Customer.bladder.Percentage * 80f, 1);
                //}
                //else {
                //    BladderCircleTransform.localScale = new Vector3((float)Customer.bladder.Percentage * 80f, (float)Customer.bladder.Percentage * 60f, 1);
                //}
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
