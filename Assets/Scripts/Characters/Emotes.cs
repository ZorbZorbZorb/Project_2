using UnityEngine;

namespace Assets.Scripts.Characters {
    [System.Serializable]
    public class Emotes {
        // https://i.imgur.com/Zul9DoY_d.webp?maxwidth=760&fidelity=grand
        [SerializeField]
        public SpriteRenderer SpriteRenderer;
        public Emote current = null;
        public float? remaining = null;
        public void Update() {
            if ( current != null ) {
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
        }
        public void Emote(Emote emote) => Emote(emote, null);
        public void Emote(Emote emote, float? time) {
            Debug.Log("Rendering " + emote == null ? "[NULL]" : emote.Path);
            current = emote;
            SpriteRenderer.sprite = emote?.Sprite;
            if ( time != null && emote != null ) {
                remaining = time;
            }
            else {
                remaining = null;
            }
        }

        public Emotes(SpriteRenderer spriteRenderer) {
            SpriteRenderer = spriteRenderer;
        }
    }
}
