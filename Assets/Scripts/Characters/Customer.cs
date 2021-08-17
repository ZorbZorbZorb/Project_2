using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using Assets.Scripts.Interfaces;
using System.Linq;
using Assets.Scripts.Objects;
using UnityEngine.EventSystems;
using Assets.Scripts.Characters;

public class Customer : MonoBehaviour {
    void Start() {

        WetSelfLeaveBathroomDelayRemaining = WetSelfLeaveBathroomDelay;
        if (Destination == null) {
            Destination = this.transform.position;
        }
        UID = GameController.GetUid();
        Menu.enabled = false;

        Emotes = new Emotes(this, EmoteSpriteRenderer, BladderCircleTransform);

        SetupButtons();
    }

    public void SetupCustomer(int minBladderPercent, int maxBladderPercent) {
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

        if (Menu.enabled) {
            // Menu auto-close
            if (!CanDisplayMenu()) {
                MenuClose();
            } 
            // Menu update
            else {
                MenuUpdate();
            }
        }

        // Move sprite
        MoveUpdate();

        // Debug logging
        FrameActionDebug();
    }

    public bool HasNext = false;
    public delegate void NextAction();
    public NextAction Next;
    public float NextDelay = 0f;

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
        else if ( FeelsNeedToGo ) {
            return Collections.CustomerDesperationState.State1;
        }
        else {
            return Collections.CustomerDesperationState.State0;
        }
    }

    void Think() {
        // Next action delegate code
        if (Next != null) {
            if ( NextDelay > 0f) {
                return;
            }
            else {
                NextAction last = Next;
                Next();
                // Clear if not updated
                if (Next.Method == last.Method) {
                    Next = null;
                }
            }
        }

        // If about to leave or has left
        if ( position == Collections.Location.Outside ) {
            if (AtDestination()) {
                GameController.controller.RemoveCustomer(this);
            }
        }
        // If wet self and finished wetting self
        if (IsWet && !IsWetting) {
            if (WetSelfLeaveBathroomDelayRemaining > 0) {
                WetSelfLeaveBathroomDelayRemaining -= Time.deltaTime;
            }
            else if (position != Collections.Location.Outside) {
                Leave();
            }
        }

        // If not in any bathroom and bladder is full, go to bathroom
        if ( MinTimeBetweenChecksNow < MinTimeBetweenChecks ) {
            return;
        }
        else if (position == Collections.Location.Bar) {
            MinTimeBetweenChecksNow = 0f;
            if (FeelsNeedToGo) {
                if ( MinTimeAtBarNow < MinTimeAtBar ) {
                    return;
                }
                else {
                    // Try to enter the bathroom
                    if ( !EnterDoorway() ) {
                        MinTimeAtBarNow = MinTimeAtBar / 1.5f;
                    }
                    else {
                        MinTimeAtBarNow = 0f;
                    }
                }
            }
            else {
                if (Random.Range(0, 11) == 0) {
                    Leave();
                }
            }
        }
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
        FeelsNeedToGo = bladder.FeltNeed > 0.40d;
        DesperationState = GetDesperationState();

        // Can customer relieve themselves now?
        Collections.ReliefType reliefType = Occupying?.ReliefType ?? Collections.ReliefType.None;

        // Get the relief the customer is occupying, if applicable
        Relief relief = reliefType == Collections.ReliefType.None ? null : (Relief)Occupying;
        
        // Behavior depending on if have reached an area they can relieve themselves
        if (reliefType == Collections.ReliefType.None) {
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
            if ( !bladder.Emptying && bladder.Percentage < 0.1 ) {
                ActionState = relief.StatePantsUp;
                if ( RemainingUrinateStopDelay > 0 ) {
                    RemainingUrinateStopDelay -= 1 * Time.deltaTime;
                }
                else {
                    if (!HasNext) {
                        HasNext = true;
                        Next = EndPeeingWithThing;
                    }
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
                    ActionState = relief.StatePeeing;
                }
            }
            // Wait for bladder to empty.
            // Display emptying bladder animation
            else if ( bladder.Emptying ) {
            bladder.ShouldWetNow = false;
                // Update pee stream emote
                Emote emote = Emote.GetPeeStreamEmote(bladder.Percentage);
                if (emote == null) {
                    Debug.Break();
                    throw new NullReferenceException();
                }
                if (Emotes.currentEmote != emote) {
                    Emotes.Emote(emote);
                }
            }
        }
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

    public bool CheatPeeNow = false;

    public static Bar Bar;

    public bool Active = false;
    public int UID = GameController.GetUid();
    public Collections.CustomerDesperationState lastState;
    public string DisplayName { get; set; }
    public char Gender { get; set; }
    // Enum for behavior types
    public bool FeelsNeedToGo = false;
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

    // Times
    public double UrinateStartDelay;
    public double UrinateStopDelay;
    private double RemainingUrinateStartDelay;
    private double RemainingUrinateStopDelay;
    public float WetSelfLeaveBathroomDelay = 6f;
    public float WetSelfLeaveBathroomDelayRemaining;
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
        if (sprite != SRenderer.sprite) {
            SRenderer.sprite = sprite;
        }

        // Sprite shaking to show desperation
        // TODO: Perhaps shake more or less when shy, maybe have shaking be the true desperation state?
        // Notice: The sprite is parented to a customer gameobject and is not a part of it. this.gameObject.transform can be used to re-parent it.
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

    // Class that contains all of the emotes / thought bubbles / etc for the person
    [SerializeField] public RectTransform BladderCircleTransform;
    [SerializeField] public SpriteRenderer EmoteSpriteRenderer;
    public Emotes Emotes;
    #endregion

    #region Desperation/Wetting/Peeing
    // Current willingness to use a urinal for relief
    public bool WillUseUrinal() {
        // Yup
        if ( Gender == 'm' ) {
            return true;
        }
        // Only if they're about to lose it
        if ( Gender == 'f' ) {
            return bladder.LosingControl || bladder.FeltNeed > 0.93;
        }
        throw new NotImplementedException();
    }
    // Current willingness to use a sink for relief
    public bool WillUseSink() {
        // It's just a weird urinal you wash your hands in, right?
        if (Gender == 'm') {
            return bladder.LosingControl || bladder.FeltNeed > 0.93d;
        }
        // Girls will only use the sink if they're wetting themselves
        if ( Gender == 'f' ) {
            return bladder.LosingControl || bladder.FeltNeed > 0.99d;
        }
        throw new NotImplementedException();
    }
    private Collections.ReliefType ReliefType => Occupying?.ReliefType ?? Collections.ReliefType.None;

    public void BeginPeeingWithThing() {
        IsRelievingSelf = true;
        Emotes.Emote(Emote.PantsDown);
        Emotes.ShowBladderCircle(true);
        // Add a delay between starting and stopping urination
        RemainingUrinateStartDelay = UrinateStartDelay;
        RemainingUrinateStopDelay = UrinateStopDelay;
        // Make it take 2.5x as long for them to finish up if you made them hold it to the point they were about to lose it
        if ( bladder.LosingControl ) {
            RemainingUrinateStopDelay = RemainingUrinateStopDelay * 2.5d;
            Debug.Log($"Customer {UID} will take longer when they finish up because they were losing control");
        }
        Collections.ReliefType reliefType = Occupying?.ReliefType ?? Collections.ReliefType.None;
        switch ( reliefType ) {
            case Collections.ReliefType.Toilet:
            ActionState = Collections.CustomerActionState.ToiletPantsDown;
            break;
            case Collections.ReliefType.Urinal:
            ActionState = Collections.CustomerActionState.UrinalPantsDown;
            break;
            case Collections.ReliefType.Sink:
            ActionState = Collections.CustomerActionState.SinkPantsDown;
            break;
            case Collections.ReliefType.Towel:
            ActionState = Collections.CustomerActionState.TowelPantsDown;
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

        // Toggle closed
        if (Menu.enabled) {
            MenuClose();
        }

        // Toggle opened
        else if ( CanDisplayMenu() ) {
            // Close any open customer menus
            GameController.controller.CloseOpenMenus();
            // Open this customers menu
            MenuOpen();
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
    // The menu
    [SerializeField]
    public Canvas Menu;
    [SerializeField]
    public Text LastPeedText;

    // Can the menu be displayed?
    public bool CanDisplayMenu() {
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
    // TODO: Close menu when a button is clicked
    // Menu Open
    public void MenuOpen() {
        Menu.enabled = true;
    }
    // Menu Close
    public void MenuClose() {
        Menu.enabled = false;
    }
    private void MenuUpdate() {
        ButtonUrinal.interactable = WillUseUrinal();
        ButtonSink.interactable = WillUseSink();

        TimeSpan secondsSincePee = DateTime.Now - bladder.LastPeedAt;
        LastPeedText.text = $"Last peed\r\n{secondsSincePee.ToString("hh")}:{secondsSincePee.ToString("mm")}:{secondsSincePee.ToString("ss")} ago\r\nDrinks had: {bladder.DrinksHad}";
    }
    /// <summary>
    /// Adds the listeners for buttons. Makes them do stuff when you click them
    /// </summary>
    private void SetupButtons() {
        ButtonWaitingRoom.onClick.AddListener(delegate {
            MenuOptionGotoWaiting();
        });
        ButtonDecline.onClick.AddListener(delegate {
            MenuOptionDismiss();
        });
        ButtonToilet.onClick.AddListener(delegate {
            MenuOptionGotoToilet();
        });
        ButtonUrinal.onClick.AddListener(delegate {
            MenuOptionGotoUrinal();
        });
        ButtonSink.onClick.AddListener(delegate {
            MenuOptionGotoSink();
        });
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
    // Sends this customer to the toilets
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
    #endregion

    #region Movements
    // Sends this customer to relief
    public void EnterRelief(Relief relief) {
        position = Collections.Location.Relief;
        UseInteractable(relief);
        BeginPeeingWithThing();
    }
    // Goes to the doorway queue
    public bool EnterDoorway() {
        if ( DoorwayQueue.doorwayQueue.HasOpenWaitingSpot() ) {
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
