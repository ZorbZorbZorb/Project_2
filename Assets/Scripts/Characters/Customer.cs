using Assets.Scripts;
using Assets.Scripts.Characters;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Customer : MonoBehaviour {
    void Start() {
        if ( Destination == null ) {
            Destination = transform.position;
        }
        UID = GameController.GetUid();

        // Set up the emotes system for this customer
        Emotes = new Emotes(this, EmoteSpriteRenderer, BladderCircleTransform, EmotesBladderAmountText);
    }
    void Update() {
        // Fastest possible exit
        if ( !Active ) {
            return;
        }

        TotalTimeAtBar += Time.deltaTime;
        MinTimeAtBarNow += Time.deltaTime;
        MinTimeBetweenChecksNow += Time.deltaTime;
        NextDelay -= Time.deltaTime;

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
        if ( GameController.GC.DebugStateLogging ) {
            FrameActionDebug();
        }

    }
    public static GameController GC = null;
    public void SetupCustomer(int minBladderPercent, int maxBladderPercent) {
        marshal = CustomerSpriteController.Controller[Gender];

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
        MenuButton MenuButtonUrinal = new MenuButton(this, BathroomMenu, ButtonUrinal, () => { MenuOptionGotoUrinal(); }, WillUseUrinal);
        MenuButton MenuButtonSink = new MenuButton(this, BathroomMenu, ButtonSink, () => { MenuOptionGotoSink(); }, WillUseSink);

        MenuButton MenuButtonReliefStop = new MenuButton(this, ReliefMenu, ButtonReliefStop, () => { MenuOptionStopPeeing(); });

        Funds = Random.Range(20f, 100f);

        bladder = new Bladder();
        bladder.SetupBladder(this, minBladderPercent, maxBladderPercent);
        bladder.customer = this;

        UrinateStartDelay = 4d;
        UrinateStopDelay = 6d;

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

    public Bathroom CurrentBathroom = null;
    public Bathroom OtherBathroom = null;
    public double Funds = 0d;
    public float LastDrinkAt = -25f;
    public float DrinkInterval = 30f;
    public int EnteredTicksElapsed = 0;
    private CustomerSpriteController marshal;
    public CustomerAnimator customerAnimator;
    private Collections.CustomerDesperationState GetDesperationState() {
        if ( IsWetting ) {
            return Collections.CustomerDesperationState.State5;
        }
        else if ( IsWet ) {
            return Collections.CustomerDesperationState.State6;
        }
        else if ( bladder.LosingControl || bladder.FeltNeed > 0.90d ) {
            return Collections.CustomerDesperationState.State4;
        }
        else if ( bladder.FeltNeed > 0.80d ) {
            return Collections.CustomerDesperationState.State3;
        }
        else if ( bladder.FeltNeed > 0.55d ) {
            return Collections.CustomerDesperationState.State2;
        }
        else if ( WantsToEnterBathroom() ) {
            return Collections.CustomerDesperationState.State1;
        }
        else {
            return Collections.CustomerDesperationState.State0;
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

        // If about to leave or has left
        if ( Location == Location.Outside && AtDestination && transform.position == Collections.OffScreenTop ) {
            GC.RemoveCustomer(this);
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
        if ( WantsToBuyDrink() && Funds >= Bar.DrinkCost && DesperationState != Collections.CustomerDesperationState.State4 ) {
            BuyDrink();
        }

        // Returns true if think about things in bar should return.
        bool ThinkAboutEnteringBathroom() {
            // If they just got sent away don't let them rejoin the line at all.
            if ( MinTimeAtBarNow < 8d ) {
                return false;
            }

            // If they're about to wet and werent just turned away, have them try to go to the bathroom
            if ( bladder.StartedLosingControlThisFrame ) {
                Debug.Log($"Customer {UID} trying to enter bathroom because they are losing control.");
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
                if ( DesperationState == Collections.CustomerDesperationState.State3 ) {
                    Debug.Log($"Customer {UID} trying to enter bathroom because they became more desperate.");
                    if ( TryEnterBathroom() ) {
                        return true;
                    }
                }
                if ( DesperationState == Collections.CustomerDesperationState.State4 ) {
                    Debug.Log($"Customer {UID} trying to enter bathroom because they became more desperate.");
                    if ( TryEnterBathroom() ) {
                        return true;
                    }
                }
            }

            return false;
        }

        bool TryEnterBathroom() {
            if ( !EnterDoorway() ) {
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
        Debug.Log($"Customer {UID} bought a drink");
        LastDrinkAt = TotalTimeAtBar;
        bladder.Stomach += Bar.DrinkAmount;
        //Funds -= Bar.DrinkCost;
        //GameController.AddFunds(Bar.DrinkCost);
    }

    #region Wants to X...
    public bool WantsToEnterBathroom() {
        return bladder.FeltNeed > 0.40d;
    }
    public bool WantsToLeaveBar() {
        Collections.CustomerDesperationState[] tooDesperateStates;

        if ( GC.timeTicksElapsed >= ( GC.nightMaxCustomerSpawnTime + GC.nightMaxCustomerSpawnTime / 2 ) ) {
            tooDesperateStates = new Collections.CustomerDesperationState[] {
                Collections.CustomerDesperationState.State4
            };
        }
        else {
            tooDesperateStates = new Collections.CustomerDesperationState[] {
                Collections.CustomerDesperationState.State4,
                Collections.CustomerDesperationState.State3
            };
        }

        // Basic assertions
        bool tooDesperate = tooDesperateStates.Contains(DesperationState);
        bool wetted = IsWet && !IsWetting;
        bool stayedTooLong = EnteredTicksElapsed - GC.timeTicksElapsed > 10;
        bool noMoreFunds = Funds < Bar.DrinkCost && TotalTimeAtBar - LastDrinkAt > 30f;
        bool tooLateAtNight = GC.timeTicksElapsed >= GC.nightMaxCustomerSpawnTime;

        // Compound assertions
        bool wouldNormallyLeave = stayedTooLong || noMoreFunds || tooLateAtNight;

        // The thought juice
        return wetted || ( !tooDesperate && wouldNormallyLeave );
    }
    public bool WantsToBuyDrink() {
        return TotalTimeAtBar - LastDrinkAt > DrinkInterval;
    }
    #endregion

    private Collections.CustomerActionState LastActionState;
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
                        ActionState = Collections.CustomerActionState.Peeing;
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
                    RemainingUrinateStartDelay -= 1 * Time.deltaTime;
                }
                else {
                    bladder.Emptying = true;
                    ActionState = Collections.CustomerActionState.Peeing;
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
        List<Vector3> vectors = Assets.Scripts.Navigation.Navigate(Location, target.Location);
        vectors.Add(Gender == 'm' ? target.CustomerPositionM : target.CustomerPositionF);
        Navigation.AddRange(vectors);
        Destination = vectors.Last();
        AtDestination = false;
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
        List<Vector3> vectors = Assets.Scripts.Navigation.Navigate(Location, location);
        if ( vectors.Any() ) {
            Navigation.AddRange(vectors);
            Destination = vectors.Last();
            AtDestination = false;
        }
    }
    [Obsolete]
    public void MoveTo(Vector3 destination) {
        Navigation.Add(destination);
        Destination = destination;
        AtDestination = false;
    }
    private void MoveUpdate() {
        AtDestination = Navigation.Count == 0;
        if ( AtDestination ) {
            return;
        }

        Vector3 next = Navigation.First();

        if ( transform.position.x != next.x || transform.position.y != next.y ) {
            float distanceX = Math.Abs(transform.position.x - next.x);
            float distanceY = Math.Abs(transform.position.y - next.y);
            float moveAmount = Math.Max(2.1f * ( distanceX + distanceY ), (float)MoveSpeed);
            transform.position = Vector3.MoveTowards(transform.position, next, moveAmount * Time.deltaTime);
        }
        else {
            Navigation.Remove(next);
        }
    }
    public bool AtDestination { get; private set; }

    public SpriteRenderer SRenderer;
    public Animator animator;
    public string animatorStateName;
    public string lastAnimatorStateName;

    [SerializeField]
    public Text EmotesBladderAmountText;

    public bool CheatPeeNow = false;

    public bool Active = false;
    public int UID = GameController.GetUid();
    public Collections.CustomerDesperationState lastState;
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
    public Collections.CustomerDesperationState DesperationState = Collections.CustomerDesperationState.State0;
    private Collections.CustomerDesperationState LastDesperationState = Collections.CustomerDesperationState.State0;
    public bool DesperationStateChangeThisUpdate = false;
    public Collections.BladderControlState CustomerState = Collections.BladderControlState.Normal;
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
    public Collections.CustomerActionState ActionState = Collections.CustomerActionState.None;
    public Location Location = Location.Bar;
    public CustomerInteractable Occupying;

    // Movement
    public Vector3 Destination;
    public List<Vector3> Navigation = new List<Vector3>();
    public double MoveSpeed;

    #region Sprites

    // Class that contains all of the emotes / thought bubbles / etc for the person
    [SerializeField] public RectTransform BladderCircleTransform;
    [SerializeField] public SpriteRenderer EmoteSpriteRenderer;
    public Emotes Emotes;
    #endregion

    #region Desperation/Wetting/Peeing
    // Current willingness to use a urinal for relief
    public bool WillUseUrinal(Customer customer) {
        // Yup
        if ( customer.Gender == 'm' ) {
            return true;
        }
        // Only if they're about to lose it
        if ( customer.Gender == 'f' ) {
            return GC.DebugCustomersWillinglyUseAny || bladder.LosingControl || bladder.FeltNeed > 0.93;
        }
        throw new NotImplementedException();
    }
    // Current willingness to use a sink for relief
    public bool WillUseSink(Customer customer) {
        // It's just a weird urinal you wash your hands in, right?
        if ( customer.Gender == 'm' ) {
            return GC.DebugCustomersWillinglyUseAny || bladder.LosingControl || bladder.FeltNeed > 0.93d;
        }
        // Girls will only use the sink if they're wetting themselves
        if ( customer.Gender == 'f' ) {
            return GC.DebugCustomersWillinglyUseAny || bladder.LosingControl || bladder.FeltNeed > 0.99d;
        }
        throw new NotImplementedException();
    }
    private ReliefType ReliefType => Occupying?.RType ?? ReliefType.None;

    public void BeginPeeingWithThing() {
        IsRelievingSelf = true;
        Emotes.Emote(Emote.PantsDown);
        Emotes.ShowBladderCircle(true);

        // Make them pee slower if they don't need to go badly to incentivize making them hold it longer
        bladder.DrainRateNow = bladder.DrainRate * 0.75;
        // Make it take 2.5x as long for them to finish up if you made them hold it to the point they were about to lose it
        RemainingUrinateStopDelay = UrinateStopDelay;
        if ( bladder.LosingControl ) {
            RemainingUrinateStopDelay *= 2.5d;
            Debug.Log($"Customer {UID} will take longer when they finish up because they were losing control");
        }
        ReliefType reliefType = Occupying?.RType ?? ReliefType.None;
        switch ( reliefType ) {
            case ReliefType.Toilet:
            case ReliefType.Urinal:
            case ReliefType.Sink:
                ActionState = Collections.CustomerActionState.PantsDown;
                SetNext((float)UrinateStartDelay, () => {
                    bladder.Emptying = true;
                    Relief relief = reliefType == ReliefType.None ? null : (Relief)Occupying;
                    ActionState = Collections.CustomerActionState.Peeing;
                });
                break;
            default:
                throw new NotImplementedException();
        }
    }
    public void EndPeeingWithThing() {
        Debug.Log($"Customer {UID} finished relieving themselves.");
        IsRelievingSelf = false;
        Emotes.Emote(null);
        Emotes.ShowBladderCircle(false);
        ActionState = Collections.CustomerActionState.None;
        if ( IsWet ) {
            MoveTo(Location.Outside);
            Location = Location.Outside;
            Occupying.OccupiedBy = null;
            Occupying = null;
        }
        else if ( CurrentBathroom.SinksLine.HasOpenWaitingSpot() ) {
            CurrentBathroom.EnterSinkQueue(this);
        }
        else {
            ActionState = Collections.CustomerActionState.None;
            Seat seat = Bar.Singleton.GetOpenSeat();
            seat.MoveCustomerIntoSpot(this);
        }

        if ( ReliefType == ReliefType.Toilet ) {
            ( (Toilet)Occupying ).AltSRenderer.sprite = Collections.spriteStallOpened;
        }
    }
    public void BeginPeeingSelf() {
        GameController.AddWetting();
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
        DesperationState = Collections.CustomerDesperationState.State5;
        ActionState = Collections.CustomerActionState.Wetting;
    }
    public void EndPeeingSelf() {
        Debug.Log("StopWettingSelf");
        Emotes.Emote(null);
        Emotes.ShowBladderCircle(false);
        IsWetting = false;
        ActionState = Collections.CustomerActionState.None;
    }
    #endregion

    #region OccupyingCustomerInteractables
    public void StopOccupyingAll() {
        if ( Occupying != null ) {
            Occupying.OccupiedBy = null;
            Occupying = null;
        }
    }
    public void UseInteractable(CustomerInteractable thing) {
        // Use caution when thing intended to be occupied is already occupied by customer.
        if ( Occupying != null ) {
            Occupying.OccupiedBy = null;
        }
        Location = thing.Location;
        MoveTo(thing);
        // Move the customer to the thing. Flip sprite if necessary
        if (thing.Alignment == Alignment.Horizontal) {
            SRenderer.flipX = thing.Facing == Orientation.West;
        }

        Location = thing.Location;
        Occupying = thing;
        thing.OccupiedBy = this;
        CanWetNow = thing.CanWetHere;
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
        bool inBathroom = Location == Location.BathroomM || Location == Location.BathroomF;
        bool firstInLine = CurrentBathroom != null && CurrentBathroom.doorwayQueue.IsNextInLine(this);
        bool wet = IsWet || IsWetting;
        return AtDestination && !wet && ( inBathroom || firstInLine );
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
        Seat seat = Bar.Singleton.GetOpenSeat();
        EnterBar(seat);
    }
    // Sends this customer to the waiting room
    public WaitingSpot MenuOptionGotoWaiting() {
        if ( CurrentBathroom.waitingRoom.HasOpenWaitingSpot() ) {
            WaitingSpot waitingSpot = CurrentBathroom.waitingRoom.GetNextWaitingSpot();
            waitingSpot.MoveCustomerIntoSpot(this);
            Location = CurrentBathroom.Location;
            return waitingSpot;
        }
        else {
            Debug.LogError("Message should pop up telling player there are no open spot left!");
            return null;
        }
    }
    // Sends this customer to the toilets.
    public bool MenuOptionGotoToilet() {
        if ( CurrentBathroom.HasToiletAvailable ) {
            EnterRelief(CurrentBathroom.GetToilet());
            return true;
        }
        return false;
    }
    public bool MenuOptionGotoUrinal() {
        if ( CurrentBathroom.HasUrinalAvailable ) {
            EnterRelief(CurrentBathroom.GetUrinal());
            return true;
        }
        return false;
    }
    public bool MenuOptionGotoSink() {
        if ( CurrentBathroom.HasSinkForRelief ) {
            EnterRelief(CurrentBathroom.GetSink());
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
            ActionState = Collections.CustomerActionState.PantsUp;
            SetNext(t, () => {
                EndPeeingWithThing();
            });
        }, () => !bladder.Emptying);
        return true;
    }
    #endregion

    #region CustomerPhysicalActions
    // Sends this customer to relief
    public void EnterRelief(Relief relief) {
        UseInteractable(relief);
        BeginPeeingWithThing();
    }
    // Goes to the doorway queue
    public bool EnterDoorway() {
        CurrentBathroom = Gender == 'm' ? Bathroom.BathroomM : Bathroom.BathroomF;
        OtherBathroom = Gender == 'm' ? Bathroom.BathroomF : Bathroom.BathroomM;

        if ( CurrentBathroom.doorwayQueue.HasOpenWaitingSpot() ) {
            // Makes customer hold on for a while longer when entering doorway.
            bladder.ResetLossOfControlTime();
            WaitingSpot waitingSpot = CurrentBathroom.doorwayQueue.GetNextWaitingSpot();
            waitingSpot.MoveCustomerIntoSpot(this);
            Location = Location.Hallway;
            CanReenterBathroom = false;
            return true;
        }
        #warning Hey, this is where you add the code to make them use the opposite sex's restroom
        // If queue in doorway is full this counts as instantly getting sent away.
        else {
            CanReenterBathroom = false;
            return false;
        }
    }
    // Goes back to the bar
    public void EnterBar(Seat seat) {
        MinTimeAtBarNow = 0f;
        MinTimeBetweenChecksNow = 0f;
        seat.MoveCustomerIntoSpot(this);
        HasNext = false;
    }
    public void EnterBar() {
        Seat seat = Bar.Singleton.GetOpenSeat();
        MinTimeAtBarNow = 0f;
        MinTimeBetweenChecksNow = 0f;
        seat.MoveCustomerIntoSpot(this);
        HasNext = false;
    }
    // Fully leaves the area
    public void Leave() {
        StopOccupyingAll();
        MoveTo(Location.Outside);
        SetNext(0f, () => { 
            Location = Location.Outside; 
        }, () => AtDestination );
    }
    #endregion

}
