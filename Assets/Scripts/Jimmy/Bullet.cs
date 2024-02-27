using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    float elapsedTime = 0f;
    bool init = false;
    public void Init(Vector3 dir)
    {
        rb.velocity = dir * 100;
        init = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (init)
        {
            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime > 5f)
            {
                Destroy(gameObject);
            }
        }
    }
}
