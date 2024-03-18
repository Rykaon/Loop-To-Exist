using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldCheckCollision : MonoBehaviour
{
    public bool canCheck = false;
    public bool hasCollide = false;

    private void OnTriggerEnter(Collider other)
    {
        if (canCheck)
        {
            if (other.transform.tag == "Wall" || other.transform.tag == "Floor")
            {
                hasCollide = true;
            }
        }
    }
}
