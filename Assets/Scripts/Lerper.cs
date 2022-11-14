using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lerper : MonoBehaviour
{
    public GameObject player;
    float timer = 0f;
    float _timer = 0f;
    public float time = 8f;
    public Vector3 startPos;

    Vector3 _targetPos;
    public Vector3 targetPos
    {
        get => _targetPos;
        set
        {
            _targetPos = value;
            Debug.Log(timer);
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
            _timer += Time.deltaTime;

            if (timer <= time)
            {
                player.transform.position = startPos + velocity * timer;
            }
        }
    }
}
