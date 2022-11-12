using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lerper : MonoBehaviour
{
    public GameObject player;
    public Vector3 startPos, targetPos;
    public float timer = 0;
    public float time = 0.008f;

    // Update is called once per frame
    void Update()
    {
        if (player != null && startPos != null && targetPos != null)
        {
            player.transform.position = Vector3.Lerp(startPos, targetPos, timer / time);
            timer += Time.deltaTime;
        }
    }
}
