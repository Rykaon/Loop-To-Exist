using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : StateManager
{
    ///////////////////////////////////////////////////
    ///            FONCTIONS HÉRITÉES               ///
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

    public override void SetHoldObject(PlayerManager player, Transform endPosition, float time)
    {
        base.SetHoldObject(player, endPosition, time);
    }

    public override void InitializeHoldObject(Transform parent)
    {
        base.InitializeHoldObject(parent);
    }

    public override void ThrowObject(float throwForceHorizontal, float throwForceVertical, Vector3 hitpoint)
    {
        base.ThrowObject(throwForceHorizontal, throwForceVertical, hitpoint);
    }

    public override void SetEquipObject(PlayerManager player, Transform endPosition, float time)
    {
        base.SetEquipObject(player, endPosition, time);
    }

    public override void InitializeEquipObject(Transform parent)
    {
        base.InitializeEquipObject(parent);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }
}
