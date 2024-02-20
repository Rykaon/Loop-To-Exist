using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualCameraManager : MonoBehaviour
{
    public void SetVirtualCameraPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void SetVirtualCameraRotation(Vector3 rotation)
    {
        transform.rotation = Quaternion.Euler(rotation);
    }
}
