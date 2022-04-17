using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Customers {
    // Pee is stored in the balls
    [Serializable]
    public class Bladder {
        [NonSerialized]
        public Customer Customer;
        public BladderSize BladderSize;  // Internal for keeping track of values and debugging
        public float Stomach;  // The stomach is stored in the bladder
        public float Amount;
        public float Max;
        public float DrainRate;
        public float DrainRateNow;
        public float NormalizedPercentEmptied;
        public float NormalizedPercentEmptiedStart;
        public float FillRate;
        public float ControlRemaining;
        public float LossOfControlTime;  //  Time remaining before tranfering from about to wet to wetting
        public float LossOfControlTimeNow;
        public bool StruggleStopPeeing;
        public bool StruggleStopSpurt = false;
        public bool StruggleStopSpurtNow = false;

        /// <summary>
        /// Amount to multiply the normal drain rate by, to make the customer pee faster when their bladder is fuller
        /// </summary>
        public float DrainMultiplier => Mathf.Min(0.75f + Mathf.Pow(0.7f * Amount / Max, 2), 2f);

        // Leak amount?
        public bool Emptying = false;  // Flag to set if emptying or filling
        public bool LosingControl = false;  // Flag for losing control
        public bool Wetting = false;  // For use by customer class
        public bool ShouldWetNow = false;  // Used by customer to tell when bladder wants to start involuntarily emptying

        public bool StartedLosingControlThisFrame;

        public float Percentage => Amount / Max;

        /// <summary>
        /// Forces bladder to hold on a bit longer by resetting loss of control time.
        /// <para>does not reset state for wetting or control remaining.</para>
        /// </summary>
        public void ResetLossOfControlTime() {
            LossOfControlTimeNow = LossOfControlTime;
        }

        public void Update() {
            // Set all started/stopped x this frame bools to false
            ResetFrameStates();

            // If Emptying
            if ( Emptying ) {
                // Calculate the normalized amount emptied. It's okay to do it this way for now, because it only matters
                //   for wettings or determining how much someone has emptied BEFORE having them stop.
                NormalizedPercentEmptiedStart = Mathf.Max(NormalizedPercentEmptiedStart, (float)Amount);
                NormalizedPercentEmptied = 1f - ( (float)Amount / NormalizedPercentEmptiedStart );

                if ( StruggleStopPeeing ) {
                    // If finished struggling to stop peeing
                    if ( DrainRateNow <= 0d ) {
                        Emptying = false;
                        StruggleStopPeeing = false;
                        DrainRateNow = DrainRate;
                    }
                    // Struggle to stop peeing
                    else {
                        if ( StruggleStopSpurtNow ) {
                            if ( DrainRateNow > DrainRate * 1.5f ) {
                                StruggleStopSpurtNow = false;
                            }
                            else {
                                DrainRateNow += ( 15f * Customer.DeltaTime );
                            }
                        }
                        else {
                            // Guys will be better at interrupting peeing than girls
                            if ( Customer.Gender == 'm' ) {
                                DrainRateNow -= ( 16f * Customer.DeltaTime );
                            }
                            else {
                                DrainRateNow -= ( 8f * Customer.DeltaTime );
                            }
                        }
                        // To make it more interesting heres some naive for spurting when stopping
                        if ( !StruggleStopSpurtNow && !StruggleStopSpurt && DrainRateNow < DrainRate / 2 ) {
                            StruggleStopSpurt = true;
                            if ( Random.Range(0, 2) == 1 && Percentage > 0.4d ) {  // 50/50 for this behavior to trigger when full
                                StruggleStopSpurtNow = true;
                            }
                        }
                        // TODO add posibility for wetting by reducing control here?
                    }
                }
                DoBladderEmpty();
                return;
            }
            // If Losing Control
            else if ( LosingControl ) {
                DoBladderFill();
                float timeToSubtract = 1 * Customer.DeltaTime;
                LossOfControlTimeNow -= timeToSubtract;
                if ( LossOfControlTimeNow <= 0 ) {
                    LosingControl = false;
                    ShouldWetNow = true;
                }
            }
            // If Filling
            else {
                DoBladderFill();
                if ( ControlRemaining <= 0d ) {
                    if ( LosingControl == false ) {
                        StartedLosingControlThisFrame = true;
                    }
                    LosingControl = true;
                }
            }
        }
        /// <summary>
        /// Fill a customers bladder. Called once per update
        /// </summary>
        private void DoBladderFill() {
            float amountToAdd = FillRate * Customer.DeltaTime;
            if ( GameController.GC.RapidBladderFill ) {
                amountToAdd *= 3;
            }
            Amount += amountToAdd;
            if ( Stomach > 0 ) {
                amountToAdd = Customer.DeltaTime * ( 2f + ( Stomach / 200f ) );
                Stomach -= amountToAdd;
                Amount += amountToAdd;
            }
            float percentFull = Percentage;
            if ( percentFull > 0.8f && percentFull < 1.0f ) {
                ControlRemaining -= 0.5f * Customer.DeltaTime;
                if ( ControlRemaining < 0f ) {
                    ControlRemaining = 0f;
                }
            }
            else if ( percentFull > 1.0f ) {
                ControlRemaining -= 5f * Customer.DeltaTime;
                if ( ControlRemaining < 0f ) {
                    ControlRemaining = 0f;
                }
            }
        }
        /// <summary>
        /// Empty a customers bladder. Called once per update
        /// </summary>
        private void DoBladderEmpty() {
            float amountToRemove = DrainRateNow * DrainMultiplier * Customer.DeltaTime;

            if ( GameController.GC.RapidBladderEmpty ) {
                amountToRemove *= 3f;
            }

            Amount -= amountToRemove;
            if ( Percentage < 0.9f ) {
                LosingControl = false;
            }
            if ( Amount < 1f ) {
                Amount = 0f;
                Emptying = false;
                Wetting = false;
                ShouldWetNow = false;
                DrainRateNow = DrainRate;
                if ( ControlRemaining < 1f ) {
                    ControlRemaining = 1f;
                }
            }
        }
        private void ResetFrameStates() {
            StartedLosingControlThisFrame = false;
        }
        public void StopPeeingEarly() {
            StruggleStopPeeing = true;
        }
        /// <summary>
        /// </summary>
        /// <param name="size">The size this bladder should be.
        /// <param name="startFull">Should this bladder start full?</param>
        public Bladder(Customer customer, BladderSize size, bool startFull) {
            var settings = GameSettings.Current.BladderSettings;
            Customer = customer;

            // Set the maximum amount the bladder can expand to hold
            BladderSize = size;
            Max = GetRandomBladderMax(size);

            // Determine the starting fullness
            Amount = GetBladderStartingFullness(Max, Random.Range(0f, 1f), startFull);

            ControlRemaining = settings.DefaultControlRemaining;
            LossOfControlTime = customer.Gender == 'm' ? settings.DefaultLossOfControlTimeM : settings.DefaultLossOfControlTimeF;
            LossOfControlTimeNow = LossOfControlTime;
            ControlRemaining = settings.DefaultControlRemaining;

            FillRate = settings.DefaultFillRate;
            DrainRate = settings.DefaultDrainRate;
            DrainRateNow = settings.DefaultDrainRate;
            Stomach = 0f;

            ResetFrameStates();
        }
        /// <summary>
        /// </summary>
        /// <param name="startFull">Should this bladder start full?</param>
        public Bladder(Customer customer, bool startFull) : this(customer, GetRandomBladderSize(), startFull) { }

        #region Internal Methods

        /// <summary>
        /// Decides a bladder size at random using the weighted distribution table in <see cref="GameSettings.BladderSettings"/>
        /// </summary>
        /// <returns>Bladder size</returns>
        private static BladderSize GetRandomBladderSize() {
            var settings = GameSettings.Current.BladderSettings;
            int x = Random.Range(0, settings.ChanceTotal);
            if ( x < settings.ChanceSmall ) {
                return BladderSize.Small;
            }
            else if ( x < settings.ChanceMedium + settings.ChanceSmall ) {
                return BladderSize.Medium;
            }
            else if ( x < settings.ChanceLarge + settings.ChanceMedium + settings.ChanceSmall ) {
                return BladderSize.Large;
            }
            else {
                return BladderSize.Massive;
            }
        }
        /// <summary>
        /// Returns a random <see cref="Max"/> size
        /// </summary>
        /// <returns>
        /// Dependent on <paramref name="size"/>. See <see cref="GameSettings.Bladder"/>
        /// </returns>
        private static float GetRandomBladderMax(BladderSize size) {
            var settings = GameSettings.Current.BladderSettings;
            switch ( size ) {
                case BladderSize.Small:
                    return Random.Range(settings.SizeMinSmall, settings.SizeMaxSmall);
                case BladderSize.Medium:
                    return Random.Range(settings.SizeMinMedium, settings.SizeMaxMedium);
                case BladderSize.Large:
                    return Random.Range(settings.SizeMinLarge, settings.SizeMaxLarge);
                case BladderSize.Massive:
                    return Random.Range(settings.SizeMinMassive, settings.SizeMaxMassive);
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Returns a starting <see cref="Amount"/> depending on the value of <paramref name="t"/>, the customers 
        /// <paramref name="max"/> and if the customer should <paramref name="startFull"/>
        /// <para>https://www.desmos.com/calculator/nw8zdk2krx</para>
        /// </summary>
        /// <param name="max"></param>
        /// <param name="t">0 &#8804; <paramref name="t"/> &#8804; 1</param>
        /// <param name="startFull"></param>
        /// <returns>(<paramref name="max"/> * 0.1)~ &#8804; x &#8804; (<paramref name="max"/> * 0.9) </returns>
        private static float GetBladderStartingFullness(float max, float t, bool startFull) {
            float percent = MathF.Pow(-( ( t * 1.5f ) - 0.75f ), 2f) / 2f;
            percent += startFull ? 0.95f : 0.4f;
            return percent * max;
        }

        #endregion
    }
}

public enum BladderSize {
    Small,
    Medium,
    Large,
    Massive
}
