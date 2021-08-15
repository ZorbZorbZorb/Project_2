using UnityEngine;

namespace Assets.Scripts.Characters {
    [System.Serializable]
    public class Emotes {
        // https://i.imgur.com/Zul9DoY_d.webp?maxwidth=760&fidelity=grand
        [SerializeField]
        public RectTransform BladderCircleTransform;
        [SerializeField]
        public SpriteRenderer EmoteSpriteRenderer;
        public Emote currentEmote = null;
        public float? remaining = null;
        private bool bladderCircleActive = false;
        public readonly Customer Customer;
        public void Update() {
            // Update emotes
            if ( currentEmote != null ) {
                // Abort if emote is permanent
                if ( remaining == null) {
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
            // Update bladder display
            if (bladderCircleActive) {
                double value = Customer.bladder.Percentage * 80;
                if (Customer.bladder.Percentage > 0.66) {
                    BladderCircleTransform.localScale = new Vector3((float)Customer.bladder.Percentage * 80f, (float)Customer.bladder.Percentage * 95f, 1);
                }
                else if (Customer.bladder.Percentage > 0.33) {
                    BladderCircleTransform.localScale = new Vector3((float)Customer.bladder.Percentage * 80f, (float)Customer.bladder.Percentage * 80f, 1);
                }
                else {
                    BladderCircleTransform.localScale = new Vector3((float)Customer.bladder.Percentage * 80f, (float)Customer.bladder.Percentage * 60f, 1);
                }
            }
        }
        public void ShowBladderCircle(bool value) {
            bladderCircleActive = value;
            BladderCircleTransform.gameObject.SetActive(value);
        }
        public void Emote(Emote emote) => Emote(emote, null);
        public void Emote(Emote emote, float? time) {
            Debug.Log("Rendering " + emote == null ? "[NULL]" : emote.Path);
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
