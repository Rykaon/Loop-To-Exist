using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;

public class StickedWallElements : MonoBehaviour
{
    public StickedWallManager manager;
    public StateManager stateManager;
    public bool isWallDestroyed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Ground" && other.gameObject.name == "Cube.008")
        {
            StartCoroutine(DestroyWallElement(stateManager));
        }
    }

    private IEnumerator DestroyWallElement(StateManager manager)
    {
        float dissolveTime = 1f;

        Material initMat = manager.renderer.material;
        Material dissolveMat = Instantiate(this.manager.dissolveMaterial);
        dissolveMat.SetTexture("_Albedo", initMat.mainTexture);
        dissolveMat.SetFloat("_AlphaTreshold", 0f);
        manager.renderer.material = null;
        manager.renderer.material = dissolveMat;

        manager.rigidBody.isKinematic = true;

        float elapsedTime = 0f;
        float treshold = 0f;
        while (elapsedTime < dissolveTime)
        {
            float time = elapsedTime / dissolveTime;
            treshold = Mathf.Lerp(treshold, 1f, time);
            dissolveMat.SetFloat("_AlphaTreshold", treshold);
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        Destroy(this.gameObject);
    }
}
