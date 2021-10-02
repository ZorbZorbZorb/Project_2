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

    void Start() {
        ButtonNewGame.onClick.AddListener(NewGame);
        ButtonQuit.onClick.AddListener(Quit);
    }

    public void NewGame() {
        SceneManager.LoadScene(1);
    }
    public void Quit() {
        Application.Quit();
    }
}
