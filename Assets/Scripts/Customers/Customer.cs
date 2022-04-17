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
        private void Awake() {
            UID = GameController.GetUid();
            // Cache sorting layer id
            SRenderer.sortingLayerName = "AboveOverlay";
            sortingLayerIdAboveOverlay = SRenderer.sortingLayerID;
        }
        void Start() {
            Emotes.Start();
        }
        void Update() {
            // Fastest possible exit
            if ( !Active ) {
                return;
            }

            DeltaTime = Time.deltaTime;
            if ( GC.RapidSimulation ) {
                DeltaTime *= 10f;
            }

            TotalTimeAtBar += DeltaTime;
            MinTimeAtBarNow += DeltaTime;
            MinTimeBetweenChecksNow += DeltaTime;
            NextDelay -= DeltaTime;

            // Update bladder
            bladder.Update();

            if ( !bladder.ShouldWetNow ) {
                UpdateDesperationState();
            }

            // Update peeing logic
            PeeLogicUpdate();

            // Think
            Think();

            // Update anim
            customerAnimator.Update();
            // Emote think
            Emotes.Update();

            // Menu updates
            BathroomMenu.Update();
            ReliefMenu.Update();

            // Move sprite
            MoveUpdate();

            // Debug logging
            if ( GameController.GC.LogCustomerStates ) {
                FrameActionDebug();
            }

        }

        public void SetupCustomer(int minBladderPercent, int maxBladderPercent) {
            marshal = CustomerSpriteController.Controller[Gender];

            // Set up the customers animator
            customerAnimator = new CustomerAnimator(this, SRenderer, animator, marshal);

            // Get references to game objects for the customer
            BathroomMenuCanvas = gameObject.transform.Find("BathroomMenuCanvas").GetComponent<Canvas>();
            ReliefMenuCanvas = gameObject.transform.Find("ReliefMenuCanvas").GetComponent<Canvas>();
            // Set up the menus for this customer
            BathroomMenu = new Menu(BathroomMenuCanvas);
            BathroomMenu.canOpenNow = CanDisplayBathroomMenu;
            ReliefMenu = new Menu(ReliefMenuCanvas);
            ReliefMenu.canOpenNow = CanDisplayReliefMenu;

            ButtonWaitingRoom = gameObject.transform.Find("BathroomMenuCanvas/ButtonWait").GetComponent<Button>();
            ButtonDecline = gameObject.transform.Find("BathroomMenuCanvas/ButtonDecline").GetComponent<Button>();
            ButtonToilet = gameObject.transform.Find("BathroomMenuCanvas/ButtonToilet").GetComponent<Button>();
            ButtonUrinal = gameObject.transform.Find("BathroomMenuCanvas/ButtonUrinal").GetComponent<Button>();
            ButtonSink = gameObject.transform.Find("BathroomMenuCanvas/ButtonSink").GetComponent<Button>();
            ButtonReliefStop = gameObject.transform.Find("ReliefMenuCanvas/ButtonReliefStop").GetComponent<Button>();

            // Set up the buttons for the menus
            MenuButton MenuButtonWaitingRoom = new MenuButton(this, BathroomMenu, ButtonWaitingRoom, () => { MenuOptionGotoWaiting(); });
            MenuButton MenuButtonDecline = new MenuButton(this, BathroomMenu, ButtonDecline, () => { MenuOptionDismiss(); });
            MenuButton MenuButtonToilet = new MenuButton(this, BathroomMenu, ButtonToilet, () => { MenuOptionGotoToilet(); });
            MenuButton MenuButtonUrinal = new MenuButton(this, BathroomMenu, ButtonUrinal, () => { MenuOptionGotoUrinal(); }, WillUseUrinal, CanUseUrinal);
            MenuButton MenuButtonSink = new MenuButton(this, BathroomMenu, ButtonSink, () => { MenuOptionGotoSink(); }, WillUseSink, CanUseSink);

            MenuButton MenuButtonReliefStop = new MenuButton(this, ReliefMenu, ButtonReliefStop, () => { MenuOptionStopPeeing(); });

            Funds = Random.Range(20f, 100f);

            bladder = new Bladder();
            bladder.SetupBladder(this, minBladderPercent, maxBladderPercent);
            bladder.customer = this;

            UrinateStartDelay = 4d;
            UrinateStopDelay = 6d;

            customerAnimator.Update();
            Emotes.Update();
            BathroomMenu.Update();
            ReliefMenu.Update();

            bladder.Update();
            UpdateDesperationState();
            PeeLogicUpdate();
        }

        #region NextAction

        public bool HasNext = false;
        public delegate void NextAction();
        public NextAction Next;
        public float NextDelay = 0f;
        /// <summary>Next will wait to trigger until this function returns true</summary>
        public Func<bool> NextWhenTrue = null;
        void SetNext(float delay, NextAction d, Func<bool> whenTrue = null) {
            HasNext = true;
            NextDelay = delay;
            NextWhenTrue = whenTrue;
            Next = d;
        }

        #endregion

        internal float DeltaTime { get; private set; }
        public static GameController GC = null;
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
        public double Funds = 0d;
        public float LastDrinkAt = -25f;
        public float DrinkInterval = 30f;
        public int EnteredTicksElapsed = 0;
        private CustomerSpriteController marshal;
        public CustomerAnimator customerAnimator;
        private CustomerDesperationState GetDesperationState() {
            if ( IsWetting ) {
                return CustomerDesperationState.State5;
            }
            else if ( IsWet ) {
                return CustomerDesperationState.State6;
            }
            else if ( bladder.LosingControl || bladder.FeltNeed > 0.90d ) {
                return CustomerDesperationState.State4;
            }
            else if ( bladder.FeltNeed > 0.80d ) {
                return CustomerDesperationState.State3;
            }
            else if ( bladder.FeltNeed > 0.55d ) {
                return CustomerDesperationState.State2;
            }
            else if ( WantsToEnterBathroom() ) {
                return CustomerDesperationState.State1;
            }
            else {
                return CustomerDesperationState.State0;
            }
        }

        void Think() {
            // Next action handler
            if ( Next != null ) {
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

            // If wet self and finished wetting self
            if ( IsWet && !IsWetting ) {
                SetNext(WetSelfLeaveBathroomDelay, () => { Leave(); });
            }

            if ( IsWetting ) {
                return;
            }

            if ( !AtDestination ) {
                return;
            }

            // If in bar...
            if ( Location == Location.Bar ) {
                ThinkAboutThingsInBar();
            }
        }
        private void ThinkAboutThingsInBar() {

            // Should get up to pee?
            if ( WantsToEnterBathroom() ) {
                if ( ThinkAboutEnteringBathroom() ) {
                    return;
                }
            }

            // Should leave?
            if ( WantsToLeaveBar() ) {
                Leave();
            }

            // Should buy drink?
            //if ( WantsToBuyDrink() && Funds >= Bar.DrinkCost && DesperationState != CustomerDesperationState.State4 ) {
            //    BuyDrink();
            //}

            // Returns true if think about things in bar should return.
            bool ThinkAboutEnteringBathroom() {
                // If they just got sent away don't let them rejoin the line at all.
                if ( MinTimeAtBarNow < 8d ) {
                    return false;
                }

                // If they're about to wet and werent just turned away, have them try to go to the bathroom
                if ( bladder.StartedLosingControlThisFrame ) {
                    //Debug.Log($"Customer {UID} trying to enter bathroom because they are losing control.");
                    bladder.ResetLossOfControlTime();
                    if ( TryEnterBathroom() ) {
                        return true;
                    }
                }

                // Don't let customers about to wet themselves in the bar get into the line. Their fate is sealed.
                var bladderTooFull = bladder.ControlRemaining <= 0d || bladder.LossOfControlTimeNow < bladder.LossOfControlTime;
                if ( bladderTooFull ) {
                    return true;
                }

                if ( MinTimeAtBarNow >= MinTimeAtBar && !bladderTooFull && !IsWetting && !IsWet ) {
                    // Try to enter the bathroom
                    if ( TryEnterBathroom() ) {
                        return true;
                    }
                }

                // If they got more desperate this frame and have waited at least a third the required time, should they run to the bathroom right now?
                if ( DesperationStateChangeThisUpdate && ( MinTimeAtBarNow * 3d ) > MinTimeAtBar ) {
                    if ( DesperationState == CustomerDesperationState.State3 ) {
                        //Debug.Log($"Customer {UID} trying to enter bathroom because they became more desperate.");
                        if ( TryEnterBathroom() ) {
                            return true;
                        }
                    }
                    if ( DesperationState == CustomerDesperationState.State4 ) {
                        //Debug.Log($"Customer {UID} trying to enter bathroom because they became more desperate.");
                        if ( TryEnterBathroom() ) {
                            return true;
                        }
                    }
                }

                return false;
            }

            bool TryEnterBathroom() {
                var bathroom = Gender == 'm' ? Bathroom.BathroomM : Bathroom.BathroomF;
                if ( !GetInLine(bathroom) ) {
                    MinTimeAtBarNow = MinTimeAtBar / 1.5f;
                    return false;
                }
                else {
                    MinTimeAtBarNow = 0f;
                    return true;
                }
            }
        }
        public void BuyDrink() {
            //Debug.Log($"Customer {UID} bought a drink");
            LastDrinkAt = TotalTimeAtBar;
            bladder.Stomach += (float)Bar.DrinkAmount;
            //Funds -= Bar.DrinkCost;
            //GameController.AddFunds(Bar.DrinkCost);
        }

        #region Wants to X...
        public bool WantsToEnterBathroom() {
            return bladder.FeltNeed > 0.40d;
        }
        public bool WantsToLeaveBar() {
            CustomerDesperationState[] tooDesperateStates;

            if ( GC.timeTicksElapsed >= ( GC.NightMaxCustomerSpawnTime + GC.NightMaxCustomerSpawnTime / 2 ) ) {
                tooDesperateStates = new CustomerDesperationState[] {
                CustomerDesperationState.State4
            };
            }
            else {
                tooDesperateStates = new CustomerDesperationState[] {
                CustomerDesperationState.State4,
                CustomerDesperationState.State3
            };
            }

            // Basic assertions
            bool tooDesperate = tooDesperateStates.Contains(DesperationState);
            bool wetted = IsWet && !IsWetting;
            bool stayedTooLong = EnteredTicksElapsed - GC.timeTicksElapsed > 10;
            bool noMoreFunds = Funds < Bar.DrinkCost && TotalTimeAtBar - LastDrinkAt > 30f;
            bool tooLateAtNight = GC.timeTicksElapsed >= GC.NightMaxCustomerSpawnTime;

            // Compound assertions
            bool wouldNormallyLeave = stayedTooLong || noMoreFunds || tooLateAtNight;

            // The thought juice
            return wetted || ( !tooDesperate && wouldNormallyLeave );
        }
        public bool WantsToBuyDrink() {
            return TotalTimeAtBar - LastDrinkAt > DrinkInterval;
        }
        #endregion

        private CustomerActionState LastActionState;
        private void FrameActionDebug() {
            if ( !Active ) {
                return;
            }
            if ( LastActionState != ActionState ) {
                LastActionState = ActionState;
                Debug.Log($"Customer {UID} new action: {ActionState}");
            }

            // Desperation State Logging
            if ( lastState != DesperationState ) {
                lastState = DesperationState;
                AnnounceStateChange();
            }
        }
        public void AnnounceStateChange() {
            string logString = "";
            logString += $"Customer {UID} {lastState} => {DesperationState} @ Bladder: {Math.Round(bladder.Amount)} / {bladder.Max} ({Math.Round(bladder.Percentage, 2)}%)";
            logString += $"Control: {Math.Round(bladder.ControlRemaining)}";
            logString += $"Need: {Math.Round(bladder.FeltNeed, 2)} Curve: {Math.Round(bladder.FeltNeedCurve, 2)}";
            Debug.Log(logString);
        }

        private void PeeLogicUpdate() {
            if ( Next != null && !bladder.ShouldWetNow ) {
                // TODO: If bladder should wet now, issue inturrupt on actions by clearing next
                return;
            }

            // Can customer relieve themselves now?
            ReliefType reliefType = Occupying?.RType ?? ReliefType.None;

            // Get the relief the customer is occupying, if applicable
            Relief relief = reliefType == ReliefType.None ? null : (Relief)Occupying;

            // Behavior depending on if have reached an area they can relieve themselves
            if ( reliefType == ReliefType.None ) {
                // If should wet now
                if ( bladder.ShouldWetNow || CheatPeeNow ) {
                    CheatPeeNow = false;
                    BeginPeeingSelf();
                }
                // If finishing wetting
                else if ( IsWetting && !bladder.Emptying ) {
                    EndPeeingSelf();
                }
            }
            else {
                // When finished peeing
                if ( !bladder.Emptying ) {
                    if ( Next == null ) {
                        SetNext(0f, () => {
                            ActionState = CustomerActionState.PantsUp;
                            Emotes.Emote(Emote.PantsUp);
                            SetNext((float)RemainingUrinateStopDelay, () => {
                                EndPeeingWithThing();
                            });
                        });
                    }
                }
                // If bladder isnt emptying, set it to empty
                // Display unzip/pantsdown animation
                else if ( !bladder.Emptying ) {
                    if ( RemainingUrinateStartDelay > 0 ) {
                        RemainingUrinateStartDelay -= 1 * DeltaTime;
                    }
                    else {
                        bladder.Emptying = true;
                        ActionState = CustomerActionState.Peeing;
                    }
                }
                // Wait for bladder to empty.
                // Display emptying bladder animation
                else if ( bladder.Emptying ) {
                    bladder.ShouldWetNow = false;
                    // Update pee stream emote
                    Emote emote = Emote.GetPeeStreamEmote(bladder.Percentage);
                    if ( emote == null ) {
                        Debug.Break();
                        throw new NullReferenceException();
                    }
                    if ( Emotes.currentEmote != emote ) {
                        Emotes.Emote(emote);
                    }
                }
            }
        }
        /// <summary>
        /// Updates the customers desperation state and state machine
        /// </summary>
        private void UpdateDesperationState() {
            DesperationState = GetDesperationState();
            DesperationStateChangeThisUpdate = DesperationState != LastDesperationState;
            LastDesperationState = DesperationState;
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
        public void MoveTo(CustomerInteractable target) {
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
        public void MoveTo(Location location) {
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
        /// Internal method that sets up customer pathing for an array of vectors.
        /// No logic exists here to determine where they should go, just calls the correct
        /// function to set the customer to move in <see cref="MoveUpdate"/>.
        /// </summary>
        /// <param name="vectors"></param>
        private void MoveTo(Vector2[] vectors) {
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

            void SetBezierPath(Vector2[] vectors) {
                if ( VertexPath2.PathCache.ContainsKey(vectors) ) {
                    Path = VertexPath2.PathCache[vectors];
                }
                else {
                    BezierPath bezierPath = new BezierPath(vectors, false, PathSpace.xy);
                    ApproximatePathLength = 0f;
                    for ( int i = 1; i < vectors.Count(); i++ ) {
                        ApproximatePathLength += Vector2.Distance(vectors[0], vectors[1]);
                    }
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
        private static void DebugDrawVertexPath(VertexPath2 vertexPath, Color color, float time = 3f) {
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
        /// Debugging function that moves the customer to the end of their path instantly.
        /// </summary>
        public void TeleportToDestination() {
            if ( Path != null ) {
                transform.position = Path.GetPointAtTime(1f, EndOfPathInstruction.Stop);
                Path = null;
            }
        }
        /// <summary>
        /// Calculates a linear bezier point
        /// </summary>
        /// <param name="t">Time, from 0f to 1f</param>
        /// <param name="pointA">Starting point</param>
        /// <param name="pointB">Ending point</param>
        /// <returns>BezierPoint</returns>
        public static Vector2 GetBezierPoint(float t, Vector2 pointA, Vector2 pointB) {
            return pointA + t * ( pointB - pointA );
        }
        /// <summary>
        /// Calculates a quadratic bezier point
        /// </summary>
        /// <param name="t">Time, from 0f to 1f</param>
        /// <param name="pointA">Starting point</param>
        /// <param name="pointB">Approached point</param>
        /// <param name="pointC">Ending point</param>
        /// <returns>BezierPoint</returns>
        public static Vector2 GetBezierPoint(float t, Vector2 pointA, Vector2 pointB, Vector2 pointC) {
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;
            return ( uu * pointA ) + ( 2f * u * t * pointB ) + ( tt * pointC );
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
        public static Vector2 GetBezierPoint(float t, Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD) {
            var tt = t * t;
            var u = 1 - t;
            var uu = u * u;
            return uu * u * pointA + 3 * uu * t * pointB + 3 * u * tt * pointC + tt * t * pointD;
        }
        public static float EstimatePathLength(Vector2 pointA, Vector2 pointB, Vector2 pointC) {
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
        public static float EstimatePathLength(Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD) {
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
        public bool AtDestination => MovementType == MovementType.None;
        public Vector2[] PathVectors = new Vector2[0];
        public VertexPath2 Path;
        /// <summary>0f is the start of the path, 1f is the end of the path.</summary>
        public float PathTime = 0f;
        public float ApproximatePathLength;
        public float PathLength;
        public float PathMoveSpeed;
        /// <summary>
        /// https://www.desmos.com/calculator
        /// <para>-\left(0.85-\left(x\cdot1.7\right)\right)^{4}+1.1</para>
        /// </summary>
        public float PathMoveSpeedMultiplier => -Mathf.Pow(-( 0.8f - ( PathTime * 1.6f ) ), 4) + 1.1f;

        public float MoveSpeed;
        public MovementType MovementType = MovementType.None;

        /// <summary>Records the y position that the bathrooms start at, for moving the customer behind the door overlay</summary>
        public static float BathroomStartX;
        public static float BathroomStartY;

        #endregion

        private int sortingLayerIdAboveOverlay = -1;

        public SpriteRenderer SRenderer;
        public Animator animator;
        public string animatorStateName;
        public string lastAnimatorStateName;

        [SerializeField]
        public Text EmotesBladderAmountText;

        public bool CheatPeeNow = false;

        public bool Active = false;
        public int UID = GameController.GetUid();
        public CustomerDesperationState lastState;
        public string DisplayName { get; set; }
        [SerializeField] public char Gender { get; set; }
        // Enum for behavior types
        public bool CanReenterBathroom = true;
        public float CanReenterBathroomIn = 0f;
        public bool IsRelievingSelf = false;
        public bool IsFinishedPeeing;
        [SerializeField]
        public bool IsWetting = false;
        public bool CanWetNow = false;
        public bool IsWet = false;
        public CustomerDesperationState DesperationState = CustomerDesperationState.State0;
        private CustomerDesperationState LastDesperationState = CustomerDesperationState.State0;
        public bool DesperationStateChangeThisUpdate = false;
        public BladderControlState CustomerState = BladderControlState.Normal;
        public Bladder bladder;

        // Times
        public double UrinateStartDelay;
        public double UrinateStopDelay;
        private double RemainingUrinateStartDelay;
        private double RemainingUrinateStopDelay;
        public float WetSelfLeaveBathroomDelay = 6f;
        public float TotalTimeAtBar = 0f;
        public float MinTimeAtBar = 60f;
        public float MinTimeAtBarNow = 0f;
        public float MinTimeBetweenChecks = 8f;
        public float MinTimeBetweenChecksNow = 0f;

        // Position
        public CustomerActionState ActionState = CustomerActionState.None;
        public Location Location = Location.None;
        public CustomerInteractable Occupying;

        // Class that contains all of the emotes / thought bubbles / etc for the person
        public Emotes Emotes;

        #region Desperation/Wetting/Peeing
        // Current willingness to use a urinal for relief
        public static bool WillUseUrinal(Customer customer) {
            // Yup
            if ( customer.Gender == 'm' ) {
                return true;
            }
            // Only if they're about to lose it
            if ( customer.Gender == 'f' ) {
                return GC.CustomersWillUseAnything || customer.bladder.LosingControl || customer.bladder.FeltNeed > 0.93;
            }
            throw new NotImplementedException();
        }
        public static bool CanUseUrinal(Customer customer) {
            Bathroom bathroom = customer.GetCurrentBathroom();
            return bathroom == null ? false : bathroom.Urinals.Any(x => x.OccupiedBy == null);
        }
        // Current willingness to use a sink for relief
        public static bool WillUseSink(Customer customer) {
            if (GC.CustomersWillUseAnything) {
                return true;
            }

            switch (customer.Gender) {
                case 'm':
                    // It's just a weird urinal you wash your hands in, right?
                    return customer.bladder.LosingControl || customer.bladder.FeltNeed > 0.93d;
                case 'f':
                    // Girls will only use the sink if they're wetting themselves
                    return customer.bladder.LosingControl || customer.bladder.FeltNeed > 0.99d;
                default:
                    throw new NotImplementedException();
            }
        }
        public static bool CanUseSink(Customer customer) {
            Bathroom bathroom = customer.GetCurrentBathroom();
            if ( bathroom == null ) {
                return false;
            }
            bool hasUnoccupiedSink = bathroom.Sinks.Any(x => x.OccupiedBy == null);
            bool sinksLineEmpty = bathroom.SinksLine.Items[0].OccupiedBy == null;

            return sinksLineEmpty && hasUnoccupiedSink;
        }
        private ReliefType ReliefType => Occupying?.RType ?? ReliefType.None;

        public void BeginPeeingWithThing() {
            IsRelievingSelf = true;
            Emotes.Emote(Emote.PantsDown);
            Emotes.ShowBladderCircle(true);

            // Make them pee slower if they don't need to go badly to incentivize making them hold it longer
            bladder.DrainRateNow = bladder.DrainRate * 0.75f;
            // Make it take 2.5x as long for them to finish up if you made them hold it to the point they were about to lose it
            RemainingUrinateStopDelay = UrinateStopDelay;
            if ( bladder.LosingControl ) {
                RemainingUrinateStopDelay *= 2.5d;
                //Debug.Log($"Customer {UID} will take longer when they finish up because they were losing control");
            }
            ReliefType reliefType = Occupying?.RType ?? ReliefType.None;
            switch ( reliefType ) {
                case ReliefType.Toilet:
                case ReliefType.Urinal:
                case ReliefType.Sink:
                    ActionState = CustomerActionState.PantsDown;
                    SetNext((float)UrinateStartDelay, () => {
                        bladder.Emptying = true;
                        Relief relief = reliefType == ReliefType.None ? null : (Relief)Occupying;
                        ActionState = CustomerActionState.Peeing;
                    });
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        public void EndPeeingWithThing() {
            IsRelievingSelf = false;
            Emotes.Emote(null);
            Emotes.ShowBladderCircle(false);
            ActionState = CustomerActionState.None;
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
                ( (Toilet)Occupying ).AltSRenderer.sprite = Collections.spriteStallOpened;
            }
        }
        public void BeginPeeingSelf() {
            bladder.Emptying = true;
            Emote emote = Emote.GetPeeStreamEmote(bladder.Percentage);
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
            ActionState = CustomerActionState.Wetting;
        }
        public void EndPeeingSelf() {
            Emotes.Emote(null);
            Emotes.ShowBladderCircle(false);
            IsWetting = false;
            ActionState = CustomerActionState.None;
        }
        #endregion

        #region OccupyingCustomerInteractables
        public void StopOccupyingAll() {
            if ( Occupying != null ) {
                Occupying.OccupiedBy = null;
                Occupying = null;
            }
        }
        public void Occupy(CustomerInteractable thing) {
            if ( Occupying != thing ) {
                Unoccupy();
                MoveTo(thing);
                Location = thing.Location;
                // Move the customer to the thing. Flip sprite if necessary
                if ( thing.Alignment == Alignment.Horizontal ) {
                    SRenderer.flipX = thing.Facing == Orientation.East;
                }

                Location = thing.Location;
                Occupying = thing;
                thing.OccupiedBy = this;
                CanWetNow = thing.CanWetHere;
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

            if ( IsRelievingSelf ) {
                ReliefMenu.Toggle();
            }
            else {
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
        [SerializeField] public Menu BathroomMenu;
        private Canvas BathroomMenuCanvas;
        /// <summary>This menu is only available when the customer is relieving themselves</summary>
        [SerializeField] public Menu ReliefMenu;
        private Canvas ReliefMenuCanvas;

        /// <summary>
        /// Code for if bathroom menu can be displayed
        /// </summary>
        /// <returns></returns>
        public bool CanDisplayBathroomMenu() {
            if (!AtDestination) {
                return false;
            }
            bool inBathroom = Location == Location.BathroomM || Location == Location.BathroomF;
            bool firstInLine = Occupying != null && Occupying is WaitingSpot spot && spot.Bathroom.Line.IsNextInLine(this);
            bool acting = ActionState != CustomerActionState.None;
            return AtDestination && !IsWet && !acting && ( inBathroom || firstInLine );
        }
        /// <summary>
        /// Code for if relief menu can be displayed
        /// </summary>
        /// <returns></returns>
        public bool CanDisplayReliefMenu() {
            return IsRelievingSelf && bladder.Percentage > 0.1d && bladder.Emptying && ( ReliefType == ReliefType.Urinal || ReliefType == ReliefType.Sink );
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
        /// This option commands the customer to stop relief with object
        /// </summary>
        /// <returns></returns>
        public bool MenuOptionStopPeeing() {
            // TODO: Make them wet themselves if they are still very full or have no control remaining and are commanded to stop
            bladder.StopPeeingEarly();
            Emotes.Emote(Emote.StruggleStop);
            SetNext(0f, () => {
                float t = 1f;
                Emotes.Emote(Emote.PantsUp, t);
                ActionState = CustomerActionState.PantsUp;
                SetNext(t, () => {
                    EndPeeingWithThing();
                });
            }, () => !bladder.Emptying);
            return true;
        }
        #endregion

        #region CustomerPhysicalActions
        // Goes to the doorway queue
        public bool GetInLine(Bathroom bathroom) {
            CanReenterBathroom = false;
            if ( bathroom.TryEnterQueue(this) ) {
                // Makes customer hold on for a while longer when entering doorway.
                bladder.ResetLossOfControlTime();
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
        public bool UseSink(Bathroom bathroom) {
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
                if ( bathroom.Sinks.Any(x => x.OccupiedBy.ActionState == CustomerActionState.WashingHands) ) {
                    WaitingSpot spot = bathroom.SinksLine.GetNextWaitingSpot();
                    Occupy(spot);
                    return true;
                }
            }
            return false;
        }
        // Goes back to the bar
        public void EnterBar(Seat seat) {
            MinTimeAtBarNow = 0f;
            MinTimeBetweenChecksNow = 0f;
            Occupy(seat);
            HasNext = false;
        }
        public void EnterBar() {
            Seat seat = Bar.Singleton.GetRandomOpenSeat();
            MinTimeAtBarNow = 0f;
            MinTimeBetweenChecksNow = 0f;
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