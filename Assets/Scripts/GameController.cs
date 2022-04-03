using Assets.Scripts;
using Assets.Scripts.Extensions;
using Assets.Scripts.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour {
    void Start() {
        if ( GC != null ) {
            Debug.LogError("GC singleton was already set! May have possible created a second game controller!");
        }
        GC = this;
        Customer.GC = this;

        Freecam.NoZoom = true;
        Freecam.NoPan = true;

        // Clear the menu system's caches.
        Menu.ClearForSceneReload();

        // Set the callbacks for the pause menu buttons. (restart and main menu)
        PauseMenu.SetUpButtons();
        BuildMenu.SetUpButtons();

        maxCustomers = Bar.Singleton.Seats.Count;

        barTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 21, 0, 0);

        // Load or create game data
        if ( CreateNewSaveData ) {
            CreateAndSaveNewSaveData();
        }
        else {
            LoadNightData();
            UpdateFundsDisplay();
        }

        // Construct the bathroom
        InteractableSpawnpoint.Build(gameData);

        // Debugging shenanigans
        var area = Bathroom.BathroomM.BathroomMArea;
        area.GridScaleY = 140;
        area.GridScaleX = 140;
        var point = InteractableSpawnpoint.Spawnpoints.First(x => x.IType == InteractableType.Toilet);
        var prefabs = new Relief[] { Bathroom.BathroomF.PrefabSink, Bathroom.BathroomF.PrefabToilet, Bathroom.BathroomF.PrefabUrinal };
        for ( int i = 0; i < area.GridPointsX; i++ ) {
            for ( int j = 0; j < area.GridPointsY; j++ ) {
                var position = area.GetGridPosition((i, j));
                var vector = new Vector3(position.x, position.y);
                var prefab = Instantiate(prefabs.Random(), vector, point.transform.rotation);
                prefab.Orientation = new Orientation[] { Orientation.North, Orientation.East, Orientation.West }.Random();
            }
        }
        

        // Cheat construct bathroom if toggled when starting
        if ( DebugBuildAll ) {
            InteractableSpawnpoint.BuildAll();
            DebugBuildAll = false;
        }

        // Start build mode if not first night
        if ( DebugDisplayBuildMenuOnFirstNight || gameData.night > 1 ) {
            StartBuildMode();
        }
        else {
            ReadyToStartNight = true;
        }
    }

    private void CreateAndSaveNewSaveData() {
        // Make a new game data
        CreateNewSaveData = false;
        gameData = new GameData();
        // Add all points that start unlocked
        InteractableSpawnpoint.Spawnpoints
            .Where(x => x.StartsUnlocked)
            .ToList()
            .ForEach(x => gameData.UnlockedPoints.Add(x.Id));
        // Save the new data
        SaveNightData();
    }

    void Update() {
        if ( DebugBuildAll ) {
            InteractableSpawnpoint.BuildAll();
            DebugBuildAll = false;
        }

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
                StartGame();
            }
            else {
                return;
            }
        }

        else if ( DebugEndNightNow || GameEnd ) {
            if ( !GamePaused ) {
                if ( GameLost ) {
                    LoseGame();
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
            DespawnCustomerOutside();
            Think();

            // Update time and funds display once per second.
            barTimeDisplay.text = barTime.ToString("hh:mm tt");
        }

    }

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
    public GameData gameData;

    public bool DisplayedNightStartSplashScreen = false;

    public Seat SeatPrefab;
    public BarTable TablePrefab;

    // Unique Id System
    private static int uid = 0;
    public static int GetUid() => uid++;

    public float runTime = 0f;
    public int timeTicksElapsed = 0;
    public DateTime barTime;
    public Text barTimeDisplay;
    public Text fundsDisplay;
    public int AdvanceBarTimeEveryXSeconds;
    public int AdvanceBarTimeByXMinutes;

    public Customer templateCustomer;

    public static List<Customer> customers = new List<Customer>();
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

    public void SetMaxCustomers(int max) {
        maxCustomers = max;
    }

    public WaitingRoom waitingRoom;
    public DoorwayQueue doorwayQueue;

    /// <summary>
    /// References the currently active GameController singleton
    /// </summary>
    public static GameController GC = null;

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
    public void SaveNightData() {
        CreateNewSaveData = false;
        GameData.Export(0, gameData);
    }
    public void LoadNightData() {
        gameData = GameData.Import(0);
    }
    /// <summary>
    /// Resets static properties between scene reloads
    /// </summary>
    public void ResetStaticMembers() {
        uid = 0;
        GamePaused = false;
        gamePaused = false;
        customers = new List<Customer>();
        InteractableSpawnpoint.Spawnpoints = new List<InteractableSpawnpoint>();
    }

    #region BuildMenu
    public bool InBuildMenu;
    public BuildMenu BuildMenu;
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
    #endregion

    #region PauseMenu
    public PauseMenu PauseMenu;
    private static bool gamePaused = false;
    public static bool GamePaused {
        get => gamePaused;
        private set => gamePaused = value;
    }
    /// <summary>
    /// Pauses the game.
    /// <para>Closes all open menus and enables the game paused canvas</para>
    /// </summary>
    void PauseGame() {
        Time.timeScale = 0;
        gamePaused = true;
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
            Debug.LogWarning("Player wants to resume game but game has ended.");
            return;
        }
        Time.timeScale = 1;
        gamePaused = false;
        Freecam.NoZoom = false;
        Freecam.NoPan = false;
        PauseMenu.Close();
        Debug.Log("Game resumed.");
    }
    private void OpenMenu() {
        gamePaused = true;
        PauseGame();
    }
    private void CloseMenu() {
        gamePaused = false;
        ResumeGame();
    }
    /// <summary>
    /// Toggles the pause menu
    /// </summary>
    private void ToggleMenu() {
        if ( CanPause && gamePaused ) {
            CloseMenu();
        }
        else {
            OpenMenu();
        }
    }
    #endregion

    /// <summary>
    /// Sets the game to be lost. Displays the game over pause menu
    /// </summary>
    void LoseGame() {
        PauseGame();
        PauseMenu.SwitchToBoldTextDisplay();
    }

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
    /// Adds funds to track in save data
    /// </summary>
    /// <param name="amount">amount to add</param>
    public static void AddFunds(double amount) {
        GC.gameData.funds += amount;
        GC.UpdateFundsDisplay();
    }
    /// <summary>
    /// Adds a wetting event to track in save data
    /// </summary>
    public static void AddWetting() {
        GC.gameData.wettings++;
    }


    /// <summary>
    /// This function is called once at the start of each scene to start the night
    /// </summary>
    private void StartGame() {
        GameStarted = true;
        CanPause = true;

        // Timescale and static vars are preserved between scenes!
        ResumeGame();

        nightStartFunds = gameData.funds;

        Customer firstCustomer = CreateCustomer();
        firstCustomer.Active = true;

        if ( DebugSpawnOneCustomerOnly ) {
            DebugSpawnOneCustomerOnly = false;
            SpawningEnabled = false;
        }
    }

    public int nightMaxTime = 30;
    public int nightMaxCustomerSpawnTime = 20;

    // Think only once a second for better game performance.
    float timeAcc = 0f;

    /// <summary>
    /// Handles user key presses
    /// <para>Pressing escape will pause the game</para>
    /// </summary>
    private void HandleKeypresses() {
        if ( Input.GetKeyDown(KeyCode.Escape) ) {
            if ( CanPause ) {
                ToggleMenu();
            }
        }
    }

    // Temp method that despawns customers in the bar that dont need to go or have peed themselves
    private void DespawnCustomerOutside() {
        Customer[] targets = customers.Where(x => x.position == Collections.Location.Outside && x.AtDestination && x.Active).ToArray();
        foreach ( Customer target in targets ) {
            RemoveCustomer(target);
        }
    }
    private IEnumerable<Customer> CustomersInBar() => customers.Where(x => x.position == Collections.Location.Bar);
    private IEnumerable<Customer> CustomersInBathroom() => customers
        .Where(x => x.position == Collections.Location.Doorway || x.position == Collections.Location.Relief || x.position == Collections.Location.WaitingRoom);
    public void UpdateFundsDisplay() {
        fundsDisplay.text = "$" + Math.Round(gameData.funds, 0).ToString();
    }

    /// <summary>
    /// Advances the night forward one time tick.
    /// <para>Adds funds for each customer in the bar</para>
    /// <para>Updates time related displays on the screen</para>
    /// </summary>
    private void AdvanceTime() {
        // Generate funds for customers in bar
        int customersInBar = CustomersInBar().Count();
        int customersInBathroom = CustomersInBathroom().Count();
        AddFunds(( customersInBar * 3d ) + ( customersInBathroom * 1d ));
        // TODO: Have a little money emote display above each customer in the bar who generated funds.
        // Advance time
        timeTicksElapsed++;
        // Update time related displays
        barTime = barTime.AddMinutes(AdvanceBarTimeByXMinutes);
        barTimeDisplay.text = barTime.ToString("hh:mm tt");
    }

    // Thinks about what should happen next, spawning customers
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
            if ( customers.Count() < 1 || timeTicksElapsed >= nightMaxTime ) {
                GameEnd = true;
                return;
            }
        }

        // Customer spawning
        if ( SpawningEnabled && customers.Count < maxCustomers ) {
            ticksSinceLastSpawn++;
            bool spawnNow = Random.Range(0, 6) == 0;
            if ( ( spawnNow && ticksSinceLastSpawn > 1 ) || ticksSinceLastSpawn > 6 ) {
                ticksSinceLastSpawn = 0;
                Customer customer = SpawnCustomerInBar(desperate: false);
            }
        }

        // Update the bar time
        if ( Math.Floor(runTime / AdvanceBarTimeEveryXSeconds) > timeTicksElapsed ) {
            AdvanceTime();
        }
    }

    public Customer SpawnCustomerInBar(bool desperate) {
        Customer newCustomer = Instantiate(templateCustomer, Collections.OffScreenTop, templateCustomer.transform.rotation);
        newCustomer.Gender = Random.Range(0, 3) == 0 ? 'm' : 'f';
        customers.Add(newCustomer);
        if ( desperate ) {
            newCustomer.SetupCustomer(80, 100);
        }
        else {
            newCustomer.SetupCustomer(30, 92);
        }
        Debug.Log($"Customer {newCustomer.UID} created. state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}");
        newCustomer.Active = true;
        bool enteredDoorway = false;
        if ( newCustomer.WantsToEnterBathroom() &&
            ( newCustomer.DesperationState == Collections.CustomerDesperationState.State3 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State4 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State5 ) ) {

            enteredDoorway = newCustomer.EnterDoorway();
        }
        // Else sit right down at the bar and wait
        if ( !enteredDoorway ) {
            Seat seat = Bar.Singleton.GetOpenSeat();
            seat.MoveCustomerIntoSpot(newCustomer);
        }

        return newCustomer;
    }

    [Obsolete("Duplicate of GameController::SpawnCustomerInBar(bool)")]
    public Customer CreateCustomer() {
        Customer newCustomer = Instantiate(templateCustomer, Collections.OffScreenTop, templateCustomer.transform.rotation);
        newCustomer.Gender = Random.Range(0, 3) == 0 ? 'm' : 'f';
        customers.Add(newCustomer);

        newCustomer.EnteredTicksElapsed = timeTicksElapsed;

        // Customer count changes range of bladder fullness for next customer to enter
        int min = 35;
        int max = 105;
        if ( customers.Count() > 8 ) {
            min = 10;
            max = 80;
        }
        if ( customers.Count() == 1 ) {
            min = 85;
            max = 98;
        }

        newCustomer.SetupCustomer(min, max);
        Debug.Log($"Customer {newCustomer.UID} created. state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}");
        newCustomer.Active = true;

        // If customer enters bar and needs to go badly try to enter bathroom right away
        bool enteredDoorway = false;
        if ( newCustomer.WantsToEnterBathroom() &&
            newCustomer.DesperationState == Collections.CustomerDesperationState.State3 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State4 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State5 ) {

            enteredDoorway = newCustomer.EnterDoorway();
        }

        // Else sit right down at the bar and wait
        if ( !enteredDoorway ) {
            Seat seat = Bar.Singleton.GetOpenSeat();
            seat.MoveCustomerIntoSpot(newCustomer);
        }

        return newCustomer;
    }
    public void RemoveCustomer(Customer customer) {
        Debug.LogWarning($"Deleted customer {customer.UID}");
        customer.StopOccupyingAll();
        customers.Remove(customer);
        Destroy(customer.gameObject);
    }

}
