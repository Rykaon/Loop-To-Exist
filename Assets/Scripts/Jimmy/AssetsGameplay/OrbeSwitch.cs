using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbeSwitch : DoorSwitch
{
    [HideInInspector] protected Transform orbe = null;
    [SerializeField] protected Transform orbeStartPos;
    [SerializeField] protected Transform orbeEndPos;
    [SerializeField] protected GameObject vfxHolder;

    protected override void Awake()
    {
        state = State.Inactive;
    }

    protected void OnTriggerStay(Collider other)
    {
        if (orbe == null)
        {
            if (other.TryGetComponent<ObjectManager>(out ObjectManager objectManager))
            {
                if (!objectManager.isHeld && !objectManager.isEquipped && objectManager.type == StateManager.Type.Orbe)
                {
                    orbe = objectManager.transform;
                    Destroy(objectManager.outline);
                    Destroy(objectManager.rigidBody);
                    Destroy(objectManager);
                    SetActive();
                }
            }
        }
    }

    protected void SetActive()
    {
        orbeStartPos.position = new Vector3(orbeStartPos.position.x, orbe.transform.position.y, orbeStartPos.position.z);
        Vector3[] path = new Vector3[3];
        path[0] = orbe.transform.position;
        path[1] = orbeStartPos.position;
        path[2] = orbeEndPos.position;

        orbe.DOPath(path, 1.25f).SetEase(Ease.OutElastic);
        vfxHolder.SetActive(true);
    }
}
