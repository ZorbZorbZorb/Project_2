using Assets.Scripts;
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

    void Start() {
        if (GameSaveData.Exists(0)) {
            GameSaveData data = GameSaveData.Import(0);
            TextContinue.text = $"Continue (Night {data.night})";
            ButtonContinue.interactable = true;
        }

        ButtonContinue.onClick.AddListener(ContinueGame);
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
