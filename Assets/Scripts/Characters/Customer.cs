using Assets.Scripts;
using Assets.Scripts.Characters;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Customer : MonoBehaviour {
    void Start() {

        if (Destination == null) {
            Destination = this.transform.position;
        }
        UID = GameController.GetUid();

        // Set up the emotes system for this customer
        Emotes = new Emotes(this, EmoteSpriteRenderer, BladderCircleTransform, EmotesBladderAmountText);

    }

    public void SetupCustomer(int minBladderPercent, int maxBladderPercent) {
        // Set up the menus for this customer
        BathroomMenu = new Menu(BathroomMenuCanvas);
        BathroomMenu.canOpenNow = CanDisplayBathroomMenu;
        ReliefMenu = new Menu(ReliefMenuCanvas);
        ReliefMenu.canOpenNow = CanDisplayReliefMenu;

        // Set up the buttons for the menus
        MenuButton MenuButtonWaitingRoom = new MenuButton(this, BathroomMenu, ButtonWaitingRoom, () => { MenuOptionGotoWaiting(); });
        MenuButton MenuButtonDecline = new MenuButton(this, BathroomMenu, ButtonDecline, () => { MenuOptionDismiss(); });
        MenuButton MenuButtonToilet = new MenuButton(this, BathroomMenu, ButtonToilet, () => { MenuOptionGotoToilet(); });
        MenuButton MenuButtonUrinal = new MenuButton(this, BathroomMenu, ButtonUrinal, () => { MenuOptionGotoUrinal(); }, WillUseUrinal);
        MenuButton MenuButtonSink = new MenuButton(this, BathroomMenu, ButtonSink, () => { MenuOptionGotoSink(); }, WillUseSink);

        MenuButton MenuButtonReliefStop = new MenuButton(this, ReliefMenu, ButtonReliefStop, () => { MenuOptionStopPeeing(); });

        Funds = Random.Range(20f, 100f);

        bladder = new Bladder();
        bladder.SetupBladder(minBladderPercent, maxBladderPercent);

        UrinateStartDelay = 4d;
        UrinateStopDelay = 6d;

        bladder.Update();
        PeeLogicUpdate();
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

        // Update peeing logic
        PeeLogicUpdate();

        // Think
        Think();

        // Update anim
        SpriteUpdate();
        // Emote think
        Emotes.Update();

        // Menu updates
        BathroomMenu.Update();
        ReliefMenu.Update();

        // Move sprite
        MoveUpdate();

        // Debug logging
        FrameActionDebug();
    }

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
    public double Funds = 0d;
    public float LastDrinkAt = -25f;
    public float DrinkInterval = 30f;

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
        else if ( bladder.FeltNeed > 0.55d) {
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
            else if (NextWhenTrue == null || NextWhenTrue()) {
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
        if ( position == Collections.Location.Outside && AtDestination() && transform.position == Collections.OffScreenTop ) {
            GameController.controller.RemoveCustomer(this);
        }

        // If wet self and finished wetting self
        if ( IsWet && !IsWetting ) {
            SetNext(WetSelfLeaveBathroomDelay, () => { Leave(); });
        }

        if ( IsWetting ) {
            return;
        }

        if (!AtDestination()) {
            return;
        }

        // If in bar...
        if ( position == Collections.Location.Bar ) {
            ThinkAboutThingsInBar();
        }
    }

    private void ThinkAboutThingsInBar() {

        // Should get up to pee?
        if ( WantsToEnterBathroom() ) {

            // If they just got sent away don't let them rejoin the line at all.
            if ( MinTimeAtBarNow < 8d ) {
                return;
            }

            // If they're about to wet and werent just turned away, have them try to go to the bathroom
            if ( bladder.StartedLosingControlThisFrame ) {
                Debug.Log($"Customer {UID} trying to enter bathroom because they are losing control.");
                bladder.ResetLossOfControlTime();
                TryEnterBathroom();
            }

            // Don't let customers about to wet themselves in the bar get into the line. Their fate is sealed.
            var bladderTooFull = bladder.ControlRemaining <= 0d || bladder.LossOfControlTimeNow < bladder.LossOfControlTime;
            if (bladderTooFull) {
                return;
            }

            if ( MinTimeAtBarNow >= MinTimeAtBar && !bladderTooFull && !IsWetting && !IsWet ) {
                // Try to enter the bathroom
                TryEnterBathroom();
                return;
            }

            // If they got more desperate this frame and have waited at least a third the required time, should they run to the bathroom right now?
            if (DesperationStateChangeThisUpdate && (MinTimeAtBarNow * 3d) > MinTimeAtBar) {
                if (DesperationState == Collections.CustomerDesperationState.State3) {
                    Debug.Log($"Customer {UID} trying to enter bathroom because they became more desperate.");
                    TryEnterBathroom();
                }
                if (DesperationState == Collections.CustomerDesperationState.State4) {
                    Debug.Log($"Customer {UID} trying to enter bathroom because they became more desperate.");
                    TryEnterBathroom();
                }
            }

        }

        // Should leave?
        else if ( WantsToLeaveBar() ) {
            Leave();
        }

        // Should buy drink?
        else {
            if ( Funds >= Bar.DrinkCost && TotalTimeAtBar - LastDrinkAt > DrinkInterval ) {
                BuyDrink();
            }
        }

        void TryEnterBathroom() {
            if ( !EnterDoorway() ) {
                MinTimeAtBarNow = MinTimeAtBar / 1.5f;
            }
            else {
                MinTimeAtBarNow = 0f;
            }
        }
    }

    public void BuyDrink() {
        Debug.Log($"Customer {UID} bought a drink");
        LastDrinkAt = TotalTimeAtBar;
        bladder.Stomach += Bar.DrinkAmount;
        Funds -= Bar.DrinkCost;
        GameController.AddFunds(Bar.DrinkCost);

    }

    /// <summary>
    /// Customer wants to enter the bathroom right now.
    /// </summary>
    /// <returns></returns>
    public bool WantsToEnterBathroom() {
        return bladder.FeltNeed > 0.40d;
    }
    /// <summary>
    /// Customer wants to buy a drink right now.
    /// </summary>
    /// <returns></returns>
    public bool WantsToLeaveBar() {
        Collections.CustomerDesperationState[] tooDesperateStates = {
            Collections.CustomerDesperationState.State4,
            Collections.CustomerDesperationState.State3
        };
        bool tooDesperate = tooDesperateStates.Contains(DesperationState);

        return
            ( IsWet && !IsWetting ) ||

            !tooDesperate &&
            ((TotalTimeAtBar / GameController.controller.AdvanceBarTimeEveryXSeconds ) * GameController.controller.AdvanceBarTimeByXMinutes > 120 || 
                Funds < Bar.DrinkCost && TotalTimeAtBar - LastDrinkAt > 30f);
    }

    private Collections.CustomerActionState LastActionState;
    private void FrameActionDebug() {
        if (!Active) {
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
        Debug.LogWarning(logString);
    }

    private void PeeLogicUpdate() {
        if ( Next != null && !bladder.ShouldWetNow ) {
            // TODO: If bladder should wet now, issue inturrupt on actions by clearing next
            return;
        }

        UpdateDesperationState();

        // Can customer relieve themselves now?
        Collections.ReliefType reliefType = Occupying?.ReliefType ?? Collections.ReliefType.None;

        // Get the relief the customer is occupying, if applicable
        Relief relief = reliefType == Collections.ReliefType.None ? null : (Relief)Occupying;

        // Behavior depending on if have reached an area they can relieve themselves
        if ( reliefType == Collections.ReliefType.None ) {
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
                if (!HasNext) {
                    SetNext(0f, () => {
                        ActionState = relief.StatePantsUp;
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
                if (!HasNext) {

                }
                if ( RemainingUrinateStartDelay > 0 ) {
                    RemainingUrinateStartDelay -= 1 * Time.deltaTime;
                }
                else {
                    bladder.Emptying = true;
                    ActionState = relief.StatePeeing;
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

    public void MoveToVector3(Vector3 destination) {
        Navigation.Add(destination);
        Destination = destination;
    }
    private void MoveUpdate() {
        if (Navigation.Count == 0) {
            return;
        }
        Vector3 next = Navigation.First();

        if ( transform.position.x != next.x || transform.position.y != next.y ) {
            float distanceX = Math.Abs(transform.position.x - next.x);
            float distanceY = Math.Abs(transform.position.y - next.y);
            float moveAmount = Math.Max(2.1f * (distanceX + distanceY), (float)MoveSpeed);
            transform.position = Vector3.MoveTowards(transform.position, next, moveAmount * Time.deltaTime);
        }
        else {
            Navigation.Remove(next);
        }
    }
    public bool AtDestination() {
        return transform.position == Destination && Navigation.Count() < 2;
    }

    [SerializeField]
    public SpriteRenderer SRenderer;

    [SerializeField]
    public Text EmotesBladderAmountText;

    public bool CheatPeeNow = false;

    public static Bar Bar;

    public bool Active = false;
    public int UID = GameController.GetUid();
    public Collections.CustomerDesperationState lastState;
    public string DisplayName { get; set; }
    [SerializeField]
    public char Gender { get; set; }
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
    public Collections.BladderControlState CustomerState = Collections.BladderControlState.Normal;
    public Bladder bladder = new Bladder();
    public int Shyness { get; set; }
    // State machine for desperation state
    private Collections.CustomerDesperationState LastDesperationState = Collections.CustomerDesperationState.State0;
    public bool DesperationStateChangeThisUpdate = false;

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
    public Collections.Location position = Collections.Location.Bar;
    public CustomerInteractable Occupying;

    // Movement
    public Vector3 Destination;
    public List<Vector3> Navigation = new List<Vector3>();
    [SerializeField]
    public double MoveSpeed;

    #region Sprites
    public void SpriteUpdate() {
        // Set the sprite
        Sprite sprite = Collections.GetPersonSprite(this);
        if ( sprite != SRenderer.sprite ) {
            SRenderer.sprite = sprite;
        }

        // Sprite shaking to show desperation
        // TODO: Perhaps shake more or less when shy, maybe have shaking be the true desperation state?
        // Notice: The sprite is parented to a customer gameobject and is not a part of it. this.gameObject.transform can be used to re-parent it.
        // Do not run if paused.
        if ( !GameController.GamePaused ) {
            switch ( DesperationState ) {
                case Collections.CustomerDesperationState.State4:
                if ( Time.frameCount % 2 == 0 ) {
                    SRenderer.transform.position = this.gameObject.transform.position + new Vector3(Random.Range(0, 5), Random.Range(0, 5), 0);
                }
                break;
                case Collections.CustomerDesperationState.State3:
                if ( Time.frameCount % 20 == 0 ) {
                    SRenderer.transform.position = this.gameObject.transform.position + new Vector3(Random.Range(0, 4), 0, 0);
                }
                break;
                default:
                SRenderer.transform.position = this.gameObject.transform.position;
                break;
            }
        }
    }

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
            return bladder.LosingControl || bladder.FeltNeed > 0.93;
        }
        throw new NotImplementedException();
    }
    // Current willingness to use a sink for relief
    public bool WillUseSink(Customer customer) {
        // It's just a weird urinal you wash your hands in, right?
        if ( customer.Gender == 'm') {
            return bladder.LosingControl || bladder.FeltNeed > 0.93d;
        }
        // Girls will only use the sink if they're wetting themselves
        if ( customer.Gender == 'f' ) {
            return bladder.LosingControl || bladder.FeltNeed > 0.99d;
        }
        throw new NotImplementedException();
    }
    private Collections.ReliefType ReliefType => Occupying?.ReliefType ?? Collections.ReliefType.None;

    public void BeginPeeingWithThing() {
        IsRelievingSelf = true;
        Emotes.Emote(Emote.PantsDown);
        Emotes.ShowBladderCircle(true);

        // Make it take 2.5x as long for them to finish up if you made them hold it to the point they were about to lose it
        RemainingUrinateStopDelay = RemainingUrinateStopDelay;
        if ( bladder.LosingControl ) {
            RemainingUrinateStopDelay = RemainingUrinateStopDelay * 2.5d;
            Debug.Log($"Customer {UID} will take longer when they finish up because they were losing control");
        }
        Collections.ReliefType reliefType = Occupying?.ReliefType ?? Collections.ReliefType.None;
        switch ( reliefType ) {
            case Collections.ReliefType.Toilet:
            ActionState = Collections.CustomerActionState.ToiletPantsDown;
            SetNext((float)UrinateStartDelay, () => {
                bladder.Emptying = true;
                Relief relief = reliefType == Collections.ReliefType.None ? null : (Relief)Occupying;
                ActionState = relief.StatePeeing;
            });
            break;
            case Collections.ReliefType.Urinal:
            ActionState = Collections.CustomerActionState.UrinalPantsDown;
            SetNext((float)UrinateStartDelay, () => {
                bladder.Emptying = true;
                Relief relief = reliefType == Collections.ReliefType.None ? null : (Relief)Occupying;
                ActionState = relief.StatePeeing;
            });
            break;
            case Collections.ReliefType.Sink:
            ActionState = Collections.CustomerActionState.SinkPantsDown;
            SetNext((float)UrinateStartDelay, () => {
                bladder.Emptying = true;
                Relief relief = reliefType == Collections.ReliefType.None ? null : (Relief)Occupying;
                ActionState = relief.StatePeeing;
            });
            break;
            case Collections.ReliefType.Towel:
            ActionState = Collections.CustomerActionState.TowelPantsDown;
            throw new NotImplementedException();
            break;
        }
    }
    public void EndPeeingWithThing() {
        Debug.Log($"Customer {UID} finished relieving themselves.");
        IsRelievingSelf = false;
        Emotes.Emote(null);
        Emotes.ShowBladderCircle(false);
        ActionState = Collections.CustomerActionState.None;
        if ( IsWet ) {
            if ( position != Collections.Location.Bar ) {
                foreach (Vector3 keyframe in Collections.NavigationKeyframesFromBathroomToBar) {
                    MoveToVector3(keyframe);
                }
            }
            MoveToVector3(Collections.OffScreenTop);
            position = Collections.Location.Outside;
            Occupying.OccupiedBy = null;
            Occupying = null;
        }
        else if (Bathroom.bathroom.Sinks.Line.HasOpenWaitingSpot() && !Bathroom.bathroom.Sinks.AllSinksBeingPeedIn()) {
            Bathroom.bathroom.Sinks.EnterLine(this);
        }
        else {
            ActionState = Collections.CustomerActionState.None;
            Seat seat = Bar.GetOpenSeat();
            seat.MoveCustomerIntoSpot(this);
        }

        if ( ReliefType == Collections.ReliefType.Toilet ) {
            ( (Toilet)Occupying ).SRenderer.sprite = Collections.spriteStallOpened;
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
        if (AtDestination() && Occupying != null && Occupying.CanBeSoiled) {
            Occupying.IsSoiled = true;
        }
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
        if ( Occupying != null ) {
            Occupying.OccupiedBy = null;
        }
        if (position == Collections.Location.Bar && thing.CustomerLocation != position) {
            foreach (Vector3 keyframe in Collections.NavigationKeyframesFromBarToBathroom) {
                MoveToVector3(keyframe);
            }
        }
        MoveToVector3(Gender == 'f' ? thing.CustomerPositionF : thing.CustomerPositionM);

        Occupying = thing;
        thing.OccupiedBy = this;
        position = thing.CustomerLocation;
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
        if (GameController.GamePaused) {
            return;
        }

        if (IsRelievingSelf) {
            ReliefMenu.Toggle();
        }
        else {
            BathroomMenu.Toggle();
        }
    }

    // Button for sending away
    [SerializeField]
    public Button ButtonDecline;
    // Button for waiting in line
    [SerializeField]
    public Button ButtonWaitingRoom;
    // Button to use toilet
    [SerializeField]
    public Button ButtonToilet;
    // Button to use urinal
    [SerializeField]
    public Button ButtonUrinal;
    // Button to use sink
    [SerializeField]
    public Button ButtonSink;
    // Button to stop peeing
    [SerializeField]
    public Button ButtonReliefStop;
    /// <summary>This menu is available when the customer is the restroom</summary>
    [SerializeField]
    public Menu BathroomMenu;
    public Canvas BathroomMenuCanvas;
    /// <summary>This menu is only available when the customer is relieving themselves</summary>
    [SerializeField]
    public Menu ReliefMenu;
    public Canvas ReliefMenuCanvas;

    /// <summary>
    /// Code for if bathroom menu can be displayed
    /// </summary>
    /// <returns></returns>
    public bool CanDisplayBathroomMenu() {
        // Cannot be wetting or wet
        if (IsWetting || IsWet) {
            return false;
        }
        // Must have arrived at current destination
        if (Destination != transform.position) {
            return false;
        }
        // If in waiting room
        if (position == Collections.Location.WaitingRoom) {
            return true;
        }
        // If in doorway and first in line
        if (position == Collections.Location.Doorway) {
            return DoorwayQueue.doorwayQueue.IsNextInLine(this);
        }
        return false;
    }
    /// <summary>
    /// Code for if relief menu can be displayed
    /// </summary>
    /// <returns></returns>
    public bool CanDisplayReliefMenu() {
        return IsRelievingSelf && bladder.Percentage > 0.1d && bladder.Emptying && (ReliefType == Collections.ReliefType.Urinal || ReliefType == Collections.ReliefType.Sink);
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
        if ( WaitingRoom.waitingRoom.HasOpenWaitingSpot() ) {
            WaitingSpot waitingSpot = WaitingRoom.waitingRoom.GetNextWaitingSpot();
            waitingSpot.MoveCustomerIntoSpot(this);
            position = Collections.Location.WaitingRoom;
            return waitingSpot;
        }
        else {
            Debug.LogError("Message should pop up telling player there are no open spot left!");
            return null;
        }
    }
    // Sends this customer to the toilets.
    public bool MenuOptionGotoToilet() {
        if (Bathroom.bathroom.HasToiletAvailable) {
            EnterRelief(Bathroom.bathroom.GetToilet());
            return true;
        }
        return false;
    }
    public bool MenuOptionGotoUrinal() {
        if (Bathroom.bathroom.HasUrinalAvailable) {
            EnterRelief(Bathroom.bathroom.GetUrinal());
            return true;
        }
        return false;
    }
    public bool MenuOptionGotoSink() {
        if (Bathroom.bathroom.HasSinkForRelief) {
            EnterRelief(Bathroom.bathroom.Sinks.FirstUnoccupiedSink());
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
            EndPeeingWithThing();
        }, () => !bladder.Emptying);
        return true;
    }
    #endregion

    #region CustomerPhysicalActions
    // Sends this customer to relief
    public void EnterRelief(Relief relief) {
        position = Collections.Location.Relief;
        UseInteractable(relief);
        BeginPeeingWithThing();
    }
    // Goes to the doorway queue
    public bool EnterDoorway() {
        if ( DoorwayQueue.doorwayQueue.HasOpenWaitingSpot() ) {
            // Makes customer hold on for a while longer when entering doorway.
            bladder.ResetLossOfControlTime();
            WaitingSpot waitingSpot = DoorwayQueue.doorwayQueue.GetNextWaitingSpot();
            waitingSpot.MoveCustomerIntoSpot(this);
            position = Collections.Location.Doorway;
            CanReenterBathroom = false;
            return true;
        }
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
        if ( position != Collections.Location.Bar) {
            foreach ( Vector3 keyframe in Collections.NavigationKeyframesFromBathroomToBar ) {
                MoveToVector3(keyframe);
            }
        }
        MoveToVector3(Collections.OffScreenTop);
        position = Collections.Location.Outside;
    }
    #endregion

}
