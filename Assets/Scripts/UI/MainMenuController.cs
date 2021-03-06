using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour {

    [SerializeField] public Button ButtonContinue;
    [SerializeField] public Button ButtonNewGame;
    [SerializeField] public Button ButtonOptions;
    [SerializeField] public Button ButtonQuit;

    [SerializeField] public Text TextContinue;

    private void Awake() {
        // Reset screen resolution
        Screen.SetResolution(1366, 768, FullScreenMode.Windowed, GameController.TargetFramerate);
        Application.targetFrameRate = GameController.TargetFramerate;
    }
    void Start() {
        if (GameSaveData.Exists(1)) {
            try {
                GameSaveData data = GameSaveData.Import(1);
                TextContinue.text = $"Continue (Night {data.Night})";
                ButtonContinue.interactable = true;
                ButtonContinue.onClick.AddListener(ContinueGame);
            }
            catch {
                Debug.LogError("Save data load failed");
                TextContinue.text = $"Save data load failed";
            }
        }

        ButtonNewGame.onClick.AddListener(NewGame);
        ButtonQuit.onClick.AddListener(Quit);
    }

    public void NewGame() {
        GameController.CreateNewSaveData = true;
        SceneManager.LoadScene(1);
    }

    public void ContinueGame() {
        GameController.CreateNewSaveData = false;
        SceneManager.LoadScene(1);
    }

    public void Quit() {
        Application.Quit();
    }
}
