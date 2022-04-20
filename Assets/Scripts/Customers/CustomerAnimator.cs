using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Customers {
    [Serializable]
    public class CustomerAnimator {

        private readonly Customer customer;
        private readonly SpriteRenderer renderer;
        private readonly Animator animator;
        private readonly CustomerSpriteController marshal;

        private readonly string[] clipNames;

        private string animationStateName;
        private string animationStateNameLast;

        private float shakeAccumulator = 0f;

        private Color femaleColor = new(1f, 175f / 255f, 175f / 255f);
        private Color maleColor = new(175f / 255f, 175f / 255f, 255f);
        public Color Color => customer.Gender == 'm' ? maleColor : femaleColor;

        public string AnimationStateNameLast { get => animationStateNameLast; }
        public string AnimationStateName { get => animationStateName; }
        public void Update() {
            animationStateName = GetAnimation(customer.DesperationState, customer.CurrentAction, customer.Occupying, !customer.AtDestination);

            SetAnimationOrSprite();

            if ( animator.enabled ) {
                ManuallyControlAnimations();
            }

            ShakeSprite();

            // Set order in layer so customers on lower y axis cover customers on higher y axis (nearer takes precidence)
            renderer.sortingOrder = (int)-customer.transform.position.y;
        }
        private void SetAnimationOrSprite() {
            // Set animation or sprite
            if ( animationStateNameLast != animationStateName ) {
                animationStateNameLast = animationStateName;
                if ( clipNames.Contains(animationStateName) ) {
                    animator.enabled = true;
                    animator.Play(animationStateName);
                }
                else {
                    animator.enabled = false;
                    bool forceStanding = !customer.AtDestination;
                    renderer.sprite = marshal.GetSprite(customer.DesperationState, customer.CurrentAction, customer.Occupying, !customer.AtDestination, customer.IsWet);
                }
                // For debugging, we only have one animation set right now. Change the color of the animation so it matches the gender.
                renderer.color = Color;
            }
        }
        private void ManuallyControlAnimations() {
            // Is this state's animation controlled manually?
            switch ( customer.DesperationState ) {
                case CustomerDesperationState.State5:
                    animator.Play(animationStateName, 0, 1f - Math.Min(1f, customer.Bladder.Fullness));
                    break;
            }
        }
        private void ShakeSprite() {
            // Sprite shaking to show desperation
            // TODO: Perhaps shake more or less when shy, maybe have shaking be the true desperation state?
            // Notice: The sprite is parented to a customer gameobject and is not a part of it. this.gameObject.transform can be used to re-parent it.
            if ( customer.CurrentAction == CustomerAction.Leaking ) {
                shakeAccumulator += Time.deltaTime;
                if ( shakeAccumulator > 0.1f ) {
                    renderer.transform.position = customer.gameObject.transform.position + new Vector3(Random.Range(-1, 2) * 2, 0, 0);
                    shakeAccumulator -= 0.1f;
                }
            }
            else {
                switch ( customer.DesperationState ) {
                    case CustomerDesperationState.State4:
                        shakeAccumulator += Time.deltaTime;
                        if ( shakeAccumulator > 0.1f ) {
                            renderer.transform.position = customer.gameObject.transform.position + new Vector3(Random.Range(-1, 2), Random.Range(-1, 2), 0);
                            shakeAccumulator -= 0.1f;
                        }
                        break;
                    case CustomerDesperationState.State3:
                        shakeAccumulator += Time.deltaTime;
                        if ( shakeAccumulator > 0.15f ) {
                            renderer.transform.position = customer.gameObject.transform.position + new Vector3(Random.Range(-1, 2), 0, 0);
                            shakeAccumulator -= 0.15f;
                        }
                        break;
                    default:
                        renderer.transform.position = customer.gameObject.transform.position;
                        break;
                }
            }
        }
        public CustomerAnimator( Customer _customer, SpriteRenderer _renderer, Animator _animator, CustomerSpriteController _marshal ) {
            customer = _customer;
            renderer = _renderer;
            animator = _animator;
            marshal = _marshal;

            animator.speed = 0.2f;

            clipNames = animator.runtimeAnimatorController.animationClips
                .Select(x => x.name)
                .ToArray();

            // Change the colors just a bit.
            var colorMultiplier = new Vector3(Random.Range(0.75f, 1.15f), Random.Range(0.75f, 1.15f), Random.Range(0.9f, 1.1f));
            femaleColor.r = Math.Min(femaleColor.r * colorMultiplier.x, 1f);
            femaleColor.g = Math.Min(femaleColor.g * colorMultiplier.y, 1f);
            femaleColor.b = Math.Min(femaleColor.b * colorMultiplier.z, 1f);
            colorMultiplier = new Vector3(Random.Range(0.9f, 1.1f), Random.Range(0.75f, 1.15f), Random.Range(0.75f, 1.15f));
            maleColor.r = Math.Min(maleColor.r * colorMultiplier.x, 1f);
            maleColor.g = Math.Min(maleColor.g * colorMultiplier.y, 1f);
            maleColor.b = Math.Min(maleColor.b * colorMultiplier.z, 1f);
        }

        #region Static methods for looking up animations
        private static readonly Dictionary<CustomerDesperationState, string> DesperationAnimationClipLookup;
        private static readonly Dictionary<CustomerDesperationState, string> DesperationSeatAnimationClipLookup;
        private static readonly Dictionary<CustomerAction, string> PantsAnimationClipLookup;
        private static readonly Dictionary<CustomerAction, string> PantsSidewaysAnimationClipLookup;
        private static readonly Dictionary<InteractableType, Dictionary<CustomerAction, string>> ActionStateAnimationClipLookup;
        private static readonly Dictionary<InteractableType, Dictionary<CustomerAction, string>> ActionStateSidewaysAnimationClipLookup;

        public static string GetAnimation<T>( CustomerDesperationState desperationState, CustomerAction actionState, T interactable, bool forceStandingSprite )
            where T : CustomerInteractable {
            if ( !forceStandingSprite && interactable != null && interactable.ChangesCustomerSprite ) {
                if ( interactable.IType == InteractableType.Seat && (actionState == CustomerAction.None || actionState == CustomerAction.Wetting) ) {
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
        private static string GetActionAnimation<T>( CustomerAction state, T interactable ) where T : CustomerInteractable {
            Dictionary<CustomerAction, string> lookup;
            switch ( state ) {
                case CustomerAction.PantsDown:
                case CustomerAction.PantsUp:
                    lookup = interactable.Alignment == Alignment.Vertical
                        ? PantsAnimationClipLookup
                        : PantsSidewaysAnimationClipLookup;
                    break;
                default:
                    lookup = interactable.Alignment == Alignment.Vertical
                        ? ActionStateAnimationClipLookup[interactable.IType]
                        : ActionStateSidewaysAnimationClipLookup[interactable.IType];
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
                //{ CustomerDesperationState.State6, "desp_state_6" }
            };
            DesperationSeatAnimationClipLookup = new Dictionary<CustomerDesperationState, string>() {
                { CustomerDesperationState.State0, "desp_state_stool_0" },
                { CustomerDesperationState.State1, "desp_state_stool_1" },
                { CustomerDesperationState.State2, "desp_state_stool_2" },
                { CustomerDesperationState.State3, "desp_state_stool_3" },
                { CustomerDesperationState.State4, "desp_state_stool_4" },
                { CustomerDesperationState.State5, "desp_state_stool_5" },
                //{ CustomerDesperationState.State6, "desp_state_stool_6" }
            };
            PantsAnimationClipLookup = new Dictionary<CustomerAction, string>() {
                { CustomerAction.PantsDown, "pants_down" },
                { CustomerAction.PantsUp, "pants_up" }
            };
            PantsSidewaysAnimationClipLookup = new Dictionary<CustomerAction, string>() {
                { CustomerAction.PantsDown, "pants_down_side" },
                { CustomerAction.PantsUp, "pants_up_side"}
            };
            ActionStateAnimationClipLookup = new Dictionary<InteractableType, Dictionary<CustomerAction, string>>();
            ActionStateSidewaysAnimationClipLookup = new Dictionary<InteractableType, Dictionary<CustomerAction, string>>();

            // Front facing sink
            ActionStateAnimationClipLookup.Add(InteractableType.Sink, new Dictionary<CustomerAction, string>() {
                { CustomerAction.Peeing, "peeing_sink"},
                { CustomerAction.PeeingPinchOff, "peeing_sink"},
                {CustomerAction.WashingHands, "wash"}
            });
            // Front facing toilet
            ActionStateAnimationClipLookup.Add(InteractableType.Toilet, new Dictionary<CustomerAction, string>() {
                { CustomerAction.Peeing, "peeing_toilet" },
                { CustomerAction.PeeingPinchOff, "peeing_toilet" },
            });
            // Front facing urinal
            ActionStateAnimationClipLookup.Add(InteractableType.Urinal, new Dictionary<CustomerAction, string>() {
                { CustomerAction.Peeing, "peeing_urinal" },
                { CustomerAction.PeeingPinchOff, "peeing_urinal" },
            });
            // Side facing urinal
            ActionStateSidewaysAnimationClipLookup.Add(InteractableType.Urinal, new Dictionary<CustomerAction, string>() {
                { CustomerAction.Peeing, "peeing_urinal_side" },
                { CustomerAction.PeeingPinchOff, "peeing_urinal_side" },
            });
            // Front facing seat
            ActionStateAnimationClipLookup.Add(InteractableType.Seat, new Dictionary<CustomerAction, string>() {
                {CustomerAction.Drinking, "drink" }
            });

        }
        #endregion
    }
}
