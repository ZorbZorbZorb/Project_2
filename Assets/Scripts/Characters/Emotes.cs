using UnityEngine;

namespace Assets.Scripts.Characters {
    public class Emotes {
        // https://i.imgur.com/Zul9DoY_d.webp?maxwidth=760&fidelity=grand
        [SerializeField]
        public SpriteRenderer SpriteRenderer;
        public Emote current = null;
        public float remaining = float.NaN;
        public void Update() {
            if ( current != null ) {
                // Abort if emote is permanent
                if ( remaining == float.NaN ) {
                    return;
                }
                // Decrease timer if the timer isnt expired
                else if ( remaining > 0f ) {
                    remaining -= Time.deltaTime;
                }
                // Stop rendering, timer elapsed
                else {
                    Render(null);
                }
            }
        }
        public void Emote(Emote emote) => Emote(emote, float.NaN);
        public void Emote(Emote emote, float time) {
            Render(emote?.Sprite);
            if ( time != float.NaN ) {
                remaining = time;
            }
        }
        private void Render(Sprite sprite) {
            if ( sprite == null ) {
                current = null;
                SpriteRenderer.enabled = false;
            }
            else {
                SpriteRenderer.sprite = sprite;
                SpriteRenderer.enabled = true;
            }
        }

        public Emotes(SpriteRenderer spriteRenderer) {
            SpriteRenderer = spriteRenderer;
        }
    }
}
