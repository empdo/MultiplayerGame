using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using StarterAssets;


namespace MultiplayerAssets
{

    public class UIManager : MonoBehaviour
    {

        public TMP_InputField IpInput;
        public TMP_InputField PortInput;
        public Button connectButton;
        public Button disconnectButton;
        public TextMeshProUGUI pingText;

        public GameObject canvas;
        public GameObject inGameMenu;
        public GameObject startMenu;

        public GameObject enviroment;
        public ClientConnection clientConnection;

        UnityEngine.Camera localCamera;

        public ChatScript chatScript;
        private bool _UIState;
        public bool UIState

        {
            get => _UIState;
            set
            {
                _UIState = value;
                SetUIState();

            }
        }

        void Start()
        {
            _UIState = true;
            clientConnection = GetComponent<ClientConnection>();
        }

        void Update() {
            if (StarterAssetsInputs.menuState != UIState){
                UIState = StarterAssetsInputs.menuState;
                Debug.Log(UIState);
            }
        }

        void SetUIState()
        {
            if (clientConnection.localPlayer != null) {
                inGameMenu.SetActive(true);
                startMenu.SetActive(false);

                if (localCamera == null) {
                    localCamera = clientConnection.localPlayer.GetComponentInChildren<UnityEngine.Camera>();
                }

                localCamera.enabled = UIState;
            }
            else {
                inGameMenu.SetActive(false);
                startMenu.SetActive(true);
            }
            canvas.SetActive(!UIState);

        }

    }

}