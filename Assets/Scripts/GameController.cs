using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour {

    [SerializeField] public bool spawningEnabled = true;
    [SerializeField] public static bool CreateNewSaveDataOnStart = true;  // Hey, turn this off on build
    [SerializeField] public GameData gameData;

    // Unique Id System
    static private int uid = 0;
    static public int GetUid() => uid++;

    [SerializeField]
    public float runTime = 0f;
    [SerializeField]
    public int timeTicksElapsed = 0;
    [SerializeField]
    public DateTime barTime;
    [SerializeField]
    public Text barTimeDisplay;
    [SerializeField]
    public Text fundsDisplay;
    [SerializeField]
    public int AdvanceBarTimeEveryXSeconds;
    [SerializeField]
    public int AdvanceBarTimeByXMinutes;

    public Customer templateCustomer;

    public static List<Customer> customers = new List<Customer>();
    public int ticksSinceLastSpawn = 0;
    public int maxCustomers = 14;
    public double nightStartFunds;
    public bool gameStarted = false;
    public bool gameLost = false;
    public bool gameEnd = false;
    public bool fadeToBlack = false;

    public float nightStartDelay = 2f;
    [SerializeField]
    public Canvas NightStartCanvas;
    [SerializeField]
    public Text NightStartText;
    [SerializeField]
    public SpriteRenderer NightStartOverlay;

    public void SetMaxCustomers(int max) {
        maxCustomers = max;
    }

    [SerializeField]
    public WaitingRoom waitingRoom;
    [SerializeField]
    public DoorwayQueue doorwayQueue;

    public static GameController controller = null;

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
        CreateNewSaveDataOnStart = false;
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
    }

    #region PauseMenu
    [SerializeField]
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
        // Close all open menus.
        Menu.CloseAllOpenMenus();
        PauseMenu.Open();
        Debug.Log("Game paused.");
    }
    /// <summary>
    /// Resumes the game, only if the game isnt ended.
    /// </summary>
    void ResumeGame() {
        if (gameEnd) {
            Debug.LogWarning("Player wants to resume game but game has ended.");
            return;
        }
        Time.timeScale = 1;
        gamePaused = false;
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
        if ( gamePaused ) {
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
        fadeToBlack = true;
    }
    /// <summary>
    /// Fades away the night start screen and eventually disables it.
    /// </summary>
    /// <returns>Returns false if still fading. Returns true when finished fading.</returns>
    public bool FadeOutNightStartScreen() {
        if ( nightStartDelay > 0f) {
            nightStartDelay -= 1 * Time.unscaledDeltaTime;
            return false;
        }
        else if (NightStartOverlay.color.a > 0.2) {
            float rate = 0.5f * Time.unscaledDeltaTime;
            Color current = NightStartOverlay.color;
            current.a = Math.Max(current.a - rate, 0f);
            NightStartOverlay.color = current;
            return false;
        }
        else if ( NightStartOverlay.color.a <= 0.2 && NightStartText.color.a > 0) {
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
        controller.gameData.funds += amount;
        controller.UpdateFundsDisplay();
    }
    /// <summary>
    /// Adds a wetting event to track in save data
    /// </summary>
    public static void AddWetting() {
        controller.gameData.wettings++;
    }

    void Start() {
        if ( controller != null ) {
            throw new InvalidOperationException("Only one game controller may exist");
        }
        controller = this;

        // Clear the menu system's caches.
        Menu.ClearForSceneReload();

        // Set the callbacks for the pause menu buttons. (restart and main menu)
        PauseMenu.SetUpButtons();

        maxCustomers = Bar.Singleton.Seats.Length;

        barTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 21, 0, 0);

        // Load or create game data
        if ( CreateNewSaveDataOnStart ) {
            gameData = new GameData();
            gameData.bathroomSinks = Bathroom.Singleton.Sinks.Items.Count();
            gameData.bathroomToilets = Bathroom.Singleton.Toilets.Count();
            gameData.bathroomUrinals = Bathroom.Singleton.Toilets.Count();
        }
        else {
            LoadNightData();
        }

        NightStartCanvas.gameObject.SetActive(true);
        NightStartText.text = $"Night {gameData.night}";

    }

    void Update() {
        HandleKeypresses();

        if (!gameStarted) {
            if (FadeOutNightStartScreen()) {
                StartGame();
            }
            else {
                return;
            }
        }

        else if (gameEnd){
            if (!GamePaused) {
                if (gameLost) {
                    LoseGame();
                }
                else {
                    EndGame();
                }
            }
            else if ( fadeToBlack ) {
                PauseMenu.FadeOverlayToBlack();
                if (PauseMenu.FadeOverlayComplete()) {
                    fadeToBlack = true;
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

    /// <summary>
    /// This function is called once at the start of each scene to start the night
    /// </summary>
    private void StartGame() {
        gameStarted = true;

        // Timescale and static vars are preserved between scenes!
        ResumeGame();

        nightStartFunds = gameData.funds;

        Customer firstCustomer = CreateCustomer();
        firstCustomer.Active = true;
    }

    public int nightMaxTime = 30;
    public int nightMaxCustomerSpawnTime = 20;
    public double GetCustomerSpawnFactor(int currentTime, int endTime) {
        return currentTime >= endTime
            ? 0d
            : Math.Sin(( currentTime / endTime ) * Math.PI);
    }

    // Think only once a second for better game performance.
    float timeAcc = 0f;

    /// <summary>
    /// Handles user key presses
    /// <para>Pressing escape will pause the game</para>
    /// </summary>
    private void HandleKeypresses() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ToggleMenu();
        }
    }

    // Temp method that despawns customers in the bar that dont need to go or have peed themselves
    private void DespawnCustomerOutside() {
        Customer[] targets = customers.Where(x => x.position == Collections.Location.Outside && x.AtDestination() && x.Active).ToArray();
        foreach(Customer target in targets) {
            RemoveCustomer(target);
        }
    }
    private int CustomersInBarNotDesperate() {
        return customers
            .Where(x => x.DesperationState == Collections.CustomerDesperationState.State0 || x.DesperationState == Collections.CustomerDesperationState.State1)
            .Count();
    }
    private int CustomersInBarDesperate() {
        return customers
            .Where(x => 
            x.DesperationState == Collections.CustomerDesperationState.State3 || 
                x.DesperationState != Collections.CustomerDesperationState.State4)
            .Count();
    }
    private IEnumerable<Customer> CustomersInBar() => customers.Where(x => x.position == Collections.Location.Bar);
    private int GetCustomersAboutToWetCount() {
        return customers
            .Where(x =>
                x.bladder.Percentage > 1d ||
                x.bladder.LosingControl)
            .Count();
    }
    private int GetReliefAvailableCount() {
        return Bathroom.Singleton.Toilets.Count() + Bathroom.Singleton.Urinals.Count() + Bathroom.Singleton.Sinks.Items.Count();
    }
    private int GetCustomersPeeingCount() {
        return customers.Where(x => x.bladder.Emptying).Count();
    }

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
        AddFunds(customersInBar * 1d);
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
        if (maxCustomers <= 10) {
            gameEnd = true;
            gameLost = true;
            return;
        }

        // Stop spawning customers when its too late
        if (timeTicksElapsed >= nightMaxCustomerSpawnTime) {
            spawningEnabled = false;

            // End game at end time or everyone has left
            if (customers.Count() < 1 || timeTicksElapsed >= nightMaxTime ) {
                gameEnd = true;
                return;
            }
        }

        // Customer spawning
        if ( spawningEnabled && customers.Count < maxCustomers ) {
            ticksSinceLastSpawn++;
            bool spawnNow = Random.Range(0, 6) == 0;
            if ((spawnNow && ticksSinceLastSpawn > 1) || ticksSinceLastSpawn > 6) {
                ticksSinceLastSpawn = 0;
                //int customersDesperateCount = CustomersInBarDesperate();
                //int customersNotDesperateCount = CustomersInBarNotDesperate();
                int reliefCount = GetReliefAvailableCount();
                int aboutToWetCount = GetCustomersAboutToWetCount();
                int customersPeeing = GetCustomersPeeingCount();
                //if (((customersNotDesperateCount / 2) > customersDesperateCount) && (aboutToWetCount < reliefCount)) {
                if (aboutToWetCount + customersPeeing <= reliefCount) {
                    Customer customer = SpawnCustomerInBar(desperate: true);
                }
                else {
                    Customer customer = SpawnCustomerInBar(desperate: false);
                }
            }
        }

        // Update the bar time
        if ( Math.Floor( runTime / AdvanceBarTimeEveryXSeconds) > timeTicksElapsed ) {
            AdvanceTime();
        }
    }

    public Customer SpawnCustomerInBar(double metric) {
        throw new NotImplementedException();
    }
    public Customer SpawnCustomerInBar(bool desperate) {
        Customer newCustomer = Instantiate(templateCustomer);
        newCustomer.Gender = Random.Range(0, 3) == 0 ? 'm' : 'f';
        customers.Add(newCustomer);
        if (desperate) {
            newCustomer.SetupCustomer(80, 100);
        }
        else {
            newCustomer.SetupCustomer(30, 92);
        }
        Debug.Log($"Customer {newCustomer.UID} created. state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}");
        newCustomer.Active = true;
        bool enteredDoorway = false;
        if ( newCustomer.WantsToEnterBathroom() &&
            (newCustomer.DesperationState == Collections.CustomerDesperationState.State3 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State4 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State5) ) {

            enteredDoorway = newCustomer.EnterDoorway();
        }
        // Else sit right down at the bar and wait
        if ( !enteredDoorway ) {
            Seat seat = Bar.Singleton.GetOpenSeat();
            seat.MoveCustomerIntoSpot(newCustomer);
        }

        return newCustomer;
    }

    public Customer CreateCustomer() {
        Customer newCustomer = Instantiate(templateCustomer);
        newCustomer.Gender = Random.Range(0,3) == 0 ? 'm' : 'f';
        customers.Add(newCustomer);

        newCustomer.EnteredTicksElapsed = timeTicksElapsed;

        // Customer count changes range of bladder fullness for next customer to enter
        int min = 35;
        int max = 105;
        if (customers.Count() > 8) {
            min = 10;
            max = 80;
        }
        if (customers.Count() == 1) {
            min = 85;
            max = 98;
        }
        //Debug.LogWarning($"{min}, {max}");

        newCustomer.SetupCustomer(min, max);
        Debug.Log($"Customer {newCustomer.UID} created. state: {newCustomer.DesperationState} bladder: {Math.Round(newCustomer.bladder.Amount)} / {newCustomer.bladder.Max} control: {Math.Round(newCustomer.bladder.ControlRemaining)}");
        newCustomer.Active = true;

        // If customer enters bar and needs to go badly try to enter bathroom right away
        bool enteredDoorway = false;
        //Debug.Log($"Customer {newCustomer.UID} {( newCustomer.FeelsNeedToGo ? "does" : "does-not" )} need to go and is at state {newCustomer.DesperationState}");
        //Debug.Log($"{newCustomer.bladder.FeltNeed}");
        if (newCustomer.WantsToEnterBathroom() &&
            newCustomer.DesperationState == Collections.CustomerDesperationState.State3 ||
            newCustomer.DesperationState == Collections.CustomerDesperationState.State4 || 
            newCustomer.DesperationState == Collections.CustomerDesperationState.State5) {

            enteredDoorway = newCustomer.EnterDoorway();
        }

        // Else sit right down at the bar and wait
        if (!enteredDoorway) { 
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
