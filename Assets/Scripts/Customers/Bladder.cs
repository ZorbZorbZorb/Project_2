using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Customers {
    // Pee is stored in the balls
    [Serializable]
    public class Bladder {
        public double AverageMax = 700;
        [SerializeField]
        public Customer customer;
        public float Stomach;  // The stomach is stored in the bladder
        public float Amount;
        public float Max;
        public float DrainRate;
        public float DrainRateNow;
        public float NormalizedPercentEmptied;
        public float NormalizedPercentEmptiedStart;
        public float FillRate;
        public float FeltNeedCurve;  // Changes how badly this customer feels the need to go. multiplier.
        public float FeltNeed;  // How badly this customer thinks they need to go, 0.0 to 1.0
        public float ControlRemaining;
        public float LossOfControlTime;  //  Time remaining before tranfering from about to wet to wetting
        public float LossOfControlTimeNow;
        public bool StruggleStopPeeing;
        public bool StruggleStopSpurt = false;
        public bool StruggleStopSpurtNow = false;

        /// <summary>
        /// Amount to multiply the normal drain rate by, to make the customer pee faster when their bladder is fuller
        /// </summary>
        public float DrainMultiplier => Mathf.Min( 0.75f + Mathf.Pow(0.7f * Amount / Max, 2), 2f);

        public DateTime LastPeedAt;
        public int DrinksHad;

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

            // Set felt need
            FeltNeed = Mathf.Min(Mathf.Pow(Percentage, FeltNeedCurve), 1.0f);

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
                                DrainRateNow += ( 15f * customer.DeltaTime );
                            }
                        }
                        else {
                            // Guys will be better at interrupting peeing than girls
                            if ( customer.Gender == 'm' ) {
                                DrainRateNow -= ( 16f * customer.DeltaTime );
                            }
                            else {
                                DrainRateNow -= ( 8f * customer.DeltaTime );
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
                float timeToSubtract = 1 * customer.DeltaTime;
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
            float amountToAdd = FillRate * customer.DeltaTime;
            if ( GameController.GC.RapidBladderFill ) {
                amountToAdd *= 3;
            }
            Amount += amountToAdd;
            if ( Stomach > 0 ) {
                amountToAdd = customer.DeltaTime * ( 2f + ( Stomach / 200f ) );
                Stomach -= amountToAdd;
                Amount += amountToAdd;
            }
            float percentFull = Percentage;
            if ( percentFull > 0.8f && percentFull < 1.0f ) {
                ControlRemaining -= 0.5f * customer.DeltaTime;
                if ( ControlRemaining < 0f ) {
                    ControlRemaining = 0f;
                }
            }
            else if ( percentFull > 1.0f ) {
                ControlRemaining -= 5f * customer.DeltaTime;
                if ( ControlRemaining < 0f ) {
                    ControlRemaining = 0f;
                }
            }
        }
        /// <summary>
        /// Empty a customers bladder. Called once per update
        /// </summary>
        private void DoBladderEmpty() {
            float amountToRemove = DrainRateNow * DrainMultiplier * customer.DeltaTime;
            
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
                LastPeedAt = DateTime.Now;
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
        public void SetupBladder(Customer customer, int min, int max) {
            // Make guys able to hold on longer than girls can after they start losing control
            if ( customer.Gender == 'm' ) {
                LossOfControlTime = 20f;
                LossOfControlTimeNow = LossOfControlTime;
            }
            else {

                LossOfControlTime = 10f;
                LossOfControlTimeNow = LossOfControlTime;
            }

            // Randomly give maximum bladder size from 550 to 1500
            Max = Random.Range(400, 1000);
            // Randomly give fullness of min% to max%
            float fullness = 0.01f * Random.Range(min, max);
            Amount = fullness * Max;
            // Subtract some control depending on how full they already are
            if ( fullness > 0.8f ) {
                ControlRemaining -= fullness * 100f;
            }

            //https://www.desmos.com/calculator/ehsasufatr
            // Randomly give them a neediness multiplier from 0.5x to 2x
            // 0.5 resistance results in them getting desperate fast but staying that way for a long time
            // 2.0 resistance results in them showing almost nothing until bursting to go
            FeltNeedCurve = Random.Range(0.5f, 2f);
            FeltNeed = Mathf.Min(Mathf.Pow(Percentage, FeltNeedCurve), 1.0f);

            // Now guess how many drinks they've had since last goingto be this desperate
            DrinksHad = (int)Math.Round(1 + Math.Pow(( Percentage + 1 ), 2.6));

            double secondsSincePastPee = ( Amount / FillRate ) + ( ( ( Max * 0.4f ) / FillRate ) * FeltNeedCurve );
            secondsSincePastPee *= 3.5d;
            LastPeedAt = DateTime.Now.AddSeconds(-Math.Round(secondsSincePastPee));
        }

        public Bladder(float stomach = 0f, float amount = 0f, float max = 650f, float drainRate = 30f, float fillRate = 0.8f, float controlRemaining = 130f,
            float lossOfControlTime = 10f) {

            Stomach = stomach;
            Amount = amount;
            Max = max;
            DrainRate = drainRate;
            DrainRateNow = DrainRate;
            // 1d seems good.
            FillRate = fillRate;
            ControlRemaining = controlRemaining;
            LossOfControlTime = lossOfControlTime;
            LossOfControlTimeNow = lossOfControlTime;
            LastPeedAt = DateTime.Now;

            ResetFrameStates();
        }
    }
}