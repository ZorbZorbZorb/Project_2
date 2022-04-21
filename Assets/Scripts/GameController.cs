using Assets.Scripts;
using Assets.Scripts.Areas;
using Assets.Scripts.Customers;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class GameController : MonoBehaviour {

    #region Fields

    /// <summary>
    /// Ai director. Controls customer drinking, spawning, and other behaviors regarding game balance and
    ///   general player enjoyment.
    /// </summary>
    public Director AID;
    /// <summary><see cref="CustomerManager"/> singleton. Tracks custromers and handles spawning.</summary>
    [HideInInspector]
    public static CustomerManager CM;

    public static bool CreateNewSaveData = true;
    public static int TargetFramerate = -1;

    [Header("Important")]
    public bool DisplayNightStartSplash = true;
    public bool DisplayBuildMenuOnFirstNight = false;

    [Header("Settings")]
    public int NightMaxTicks = 30;
    public TimeSpan NightClock;
    private static readonly TimeSpan OneSecond = new TimeSpan(0, 0, 1);
    [Range(0, 2)]
    public float nightStartDelay = 1f;

    [Header("Camera Settings")]
    [Range(15f, 65f)]
    public float CameraTolerangeTight = 18f;
    [Range(30f, 110f)]
    public float CameraTolerangeMid = 50f;
    [Range(30f, 110f)]
    public float CameraTolerangeLoose = 68f;

    [Header("Debugging")]
    public bool Autoplay = false;
    public bool RapidSimulation = false;
    public bool OnlySpawnOneCustomer = false;
    public bool LogCustomerStates = false;
    public bool DrawPaths = false;

    [Header("Cheats")]
    public bool RapidBladderFill = false;
    public bool RapidBladderEmpty = false;
    public bool SpawnEveryTick = false;
    public bool CustomersWillUseAnything = false;
    public bool InfiniteFunds = false;
    public bool NoLose = false;
    public bool FreezeTime = false;

    [Header("Commands")]
    public bool EndNight = false;
    public bool BuildEverything = false;

    [Header("Buttons")]
    public Button PauseButton;
    public Button ContinueButton;
    public Button MenuButton;
    public Button StartButton;
    public Button RestartButton;
    public Button WestButton;
    public Button NorthButton;
    public Button EastButton;
    public Button SouthButton;

    [Header("Manually Set")]
    public Text NightText;

    // Build menu
    [HideInInspector]
    public bool InBuildMenu;
    public BuildMenu BuildMenu;

    [Header("Other")]
    // Pause menu
    public PauseMenu PauseMenu;
    [HideInInspector]
    public static bool GamePaused { get; private set; } = false;

    /// <summary>Accumulates <see cref="Time.deltaTime"/> on update</summary>
    [HideInInspector]
    public float runTime = 0f;
    /// <summary>Accumulates <see cref="Time.deltaTime"/> on update to call <see cref="Think"/> every 1 second</summary>
    [HideInInspector]
    private float timeAcc = 0f;

    // State booleans
    [HideInInspector]
    public bool CanPause = true;
    [HideInInspector]
    public bool DisplayedNightStartSplashScreen = false;

    /// <summary><see cref="GameController"/> singleton</summary>
    [HideInInspector]
    public static GameController GC = null;
    /// <summary><see cref="Freecam"/> singleton</summary>
    [HideInInspector]
    public static Freecam FC = null;

    // Save data
    public GameSaveData Game;

    public int TicksElapsed = 0;
    public int TickEveryXSeconds;
    public Text barTimeDisplay;
    public Text fundsDisplay;

    public int ThinksSinceLastGmAction = 0;
    public double nightStartFunds;
    public bool GameStarted = false;
    public bool GameLost = false;
    public bool GameEnd = false;
    public bool FadeToBlack = false;
    private bool ReadyToStartNight = false;

    public Canvas NightStartCanvas;
    public Text NightStartText;
    public Image NightStartOverlay;

    public Vector2 LastPan;
    public float LastZoom;

    public float totalSecondsPlayedInNight = 0f;
    public float totalNightTime;

    #endregion

    #region Unity Methods

    private void Awake() {
        // Reset screen resolution
        Screen.SetResolution(1366, 768, FullScreenMode.Windowed, TargetFramerate);
        Application.targetFrameRate = TargetFramerate;


        if ( GC != null ) {
            Debug.LogError("GC singleton was already set! May have possible created a second game controller!");
        }
        GC = this;
        Customer.GC = this;

        NorthButton.onClick.AddListener(() => { CycleCamera(Orientation.North); });
        SouthButton.onClick.AddListener(() => { CycleCamera(Orientation.South); });
        EastButton.onClick.AddListener(() => { CycleCamera(Orientation.East); });
        WestButton.onClick.AddListener(() => { CycleCamera(Orientation.West); });

        Emote.PeeStrong = new Emote("Sprites/Bubbles/Stream_3");
        Emote.PeeMedium = new Emote("Sprites/Bubbles/Stream_2");
        Emote.PeeWeak = new Emote("Sprites/Bubbles/Stream_1");
        Emote.PantsDown = new Emote("Sprites/Bubbles/bubble_zipper_down");
        Emote.PantsUp = new Emote("Sprites/Bubbles/bubble_zipper_up");
        Emote.StruggleStop = new Emote("Sprites/Bubbles/bubble_struggle_stop");
        Emote.PeeStreamEmotes = new Emote[] { Emote.PeeWeak, Emote.PeeWeak, Emote.PeeWeak, Emote.PeeMedium, Emote.PeeMedium, Emote.PeeStrong, Emote.PeeStrong };

        // Create customer sprite marshals
        CustomerSpriteController.Controller = new Dictionary<char, CustomerSpriteController>();
        CustomerSpriteController.NewController('m', "Sprites/People/nm");
        CustomerSpriteController.NewController('f', "Sprites/People/n");
    }

    private void Start() {
        // Create the auto-camera camera positions
        CameraPosition.AddPosition(Freecam.Center, 600);
        // Set the pause/unpause last zoom
        LastZoom = Freecam.MinZoom;
        LastPan = Freecam.Center;

        // Freecam should always be attached to the main camera
        FC = Camera.main.GetComponent<Freecam>();

        CM = new CustomerManager() {
            CustomersHolder = GameObject.FindGameObjectWithTag("CustomersHolder")
        };

        // Set the customer's static variables
        Customer.BathroomStartX =
            Bathroom.BathroomM.Bounds.Bounds.min.x - (Prefabs.PrefabCustomer.SRenderer.transform.localScale.x);
        Customer.BathroomStartY =
            Bathroom.BathroomM.Bounds.Bounds.min.y + (Prefabs.PrefabCustomer.SRenderer.transform.localScale.y);

        // Lock up the camera
        FC.Locked = true;

        // Clear the menu system's caches.
        Menu.ClearForSceneReload();

        // Set up menu buttons
        BuildMenu.SetUpButtons();
        PauseMenu.SetUpButtons();

        // Load or create game data
        if ( CreateNewSaveData ) {
            Game = GameSaveData.ImportDefault();
            CreateNewSaveData = false;
        }
        else {
            Game = GameSaveData.Import(1);
        }
        UpdateFundsDisplay();

        // Construct the play areas
        Game.Apply();

        NightText.text = $"Night {Game.Night}";

        // initialize bar time
        NightClock = new TimeSpan(0, 0, TickEveryXSeconds * NightMaxTicks);
        barTimeDisplay.text = $"{NightClock.Minutes.ToString().PadLeft(2, '0')}:{NightClock.Seconds.ToString().PadLeft(2, '0')}";

        // Start build mode if not first night
        if ( DisplayBuildMenuOnFirstNight || Game.Night > 1 ) {
            StartBuildMode();
        }
        else {
            ReadyToStartNight = true;
        }

        // Create the ai director
        AID = new Director(CM);

        // Reset frame rate just incase Unity didnt take it in Awake();
        Application.targetFrameRate = TargetFramerate;
    }

    private void Update() {

        if ( Input.anyKeyDown ) {
            HandleKeypresses();
        }
        if ( !ReadyToStartNight ) {
            return;
        }

        // Display the night start splash screen
        if ( DisplayNightStartSplash && !DisplayedNightStartSplashScreen ) {
            NightStartCanvas.gameObject.SetActive(true);
            NightStartText.text = $"Night {Game.Night}";
        }

        if ( !GameStarted ) {
            if ( FadeOutNightStartScreen() ) {
                DisplayedNightStartSplashScreen = true;
                ResumeGame();
                GameStarted = true;
                CanPause = true;
                FC.Locked = false;
                CM.CreateCustomer(0.7f);
                CM.CreateCustomer(0.6f);
                CM.CreateCustomer(0.6f);

                // Debugging only
                if ( OnlySpawnOneCustomer ) {
                    OnlySpawnOneCustomer = false;
                    AID.Disable();
                }
            }
            else {
                return;
            }
        }
        else if ( EndNight || GameEnd ) {
            if ( !GamePaused ) {
                if ( GameLost ) {
                    PauseGame();
                    PauseMenu.SwitchToBoldTextDisplay();
                }
                else {
                    EndGame();
                }
            }
            else if ( FadeToBlack ) {
                PauseMenu.FadeOverlayToBlack();
                if ( PauseMenu.FadeOverlayComplete() ) {
                    FadeToBlack = true;
                }
            }
        }


        var deltaTime = Time.deltaTime;
        if ( RapidSimulation ) {
            deltaTime *= 10f;
        }
        runTime += deltaTime;
        timeAcc += deltaTime;

        totalSecondsPlayedInNight += deltaTime;

        if ( timeAcc >= 1 ) {
            timeAcc -= 1;
            NightClock = NightClock.Subtract(OneSecond);
            barTimeDisplay.text = $"{NightClock.Minutes.ToString().PadLeft(2, '0')}:{NightClock.Seconds.ToString().PadLeft(2, '0')}";
            Think();
            AID.Act();
            if ( Autoplay ) {
                StupidIdiotAutoplayThing();
            }
            if ( SpawnEveryTick ) {
                if ( CM.RemainingSpawns > 0 ) {
                    CM.CreateCustomer(0.4f);
                }
            }
        }

        void Think() {
            // End the game if too many seats are soiled
            if ( CM.CountBrokenSeats > CM.CountWorkingSeats && !NoLose ) {
                GameEnd = true;
                GameLost = true;
                AID.Disable();
                return;
            }

            // Stop spawning customers when its too late
            if ( TicksElapsed >= NightMaxTicks ) {
                AID.Disable();

                // End game at end time or everyone has left
                if ( !CM.Customers.Any() || TicksElapsed >= NightMaxTicks + 2 ) {
                    GameEnd = true;
                    return;
                }
            }

            // Update the bar time
            if ( Math.Floor(runTime / TickEveryXSeconds) > TicksElapsed ) {
                if ( !FreezeTime ) {
                    AdvanceTime();
                }
            }
        }
    }

    #endregion

    #region External Methods

    public void ToggleGamePaused() {
        if ( CanPause ) {
            if ( GamePaused ) {
                ResumeGame();
            }
            else {
                PauseGame();
            }
        }
    }
    public void RestartCurrentNight() {
        ResetStaticMembers();
        // Reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    /// <summary>
    /// Restarts the current night in progress.
    /// Just reloads the scene to reload the current saved data, because the
    /// scene in play is unsaved.
    /// </summary>
    public void ContinueToNextNight() {
        PauseMenu.Close();
        Game.Night++;
        Game.Export(1);
        ResetStaticMembers();
        // Reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    /// <summary>
    /// Returns to the main menu without saving
    /// <para>The main menu scene should always be scene index 0, which opens on game start</para>
    /// </summary>
    public void GoToMainMenu() {
        // Timescale and static vars are preserved between scenes!
        Time.timeScale = 1;
        ResetStaticMembers();
        // Load menu scene
        SceneManager.LoadScene(0);
    }
    public void EndBuildMode() {
        InBuildMenu = false;
        ReadyToStartNight = true;
        Time.timeScale = 1;
        BuildMenu.End();

        FC.ZoomTo(Freecam.MinZoom);
        FC.PanTo(Freecam.Center);

    }
    public void UpdateFundsDisplay() {
        fundsDisplay.text = $"${Math.Round(Game.Funds, 0)}";
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Keyboard event handler
    /// </summary>
    private void HandleKeypresses() {
        // Pressing escape will pause the game
        if ( Input.GetKeyDown(KeyCode.Escape) ) {
            ToggleGamePaused();
        }

        // Camera movement hotkeys
        if ( Input.GetKeyDown(KeyCode.D) ) {
            CycleCamera(Orientation.East);
        }
        else if ( Input.GetKeyDown(KeyCode.A) ) {
            CycleCamera(Orientation.West);
        }
        else if ( Input.GetKeyDown(KeyCode.S) ) {
            CycleCamera(Orientation.South);
        }
        else if ( Input.GetKeyDown(KeyCode.W) ) {
            CycleCamera(Orientation.North);
        }
    }
    /// <summary>
    /// Plays the game automatically for testing purposes.
    /// <para>Make sure to fix the issue where menu clicks aren't opening the menu first</para>
    /// </summary>
    private void StupidIdiotAutoplayThing() {
        foreach ( Customer customer in CM.Customers ) {
            if ( customer.AtDestination ) {
                if ( customer.Location == Location.Bar ) {
                    customer.Leave();
                    break;
                }
                if ( customer.Location == Location.Hallway ) {
                    var bathroom = customer.GetCurrentBathroom();
                    if ( bathroom.Line.IsNextInLine(customer) ) {
                        if ( bathroom.HasToiletAvailable ) {
                            customer.MenuOptionGotoToilet();
                            break;
                        }
                        else if ( bathroom.HasUrinalAvailable && Customer.WillUseUrinal(customer) ) {
                            customer.MenuOptionGotoUrinal();
                            break;
                        }
                        else if ( bathroom.HasSinkForRelief && Customer.WillUseSink(customer) ) {
                            customer.MenuOptionGotoSink();
                            break;
                        }
                        else if ( bathroom.HasWaitingSpot ) {
                            customer.MenuOptionGotoWaiting();
                            break;
                        }
                    }
                }
            }
        }
        // TODO: Convert this into its own debug toggle
        foreach ( var seat in Bar.Singleton.Seats ) {
            if ( seat.OccupiedBy == null && seat.IsSoiled ) {
                seat.IsSoiled = false;
            }
        }
    }
    /// <summary>
    /// Fades away the night start screen and eventually disables it.
    /// </summary>
    /// <returns>Returns false if still fading. Returns true when finished fading.</returns>
    private bool FadeOutNightStartScreen() {
        // If the canvas isn't enabled, dont perform any action
        if ( !NightStartCanvas.gameObject.activeSelf ) {
            return true;
        }
        // Delay starting the fade by nightStartDelay seconds
        else if ( nightStartDelay > 0f ) {
            nightStartDelay -= 1 * Time.unscaledDeltaTime;
            return false;
        }
        // Start fading the background first
        else if ( NightStartOverlay.color.a > 0.2 ) {
            float rate = 0.5f * Time.unscaledDeltaTime;
            Color current = NightStartOverlay.color;
            current.a = Math.Max(current.a - rate, 0f);
            NightStartOverlay.color = current;
            return false;
        }
        // Start fading the text second
        else if ( NightStartOverlay.color.a <= 0.2 && NightStartText.color.a > 0 ) {
            float rate = 0.5f * Time.unscaledDeltaTime;
            Color current = NightStartOverlay.color;
            current.a = Math.Max(current.a - rate, 0f);
            NightStartOverlay.color = current;
            rate = 0.5f * Time.unscaledDeltaTime;
            current = NightStartText.color;
            current.a = Math.Max(current.a - rate, 0f);
            NightStartText.color = current;
            return false;
        }
        // Done fading
        else {
            NightStartCanvas.gameObject.SetActive(false);
            return true;
        }
    }
    /// <summary>
    /// Resets static properties between scene reloads
    /// </summary>
    private void ResetStaticMembers() {
        AID = null;
        GamePaused = false;
        Assets.Scripts.Customers.Navigation.Nodes = new Dictionary<Location, List<NavigationNode>>();
    }
    private void EndGame() {
        PauseGame();
        PauseMenu.SwitchToBoldTextDisplay();
        PauseMenu.SetBoldTextDisplay($"End of night {Game.Night}\r\n\r\nYou made ${Game.Funds - nightStartFunds}.");
        PauseMenu.EnableContinueButton(true);
        FadeToBlack = true;
    }
    private void StartBuildMode() {
        InBuildMenu = true;
        CanPause = false;
        Time.timeScale = 0;
        BuildMenu.Start();

        // Update build button states
        var position = FC.AutoPanning ? FC.PanIntent : FC.transform.position;
        CameraPosition.UpdatePositions(position);
        NorthButton.gameObject.SetActive(CameraPosition.HasPosition(Orientation.North));
        SouthButton.gameObject.SetActive(CameraPosition.HasPosition(Orientation.South));
        EastButton.gameObject.SetActive(CameraPosition.HasPosition(Orientation.East));
        WestButton.gameObject.SetActive(CameraPosition.HasPosition(Orientation.West));
    }
    private void CycleCamera( Orientation orientation ) {
        var position = FC.AutoPanning ? FC.PanIntent : FC.transform.position;
        CameraPosition.UpdatePositions(position);
        if ( InBuildMenu ) {
            var result = CameraPosition.Navigate(orientation, position);
            if ( result != null ) {
                // Sucessfully moved the camera.
                BuildMenu.CloseBuildOptionsMenu();
                // Recalculate camera positions to update the buttons.
                CameraPosition.UpdatePositions(result.Pan);
                NorthButton.gameObject.SetActive(CameraPosition.HasPosition(Orientation.North));
                SouthButton.gameObject.SetActive(CameraPosition.HasPosition(Orientation.South));
                EastButton.gameObject.SetActive(CameraPosition.HasPosition(Orientation.East));
                WestButton.gameObject.SetActive(CameraPosition.HasPosition(Orientation.West));
            }
        }
        else {
            CameraPosition.Navigate(orientation, position);
        }
    }
    /// <summary>
    /// Pauses the game.
    /// <para>Closes all open menus and enables the game paused canvas</para>
    /// </summary>
    private void PauseGame() {
        Time.timeScale = 0;
        GamePaused = true;
        FC.Locked = true;
        // These two shouldnt be done in production, its just a bandaid. Add a cached last pan last zoom to return to ???
        LastZoom = FC.Zoom;
        LastPan = FC.Pan;
        FC.ZoomTo(Freecam.MinZoom);
        FC.PanTo(Freecam.Center);
        // Close all open menus.
        Menu.CloseAllOpenMenus();
        PauseMenu.Open();
        Debug.Log("Game paused.");
    }
    /// <summary>
    /// Resumes the game, only if the game isnt ended.
    /// </summary>
    private void ResumeGame() {
        if ( GameEnd ) {
            Debug.LogWarning("Trying to unpause game but game has ended.");
            return;
        }
        Time.timeScale = 1;
        GamePaused = false;
        FC.Locked = false;
        FC.ZoomTo(LastZoom);
        FC.PanTo(LastPan);
        PauseMenu.Close();
        Debug.Log("Game resumed.");
    }
    /// <summary>
    /// Advances the night forward one time tick.
    /// <para>Adds funds for each customer in the bar</para>
    /// <para>Updates time related displays on the screen</para>
    /// </summary>
    private void AdvanceTime() {
        // Generate funds for customers in bar
        double amount = ( CM.CustomersInBar.Count() * 3d ) + ( CM.CustomersInBathroom.Count() * 1d );
        Game.Funds += amount;
        UpdateFundsDisplay();
        // TODO: Have a little money emote display above each customer in the bar who generated funds.
        // Advance time
        TicksElapsed++;
    }

    #endregion

}
