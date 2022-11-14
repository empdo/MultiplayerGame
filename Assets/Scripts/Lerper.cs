using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lerper : MonoBehaviour
{
    public GameObject player;
    float timer = 0;
    float time;
    public Vector3 startPos;

    Vector3 _targetPos;
    public Vector3 targetPos
    {
        get => _targetPos;
        set
        {
            _targetPos = value;
            time = timer;
            timer = 0;

            velocity = (_targetPos - startPos) / time;

            Debug.Log("Updated position, with time : " + time + " from: " + startPos + " to: " + _targetPos + " and vel: " + velocity);

        }

    }
    Vector3 velocity;
    // Update is called once per frame
    void Update()
    {
        if (player != null && velocity != null)
        {
            timer += Time.deltaTime;
            if (timer <= time)
            {
                player.transform.position = startPos + velocity * timer;
            }
        }
    }
}
