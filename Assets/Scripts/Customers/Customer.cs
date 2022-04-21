using Assets.Scripts.Areas;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Customers {
    public class Customer : MonoBehaviour {

        #region Fields

        public static GameController GC = null;

        public double Funds = 0d;
        private CustomerSpriteController marshal;
        [HideInInspector]
        public CustomerAnimator customerAnimator;
        private CustomerAction LastActionState;
        [HideInInspector]
        public Vector2[] PathVectors = new Vector2[0];
        [HideInInspector]
        public VertexPath2 Path;
        /// <summary>0f is the start of the path, 1f is the end of the path.</summary>
        private float PathTime = 0f;
        private float PathLength;
        private float PathMoveSpeed;

        public float MoveSpeed;
        public MovementType MovementType = MovementType.None;

        /// <summary>Records the y position that the bathrooms start at, for moving the customer behind the door overlay</summary>
        [HideInInspector]
        public static float BathroomStartX, BathroomStartY;

        private int sortingLayerIdAboveOverlay = -1;

        public SpriteRenderer SRenderer;
        public Animator animator;
        public string animatorStateName;
        public string lastAnimatorStateName;

        public float DrinkAmount;

        public Text EmotesBladderAmountText;

        public bool Active = false;
        public CustomerDesperationState lastState;

        // Enum for behavior types
        public bool CanReenterBathroom = true;
        public float CanReenterBathroomIn = 0f;
        public bool IsRelievingSelf = false;
        public bool IsFinishedPeeing;
        public bool IsWetting = false;
        public bool IsWet = false;
        public CustomerDesperationState DesperationState = CustomerDesperationState.State0;
        private CustomerDesperationState LastDesperationState = CustomerDesperationState.State0;
        public bool DesperationStateChangeThisUpdate = false;
        public Stomach Stomach;
        public Bladder Bladder;
        public int Leaks = 0;
        public int LeaksLimit = 0;
        public bool DisplayedLosingControlAnim = false;

        // Times
        public double UrinateStartDelay;
        public double UrinateStopDelay;
        private double RemainingUrinateStopDelay;
        public float WetSelfLeaveBathroomDelay = 6f;
        public float TotalTimeAtBar = 0f;
        public float MinTimeAtBar = 60f;
        public float MinTimeAtBarNow = 0f;
        /// <summary>
        /// Accumulates 0.5f to make customers think twice a second
        /// </summary>
        private float CustomerThinkTicker = 0f;

        // Position
        public CustomerAction CurrentAction = CustomerAction.None;
        public Location Location = Location.None;
        public CustomerInteractable Occupying;

        // Class that contains all of the emotes / thought bubbles / etc for the person
        public Emotes Emotes;

        public static Dictionary<BladderSize, Dictionary<CustomerDesperationState, int>> WillingnessToGoLookup;

        public int TimesThoughtsAboutPeeing = 0;

        #endregion

        #region Properties

        [HideInInspector]
        internal float DeltaTime { get; private set; }

        [HideInInspector]
        public bool AtDestination => MovementType == MovementType.None;
        /// <summary>
        /// https://www.desmos.com/calculator
        /// <para>-\left(0.85-\left(x\cdot1.7\right)\right)^{4}+1.1</para>
        /// </summary>
        [HideInInspector]
        private float PathMoveSpeedMultiplier => -Mathf.Pow(-(0.8f - (PathTime * 1.6f)), 4) + 1.1f;
        public char Gender { get; set; }
        public ReliefType ReliefType => Occupying != null ? Occupying.RType : ReliefType.None;

        #endregion

        private void Awake() {
            // Cache sorting layer id
            SRenderer.sortingLayerName = "AboveOverlay";
            sortingLayerIdAboveOverlay = SRenderer.sortingLayerID;
        }

        private void Start() {
            Emotes.Start();
        }

        private void Update() {
            // Fastest possible exit
            if ( !Active ) {
                return;
            }

            DeltaTime = GC.RapidSimulation ? Time.deltaTime * 10f : Time.deltaTime;

            // Update all of the time accumulators
            if ( Location == Location.Bar ) {
                MinTimeAtBarNow += DeltaTime;
            }
            else {
                MinTimeAtBarNow = 0f;
            }

            // Empty stomach into bladder
            if (Stomach.Fullness > 0f) {
                Bladder.Add(CurrentAction, Stomach.Remove(10f + (20f * Stomach.Fullness)));
            }
            Bladder.Update(CurrentAction);
            UpdateDesperationState();

            // Handle next action, or think
            if ( Next != null ) {
                NextDelay -= DeltaTime;
                NextActionHandler();
            }
            else if ( AtDestination ) {
                PeeLogicUpdate();
                if ( CustomerThinkTicker >= 0.5f ) {
                    CustomerThinkTicker -= 0.5f;
                    Think();
                }
                else {
                    CustomerThinkTicker += DeltaTime;
                }
            }

            customerAnimator.Update();
            Emotes.Update();
            BathroomMenu.Update();
            ReliefMenu.Update();

            MoveUpdate();

            // Debug logging
            if ( GameController.GC.LogCustomerStates ) {
                FrameActionDebug();
            }

        }
        public void SetupCustomer( float fullness ) {
            marshal = CustomerSpriteController.Controller[Gender];

            // Set up the customers animator
            customerAnimator = new CustomerAnimator(this, SRenderer, animator, marshal);

            // Get references to game objects for the customer
            BathroomMenuCanvas = gameObject.transform.Find("BathroomMenuCanvas").GetComponent<Canvas>();
            ReliefMenuCanvas = gameObject.transform.Find("ReliefMenuCanvas").GetComponent<Canvas>();
            // Set up the menus for this customer
            BathroomMenu = new Menu(BathroomMenuCanvas, CanDisplayBathroomMenu);
            ReliefMenu = new Menu(ReliefMenuCanvas, CanDisplayReliefMenu);

            ButtonWaitingRoom = gameObject.transform.Find("BathroomMenuCanvas/ButtonWait").GetComponent<Button>();
            ButtonDecline = gameObject.transform.Find("BathroomMenuCanvas/ButtonDecline").GetComponent<Button>();
            ButtonToilet = gameObject.transform.Find("BathroomMenuCanvas/ButtonToilet").GetComponent<Button>();
            ButtonUrinal = gameObject.transform.Find("BathroomMenuCanvas/ButtonUrinal").GetComponent<Button>();
            ButtonSink = gameObject.transform.Find("BathroomMenuCanvas/ButtonSink").GetComponent<Button>();
            ButtonReliefStop = gameObject.transform.Find("ReliefMenuCanvas/ButtonReliefStop").GetComponent<Button>();

            // Set up the buttons for the menus
            MenuButton MenuButtonWaitingRoom = new(this, BathroomMenu, ButtonWaitingRoom, () => { MenuOptionGotoWaiting(); });
            MenuButton MenuButtonDecline = new(this, BathroomMenu, ButtonDecline, () => { MenuOptionDismiss(); });
            MenuButton MenuButtonToilet = new(this, BathroomMenu, ButtonToilet, () => { MenuOptionGotoToilet(); });
            MenuButton MenuButtonUrinal = new(this, BathroomMenu, ButtonUrinal, () => { MenuOptionGotoUrinal(); }, WillUseUrinal, CanUseUrinal);
            MenuButton MenuButtonSink = new(this, BathroomMenu, ButtonSink, () => { MenuOptionGotoSink(); }, WillUseSink, CanUseSink);

            MenuButton MenuButtonReliefStop = new(this, ReliefMenu, ButtonReliefStop, () => { MenuOptionStopPeeing(); });

            Stomach = new Stomach(600f);
            Bladder = new Bladder(this, fullness);

            switch ( Bladder.Size ) {
                case BladderSize.Small:
                    LeaksLimit = 0;
                    break;
                case BladderSize.Medium:
                    LeaksLimit = 1;
                    break;
                case BladderSize.Large:
                    LeaksLimit = 2;
                    break;
                case BladderSize.Massive:
                    LeaksLimit = 3;
                    break;
            }

            UrinateStartDelay = 4d;
            UrinateStopDelay = 6d;

            customerAnimator.Update();
            Emotes.Update();
            BathroomMenu.Update();
            ReliefMenu.Update();

            UpdateDesperationState();
            PeeLogicUpdate();
        }

        public void SetWillingnessToGoLookup() {
            WillingnessToGoLookup = new Dictionary<BladderSize, Dictionary<CustomerDesperationState, int>>() {
                { BladderSize.Small, new Dictionary<CustomerDesperationState, int>() {
                    { CustomerDesperationState.State1, GameSettings.Current.SmallUsesBathroomStage1 },
                    { CustomerDesperationState.State2, GameSettings.Current.SmallUsesBathroomStage2 },
                    { CustomerDesperationState.State3, GameSettings.Current.SmallUsesBathroomStage3 },
                    { CustomerDesperationState.State4, 1 }
                }
            },
            { BladderSize.Medium, new Dictionary<CustomerDesperationState, int>() {
                    { CustomerDesperationState.State1, GameSettings.Current.MediumUsesBathroomStage1 },
                    { CustomerDesperationState.State2, GameSettings.Current.MediumUsesBathroomStage2 },
                    { CustomerDesperationState.State3, GameSettings.Current.MediumUsesBathroomStage3 },
                    { CustomerDesperationState.State4, 1 }
                }
            },
            { BladderSize.Large, new Dictionary<CustomerDesperationState, int>() {
                    { CustomerDesperationState.State1, GameSettings.Current.LargeUsesBathroomStage1 },
                    { CustomerDesperationState.State2, GameSettings.Current.LargeUsesBathroomStage2 },
                    { CustomerDesperationState.State3, GameSettings.Current.LargeUsesBathroomStage3 },
                    { CustomerDesperationState.State4, 1 }
                }
            },
            { BladderSize.Massive, new Dictionary<CustomerDesperationState, int>() {
                    { CustomerDesperationState.State1, GameSettings.Current.MassiveUsesBathroomStage1 },
                    { CustomerDesperationState.State2, GameSettings.Current.MassiveUsesBathroomStage2 },
                    { CustomerDesperationState.State3, GameSettings.Current.MassiveUsesBathroomStage3 },
                    { CustomerDesperationState.State4, 1 }
                }
            }};
        }

        #region NextAction

        public bool HasNext = false;
        public delegate void NextAction();
        public NextAction Next;
        public float NextDelay = 0f;
        /// <summary>Next will wait to trigger until this function returns true</summary>
        public Func<bool> NextWhenTrue = null;

        /// <summary>
        /// Sets the next action that the customer will perform.
        /// <para>Note that if <paramref name="delay"/> and <paramref name="whenTrue"/> are both provided
        /// <paramref name="delay"/> will tick down to 0f, then <paramref name="whenTrue"/> is awaited</para>
        /// </summary>
        /// <param name="delay">Delay to wait for before executing <paramref name="next"/> action, or awaiting <paramref name="whenTrue"/></param>
        /// <param name="next">Action to perform next</param>
        /// <param name="whenTrue">Condition to await before performing <paramref name="next"/> action.</param>
        public void SetNext( float delay, NextAction next, Func<bool> whenTrue = null ) {
            HasNext = true;
            NextDelay = delay;
            NextWhenTrue = whenTrue;
            Next = next;
        }

        #endregion

        /// <summary>
        /// Bathroom customer is in, or in line to enter.
        /// </summary>
        /// <returns>The current bathroom this customer is either in, or in line to enter.</returns>
        public Bathroom GetCurrentBathroom() {
            switch ( Location ) {
                case Location.BathroomF:
                    return Bathroom.BathroomF;
                case Location.BathroomM:
                    return Bathroom.BathroomM;
                default:
                    if ( Occupying is WaitingSpot spot ) {
                        return spot.Bathroom;
                    }
                    return null;
            }
        }

        /// <summary>
        /// Returns a bathroom the customer is willing to, and able to enter
        /// </summary>
        /// <returns>Bathroom or null</returns>
        public Bathroom GetBathroomWillingToEnter() {
            if ( DesperationState == CustomerDesperationState.State0
                || DesperationState == CustomerDesperationState.State5 ) {
                return null;
            }

            // If losing control
            if ( DesperationState == CustomerDesperationState.State4 ) {
                if ( Gender == 'f' ) {
                    // Will girl use guys restroom
                    if ( GenderCorrectBathroom.CustomersInLine >= 2 && GenderIncorrectBathroom.CustomersInLine < GenderCorrectBathroom.CustomersInLine ) {
                        return GenderIncorrectBathroom;
                    }
                    // Use correct bathroom
                    else if ( !GenderCorrectBathroom.LineIsFull ) {
                        return GenderCorrectBathroom;
                    }
                }
                else {
                    // Will guy use girls restroom
                    if ( GenderCorrectBathroom.CustomersInLine >= 2 && GenderIncorrectBathroom.LineIsEmpty ) {
                        return GenderIncorrectBathroom;
                    }
                    // Use correct bathroom
                    else if ( !GenderCorrectBathroom.LineIsFull ) {
                        return GenderCorrectBathroom;
                    }
                }
                // Both lines are full? You monster.
                return null;
            }

            switch ( Bladder.Size ) {
                case BladderSize.Small:
                    switch ( DesperationState ) {
                        case CustomerDesperationState.State1:
                            if ( GenderCorrectBathroom.CustomersWaiting <= 1 ) {
                                return RandomUseChance() ? GenderCorrectBathroom : null;
                            }
                            return null;
                        case CustomerDesperationState.State2:
                            if ( GenderCorrectBathroom.CustomersWaiting <= 3 ) {
                                return RandomUseChance() ? GenderCorrectBathroom : null;
                            }
                            return null;
                        case CustomerDesperationState.State3:
                            return RandomUseChance() ? GenderCorrectBathroom : null;
                        default:
                            throw new NotImplementedException();
                    }
                case BladderSize.Medium:
                    switch ( DesperationState ) {
                        case CustomerDesperationState.State1:
                            return null;
                        case CustomerDesperationState.State2:
                            if ( GenderCorrectBathroom.CustomersWaiting <= 2 ) {
                                return RandomUseChance() ? GenderCorrectBathroom : null;
                            }
                            return null;
                        case CustomerDesperationState.State3:
                            if ( GenderCorrectBathroom.CustomersWaiting <= 4 ) {
                                return RandomUseChance() ? GenderCorrectBathroom : null;
                            }
                            return null;
                        default:
                            throw new NotImplementedException();
                    }
                case BladderSize.Large:
                    switch ( DesperationState ) {
                        case CustomerDesperationState.State1:
                            return null;
                        case CustomerDesperationState.State2:
                            if ( GenderCorrectBathroom.CustomersWaiting == 0 && RandomUseChance()) {
                                return GenderCorrectBathroom;
                            }
                            return null;
                        case CustomerDesperationState.State3:
                            if ( GenderCorrectBathroom.CustomersWaiting <= 3 && RandomUseChance() ) {
                                return GenderCorrectBathroom;
                            }
                            return null;
                        default:
                            throw new NotImplementedException();
                    }
                case BladderSize.Massive:
                    switch ( DesperationState ) {
                        case CustomerDesperationState.State1:
                        case CustomerDesperationState.State2:
                            return null;
                        case CustomerDesperationState.State3:
                            if ( GenderCorrectBathroom.CustomersWaiting <= 2 && RandomUseChance() ) {
                                return GenderCorrectBathroom;
                            }
                            return null;
                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }

            bool RandomUseChance() {
                if ( WillingnessToGoLookup == null || !WillingnessToGoLookup.Any() ) {
                    SetWillingnessToGoLookup();
                }
                int chance = WillingnessToGoLookup[Bladder.Size][DesperationState];
                int rng = Random.Range(0, chance - TimesThoughtsAboutPeeing++);
                //string debugMessage = (chance > 0 && rng == 0 ? "T" : "F") +
                //    $" {Gender} {Bladder.BladderSize} {DesperationState} ({Mathf.RoundToInt(Bladder.Fullness * 100)}%) use c=({chance})";
                return chance > 0 && rng == 0;
            }
        }

        public Bathroom GenderCorrectBathroom => Gender == 'm' ? Bathroom.BathroomM : Bathroom.BathroomF;
        public Bathroom GenderIncorrectBathroom => Gender == 'm' ? Bathroom.BathroomF : Bathroom.BathroomM;

        private void NextActionHandler() {
            if ( NextDelay > 0f ) {
                return;
            }
            else if ( NextWhenTrue == null || NextWhenTrue() ) {
                NextAction last = Next;
                Next();
                // If next was not assigned a new method from inside next clear it out.
                if ( Next.Method == last.Method ) {
                    Next = null;
                }
                return;
            }
            else {
                return;
            }
        }
        private void Think() {
            // If wet self and finished wetting self
            if ( IsWet && !IsWetting ) {
                SetNext(WetSelfLeaveBathroomDelay, () => { Leave(); });
            }
            else if ( IsWetting || !AtDestination ) {
                return;
            }

            // If in bar...
            if ( Location == Location.Bar ) {
                Bathroom bathroom;
                switch ( DesperationState ) {
                    case CustomerDesperationState.State0:
                        break;
                    case CustomerDesperationState.State1:
                        // Wants to go pee?
                        if ( !MaybeEnterBathroom(90f) ) {
                            return;
                        }
                        break;
                    case CustomerDesperationState.State2:
                        // Wants to go pee?
                        if ( !MaybeEnterBathroom(40f) ) {
                            return;
                        }
                        break;
                    case CustomerDesperationState.State3:
                        // Wants to go pee?
                        if ( !MaybeEnterBathroom(25f) ) {
                            return;
                        }
                        break;
                    case CustomerDesperationState.State4:
                        // Losing control?
                        if ( Bladder.LosingControl && MinTimeAtBarNow > 8 ) {
                            bathroom = GetBathroomWillingToEnter();
                            if ( bathroom != null && !bathroom.TryEnterQueue(this) ) {
                                // Seal their fate
                                Debug.Log("Can't enter bathroom when bursting so going to wet.", this);
                                SetNext(0f, () => {
                                    // Here's where some special wetting in bar actions / scenes / interactions can go.
                                },
                                // Hold until no bladder strength left
                                () => Bladder.NoStrengthLeft);
                            }
                        }
                        // Just totally full
                        else {
                            MaybeEnterBathroom(30f);
                        }
                        break;
                    default:
                        return;
                }
            }
            // Make girls try to use the guys bathroom
            else if ( Location == Location.Hallway ) {
                Bathroom bathroom = GetCurrentBathroom();
                // If not next in line, and girl, and about to wet
                if ( !bathroom.Line.IsNextInLine(this) && Gender == 'f' && DesperationState == CustomerDesperationState.State4 ) {
                    int currentLineCount = bathroom.Line.CustomerCount;
                    int otherLineCount = GenderIncorrectBathroom.Line.CustomerCount;
                    bool worthEnteringOtherLine = otherLineCount + 1 < currentLineCount;
                    // If other line is significantly shorter and not already switched lines
                    if ( GenderCorrectBathroom == bathroom && worthEnteringOtherLine ) {
                        if ( GenderIncorrectBathroom.TryEnterQueue(this) ) {
                            Bladder.ResetReserveHoldingPower();
                        }
                    }
                }
            }

            // Considers entering the bathroom, and returns true if decided to, or false if not
            bool MaybeEnterBathroom( float timeRequiredAtBar ) {
                if ( MinTimeAtBarNow >= timeRequiredAtBar ) {
                    Bathroom bathroom = GetBathroomWillingToEnter();
                    return bathroom != null && bathroom.TryEnterQueue(this);
                }
                return false;
            }
        }

        public void BuyDrink() {
            float drinkAmount = 200f;
            // Move to bar drinks pickup spot
            var offset = new Vector3(Random.Range(-50f, 50f), Random.Range(-10f, -5f), 0f);
            var vector = offset + Bar.Singleton.DrinkPhantomWaitingSpot.CustomerPositionF;
            MoveTo(Location.Bar, vector);
            // This is very bad practice but doesnt break anything. If I don't do this they're still occupying their seat, and will
            //   "sit" down in the phantom waiting spot when they arrive.
            CustomerInteractable seat = Occupying;
            Occupying = Bar.Singleton.DrinkPhantomWaitingSpot;
            // Move back to table
            SetNext(4f, () => {
                MoveTo(seat);
                Occupying = seat;
                // Drink
                SetNext(0f, () => {
                    CurrentAction = CustomerAction.Drinking;
                    // Return to normal behavior
                    SetNext(3.5f, () => {
                        Stomach.Add(drinkAmount);
                        Debug.Log($"Drank {drinkAmount}ml => {Stomach.Fullness}%", this);
                        CurrentAction = CustomerAction.None;
                    });
                }, () => AtDestination);
            });
        }

        private void PeeLogicUpdate() {
            switch ( CurrentAction ) {
                case CustomerAction.Wetting:
                    if ( Bladder.IsEmpty ) {
                        EndPeeingSelf();
                    }
                    return;
                case CustomerAction.Peeing:
                    if ( Bladder.IsEmpty ) {
                        if ( Next == null ) {
                            CurrentAction = CustomerAction.PantsUp;
                            Emotes.Emote(Emote.PantsUp);
                            SetNext((float)RemainingUrinateStopDelay, () => {
                                EndPeeingWithThing();
                            });
                        }
                    }
                    else {
                        Emotes.Emote(Emote.GetPeeStreamEmote(Bladder.Fullness));
                    }
                    break;
                case CustomerAction.None:
                    if ( ReliefType == ReliefType.None ) {
                        // Should the customer wet now?
                        if ( Bladder.NoStrengthLeft ) {
                            if ( Leaks < LeaksLimit ) {
                                Debug.LogWarning("Started wetting with leak points remaining", this);
                            }
                            BeginPeeingSelf();
                        }
                        // Should the customer leak?
                        else if (Leaks < LeaksLimit && (Bladder.Strength < 0.15f || (Math.Floor(Random.Range(0f, 4f/Time.deltaTime))==0d && Bladder.Fullness > 1f && Bladder.Strength < 0.4f))) {
                            Leaks++;
                            var last = CurrentAction;
                            Debug.Log($"Performing {Leaks}/{LeaksLimit} leaks!", this);
                            CurrentAction = CustomerAction.Leaking;
                            SetNext(3f, () => { CurrentAction = last; }, null);
                        }
                        // Should the show thier about to lose it animation?
                        else if ( Bladder.LosingControl && !DisplayedLosingControlAnim && Leaks >= LeaksLimit ) {
                            DisplayedLosingControlAnim = true;
                            var last = CurrentAction;
                            Debug.Log("Performing lose control freeze!", this);
                            CurrentAction = CustomerAction.LoseControlFreeze;
                            SetNext(6f, () => { CurrentAction = last; });
                        }
                    }
                    else {
                        SetNext((float)UrinateStartDelay, () => {
                            BeginPeeingWithThing();
                        });
                    }
                    break;
                default:
                    return;
            }

        }
        /// <summary>
        /// Updates the customers desperation state and state machine
        /// </summary>
        private void UpdateDesperationState() {
            if ( CurrentAction == CustomerAction.Wetting ) {
                DesperationState = CustomerDesperationState.State5;
            }
            else if ( Bladder.Strength <= 0.4f ) {
                DesperationState = CustomerDesperationState.State4;
            }
            else if ( Bladder.Strength <= 0.6f ) {
                DesperationState = CustomerDesperationState.State3;
            }
            else if ( Bladder.Strength <= 0.75f ) {
                DesperationState = CustomerDesperationState.State2;
            }
            else if ( Bladder.Strength <= 0.85f ) {
                DesperationState = CustomerDesperationState.State1;
            }
            else {
                DesperationState = CustomerDesperationState.State0;
            }

            DesperationStateChangeThisUpdate = DesperationState != LastDesperationState;
            LastDesperationState = DesperationState;
        }
        private void FrameActionDebug() {
            if ( !Active ) {
                return;
            }
            if ( LastActionState != CurrentAction ) {
                LastActionState = CurrentAction;
                Debug.Log($"Action change | {LastActionState} \u2192 {CurrentAction}", this);
            }

            // Desperation State Logging
            if ( lastState != DesperationState ) {
                lastState = DesperationState;

                string logString = "";
                logString += $"State change | {lastState} \u2192 {DesperationState} @ Bladder: {Math.Round(Bladder.Amount)} / {Bladder.Max} ({Math.Round(Bladder.Fullness, 2)}%)";
                logString += $"Control: {Math.Round(Bladder.HoldingPower)}";
                Debug.Log(logString, this);
            }
        }

        #region Movement

        /// <summary>
        /// Moves the customer from where they are currently located to the target's vector3.
        ///   Movement begins next MoveUpdate.
        /// <para>
        ///   Does not change references, only handles movement
        /// </para>
        /// </summary>
        /// <param name="target">
        /// Interactable destination to move to. Respects male / female
        /// position properties if available
        /// </param>
        public void MoveTo( CustomerInteractable target ) {
            Vector2[] vectors;
            // Are we in the same location as the target?
            if ( Location == target.Location ) {
                switch ( Location ) {
                    case Location.BathroomM:
                    case Location.BathroomF:
                        Bathroom bathroom = GetCurrentBathroom();
                        if ( target.IType == InteractableType.Sink ) {
                            vectors = new Vector2[] {
                                transform.position,
                                bathroom.SinksLine.Items[0].transform.position,
                                target.GetCustomerPosition(Gender)
                            };
                        }
                        else {
                            vectors = new Vector2[] {
                                transform.position,
                                bathroom.Center,
                                target.GetCustomerPosition(Gender)
                            };
                        }
                        break;
                    default:
                        vectors = new Vector2[] {
                            transform.position,
                            target.GetCustomerPosition(Gender)
                        };
                        break;
                }
            }
            // Navigate to the target, then move to it.
            else {
                var navigation = Navigation.Navigate(Location, target.Location);
                vectors = new Vector2[navigation.Count + 2];
                vectors[0] = transform.position;
                navigation.CopyTo(vectors, 1);
                vectors[navigation.Count + 1] = target.GetCustomerPosition(Gender);
            }

            MoveTo(vectors);
        }
        /// <summary>
        /// Moves the customer from where they are currently located to the location's point
        ///   Movement begins next MoveUpdate.
        /// <para>
        ///   Does not change references, only handles movement
        /// </para>
        /// </summary>
        /// <param name="target">
        /// Interactable destination to move to. Respects male / female
        /// position properties if available
        /// </param>
        public void MoveTo( Location location ) {
            List<Vector2> vectors = Navigation.Navigate(Location, location);
            if ( vectors.Any() ) {
                vectors.Insert(0, transform.position);
                MoveTo(vectors.ToArray());
            }
            else {
                Debug.LogWarning("MoveTo(Location) called for location customer is already in", this);
            }
        }
        /// <summary>
        /// Moves the customer from where they are currently located to the location's point
        ///   then to the given vector. Movement begins next MoveUpdate.
        /// <para>
        ///   Does not change references, only handles movement
        /// </para>
        /// </summary>
        /// <param name="target">
        /// Interactable destination to move to. Respects male / female
        /// position properties if available
        /// </param>
        public void MoveTo( Location location, Vector2 vector ) {
            List<Vector2> vectors = Navigation.Navigate(Location, location);
            if ( vectors.Any() ) {
                vectors.Insert(0, transform.position);
                vectors.Add(vector);
                MoveTo(vectors.ToArray());
            }
            else {
                vectors.Add(transform.position);
                vectors.Add(vector);
                MoveTo(vectors.ToArray());
            }
        }
        /// <summary>
        /// Internal method that sets up customer pathing for an array of vectors.
        /// No logic exists here to determine where they should go, just calls the correct
        /// function to set the customer to move in <see cref="MoveUpdate"/>.
        /// </summary>
        /// <param name="vectors"></param>
        private void MoveTo( Vector2[] vectors ) {
            PathTime = 0f;
            PathVectors = vectors.ToArray();
            switch ( vectors.Length ) {
                case 2:
                    PathLength = Vector2.Distance(vectors[0], vectors[1]);
                    break;
                //case 3:
                //    PathLength = EstimatePathLength(vectors[0], vectors[1], vectors[2]);
                //    break;
                //case 4:
                //    PathLength = EstimatePathLength(vectors[0], vectors[1], vectors[2], vectors[3]);
                //    break;
                default:
                    SetBezierPath(PathVectors);
                    return;
            }
            PathMoveSpeed = MoveSpeed / PathLength;
            MovementType = MovementType.Manual;

            void SetBezierPath( Vector2[] vectors ) {
                if ( VertexPath2.PathCache.ContainsKey(vectors) ) {
                    Path = VertexPath2.PathCache[vectors];
                }
                else {
                    BezierPath bezierPath = new(vectors, false, PathSpace.xy);
                    Path = new VertexPath2(bezierPath, GameController.GC.transform, 20f);
                    if ( VertexPath2.PathCache.Count > 2000 ) {
                        VertexPath2.PathCache.Clear();
                    }
                    VertexPath2.PathCache.Add(vectors, Path);
                }
                PathLength = Path.length;
                PathMoveSpeed = MoveSpeed / Path.length;
                MovementType = MovementType.Path;
                if ( GameController.GC.DrawPaths ) {
                    DebugDrawVertexPath(Path, customerAnimator.Color);
                    if ( Path.localPoints.Count() > 300 ) {
                        Debug.Log($"Creating long path (np={Path.localPoints.Length} nv={vectors.Count()})", this);
                    }
                }
            }
        }
        private static void DebugDrawVertexPath( VertexPath2 vertexPath, Color color, float time = 3f ) {
            for ( int i = 1; i < vertexPath.localPoints.Length; i++ ) {
                var p0 = vertexPath.localPoints[i - 1];
                var p1 = vertexPath.localPoints[i];
                Debug.DrawLine(p0, p1, color, time);
            }
        }
        private void MoveUpdate() {
            switch ( MovementType ) {
                case MovementType.None:
                    return;
                case MovementType.Path:
                    PathTime += PathMoveSpeed * PathMoveSpeedMultiplier * DeltaTime;
                    if ( PathTime >= 1f ) {
                        // If at end of path, end movement
                        transform.position = Path.GetPointAtTime(1f, EndOfPathInstruction.Stop);
                        Path = null;
                        PathTime = 0f;
                        MovementType = MovementType.None;
                        break;
                    }
                    else {
                        transform.position = Path.GetPointAtTime(PathTime, EndOfPathInstruction.Stop);
                    }
                    break;
                case MovementType.Manual:
                    PathTime += PathMoveSpeed * PathMoveSpeedMultiplier * DeltaTime;
                    if ( PathTime >= 1f ) {
                        transform.position = PathVectors.Last();
                        MovementType = MovementType.None;
                        break;
                    }
                    else {
                        switch ( PathVectors.Length ) {
                            case 2:
                                transform.position = GetBezierPoint(PathTime, PathVectors[0], PathVectors[1]);
                                break;
                            case 3:
                                transform.position = GetBezierPoint(PathTime, PathVectors[0], PathVectors[1], PathVectors[2]);
                                break;
                            case 4:
                                transform.position = GetBezierPoint(PathTime, PathVectors[0], PathVectors[1], PathVectors[2], PathVectors[3]);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    break;
            }

            // Should we switch the character in and out of the bathroom wall overlay layer?
            if ( transform.position.x > BathroomStartX ) {
                if ( transform.position.y >= BathroomStartY ) {
                    SRenderer.sortingLayerID = 0;
                }
                else {
                    SRenderer.sortingLayerID = sortingLayerIdAboveOverlay;
                }
            }
        }
        /// <summary>
        /// Calculates a linear bezier point
        /// </summary>
        /// <param name="t">Time, from 0f to 1f</param>
        /// <param name="pointA">Starting point</param>
        /// <param name="pointB">Ending point</param>
        /// <returns>BezierPoint</returns>
        private static Vector2 GetBezierPoint( float t, Vector2 pointA, Vector2 pointB ) {
            return pointA + t * (pointB - pointA);
        }
        /// <summary>
        /// Calculates a quadratic bezier point
        /// </summary>
        /// <param name="t">Time, from 0f to 1f</param>
        /// <param name="pointA">Starting point</param>
        /// <param name="pointB">Approached point</param>
        /// <param name="pointC">Ending point</param>
        /// <returns>BezierPoint</returns>
        private static Vector2 GetBezierPoint( float t, Vector2 pointA, Vector2 pointB, Vector2 pointC ) {
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;
            return (uu * pointA) + (2f * u * t * pointB) + (tt * pointC);
        }
        /// <summary>
        /// Calculates a cubic quadratic bezier point
        /// </summary>
        /// <param name="t">Time, from 0f to 1f</param>
        /// <param name="pointA">Starting point</param>
        /// <param name="pointB">Approached point 1</param>
        /// <param name="pointC">Approached point 2</param>
        /// <param name="pointD">Ending point</param>
        /// <returns>BezierPoint</returns>
        private static Vector2 GetBezierPoint( float t, Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD ) {
            var tt = t * t;
            var u = 1 - t;
            var uu = u * u;
            return uu * u * pointA + 3 * uu * t * pointB + 3 * u * tt * pointC + tt * t * pointD;
        }
        private static float EstimatePathLength( Vector2 pointA, Vector2 pointB, Vector2 pointC ) {
            float l = 0f;
            float t = 0.1f;
            Vector2 last = pointA;
            do {
                Vector2 next = GetBezierPoint(t, pointA, pointB, pointC);
                l += Vector2.Distance(last, next);
                t += 0.1f;
                last = next;
            }
            while ( t < 1f );
            return l + Vector2.Distance(last, pointC);
        }
        private static float EstimatePathLength( Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD ) {
            float l = 0f;
            float t = 0.1f;
            Vector2 last = pointA;
            do {
                Vector2 next = GetBezierPoint(t, pointA, pointB, pointC, pointD);
                l += Vector2.Distance(last, next);
                t += 0.1f;
                last = next;
            }
            while ( t < 1f );
            return l + Vector2.Distance(last, pointC);
        }

        #endregion

        #region Desperation/Wetting/Peeing
        // Current willingness to use a urinal for relief
        public static bool WillUseUrinal( Customer customer ) {
            // Yup
            if ( customer.Gender == 'm' ) {
                return true;
            }
            // Only if they're about to lose it
            if ( customer.Gender == 'f' ) {
                return GC.CustomersWillUseAnything || customer.Bladder.LosingControl || customer.Bladder.Fullness > 0.95;
            }
            throw new NotImplementedException();
        }
        public static bool CanUseUrinal( Customer customer ) {
            Bathroom bathroom = customer.GetCurrentBathroom();
            return bathroom != null && bathroom.Urinals.Any(x => x.OccupiedBy == null);
        }
        // Current willingness to use a sink for relief
        public static bool WillUseSink( Customer customer ) {
            if ( GC.CustomersWillUseAnything ) {
                return true;
            }

            switch ( customer.Gender ) {
                case 'm':
                    // It's just a weird urinal you wash your hands in, right?
                    return customer.Bladder.LosingControl || customer.Bladder.Fullness > 0.93d;
                case 'f':
                    // Girls will only use the sink if they're wetting themselves
                    return customer.Bladder.LosingControl || customer.Bladder.Fullness > 0.99d;
                default:
                    throw new NotImplementedException();
            }
        }
        public static bool CanUseSink( Customer customer ) {
            Bathroom bathroom = customer.GetCurrentBathroom();
            if ( bathroom == null ) {
                return false;
            }
            bool hasUnoccupiedSink = bathroom.Sinks.Any(x => x.OccupiedBy == null);
            bool sinksLineEmpty = bathroom.SinksLine.Items[0].OccupiedBy == null;

            return sinksLineEmpty && hasUnoccupiedSink;
        }

        public void BeginPeeingWithThing() {
            // Make it take 2.5x as long for them to finish up if you made them hold it to the point they were about to lose it
            RemainingUrinateStopDelay = UrinateStopDelay;
            if ( Bladder.LosingControl ) {
                RemainingUrinateStopDelay *= 2.5d;
                Debug.Log($"losing control when started peeing and will take longer", this);
            }

            switch ( ReliefType ) {
                case ReliefType.Toilet:
                case ReliefType.Urinal:
                case ReliefType.Sink:
                    SetNext(0f, () => {
                        Emotes.Emote(Emote.PantsDown);
                        CurrentAction = CustomerAction.PantsDown;
                        SetNext((float)UrinateStartDelay, () => {
                            Emotes.ShowBladderCircle(true);
                            CurrentAction = CustomerAction.Peeing;
                        });
                    }, () => AtDestination);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void EndPeeingWithThing() {
            IsRelievingSelf = false;
            Emotes.Emote(null);
            Emotes.ShowBladderCircle(false);
            CurrentAction = CustomerAction.None;
            if ( IsWet ) {
                Leave();
            }
            else if ( Occupying.IType != InteractableType.Sink && UseSink(GetCurrentBathroom()) ) {
                // Handled in UseSink(Bathroom)
            }
            else {
                Seat seat = Bar.Singleton.GetRandomOpenSeat();
                Occupy(seat);
            }
            if ( ReliefType == ReliefType.Toilet ) {
                ((Toilet)Occupying).AltSRenderer.sprite = Collections.spriteStallOpened;
            }
        }
        public void BeginPeeingSelf() {
            Emote emote = Emote.GetPeeStreamEmote(Bladder.Fullness);
            if ( emote == null ) {
                Debug.Break();
                throw new NullReferenceException();
            }
            Emotes.Emote(emote);
            Emotes.ShowBladderCircle(true);
            IsWet = true;
            IsWetting = true;
            // If using something while wetting, it is now soiled.
            if ( AtDestination && Occupying != null && Occupying.CanBeSoiled ) {
                Occupying.IsSoiled = true;
            }
            DesperationState = CustomerDesperationState.State5;
            CurrentAction = CustomerAction.Wetting;
        }
        public void EndPeeingSelf() {
            Emotes.Emote(null);
            Emotes.ShowBladderCircle(false);
            IsWetting = false;
            CurrentAction = CustomerAction.None;
        }
        #endregion

        #region OccupyingCustomerInteractables
        public void StopOccupyingAll() {
            if ( Occupying != null ) {
                Occupying.OccupiedBy = null;
                Occupying = null;
            }
        }
        public void Occupy( CustomerInteractable thing ) {
            if ( Occupying != thing ) {
                Unoccupy();
                MoveTo(thing);
                Location = thing.Location;
                // Move the customer to the thing. Flip sprite if necessary
                if ( thing.Alignment == Alignment.Horizontal ) {
                    SRenderer.flipX = thing.Facing == Orientation.East;
                }

                // Set location, occupying, thing occupying ref and reset time at bar
                Location = thing.Location;
                Occupying = thing;
                thing.OccupiedBy = this;
                MinTimeAtBarNow = 0f;
            }
            else {
                Debug.LogWarning("Customer tried to occupy a thing they were already occupying");
            }
        }
        /// <summary>
        /// Tells any <see cref="CustomerInteractable"/>that it is no longer occupied by this customer, and 
        ///   also clears any occupying reference for this customer
        /// </summary>
        public void Unoccupy() {
            if ( Occupying != null ) {
                Occupying.OccupiedBy = null;
                Occupying = null;
            }
        }
        #endregion

        #region Menu
        // When customer clicked on
        private void OnMouseDown() {
            // Prevent click-through of UI elements to customer
            if ( EventSystem.current.IsPointerOverGameObject() ) {
                return;
            }

            // Can't open menu when game paused.
            if ( GameController.GamePaused ) {
                return;
            }

            if ( CurrentAction == CustomerAction.Peeing ) {
                ReliefMenu.Toggle();
            }
            else if ( ReliefType == ReliefType.None ) {
                BathroomMenu.Toggle();
            }
        }
        private void OnMouseOver() {
            if ( Occupying != null ) {
                if ( Occupying is Toilet toilet ) {
                    toilet.OnMouseOver();
                }
            }
        }
        private void OnMouseExit() {
            if ( Occupying != null ) {
                if ( Occupying is Toilet toilet ) {
                    toilet.OnMouseExit();
                }
            }
        }

        // Buttons for the customer menus
        private Button ButtonDecline;
        private Button ButtonWaitingRoom;
        private Button ButtonToilet;
        private Button ButtonUrinal;
        private Button ButtonSink;
        private Button ButtonReliefStop;
        /// <summary>This menu is available when the customer is the restroom</summary>
        public Menu BathroomMenu;
        private Canvas BathroomMenuCanvas;
        /// <summary>This menu is only available when the customer is relieving themselves</summary>
        public Menu ReliefMenu;
        private Canvas ReliefMenuCanvas;

        /// <summary>
        /// Code for if bathroom menu can be displayed
        /// </summary>
        /// <returns></returns>
        public bool CanDisplayBathroomMenu() {
            if ( !AtDestination ) {
                return false;
            }
            bool inBathroom = Location == Location.BathroomM || Location == Location.BathroomF;
            bool firstInLine = AtDestination && Occupying != null && Occupying is WaitingSpot spot 
                && spot.WaitingSpotType != WaitingSpotType.DrinksGhost && spot.Bathroom.Line.IsNextInLine(this);
            bool acting = CurrentAction != CustomerAction.None;
            return AtDestination && !IsWet && !acting && (inBathroom || firstInLine);
        }
        /// <summary>
        /// Code for if relief menu can be displayed
        /// </summary>
        /// <returns></returns>
        public bool CanDisplayReliefMenu() {
            return CurrentAction == CustomerAction.Peeing && Bladder.Fullness > 0.2d
                && (ReliefType == ReliefType.Urinal || ReliefType == ReliefType.Sink);
        }
        #endregion

        #region MenuActions
        // Sends this customer back to the establishment unrelieved
        public void MenuOptionDismiss() {
            Seat seat = Bar.Singleton.GetRandomOpenSeat();
            EnterBar(seat);
        }
        // Sends this customer to the waiting room
        public WaitingSpot MenuOptionGotoWaiting() {
            Bathroom bathroom = GetCurrentBathroom();
            if ( bathroom.waitingRoom.HasOpenWaitingSpot() ) {
                WaitingSpot waitingSpot = bathroom.waitingRoom.GetNextWaitingSpot();
                Occupy(waitingSpot);
                bathroom.UpdateAvailibility();
                return waitingSpot;
            }
            else {
                Debug.LogError("Message should pop up telling player there are no open spot left!");
                return null;
            }
        }
        // Sends this customer to the toilets.
        public bool MenuOptionGotoToilet() {
            Bathroom bathroom = GetCurrentBathroom();
            if ( bathroom.HasToiletAvailable ) {
                Occupy(bathroom.GetToilet());
                bathroom.UpdateAvailibility();
                BeginPeeingWithThing();
                return true;
            }
            return false;
        }
        public bool MenuOptionGotoUrinal() {
            Bathroom bathroom = GetCurrentBathroom();
            if ( bathroom.HasUrinalAvailable ) {
                Occupy(bathroom.GetUrinal());
                bathroom.UpdateAvailibility();
                BeginPeeingWithThing();
                return true;
            }
            return false;
        }
        public bool MenuOptionGotoSink() {
            Bathroom bathroom = GetCurrentBathroom();
            if ( bathroom.HasSinkForRelief ) {
                Occupy(bathroom.GetSink());
                bathroom.UpdateAvailibility();
                BeginPeeingWithThing();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Commands the customer to stop relief with thing
        /// </summary>
        public void MenuOptionStopPeeing() {
            CurrentAction = CustomerAction.PeeingPinchOff;
            Emotes.Emote(Emote.StruggleStop);

            // If bladder strength is >= 20 (Let out around 20%~)
            if ( Bladder.HoldingPower >= 20 ) {
                SetNext(GameSettings.Current.BladderSettings.DefaultPinchOffTime, () => {
                    Emotes.Emote(Emote.PantsUp, GameSettings.Current.PantsUpTime);
                    CurrentAction = CustomerAction.PantsUp;
                    SetNext(GameSettings.Current.PantsUpTime, () => {
                        EndPeeingWithThing();
                    });
                });
            }
            // Else fail and wet
            else {
                SetNext(GameSettings.Current.BladderSettings.DefaultPinchOffTime, () => {
                    BeginPeeingSelf();
                    SetNext(GameSettings.Current.PantsUpTime, () => {
                        EndPeeingWithThing();
                    });
                });
            }
        }
        #endregion

        #region CustomerPhysicalActions
        // Goes to the doorway queue
        public bool GetInLine( Bathroom bathroom ) {
            CanReenterBathroom = false;
            if ( bathroom.TryEnterQueue(this) ) {
                // Makes customer hold on for a while longer when entering doorway.
                Bladder.ResetReserveHoldingPower();
                return true;
            }
            else {
                return false;
            }
        }
        /// <summary>
        /// Makes a customer either use a sink, or get in line to use a sink. If a sink
        ///   isnt available, the customer will not get in line.
        /// </summary>
        /// <returns>True if customer can use sink or get in line, false if they cannot</returns>
        public bool UseSink( Bathroom bathroom ) {
            // If the customer was already occupying a sink don't use a sink
            if ( Occupying != null ) {
                if ( Occupying.IType == InteractableType.Sink ) {
                    Debug.LogError("UseSink called but customer just relieved using sink. This was disabled as of commit #247");
                    return false;
                }
                Occupying.OccupiedBy = null;
                Occupying = null;
            }

            Sink sink = bathroom.GetSink();
            if ( sink != null ) {
                sink.UseForWash(this);
                return true;
            }
            else if ( bathroom.SinksLine.HasOpenWaitingSpot() ) {
                if ( bathroom.Sinks.Any(x => x.OccupiedBy.CurrentAction == CustomerAction.WashingHands) ) {
                    WaitingSpot spot = bathroom.SinksLine.GetNextWaitingSpot();
                    Occupy(spot);
                    return true;
                }
            }
            return false;
        }
        // Goes back to the bar
        public void EnterBar( Seat seat ) {
            MinTimeAtBarNow = 0f;
            Occupy(seat);
            HasNext = false;
        }
        public void EnterBar() {
            Seat seat = Bar.Singleton.GetRandomOpenSeat();
            MinTimeAtBarNow = 0f;
            Occupy(seat);
            HasNext = false;
        }
        /// <summary>
        /// Leaves the bar, then calls <see cref="GameController.CustomerManager.RemoveCustomer(Customer)"/> on this customer.
        /// </summary>
        public void Leave() {
            StopOccupyingAll();
            MoveTo(Location.Outside);
            Location = Location.Outside;
            SetNext(0f, () => {
                GameController.CM.RemoveCustomer(this);
            }, () => AtDestination);
        }
        #endregion

    }

    public enum MovementType {
        None,
        Manual,
        Path
    }
}