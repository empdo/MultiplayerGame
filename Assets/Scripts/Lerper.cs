using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lerper : MonoBehaviour
{
    public GameObject player;
    public Vector3 startPos, targetPos;
    float timer = 0;

    float _speed = 0.5f;

    // Update is called once per frame
    void Update()
    {
        if (player != null && startPos != null && targetPos != null)
        {
            player.transform.position = Vector3.Lerp(startPos, targetPos, timer / (float)0.008);
            timer += Time.deltaTime;
        }
    }
}
