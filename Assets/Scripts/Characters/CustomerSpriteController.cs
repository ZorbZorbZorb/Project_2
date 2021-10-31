using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Collections;

namespace Assets.Scripts.Characters {
    [Serializable]
    public class CustomerSpriteController {

        public static Dictionary<char, CustomerSpriteController> Controller = new Dictionary<char, CustomerSpriteController>();

        public readonly string Root;

        private readonly Dictionary<CustomerDesperationState, Sprite> DesperationSpriteLookup;
        private readonly Dictionary<CustomerDesperationState, Sprite> DesperationSeatSpriteLookup;
        private readonly Dictionary<CustomerActionState, Sprite> PantsSpriteLookup;
        private readonly Dictionary<CustomerActionState, Sprite> PantsSidewaysSpriteLookup;
        private readonly Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, Sprite>> ActionStateSpriteLookup;
        private readonly Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, Sprite>> ActionStateSidewaysSpriteLookup;

        public Sprite GetSprite<T>(CustomerDesperationState desperationState, CustomerActionState actionState, T interactable, bool forceStandingSprite)
            where T : CustomerInteractable {

            if ( !forceStandingSprite && interactable != null && interactable.ChangesCustomerSprite ) {
                // The only time action state can be none while interacting with something where you aren't forcing standing is seated
                //   on a stool so do we only need one of these checks in the below if?
                return interactable.IType == CustomerInteractable.InteractableType.Seat && ( actionState == CustomerActionState.None || actionState == CustomerActionState.Wetting )
                    ? DesperationSeatSpriteLookup[desperationState]
                    : GetActionSprite(actionState, interactable);
            }
            else {
                return DesperationSpriteLookup[desperationState];
            }
        }
        private Sprite GetActionSprite<T>(CustomerActionState state, T interactable) where T : CustomerInteractable {
            Dictionary<CustomerActionState, Sprite> lookup;
            switch ( state ) {
                case CustomerActionState.PantsDown:
                case CustomerActionState.PantsUp:
                    lookup = interactable.Sideways ? PantsSidewaysSpriteLookup : PantsSpriteLookup;
                    break;
                default:
                    lookup = interactable.Sideways ? ActionStateSidewaysSpriteLookup[interactable.IType] : ActionStateSpriteLookup[interactable.IType];
                    break;
            }

            return lookup[state];
        }

        public static void NewController(char id, string root) {
            if ( Controller.ContainsKey(id) ) {
                Debug.LogError($"Marshal '{id}' already exists!");
                return;
            }
            try {
                CustomerSpriteController marshal = new CustomerSpriteController(root);
                Controller.Add(id, marshal);
            }
            catch ( Exception e ) {
                Debug.LogError($"Exception creating customer sprite marshal '{id}': '{e.Message}'");
            }
        }
        private CustomerSpriteController(string root) {
            Root = root;

            DesperationSpriteLookup = new Dictionary<CustomerDesperationState, Sprite>() {
                { CustomerDesperationState.State0, Resources.Load<Sprite>($"{root}/stand/desp_state_0") },
                { CustomerDesperationState.State1, Resources.Load<Sprite>($"{root}/stand/desp_state_1") },
                { CustomerDesperationState.State2, Resources.Load<Sprite>($"{root}/stand/desp_state_2") },
                { CustomerDesperationState.State3, Resources.Load<Sprite>($"{root}/stand/desp_state_3") },
                { CustomerDesperationState.State4, Resources.Load<Sprite>($"{root}/stand/desp_state_4") },
                { CustomerDesperationState.State5, Resources.Load<Sprite>($"{root}/stand/desp_state_5") },
                { CustomerDesperationState.State6, Resources.Load<Sprite>($"{root}/stand/desp_state_6") }
            };
            DesperationSeatSpriteLookup = new Dictionary<CustomerDesperationState, Sprite>() {
                { CustomerDesperationState.State0, Resources.Load<Sprite>($"{root}/sit/desp_state_stool_0") },
                { CustomerDesperationState.State1, Resources.Load<Sprite>($"{root}/sit/desp_state_stool_1") },
                { CustomerDesperationState.State2, Resources.Load<Sprite>($"{root}/sit/desp_state_stool_2") },
                { CustomerDesperationState.State3, Resources.Load<Sprite>($"{root}/sit/desp_state_stool_3") },
                { CustomerDesperationState.State4, Resources.Load<Sprite>($"{root}/sit/desp_state_stool_4") },
                { CustomerDesperationState.State5, Resources.Load<Sprite>($"{root}/sit/desp_state_stool_5") },
                { CustomerDesperationState.State6, Resources.Load<Sprite>($"{root}/sit/desp_state_stool_6") }
            };
            PantsSpriteLookup = new Dictionary<CustomerActionState, Sprite>() {
                { CustomerActionState.PantsDown, Resources.Load<Sprite>($"{root}/act/pants_down") },
                { CustomerActionState.PantsUp, Resources.Load<Sprite>($"{root}/act/pants_up") }
            };
            PantsSidewaysSpriteLookup = new Dictionary<CustomerActionState, Sprite>() {
                { CustomerActionState.PantsDown, Resources.Load<Sprite>($"{root}/act/pants_down_side") },
                { CustomerActionState.PantsUp, Resources.Load<Sprite>($"{root}/act/pants_up_side") }
            };
            ActionStateSpriteLookup = new Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, Sprite>>();
            ActionStateSidewaysSpriteLookup = new Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, Sprite>>();

            // Front facing sink
            ActionStateSpriteLookup.Add(CustomerInteractable.InteractableType.Sink, new Dictionary<CustomerActionState, Sprite>() {
                { CustomerActionState.Peeing, Resources.Load<Sprite>($"{root}/act/peeing_sink") },
                {CustomerActionState.WashingHands, Resources.Load<Sprite>($"{root}/act/wash") }
            });
            // Front facing toilet
            ActionStateSpriteLookup.Add(CustomerInteractable.InteractableType.Toilet, new Dictionary<CustomerActionState, Sprite>() {
                { CustomerActionState.Peeing, Resources.Load<Sprite>($"{root}/act/peeing_toilet") },
            });
            // Front facing urinal
            ActionStateSpriteLookup.Add(CustomerInteractable.InteractableType.Urinal, new Dictionary<CustomerActionState, Sprite>() {
                { CustomerActionState.Peeing, Resources.Load<Sprite>($"{root}/act/peeing_urinal") },
            });
            // Side facing urinal
            ActionStateSidewaysSpriteLookup.Add(CustomerInteractable.InteractableType.Urinal, new Dictionary<CustomerActionState, Sprite>() {
                { CustomerActionState.Peeing, Resources.Load<Sprite>($"{root}/act/peeing_urinal_side") },
            });
        }
    }
}
