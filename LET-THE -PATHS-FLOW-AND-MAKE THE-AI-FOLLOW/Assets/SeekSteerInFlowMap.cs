using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeekSteerInFlowMap : MonoBehaviour
{
    public FlowMap fm;
    public float maxSpeed = 1;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(fm.evaluateAt(transform.position));
        rb.velocity = (maxSpeed  * fm.evaluateAt(transform.position));
    }
}
