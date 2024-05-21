using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickedWallManager : MonoBehaviour
{
    [SerializeField] private List<StickedWallElements> elements;
    [SerializeField] private List<StateManager> states;
    [SerializeField] private Transform start;
    [SerializeField] private Transform end;
    [SerializeField] public Material dissolveMaterial;
    private Vector3 direction;
    private StateManager state;

    private void Awake()
    {
        direction = end.position - start.position;
        state = transform.GetComponent<StateManager>();
    }
    
    public void DestroyWall()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].isWallDestroyed = true;
            states[i].SetState(StateManager.State.Sticky);
            states[i].rigidBody.isKinematic = false;
            states[i].rigidBody.useGravity = true;
            states[i].rigidBody.constraints = RigidbodyConstraints.None;
            states[i].rigidBody.AddForce(direction * 15, ForceMode.Impulse);
        }

        state.SetState(StateManager.State.Sticky);
        state.rigidBody.isKinematic = false;
        state.rigidBody.useGravity = true;
        state.rigidBody.constraints = RigidbodyConstraints.None;
    }
}
