using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Customers {
    [Serializable]
    public class Bladder {

        #region Fields

        [NonSerialized]
        private readonly Customer Customer;
        public BladderSize Size;  // Internal for keeping track of values and debugging
        public float Amount;
        public float Max;
        public float FillRate;
        public float DrainRate;
        public float PinchOffTime;

        public float MaxHoldingPower;
        public float HoldingPower;
        public float HoldingPowerReserve;
        // Leak amount?

        [SerializeField]
        private float str = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// Percentage from 0f to 1f representing how full the bladder is.
        /// <para>This number can go over 1f if the bladder is overfilled.</para>
        /// </summary>
        public float Fullness => Amount / Max;
        /// <summary>
        /// Percentage from 1f to 0f representing how much holding strength the bladder has left.
        /// <para>Based on 2:1 holding strength remaining and fullness</para>
        /// </summary>
        public float Strength =>
            (MathF.Max(0f, 1f - Fullness) + (HoldingPower / MaxHoldingPower) + (HoldingPower / MaxHoldingPower)) / 3f;
        //public float Strength => HoldingPowerReserve > 0f
        //    ? Math.Max(0.01f, HoldingPower / MaxHoldingPower)
        //    : HoldingPower / MaxHoldingPower;
        public bool NoStrengthLeft => HoldingPowerReserve <= 0f && HoldingPower <= 0f;
        public bool LosingControl => HoldingPower == 0f;
        /// <summary>
        /// Denotes if the bladder is empty or not.
        /// <para>Will be false after calling <see cref="FinishPeeing"/></para>
        /// </summary>
        public bool IsEmpty { get; private set; }
        /// <summary>
        /// Amount to multiply the normal drain rate by, to make the customer pee faster when their bladder is fuller
        /// </summary>
        private float DrainMultiplier => Mathf.Min(0.75f + Mathf.Pow(0.7f * Amount / Max, 2), 2f);

        #endregion

        #region Instance External Methods

        public void Update( CustomerAction action ) {

            float fullness = Fullness;
            str = Strength;

            switch ( action ) {
                case CustomerAction.Wetting:
                case CustomerAction.Peeing:

                    Amount -= DrainRate * DrainMultiplier * Customer.DeltaTime;
                    if ( Amount < 0f ) {
                        Amount = 0f;
                        IsEmpty = true;
                    }

                    // Quickly regain control if empty
                    if ( fullness < 0.6f ) {
                        IncreaseHoldingPower(10f);
                    }
                    // Moderatly regain control if not full
                    else if ( fullness < 0.85f ) {
                        IncreaseHoldingPower(3f);
                    }
                    // Very slowly regain control if still full
                    else if ( fullness < 0.95f ) {
                        IncreaseHoldingPower(1.5f);
                    }

                    break;

                case CustomerAction.PeeingPinchOff:

                    if ( PinchOffTime > 0f ) {
                        Amount -= PinchOffTime * DrainRate * DrainMultiplier * Customer.DeltaTime;
                        if ( Amount < 0f ) {
                            Amount = 0f;
                            IsEmpty = true;
                        }
                        PinchOffTime -= Customer.DeltaTime;
                    }

                    break;

                case CustomerAction.Leaking:

                    var x = DrainRate * DrainMultiplier * Customer.DeltaTime;
                    IncreaseHoldingPower(MaxHoldingPower / 10f);
                    Amount -= x;

                    break;

                case CustomerAction.LoseControlFreeze:
                    break;

                default:
                    Amount += FillRate * Customer.DeltaTime;
                    if ( fullness > 1f ) {
                        DecreaseHoldingPower(2.5f * Mathf.Pow(fullness, 4));
                    }
                    else if ( fullness > 0.80f ) {
                        DecreaseHoldingPower(fullness);
                    }
                    else if ( fullness < 0.5f ) {
                        IncreaseHoldingPower(1f);
                    }

                    break;
            }
        }
        public void Add( CustomerAction action, float ml ) {
            if ( action != CustomerAction.Peeing && action != CustomerAction.Wetting
                && action != CustomerAction.PeeingPinchOff ) {
                Amount += ml;
            }
        }
        /// <summary>
        /// Resets <see cref="HoldingPowerReserve"/>
        /// </summary>
        public void ResetReserveHoldingPower() {
            var settings = GameSettings.Current.BladderSettings;
            HoldingPowerReserve = Customer.Gender == 'm'
                ? settings.DefaultHoldingPowerReserveM
                : settings.DefaultHoldingPowerReserveF;
        }
        public void ResetStrength() {
            var settings = GameSettings.Current.BladderSettings;
            HoldingPower = settings.DefaultHoldingPower;
            HoldingPowerReserve = Customer.Gender == 'm'
                ? settings.DefaultHoldingPowerReserveM
                : settings.DefaultHoldingPowerReserveF;
        }
        public void FinishPeeing() {
            PinchOffTime = GameSettings.Current.BladderSettings.DefaultPinchOffTime;
            ResetReserveHoldingPower();
            IsEmpty = false;
        }

        #endregion

        #region Instance Internal Methods

        private void IncreaseHoldingPower( float powerPerSecond ) {
            HoldingPower = Mathf.Min(MaxHoldingPower, HoldingPower + (powerPerSecond * Customer.DeltaTime));
        }
        private void DecreaseHoldingPower( float powerPerSecond ) {
            HoldingPower = Mathf.Max(0f, HoldingPower - (powerPerSecond * Customer.DeltaTime));
            if ( HoldingPower <= 0f ) {
                HoldingPowerReserve -= Customer.DeltaTime;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// </summary>
        /// <param name="size">The size this bladder should be.
        /// <param name="fullness">Amount / Max value to start at</param>
        public Bladder( Customer customer, BladderSize size, float fullness ) {
            var settings = GameSettings.Current.BladderSettings;
            Customer = customer;

            // Set the maximum amount the bladder can expand to hold
            Size = size;
            Max = GetRandomBladderMax(size);

            // Determine the starting fullness
            //Amount = GetBladderStartingFullness(Max, Random.Range(0f, 1f), startFull);
            Amount = fullness * Max;
            IsEmpty = false;

            MaxHoldingPower = settings.DefaultHoldingPower;
            HoldingPower = MaxHoldingPower;
            HoldingPowerReserve = customer.Gender == 'm'
                ? settings.DefaultHoldingPowerReserveM
                : settings.DefaultHoldingPowerReserveF;
            PinchOffTime = settings.DefaultPinchOffTime;

            FillRate = settings.DefaultFillRate;
            DrainRate = settings.DefaultDrainRate;
        }
        /// <summary>
        /// </summary>
        /// <param name="fullness">Amount / Max value to start at</param>
        public Bladder( Customer customer, float fullness ) : this(customer, GetRandomBladderSize(), fullness) { }

        #endregion

        #region Static Internal Methods

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
        private static float GetRandomBladderMax( BladderSize size ) {
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

        #endregion

    }
}
