﻿using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Collections;
using Random = UnityEngine.Random;
using UnityEngine.Animations;

namespace Assets.Scripts.Characters {
    [Serializable]
    public class CustomerAnimator {

        private readonly Customer customer;
        private readonly SpriteRenderer renderer;
        private readonly Animator animator;
        private readonly CustomerSpriteController marshal;

        private readonly string[] clipNames;
        
        private string animationStateName;
        private string animationStateNameLast;

        public string AnimationStateNameLast { get => animationStateNameLast; }
        public string AnimationStateName { get => animationStateName; }

        float accTest = 0f;
        public void Update() {
            animationStateName = GetAnimation(customer.DesperationState, customer.ActionState, customer.Occupying, !customer.AtDestination());
            
            // Set animation or sprite
            if (animationStateNameLast != animationStateName) {
                animationStateNameLast = animationStateName;
                if (clipNames.Contains(animationStateName)) {
                    animator.enabled = true;
                    animator.Play(animationStateName);
                }
                else {
                    animator.enabled = false;
                    renderer.sprite = marshal.GetSprite(customer.DesperationState, customer.ActionState, customer.Occupying, !customer.AtDestination());
                }
            }


            // Is this state's animation controlled manually?
            switch (customer.DesperationState) {
                case CustomerDesperationState.State5:
                    animator.Play(animationStateName, 0, customer.bladder.NormalizedPercentEmptied);
                    break;
            }

            // Sprite shaking to show desperation
            // TODO: Perhaps shake more or less when shy, maybe have shaking be the true desperation state?
            // Notice: The sprite is parented to a customer gameobject and is not a part of it. this.gameObject.transform can be used to re-parent it.
            // Do not run if paused.
            if ( !GameController.GamePaused ) {
                switch ( customer.DesperationState ) {
                    case CustomerDesperationState.State4:
                        if ( Time.frameCount % 4 == 0 ) {
                            renderer.transform.position = customer.gameObject.transform.position + new Vector3(Random.Range(-2, 3), Random.Range(-2, 3), 0);
                        }
                        break;
                    case CustomerDesperationState.State3:
                        if ( Time.frameCount % 40 == 0 ) {
                            renderer.transform.position = customer.gameObject.transform.position + new Vector3(Random.Range(-1, 2), 0, 0);
                        }
                        break;
                    default:
                        renderer.transform.position = customer.gameObject.transform.position;
                        break;
                }
            }
        }

        public CustomerAnimator(Customer _customer, SpriteRenderer _renderer, Animator _animator, CustomerSpriteController _marshal) {
            customer = _customer;
            renderer = _renderer;
            animator = _animator;
            marshal = _marshal;

            animator.speed = 0.2f;

            clipNames = animator.runtimeAnimatorController.animationClips
                .Select(x => x.name)
                .ToArray();
        }

        #region Static methods for looking up animations
        private static readonly Dictionary<CustomerDesperationState, string> DesperationAnimationClipLookup;
        private static readonly Dictionary<CustomerDesperationState, string> DesperationSeatAnimationClipLookup;
        private static readonly Dictionary<CustomerActionState, string> PantsAnimationClipLookup;
        private static readonly Dictionary<CustomerActionState, string> PantsSidewaysAnimationClipLookup;
        private static readonly Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, string>> ActionStateAnimationClipLookup;
        private static readonly Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, string>> ActionStateSidewaysAnimationClipLookup;

        public static string GetAnimation<T>(CustomerDesperationState desperationState, CustomerActionState actionState, T interactable, bool forceStandingSprite)
            where T : CustomerInteractable {

            if ( !forceStandingSprite && interactable != null && interactable.ChangesCustomerSprite ) {
                if ( interactable.IType == CustomerInteractable.InteractableType.Seat && ( actionState == CustomerActionState.None || actionState == CustomerActionState.Wetting ) ) {
                    return DesperationSeatAnimationClipLookup[desperationState];
                }
                else {
                    return GetActionAnimation(actionState, interactable);
                }
            }
            else {
                return DesperationAnimationClipLookup[desperationState];
            }
        }
        private static string GetActionAnimation<T>(CustomerActionState state, T interactable) where T : CustomerInteractable {
            Dictionary<CustomerActionState, string> lookup;
            switch ( state ) {
                case CustomerActionState.PantsDown:
                case CustomerActionState.PantsUp:
                    lookup = interactable.Sideways ? PantsSidewaysAnimationClipLookup : PantsAnimationClipLookup;
                    break;
                default:
                    lookup = interactable.Sideways ? ActionStateSidewaysAnimationClipLookup[interactable.IType] : ActionStateAnimationClipLookup[interactable.IType];
                    break;
            }

            return lookup[state];
        }
        static CustomerAnimator() {
            DesperationAnimationClipLookup = new Dictionary<CustomerDesperationState, string>() {
                { CustomerDesperationState.State0, "desp_state_0" },
                { CustomerDesperationState.State1, "desp_state_1" },
                { CustomerDesperationState.State2, "desp_state_2" },
                { CustomerDesperationState.State3, "desp_state_3" },
                { CustomerDesperationState.State4, "desp_state_4" },
                { CustomerDesperationState.State5, "desp_state_5" },
                { CustomerDesperationState.State6, "desp_state_6" }
            };
            DesperationSeatAnimationClipLookup = new Dictionary<CustomerDesperationState, string>() {
                { CustomerDesperationState.State0, "desp_state_stool_0" },
                { CustomerDesperationState.State1, "desp_state_stool_1" },
                { CustomerDesperationState.State2, "desp_state_stool_2" },
                { CustomerDesperationState.State3, "desp_state_stool_3" },
                { CustomerDesperationState.State4, "desp_state_stool_4" },
                { CustomerDesperationState.State5, "desp_state_stool_5" },
                { CustomerDesperationState.State6, "desp_state_stool_6" }
            };
            PantsAnimationClipLookup = new Dictionary<CustomerActionState, string>() {
                { CustomerActionState.PantsDown, "pants_down" },
                { CustomerActionState.PantsUp, "pants_up" }
            };
            PantsSidewaysAnimationClipLookup = new Dictionary<CustomerActionState, string>() {
                { CustomerActionState.PantsDown, "pants_down_side" },
                { CustomerActionState.PantsUp, "pants_up_side"}
            };
            ActionStateAnimationClipLookup = new Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, string>>();
            ActionStateSidewaysAnimationClipLookup = new Dictionary<CustomerInteractable.InteractableType, Dictionary<CustomerActionState, string>>();

            // Front facing sink
            ActionStateAnimationClipLookup.Add(CustomerInteractable.InteractableType.Sink, new Dictionary<CustomerActionState, string>() {
                { CustomerActionState.Peeing, "peeing_sink"},
                {CustomerActionState.WashingHands, "wash"}
            });
            // Front facing toilet
            ActionStateAnimationClipLookup.Add(CustomerInteractable.InteractableType.Toilet, new Dictionary<CustomerActionState, string>() {
                { CustomerActionState.Peeing, "peeing_toilet" },
            });
            // Front facing urinal
            ActionStateAnimationClipLookup.Add(CustomerInteractable.InteractableType.Urinal, new Dictionary<CustomerActionState, string>() {
                { CustomerActionState.Peeing, "peeing_urinal" },
            });
            // Side facing urinal
            ActionStateSidewaysAnimationClipLookup.Add(CustomerInteractable.InteractableType.Urinal, new Dictionary<CustomerActionState, string>() {
                { CustomerActionState.Peeing, "peeing_urinal_side" },
            });
        }
        #endregion
    }
}