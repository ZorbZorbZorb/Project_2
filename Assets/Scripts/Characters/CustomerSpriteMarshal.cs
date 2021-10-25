using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Collections;

namespace Assets.Scripts.Characters {
    [Serializable]
    public class CustomerSpriteMarshal {

        public static Dictionary<char, CustomerSpriteMarshal> Marshals = new Dictionary<char, CustomerSpriteMarshal>();

        public readonly string Root;

        private Dictionary<CustomerDesperationState, Sprite> DesperationSpriteLookup;
        private Dictionary<CustomerDesperationState, Sprite> DesperationSeatSpriteLookup;
        private Dictionary<CustomerActionState, Sprite> PantsSpriteLookup;
        private Dictionary<CustomerActionState, Sprite> PantsSidewaysSpriteLookup;
        private Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, Sprite>> ActionStateSpriteLookup;
        private Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, Sprite>> ActionStateSidewaysSpriteLookup;

        public Sprite GetSprite<T>(CustomerDesperationState desperationState, CustomerActionState actionState, T interactable, bool forceStandingSprite)
            where T : CustomerInteractable {

            if ( !forceStandingSprite && interactable != null && interactable.ChangesCustomerSprite ) {
                // The only time action state can be none while interacting with something where you aren't forcing standing is seated
                //   on a stool so do we only need one of these checks in the below if?
                if ( interactable.IType == CustomerInteractable.InteractableType.Seat && actionState == CustomerActionState.None ) {
                    return DesperationSeatSpriteLookup[desperationState];
                }
                else {
                    return GetActionSprite(actionState, interactable);
                }
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

        public static void NewMarshal(char id, string root) {
            if ( Marshals.ContainsKey(id) ) {
                Debug.LogError($"Marshal '{id}' already exists!");
                return;
            }
            try {
                CustomerSpriteMarshal marshal = new CustomerSpriteMarshal(root);
                Marshals.Add(id, marshal);
            }
            catch ( Exception e ) {
                Debug.LogError($"Exception creating customer sprite marshal '{id}': '{e.Message}'");
            }
        }
        private CustomerSpriteMarshal(string root) {
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
