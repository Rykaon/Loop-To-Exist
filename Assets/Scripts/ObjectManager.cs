using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : StateManager
{
    ///////////////////////////////////////////////////
    ///            FONCTIONS H�RIT�ES               ///
    ///////////////////////////////////////////////////

    public override void Initialize(GameManager instance)
    {
        base.Initialize(instance);
    }

    public override void SetState(State state)
    {
        base.SetState(state);
    }

    public override void ResetState()
    {
        base.ResetState();
    }

    public override void SetHoldObject(Transform endPosition, float time)
    {
        base.SetHoldObject(endPosition, time);
    }

    public override void InitializeHoldObject(Transform parent)
    {
        base.InitializeHoldObject(parent);
    }

    public override void ThrowObject(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        base.ThrowObject(throwForceHorizontal, throwForceVertical, hitpoint);
    }

    public override void SetEquipObject(Transform endPosition, float time)
    {
        base.SetEquipObject(endPosition, time);
    }

    public override void InitializeEquipObject(Transform parent)
    {
        base.InitializeEquipObject(parent);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    protected override void OnJointBreak(float breakForce)
    {
        base.OnJointBreak(breakForce);
    }
}
