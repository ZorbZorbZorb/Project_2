using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public partial class GameController : MonoBehaviour {
    void Start() {
        if ( GC != null ) {
            Debug.LogError("GC singleton was already set! May have possible created a second game controller!");
        }
        GC = this;
        Customer.GC = this;

        // Shut off all of the Area2d colliders
        Bathroom.BathroomM.Area.Area.enabled = false;
        Bathroom.BathroomF.Area.Area.enabled = false;
        Bar.Singleton.Area.Area.enabled = false;

        // Lock up the camera
        Freecam.NoZoom = true;
        Freecam.NoPan = true;

        // Clear the menu system's caches.
        Menu.ClearForSceneReload();

        // Set up menu buttons
        BuildMenu.SetUpButtons();
        PauseMenu.SetUpButtons();

        // Load the prefabs
        Prefabs.Load();

        // Load or create game data
        if ( CreateNewSaveData ) {
            game = GameSaveData.ImportDefault();
        }
        else {
            game = GameSaveData.Import(0);
        }
        UpdateFundsDisplay();

        // Construct the play areas
        game.Apply();

        // Cheat construct bathroom if toggled when starting
        if ( DebugBuildAll ) {
            InteractableSpawnpoint.BuildAll();
            DebugBuildAll = false;
        }

        // initialize bar time
        barTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 0, 0);

        // Start build mode if not first night
        if ( DebugDisplayBuildMenuOnFirstNight || game.Night > 1 ) {
            StartBuildMode();
        }
        else {
            ReadyToStartNight = true;
        }
    }
    void Update() {
        HandleKeypresses();
        if ( !ReadyToStartNight ) {
            return;
        }

        // Display the night start splash screen
        if ( DisplayNightStartSplash && !DisplayedNightStartSplashScreen ) {
            NightStartCanvas.gameObject.SetActive(true);
            NightStartText.text = $"Night {gameData.night}";
        }

        if ( !GameStarted ) {
            if ( FadeOutNightStartScreen() ) {
                DisplayedNightStartSplashScreen = true;
                ResumeGame();
                GameStarted = true;
                CanPause = true;
                CustomersManager.CreateCustomer(desperate: true);

                // Debugging only
                if ( DebugSpawnOneCustomerOnly ) {
                    DebugSpawnOneCustomerOnly = false;
                    SpawningEnabled = false;
                }
            }
            else {
                return;
            }
        }
        else if ( DebugEndNightNow || GameEnd ) {
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

        runTime += Time.deltaTime;
        timeAcc += Time.deltaTime;
        if ( timeAcc >= 1 ) {
            timeAcc -= 1;
            Think();

            // Update time and funds display once per second.
            barTimeDisplay.text = barTime.ToString("hh:mm tt");
        }

    }
    private void Think() {
        // Update max seating in bar
        maxCustomers = Bar.Singleton.Seats
            .Where(x => !x.IsSoiled)
            .Count();

        // End the game if too many seats are soiled
        if ( maxCustomers < ( Bar.Singleton.Seats.Count / 2 ) ) {
            if ( !DebugNoLose ) {
                GameEnd = true;
                GameLost = true;
                return;
            }
        }

        // Stop spawning customers when its too late
        if ( timeTicksElapsed >= nightMaxCustomerSpawnTime ) {
            SpawningEnabled = false;

            // End game at end time or everyone has left
            if ( !CustomersManager.Customers.Any() || timeTicksElapsed >= nightMaxTime ) {
                GameEnd = true;
                return;
            }
        }

        // Customer spawning
        if ( SpawningEnabled && !CustomersManager.AtCapacity ) {
            ticksSinceLastSpawn++;
            bool spawnNow = Random.Range(0, 6) == 0;
            if ( ( spawnNow && ticksSinceLastSpawn > 1 ) || ticksSinceLastSpawn > 6 ) {
                ticksSinceLastSpawn = 0;
                Customer customer = CustomersManager.CreateCustomer(desperate: false);
            }
        }

        // Update the bar time
        if ( Math.Floor(runTime / AdvanceBarTimeEveryXSeconds) > timeTicksElapsed ) {
            AdvanceTime();
        }
    }

    // Build menu
    public bool InBuildMenu;
    public BuildMenu BuildMenu;

    // Pause menu
    public PauseMenu PauseMenu;
    public static bool GamePaused { get; private set; } = false;

    /// <summary>Accumulates <see cref="Time.deltaTime"/> on update</summary>
    public float runTime = 0f;
    /// <summary>Accumulates <see cref="Time.deltaTime"/> on update to call <see cref="Think"/> every 1 second</summary>
    private float timeAcc = 0f;
    public int nightMaxTime = 30;
    public int nightMaxCustomerSpawnTime = 20;

    // Debugging, options and cheats for development
    public bool DebugRapidFill = false;
    public bool DebugRapidPee = false;
    public bool DebugNoLose = false;
    public bool DebugSpawnOneCustomerOnly = false;
    public bool DebugEndNightNow = false;
    public bool DebugDisplayBuildMenuOnFirstNight = false;
    public bool DebugInfiniteMoney = false;
    public bool DebugBuildAll = false;
    public bool DebugCustomersWillinglyUseAny = false;
    public bool DebugStateLogging = false;
    public bool DisplayNightStartSplash = true;

    public bool SpawningEnabled = true;
    public bool CanPause = true;
    public static bool CreateNewSaveData = true;  // Hey, turn this off on build
    public bool DisplayedNightStartSplashScreen = false;
    
    // Managers
    public CustomerManager CustomersManager = new CustomerManager();

    // Save data
    public GameSaveData game;

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
    public int maxCustomers = 14;
    public double nightStartFunds;
    public bool GameStarted = false;
    public bool GameLost = false;
    public bool GameEnd = false;
    public bool FadeToBlack = false;
    private bool ReadyToStartNight = false;

    public float nightStartDelay = 2f;
    public Canvas NightStartCanvas;
    public Text NightStartText;
    public SpriteRenderer NightStartOverlay;

    /// <summary><see cref="GameController"/> singleton</summary>
    public static GameController GC = null;
    /// <summary><see cref="Freecam"/> singleton</summary>
    public static Freecam Freecam = null;

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
        gameData.night++;
        SaveNightData();
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
        InteractableSpawnpoint.Spawnpoints = new List<InteractableSpawnpoint>();
    }

    #region Menus
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
        maxCustomers = Bar.Singleton.Seats.Count;
    }
    /// <summary>
    /// Pauses the game.
    /// <para>Closes all open menus and enables the game paused canvas</para>
    /// </summary>
    void PauseGame() {
        Time.timeScale = 0;
        GamePaused = true;
        Freecam.NoZoom = true;
        Freecam.NoPan = true;
        // These two shouldnt be done in production, its just a bandaid. Add a cached last pan last zoom to return to ???
        Freecam.UnzoomCamera();
        Freecam.UnpanCamera();
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
        Freecam.NoZoom = false;
        Freecam.NoPan = false;
        PauseMenu.Close();
        Debug.Log("Game resumed.");
    }
    #endregion

    void EndGame() {
        PauseGame();
        PauseMenu.SwitchToBoldTextDisplay();
        PauseMenu.SetBoldTextDisplay($"End of night {gameData.night}\r\n\r\nYou made ${gameData.funds - nightStartFunds}.");
        PauseMenu.EnableContinueButton(true);
        FadeToBlack = true;
    }

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
    /// Adds a wetting event to track in save data
    /// </summary>
    public static void AddWetting() {
        GC.gameData.wettings++;
    }


    /// <summary>
    /// Handles user key presses
    /// <para>Pressing escape will pause the game</para>
    /// </summary>
    private void HandleKeypresses() {
        if ( Input.GetKeyDown(KeyCode.Escape) ) {
            if ( CanPause ) {
                if (GamePaused) {
                    ResumeGame();
                }
                else {
                    PauseGame();
                }
            }
        }
    }

    /// <summary>
    /// Advances the night forward one time tick.
    /// <para>Adds funds for each customer in the bar</para>
    /// <para>Updates time related displays on the screen</para>
    /// </summary>
    private void AdvanceTime() {
        // Generate funds for customers in bar
        double amount = ( CustomersManager.CustomersInBar.Count() * 3d ) + ( CustomersManager.CustomersInBathroom.Count() * 1d );
        GC.gameData.funds += amount;
        GC.UpdateFundsDisplay();
        // TODO: Have a little money emote display above each customer in the bar who generated funds.
        // Advance time
        timeTicksElapsed++;
        // Update time related displays
        barTime = barTime.AddMinutes(AdvanceBarTimeByXMinutes);
        barTimeDisplay.text = barTime.ToString("hh:mm tt");
    }
    public void UpdateFundsDisplay() {
        fundsDisplay.text = "$" + Math.Round(gameData.funds, 0).ToString();
    }
    
}
