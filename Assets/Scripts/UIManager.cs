using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MultiplayerAssets
{

    public class UIManager : MonoBehaviour
    {

        public TMP_InputField IpInput;
        public TMP_InputField PortInput;
        public Button submitButton;
        public TextMeshProUGUI pingText;

        public GameObject enviroment;
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
        }
        void SetUIState()
        {

            submitButton.gameObject.SetActive(_UIState);
            PortInput.gameObject.SetActive(_UIState);
            IpInput.gameObject.SetActive(_UIState);
            enviroment.SetActive(!_UIState);

        }

    }

}