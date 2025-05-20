using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NUnit.Framework.Internal;
using TMPro;
using LiteNetLib;
using RepGame;
using RepGame.Core;
using System.Collections.Generic;
using RepGameModels;

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
            // 使用 Transform.Find 查找子路径下的对象
            panelServer = transform.Find("Panel_Server").gameObject;
            panelStart = transform.Find("Panel_Start").gameObject;
            panelMain = transform.Find("Panel_Main").gameObject;

            // Initialize panels
            panelServer.SetActive(true);
            panelStart.SetActive(false);
            panelMain.SetActive(false);

            // 查找按钮并添加监听器
            startGameButton = transform.Find("Panel_Start/Start").GetComponent<Button>();
            exitGameButton = transform.Find("Panel_Start/Quit").GetComponent<Button>();

            serverStatusText = transform.Find("Panel_Server/ServerStatus").GetComponent<TextMeshProUGUI>();

            startGameButton.onClick.AddListener(OnStartGameClicked);
            exitGameButton.onClick.AddListener(OnExitGameClicked);
        }
        
        private void OnEnable()
        {
            // Subscribe to the "StartCardGame" event
            EventManager.Subscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
        }
    
        private void OnDisable()
        {
            // Unsubscribe from the event to prevent memory leaks
            EventManager.Unsubscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
        }

        public void OnStartGameClicked()
        {
            // Trigger the "StartCardGame" event
            EventManager.TriggerEvent("StartCardGame");


            // Transition to Main Panel
            panelStart.SetActive(false);
            panelMain.SetActive(true);
        }

        private void InitPlayerCards(List<CardModel> cardModels)
        {
            // Transition to Main Panel
            panelStart.SetActive(true);
            panelServer.SetActive(false);

        }


        private void OnExitGameClicked()
        {
            // Exit the game
            Debug.Log("Exiting game...");
            Application.Quit();
        }
    }
}
