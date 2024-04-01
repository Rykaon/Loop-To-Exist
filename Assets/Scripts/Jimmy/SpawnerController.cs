using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[RequireComponent(typeof(VisualEffect))]
public class SpawnerController : VFXOutputEventAbstractHandler
{
    public int id;

    public override bool canExecuteInEditor => throw new System.NotImplementedException();

    public void OnSpawn(VFXEventAttribute eventAttribute)
    {
        Debug.Log("yo spawn");
    }

    public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
    {
        Debug.Log("jfkjfb");


        throw new System.NotImplementedException();
    }
}
