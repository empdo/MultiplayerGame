using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject aimObject;

    public GameObject leye;
    public GameObject reye;

    public void PlayerRotation(float pitch, float yawn)
    {
        aimObject.transform.rotation = Quaternion.Euler(new Vector3(pitch, yawn, 0));

        transform.rotation = Quaternion.Euler(new Vector3(0, yawn, 0));

        leye.transform.rotation = Quaternion.Euler(new Vector3(-pitch - 60f, 1, 0));
        reye.transform.rotation = Quaternion.Euler(new Vector3(-pitch - 60f, 1, 0));

    }
}
