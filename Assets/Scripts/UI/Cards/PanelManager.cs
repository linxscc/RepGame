using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NUnit.Framework.Internal;
using TMPro;
using LiteNetLib;
using RepGame;
using RepGame.Core;

namespace RepGame.UI
{
    public class PanelManager : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject panelServer;
        public GameObject panelStart;
        public GameObject panelMain;

        [Header("Server UI Elements")]
        public TextMeshProUGUI serverStatusText;

        [Header("Start Panel Buttons")]
        public Button startGameButton;
        public Button exitGameButton;

        void Start()
        {
            // Initialize panels
            panelServer.SetActive(true);
            panelStart.SetActive(false);
            panelMain.SetActive(false);

            // Add button listeners
            startGameButton.onClick.AddListener(OnStartGameClicked);
            exitGameButton.onClick.AddListener(OnExitGameClicked);
        }

        public void OnStartGameClicked()
        {
            // Trigger the "StartCardGame" event
            EventManager.TriggerEvent("StartCardGame");
            

            // Transition to Main Panel
            panelStart.SetActive(false);
            panelMain.SetActive(true);
        }

        private void OnExitGameClicked()
        {
            // Exit the game
            Debug.Log("Exiting game...");
            Application.Quit();
        }
    }
}
