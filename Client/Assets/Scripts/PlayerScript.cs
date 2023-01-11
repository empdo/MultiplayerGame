using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject aimObject;

    public GameObject leye;
    public GameObject reye;

    public GameObject player;
    float timer = 0f;
    public float time;
    public Vector3 startPos;

    public List<Vector3> positions = new List<Vector3>();

    Vector3 _targetPos;
    public Vector3 targetPos
    {
        get => _targetPos;
        set
        {
            _targetPos = value;
            Debug.Log("timer: " + timer);
            timer = 0;

            velocity = (_targetPos - startPos) / time;

            Debug.Log("Updated position, with time : " + time + " from: " + startPos + " to: " + _targetPos + " and vel: " + velocity);

        }
    }

    Vector3 velocity;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (velocity != null)
        {
            timer += Time.fixedDeltaTime;

            if (timer <= time)
            {
                transform.position = startPos + velocity * timer;
            }
        }
    }

    public void PlayerRotation(float pitch, float yawn)
    {
        aimObject.transform.rotation = Quaternion.Euler(new Vector3(pitch, yawn, 0));

        transform.rotation = Quaternion.Euler(new Vector3(0, yawn, 0));

        leye.transform.rotation = Quaternion.Euler(new Vector3(-pitch - 60f, 1, 0));
        reye.transform.rotation = Quaternion.Euler(new Vector3(-pitch - 60f, 1, 0));

    }
}
