using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerHandler : MonoBehaviour
{
    protected PlayerManager playerManager;
    public abstract void Initialize(PlayerManager manager);
    public abstract void UpdateComponent();
}
