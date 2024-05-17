using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbeSwitch : DoorSwitch
{
    [HideInInspector] protected ObjectManager orbe = null;

    protected override void Awake()
    {
        state = State.Inactive;

        tagsToCheck = new List<string>();
        tagsToCheck.Add("Object");
    }

    protected void OnTriggerStay(Collider other)
    {
        if (orbe == null)
        {
            if (other.TryGetComponent<ObjectManager>(out ObjectManager objectManager))
            {
                if (!objectManager.isHeld && !objectManager.isEquipped && objectManager.type == StateManager.Type.Orbe)
                {
                    orbe = objectManager;
                }
            }
        }
    }

    protected void SetActive()
    {

    }
}
