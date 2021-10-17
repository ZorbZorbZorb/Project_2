using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Objects {
    public abstract class Relief : CustomerInteractable {
        public Customer InUseBy;
        public Customer ReservedBy;
        public override Collections.Location CustomerLocation => Collections.Location.Relief;
        // Confucius says, Man whos dick is out of pants, cannot piss in pants
        public override bool CanWetHere => false;

        [SerializeField] protected internal Sprite SpritePantsDownM;
        [SerializeField] protected internal Sprite SpritePantsUpM;
        [SerializeField] protected internal Sprite SpritePeeingM;
        [SerializeField] protected internal Sprite SpritePantsDownF;
        [SerializeField] protected internal Sprite SpritePantsUpF;
        [SerializeField] protected internal Sprite SpritePeeingF;
        [SerializeField] protected internal Sprite SpriteSidewaysPantsDownM;
        [SerializeField] protected internal Sprite SpriteSidewaysPantsUpM;
        [SerializeField] protected internal Sprite SpriteSidewaysPeeingM;
        [SerializeField] protected internal Sprite SpriteSidewaysPantsDownF;
        [SerializeField] protected internal Sprite SpriteSidewaysPantsUpF;
        [SerializeField] protected internal Sprite SpriteSidewaysPeeingF;
        protected internal Dictionary<Collections.CustomerActionState, Sprite> SpriteLookupM = null;
        protected internal Dictionary<Collections.CustomerActionState, Sprite> SpriteLookupF = null;
        protected internal override void BuildSpriteLookup() {
            // Yeah these could be ternary but that makes it harder to understand when you try to read it.
            //   If sideways, use the sideways sprites to build the lookup, otherwise use the normal ones.
            if ( Sideways ) {
                SpriteLookupM = new Dictionary<Collections.CustomerActionState, Sprite>() {
                    { Collections.CustomerActionState.PantsDown, SpriteSidewaysPantsDownM },
                    { Collections.CustomerActionState.PantsUp, SpriteSidewaysPantsUpM },
                    { Collections.CustomerActionState.Peeing, SpriteSidewaysPeeingM }
                };
                SpriteLookupF = new Dictionary<Collections.CustomerActionState, Sprite>() {
                    { Collections.CustomerActionState.PantsDown, SpriteSidewaysPantsDownF },
                    { Collections.CustomerActionState.PantsUp, SpriteSidewaysPantsUpF },
                    { Collections.CustomerActionState.Peeing, SpriteSidewaysPeeingF }
                };
            }
            else {
                SpriteLookupM = new Dictionary<Collections.CustomerActionState, Sprite>() {
                    { Collections.CustomerActionState.PantsDown, SpritePantsDownM },
                    { Collections.CustomerActionState.PantsUp, SpritePantsUpM },
                    { Collections.CustomerActionState.Peeing, SpritePeeingM }
                };
                SpriteLookupF = new Dictionary<Collections.CustomerActionState, Sprite>() {
                    { Collections.CustomerActionState.PantsDown, SpritePantsDownF },
                    { Collections.CustomerActionState.PantsUp, SpritePantsUpF },
                    { Collections.CustomerActionState.Peeing, SpritePeeingF }
                };
            }
        }

        private void Start() {
            MainSRenderer.sprite = Sideways ? MainSpritesSideways[0] : MainSprites[0];
        }
    }
}
