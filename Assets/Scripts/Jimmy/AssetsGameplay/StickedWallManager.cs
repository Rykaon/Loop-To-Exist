using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickedWallManager : MonoBehaviour
{
    [SerializeField] private List<StickedWallElements> elements;
    [SerializeField] private List<StateManager> states;
    [SerializeField] private Transform start;
    [SerializeField] private Transform end;
    private Vector3 direction;

    private void Awake()
    {
        direction = end.position - start.position;
    }

    public void DestroyWall()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].isWallDestroyed = true;
            states[i].SetState(StateManager.State.Sticky);
            states[i].rigidBody.isKinematic = false;
            states[i].rigidBody.AddForce(direction * 10, ForceMode.Impulse);
        }
    }
}
