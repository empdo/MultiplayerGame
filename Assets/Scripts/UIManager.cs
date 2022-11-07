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
        private bool _UIState;
        public bool UIState
        {
        get => _UIState;
        set {
            _UIState = value;
            SetUIState();

        }
        }
        void SetUIState()
        {

            submitButton.gameObject.SetActive(_UIState);
            PortInput.gameObject.SetActive(_UIState);
            IpInput.gameObject.SetActive(_UIState);

        }

    }

}