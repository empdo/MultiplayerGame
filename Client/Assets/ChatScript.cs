using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

namespace MultiplayerAssets {

public class ChatScript : MonoBehaviour
{
    public TextMeshProUGUI textPrefab;

    List<TextMeshProUGUI> chatContent = new List<TextMeshProUGUI>();

    public float timer = 0;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (chatContent.Count > 0) {
            if (timer > 5) {

            Destroy(chatContent[0]);
            chatContent.Remove(chatContent[0]);

            foreach (TextMeshProUGUI text in chatContent) {
                text.transform.position -= new Vector3(0, 55, 0);
            }

            timer = 0;
            }
            timer += Time.deltaTime;
        }
    }

    public void AddMessage(string message) {
        TextMeshProUGUI messageObj = Instantiate(textPrefab, new Vector3(220, 25 + chatContent.Count * 55), Quaternion.identity);
        messageObj.text = message;
        chatContent.Add(messageObj);
        messageObj.transform.parent = transform;
    }
}

}