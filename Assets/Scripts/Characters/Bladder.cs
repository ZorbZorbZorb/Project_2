using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Pee is stored in the balls
[Serializable]
public class Bladder {
    public double AverageMax = 700;
    [SerializeField]
    public Customer customer;
    public double Stomach;  // The stomach is stored in the bladder
    public double Amount;
    public double Max;
    public double DrainRate;
    public double DrainRateNow;
    public float NormalizedPercentEmptied;
    public float NormalizedPercentEmptiedStart;
    public double FillRate;
    public double FeltNeedCurve;  // Changes how badly this customer feels the need to go. multiplier.
    public double FeltNeed;  // How badly this customer thinks they need to go, 0.0 to 1.0
    public double ControlRemaining;
    public double LossOfControlTime;  //  Time remaining before tranfering from about to wet to wetting
    public double LossOfControlTimeNow;
    public bool StruggleStopPeeing;
    public bool StruggleStopSpurt = false;
    public bool StruggleStopSpurtNow = false;
    
    public DateTime LastPeedAt;
    public int DrinksHad;

    // Leak amount?
    public bool Emptying = false;  // Flag to set if emptying or filling
    public bool LosingControl = false;  // Flag for losing control
    public bool Wetting = false;  // For use by customer class
    public bool ShouldWetNow = false;  // Used by customer to tell when bladder wants to start involuntarily emptying

    public bool StartedLosingControlThisFrame;

    public double Percentage => Amount / Max;

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
        FeltNeed = Math.Min(Math.Pow(Percentage, FeltNeedCurve), 1.0d);

        // If Emptying
        if (Emptying) {
            // Calculate the normalized amount emptied. It's okay to do it this way for now, because it only matters
            //   for wettings or determining how much someone has emptied BEFORE having them stop.
            NormalizedPercentEmptiedStart = Mathf.Max(NormalizedPercentEmptiedStart, (float)Amount);
            NormalizedPercentEmptied = 1f - ( (float)Amount / NormalizedPercentEmptiedStart );

            if (StruggleStopPeeing) {
                // If finished struggling to stop peeing
                if (DrainRateNow <= 0d) {
                    Emptying = false;
                    StruggleStopPeeing = false;
                    DrainRateNow = DrainRate;
                }
                // Struggle to stop peeing
                else {
                    if (StruggleStopSpurtNow) {
                        if ( DrainRateNow > DrainRate * 1.5d) {
                            StruggleStopSpurtNow = false;
                        }
                        else {
                            DrainRateNow += (15d * Time.deltaTime);
                        }
                    }
                    else {
                        // Guys will be better at interrupting peeing than girls
                        if (customer.Gender == 'm') {
                            DrainRateNow -= ( 16d * Time.deltaTime );
                        }
                        else {
                            DrainRateNow -= (8d * Time.deltaTime);
                        }
                    }
                    // To make it more interesting heres some naive for spurting when stopping
                    if (!StruggleStopSpurtNow && !StruggleStopSpurt && DrainRateNow < DrainRate / 2) {
                        StruggleStopSpurt = true;
                        if (Random.Range(0, 2) == 1 && Percentage > 0.4d) {  // 50/50 for this behavior to trigger when full
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
        else if (LosingControl) {
            DoBladderFill();
            double timeToSubtract = 1 * Time.deltaTime;
            LossOfControlTimeNow -= timeToSubtract;
            if (LossOfControlTimeNow <= 0) {
                LosingControl = false;
                ShouldWetNow = true;
            }
        }
        // If Filling
        else {
            DoBladderFill();
            if ( ControlRemaining <= 0d ) {
                if (LosingControl == false) {
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
        double amountToAdd = FillRate * Time.deltaTime;
        if ( GameController.GC.DebugRapidFill ) {
            amountToAdd *= 10;
        }
        Amount += amountToAdd;
        if (Stomach > 0) {
            amountToAdd = Time.deltaTime * (2 + (Stomach/200));
            Stomach -= amountToAdd;
            Amount += amountToAdd;
        }
        double percentFull = Percentage;
        if (percentFull > 0.8d && percentFull < 1.0d) {
            ControlRemaining -= 0.5 * Time.deltaTime;
            if ( ControlRemaining < 0 ) {
                ControlRemaining = 0;
            }
        }
        else if (percentFull > 1.0d) {
            ControlRemaining -= 5 * Time.deltaTime;
            if ( ControlRemaining < 0 ) {
                ControlRemaining = 0;
            }
        }
    }
    /// <summary>
    /// Empty a customers bladder. Called once per update
    /// </summary>
    private void DoBladderEmpty() {
        double amountToRemove = Math.Min(DrainRateNow * Time.deltaTime, Amount);
        if (GameController.GC.DebugRapidPee) {
            amountToRemove *= 4;
        }
        Amount -= amountToRemove;
        if (Percentage < 0.9d) {
            LosingControl = false;
        }
        if ( Amount < 1d ) {
            Emptying = false;
            Wetting = false;
            ShouldWetNow = false;
            DrainRateNow = DrainRate;
            LastPeedAt = DateTime.Now;
            if (ControlRemaining < 1d) {
                ControlRemaining = 1d;
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
        if (customer.Gender == 'm') {
            LossOfControlTime = 20d;
            LossOfControlTimeNow = LossOfControlTime;
        }
        else {

            LossOfControlTime = 10d;
            LossOfControlTimeNow = LossOfControlTime;
        }

        // Randomly give maximum bladder size from 550 to 1500
        Max = Random.Range(400, 1000);
        // Randomly give fullness of min% to max%
        double fullness = 0.01d * Random.Range(min, max);
        Amount = fullness * Max;
        // Subtract some control depending on how full they already are
        if ( fullness > 0.8d ) {
            ControlRemaining -= fullness * 100;
        }

        //https://www.desmos.com/calculator/ehsasufatr
        // Randomly give them a neediness multiplier from 0.5x to 2x
        // 0.5 resistance results in them getting desperate fast but staying that way for a long time
        // 2.0 resistance results in them showing almost nothing until bursting to go
        FeltNeedCurve = Random.Range(0.5f, 2f);
        FeltNeed = Math.Min(Math.Pow(Percentage, FeltNeedCurve), 1.0d);

        // Now guess how many drinks they've had since last goingto be this desperate
        DrinksHad = (int)Math.Round(1 + Math.Pow((Percentage + 1), 2.6));

        double secondsSincePastPee = ( Amount / FillRate ) + ((( Max * 0.4d ) / FillRate) * FeltNeedCurve );
        secondsSincePastPee *= 3.5d;
        LastPeedAt = DateTime.Now.AddSeconds(-Math.Round(secondsSincePastPee));
    }

    public Bladder(double stomach=0, double amount=0, double max=650, double drainRate=30, double fillRate=0.8d, double controlRemaining=130, 
        double lossOfControlTime=10) {

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
