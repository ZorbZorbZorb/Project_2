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

public class Customer : MonoBehaviour {

    void Start() {
        WetSelfLeaveBathroomDelayRemaining = WetSelfLeaveBathroomDelay;
        Destination = this.transform.position;
        UID = GameController.GetUid();
        Menu.enabled = false;

        InitializeCustomer();
        SetupButtons();
    }

    public void InitializeCustomer() {

        bladder = new Bladder();
        bladder.SetupBladder();

        UrinateStartDelay = 4d;
        UrinateStopDelay = 6d;
    }

    private Collections.CustomerDesperationState CalculateState() {
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
        else if ( bladder.Percentage > 0.65d) {
            return Collections.CustomerDesperationState.State2;
        }
        else if ( bladder.Percentage > 0.40d ) {
            return Collections.CustomerDesperationState.State1;
        }
        else {
            return Collections.CustomerDesperationState.State0;
        }
    }

    void Update() {
        // Fastest possible exit
        if ( !Active ) {
            return;
        }

        // Update bladder
        bladder.Update();

        // Update peeing logic
        PeeLogicUpdate();

        // Calculate outward state to show to player
        DesperationState = CalculateState();

        // Think
        Think();

        // Update anim
        SpriteUpdate();
        // Update bubble
        ThoughtBubbleUpdate();

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
        MiscDebug();
    }

    void Think() {
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
            else {
                Leave();
            }
        }
        // If not in any bathroom and bladder is full, go to bathroom
        else if ( FeelsNeedToGo ) {
            if (CanReenterBathroom) {
                // Only if in bar and not recently dismissed by player
                if ( position == Collections.Location.Bar) {
                    if (!EnterDoorway()) {
                        CanReenterBathroom = false;
                        CanReenterBathroomIn = 60f;
                    }
                }
            }
            else {
                if ( CanReenterBathroomIn < 0f) {
                    CanReenterBathroom = true;
                }
                else {
                    CanReenterBathroomIn -= Time.deltaTime;
                }
            }
        }
    }

    void MiscDebug() {
        lastState = DesperationState;
        if ( Time.frameCount % 2000 == 0 ) {
            //string logString2 = $"state: {state} bladder: {Math.Round(bladder.Amount)} / {bladder.Max} control: {Math.Round(bladder.ControlRemaining)} losecontrol:{bladder.LosingControl} lossControlBuffer:{Math.Round(bladder.LossOfControlTimeRemaining)} emptying: {bladder.Emptying}";
            //Debug.Log(logString2);
        }
        if ( bladder.StartedLosingControlThisFrame ) {
            Debug.Log($"Customer {UID} started losing control at {Math.Round(bladder.Amount)} / {Math.Round(bladder.Max)} bladder");
        }
    }

    private Collections.CustomerActionState LastActionState;
    private void FrameActionDebug() {
        if ( LastActionState != ActionState ) {
            LastActionState = ActionState;
            Debug.Log($"Customer {UID} new action: {ActionState}");
        }

        // Desperation State Logging
        if ( lastState != DesperationState ) {
            if ( Time.frameCount > 10 ) {
                AnnounceStateChange();
            }
        }
        else {
            if ( Time.frameCount % 1000 == 0 ) {
                AnnounceState();
            }
        }

        void AnnounceStateChange() {
            string logString = "";
            logString += $"Customer {UID} {lastState} => {DesperationState} @ Bladder: {Math.Round(bladder.Amount)} / {bladder.Max} ({Math.Round(bladder.Percentage, 2)}%)";
            logString += $"Control: {Math.Round(bladder.ControlRemaining)}";
            logString += $"Need: {Math.Round(bladder.FeltNeed, 2)} Curve: {Math.Round(bladder.FeltNeedCurve, 2)}";
            Debug.LogWarning(logString);
        }
        void AnnounceState() {
            string logString = "";
            logString += $"Customer {UID} {DesperationState} Bladder: {Math.Round(bladder.Amount)} / {bladder.Max} ({Math.Round(bladder.Percentage, 2)}%)";
            logString += $"Control: {Math.Round(bladder.ControlRemaining)}";
            logString += $"Need: {Math.Round(bladder.FeltNeed, 2)} Curve: {Math.Round(bladder.FeltNeedCurve, 2)}";
            logString += $"Emptying: {bladder.Emptying} IsWetting: {IsWetting} IsWet: {IsWet} IsFinishedPeeing: {IsFinishedPeeing}";
            Debug.Log(logString);
        }
    }

    private void PeeLogicUpdate() {
        FeelsNeedToGo = bladder.FeltNeed > 0.33d;


        // Can customer relieve themselves now?
        Collections.IReliefType reliefType = Occupying?.ReliefType ?? Collections.IReliefType.None;

        // Get the relief the customer is occupying, if applicable
        Relief relief = reliefType == Collections.IReliefType.None ? null : (Relief)Occupying;
        
        // Behavior depending on if have reached an area they can relieve themselves
        if (reliefType == Collections.IReliefType.None) {
            // If should wet now
            if ( bladder.ShouldWetNow ) {
                StartWettingSelf();
            }
            // If finishing wetting
            else if ( IsWetting && !bladder.Emptying ) {
                StopWettingSelf();
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
                    ReliefEnd();
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
            }
            bladder.ShouldWetNow = false;
        }
    }

    public void MoveToVector3(Vector3 destination) {
        Destination = destination;
        Debug.Log($"Customer {UID} moving to {destination}");
    }
    private void MoveUpdate() {
        if ( transform.position.x != Destination.x || transform.position.y != Destination.y ) {
            float distanceX = Math.Abs(transform.position.x - Destination.x);
            float distanceY = Math.Abs(transform.position.y - Destination.y);
            double moveAmount = Math.Max(( MoveSpeed * Time.deltaTime * ( distanceX + distanceY ) ) + 1, 1d);
            transform.position = Vector3.MoveTowards(transform.position, Destination, (float)moveAmount);
        }
    }
    public bool AtDestination() {
        return transform.position == Destination;
    }

    [SerializeField]
    public SpriteRenderer SRenderer;

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
    public bool IsWetting = false;
    public bool CanWetNow = false;
    public bool IsWet = false;
    public Collections.CustomerDesperationState DesperationState = Collections.CustomerDesperationState.State0;
    public Collections.BladderControlState CustomerState = Collections.BladderControlState.Normal;
    public Bladder bladder = new Bladder();
    //private int minutesSinceLastRelief;
    //public string LastReliefAt => $"{minutesSinceLastRelief % 60}";
    public int Shyness { get; set; }

    // Times
    public double UrinateStartDelay;
    public double UrinateStopDelay;
    private double RemainingUrinateStartDelay;
    private double RemainingUrinateStopDelay;
    public float WetSelfLeaveBathroomDelay = 6f;
    public float WetSelfLeaveBathroomDelayRemaining;

    // Position
    public Collections.CustomerActionState ActionState = Collections.CustomerActionState.None;
    public Collections.Location position = Collections.Location.Bar;
    public CustomerInteractable Occupying;

    // Movement
    public Vector3 Destination;
    [SerializeField]
    public double MoveSpeed;

    #region Sprites
    [SerializeField]
    public SpriteRenderer ThoughtBubbleSpriteRenderer;
    public void SpriteUpdate() {
        // Sprite changing / sprite hiding
        if ( Occupying != null && Occupying.HidesCustomer ) {
            if ( AtDestination() ) {
                //SRenderer.enabled = false;
            }
        }
        else {
            //SRenderer.enabled = true;
        }

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

    private void ThoughtBubbleUpdate() {
        if (ActionState == Collections.CustomerActionState.None) {
            ThoughtBubbleSpriteRenderer.enabled = false;
            return;
        }

        Sprite bubbleSprite = Collections.GetBubbleSprite(this);
        if (bubbleSprite == null) {
            ThoughtBubbleSpriteRenderer.enabled = false;
        }
        else {
            ThoughtBubbleSpriteRenderer.enabled = true;
            ThoughtBubbleSpriteRenderer.sprite = bubbleSprite;
        }
    }

    private void BladderDisplayUpdate() {

    }
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
    private Collections.IReliefType OccupyingReliefType => Occupying?.ReliefType ?? Collections.IReliefType.None;
    public void ReliefStart() {
        IsRelievingSelf = true;
        // Add a delay between starting and stopping urination
        RemainingUrinateStartDelay = UrinateStartDelay;
        RemainingUrinateStopDelay = UrinateStopDelay;
        // Make it take 2.5x as long for them to finish up if you made them hold it to the point they were about to lose it
        if ( bladder.LosingControl ) {
            RemainingUrinateStopDelay = RemainingUrinateStopDelay * 2.5d;
            Debug.Log($"Customer {UID} will take longer when they finish up because they were losing control");
        }
        Collections.IReliefType reliefType = Occupying?.ReliefType ?? Collections.IReliefType.None;
        switch ( reliefType ) {
            case Collections.IReliefType.Toilet:
            ActionState = Collections.CustomerActionState.ToiletPantsDown;
            break;
            case Collections.IReliefType.Urinal:
            ActionState = Collections.CustomerActionState.UrinalPantsDown;
            break;
            case Collections.IReliefType.Sink:
            ActionState = Collections.CustomerActionState.SinkPantsDown;
            break;
            case Collections.IReliefType.Towel:
            ActionState = Collections.CustomerActionState.TowelPantsDown;
            break;
        }
    }
    public void ReliefEnd() {
        Debug.Log($"Customer {UID} finished relieving themselves.");
        IsRelievingSelf = false;
        if ( IsWet ) {
            position = Collections.Location.Outside;
        }
        if ( OccupyingReliefType == Collections.IReliefType.Toilet ) {
            ( (Toilet)Occupying ).SRenderer.sprite = Collections.spriteStallOpened;
        }
        position = Collections.Location.Bar;
        Occupying.OccupiedBy = null;
        Occupying = null;
        ActionState = Collections.CustomerActionState.None;
        MoveToVector3(Collections.OffScreenBottom);
    }
    public void StartWettingSelf() {
        bladder.Emptying = true;
        IsWet = true;
        IsWetting = true;
        ActionState = Collections.CustomerActionState.Wetting;
    }
    public void StopWettingSelf() {
        Debug.Log("StopWettingSelf");
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
        Occupying = thing;
        thing.OccupiedBy = this;
        position = thing.CustomerLocation;
        CanWetNow = thing.CanWetHere;
        if (Gender == 'f') {
            MoveToVector3(thing.CustomerPositionF);
        }
        else if (Gender == 'm') {
            MoveToVector3(thing.CustomerPositionM);
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
        // Get out of here stalker
        StopOccupyingAll();
        MoveToVector3(Collections.OffScreenBottom);
        position = Collections.Location.Bar;
        CanReenterBathroom = false;
        CanReenterBathroomIn = 60f;
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
        if (Bathroom.bathroom.HasSinkAvailable) {
            EnterRelief(Bathroom.bathroom.GetSink());
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
        ReliefStart();
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
    public void EnterBar() {
        StopOccupyingAll();
        position = Collections.Location.Bar;
        MoveToVector3(Collections.OffScreenBottom);
    }
    // Fully leaves the area
    public void Leave() {
        StopOccupyingAll();
        MoveToVector3(Collections.OffScreenBottom);
        position = Collections.Location.Outside;
    }
    #endregion
}
