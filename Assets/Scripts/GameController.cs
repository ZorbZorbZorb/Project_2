using Assets.Scripts;
using Assets.Scripts.Areas;
using Assets.Scripts.Customers;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public partial class GameController : MonoBehaviour {

    #region Fields

    [Header("Settings")]
    public bool DisplayNightStartSplash = true;
    public bool DisplayBuildMenuOnFirstNight = false;
    public int NightMaxTime = 30;
    public int NightMaxCustomerSpawnTime = 20;
    [SerializeField, Range(15f,65f)]
    public float CameraTolerangeTight = 18f;
    [SerializeField, Range(30f, 110f)]
    public float CameraTolerangeMid = 50f;
    [SerializeField, Range(30f, 110f)]
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
    public bool RapidCustomerSpawn = false;
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
    public bool SpawningEnabled = true;
    [HideInInspector]
    public bool CanPause = true;
    [HideInInspector]
    public static bool CreateNewSaveData = true;  // Hey, turn this off on build
    [HideInInspector]
    public bool DisplayedNightStartSplashScreen = false;

    /// <summary><see cref="CustomerManager"/> singleton. Tracks custromers and handles spawning.</summary>
    [HideInInspector]
    public CustomerManager CM;
    /// <summary><see cref="GameController"/> singleton</summary>
    [HideInInspector]
    public static GameController GC = null;
    /// <summary><see cref="Freecam"/> singleton</summary>
    [HideInInspector]
    public static Freecam FC = null;

    // Save data
    public GameSaveData Game;

    // Unique Id System
    private static int uid = 0;
    public static int GetUid() => uid++;

    public int timeTicksElapsed = 0;
    public DateTime barTime;
    public Text barTimeDisplay;
    public Text fundsDisplay;
    public int AdvanceBarTimeEveryXSeconds;
    public int AdvanceBarTimeByXMinutes;

    public int ticksSinceLastSpawn = 0;
    public double nightStartFunds;
    public bool GameStarted = false;
    public bool GameLost = false;
    public bool GameEnd = false;
    public bool FadeToBlack = false;
    private bool ReadyToStartNight = false;

    public float nightStartDelay = 2f;
    public Canvas NightStartCanvas;
    public Text NightStartText;
    public Image NightStartOverlay;

    #endregion

    private void Awake() {
        if ( GC != null ) {
            Debug.LogError("GC singleton was already set! May have possible created a second game controller!");
        }
        GC = this;
        Customer.GC = this;

        NorthButton.onClick.AddListener(() => { CycleCamera(Orientation.North); });
        SouthButton.onClick.AddListener(() => { CycleCamera(Orientation.South); });
        EastButton.onClick.AddListener(() => { CycleCamera(Orientation.East); });
        WestButton.onClick.AddListener(() => { CycleCamera(Orientation.West); });
    }
    void Start() {

        CameraPosition.AddPosition(Freecam.Center, 450);
        CameraPosition.AddPosition(Bathroom.BathroomM.transform.position, 450);
        CameraPosition.AddPosition(Bathroom.BathroomF.transform.position, 450);

        // Freecam should always be attached to the main camera
        FC = Camera.main.GetComponent<Freecam>();

        CM = new CustomerManager() {
            CustomersHolder = GameObject.FindGameObjectWithTag("CustomersHolder")
        };

        // Set the customer's static variables
        Customer.BathroomStartX =
            Bathroom.BathroomM.Area.Area.bounds.min.x - ( Prefabs.PrefabCustomer.SRenderer.transform.localScale.x );
        Customer.BathroomStartY =
            Bathroom.BathroomM.Area.Area.bounds.min.y + ( Prefabs.PrefabCustomer.SRenderer.transform.localScale.y );

        // Shut off all of the Area2d colliders
        Bathroom.BathroomM.Area.Area.enabled = false;
        Bathroom.BathroomF.Area.Area.enabled = false;
        Bar.Singleton.Area.Area.enabled = false;

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
        }
        else {
            Game = GameSaveData.Import(0);
        }
        UpdateFundsDisplay();

        // Construct the play areas
        Game.Apply();

        // initialize bar time
        barTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 0, 0);

        // Start build mode if not first night
        if ( DisplayBuildMenuOnFirstNight || Game.Night > 1 ) {
            StartBuildMode();
        }
        else {
            ReadyToStartNight = true;
            CM.MaxCustomers = Bar.Singleton.Seats.Count;
        }
    }
    void Update() {
        if (Input.anyKeyDown) {
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
                CM.CreateCustomer(desperate: true);

                // Debugging only
                if ( OnlySpawnOneCustomer ) {
                    OnlySpawnOneCustomer = false;
                    SpawningEnabled = false;
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
        if (RapidSimulation) {
            deltaTime *= 10f;
        }
        runTime += deltaTime;
        timeAcc += deltaTime;

        if ( timeAcc >= 1 ) {
            timeAcc -= 1;
            Think();
            if (Autoplay) {
                StupidIdiotAutoplayThing();
            }
            // Update time and funds display once per second.
            barTimeDisplay.text = barTime.ToString("hh:mm tt");
        }

        void HandleKeypresses() {
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
    }
    private void StupidIdiotAutoplayThing() {
        var remaining = CM.MaxCustomers - CM.Customers.Count;
        if ( RapidCustomerSpawn ) {
            for ( int i = 0; i < Math.Min(remaining, 4); i++ ) {
                CM.CreateCustomer(desperate: true);
            }
        }
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
                        else if ( bathroom.HasUrinalAvailable && Customer.WillUseUrinal(customer)) {
                            customer.MenuOptionGotoUrinal();
                            break;
                        }
                        else if (bathroom.HasSinkForRelief && Customer.WillUseSink(customer) ) {
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
        foreach(var seat in Bar.Singleton.Seats) {
            if (seat.OccupiedBy == null && seat.IsSoiled) {
                seat.IsSoiled = false;
            }
        }
    }
    private void Think() {
        // Update max seating in bar
        CM.MaxCustomers = Bar.Singleton.Seats
            .Where(x => !x.IsSoiled)
            .Count();

        // End the game if too many seats are soiled
        if ( CM.MaxCustomers < ( Bar.Singleton.Seats.Count / 2 ) ) {
            if ( !NoLose ) {
                GameEnd = true;
                GameLost = true;
                return;
            }
        }

        // Stop spawning customers when its too late
        if ( timeTicksElapsed >= NightMaxCustomerSpawnTime ) {
            SpawningEnabled = false;

            // End game at end time or everyone has left
            if ( !CM.Customers.Any() || timeTicksElapsed >= NightMaxTime ) {
                GameEnd = true;
                return;
            }
        }

        // Customer spawning
        if ( SpawningEnabled && !CM.AtCapacity ) {
            ticksSinceLastSpawn++;
            bool spawnNow = Random.Range(0, 6) == 0;
            if ( ( spawnNow && ticksSinceLastSpawn > 1 ) || ticksSinceLastSpawn > 6 ) {
                ticksSinceLastSpawn = 0;
                Customer customer = CM.CreateCustomer(desperate: false);
            }
        }

        // Update the bar time
        if ( Math.Floor(runTime / AdvanceBarTimeEveryXSeconds) > timeTicksElapsed ) {
            if (!FreezeTime) {
                AdvanceTime();
            }
        }
    }
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

    #region Nights, Game state, and Startup
    /// <summary>
    /// Fades away the night start screen and eventually disables it.
    /// </summary>
    /// <returns>Returns false if still fading. Returns true when finished fading.</returns>
    public bool FadeOutNightStartScreen() {
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
    /// Restarts the current night in progress.
    /// Just reloads the scene to reload the current saved data, because the
    /// scene in play is unsaved.
    /// </summary>
    public void RestartCurrentNight() {
        ResetStaticMembers();
        // Reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void ContinueToNextNight() {
        PauseMenu.Close();
        Game.Night++;
        Game.Export(0);
        ResetStaticMembers();
        // Reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    /// <summary>
    /// Resets static properties between scene reloads
    /// </summary>
    public void ResetStaticMembers() {
        uid = 0;
        GamePaused = false;
    }
    void EndGame() {
        PauseGame();
        PauseMenu.SwitchToBoldTextDisplay();
        PauseMenu.SetBoldTextDisplay($"End of night {Game.Night}\r\n\r\nYou made ${Game.Funds - nightStartFunds}.");
        PauseMenu.EnableContinueButton(true);
        FadeToBlack = true;
    }
    #endregion

    #region Menus    
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
    public void StartBuildMode() {
        InBuildMenu = true;
        CanPause = false;
        Time.timeScale = 0;
        BuildMenu.Open();
    }
    public void EndBuildMode() {
        InBuildMenu = false;
        ReadyToStartNight = true;
        Time.timeScale = 1;
        BuildMenu.Close();
        CM.MaxCustomers = Bar.Singleton.Seats.Count;
    }
    void CycleCamera(Orientation orientation) {
        var position = FC.AutoPanning ? FC.PanIntent : FC.transform.position;
        CameraPosition.UpdatePositions(position);
        if ( InBuildMenu ) {
            var result = CameraPosition.Navigate(orientation, position);
            if ( result != null ) {
                // Sucessfully moved the camera. Recalculate camera positions to update the buttons.
                CameraPosition.UpdatePositions(result.Pan);
                NorthButton.interactable = CameraPosition.HasPosition(Orientation.North);
                SouthButton.interactable = CameraPosition.HasPosition(Orientation.South);
                EastButton.interactable = CameraPosition.HasPosition(Orientation.East);
                WestButton.interactable = CameraPosition.HasPosition(Orientation.West);
            }
        }
        else {
            CameraPosition.Navigate(Orientation.North, position);
        }
    }
    /// <summary>
    /// Pauses the game.
    /// <para>Closes all open menus and enables the game paused canvas</para>
    /// </summary>
    public void PauseGame() {
        Time.timeScale = 0;
        GamePaused = true;
        FC.Locked = true;
        // These two shouldnt be done in production, its just a bandaid. Add a cached last pan last zoom to return to ???
        FC.ZoomTo(Freecam.MinZoom, instant: true);
        FC.UnpanCamera();
        // Close all open menus.
        Menu.CloseAllOpenMenus();
        PauseMenu.Open();
        Debug.Log("Game paused.");
    }
    /// <summary>
    /// Resumes the game, only if the game isnt ended.
    /// </summary>
    void ResumeGame() {
        if ( GameEnd ) {
            Debug.LogWarning("Trying to unpause game but game has ended.");
            return;
        }
        Time.timeScale = 1;
        GamePaused = false;
        FC.Locked = false;
        PauseMenu.Close();
        Debug.Log("Game resumed.");
    }
    #endregion

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
        timeTicksElapsed++;
        // Update time related displays
        barTime = barTime.AddMinutes(AdvanceBarTimeByXMinutes);
        barTimeDisplay.text = barTime.ToString("hh:mm tt");
    }
    public void UpdateFundsDisplay() {
        fundsDisplay.text = $"${Math.Round(Game.Funds, 0)}";
    }
}
