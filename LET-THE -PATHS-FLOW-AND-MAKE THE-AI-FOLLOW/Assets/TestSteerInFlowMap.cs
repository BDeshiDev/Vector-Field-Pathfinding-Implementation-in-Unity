using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSteerInFlowMap : MonoBehaviour
{
    public SteeringBehaviour seekBehaviour;
    public float maxSpeed = 1;

    private Rigidbody rb;
    public Vector3 curVel;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.velocity = curVel = (seekBehaviour.getNewVelWithSteerApplied(rb.velocity,maxSpeed,maxSpeed *.5f));
        //bad rotation code but doesn't matter for now
        rb.rotation = Quaternion.Slerp(rb.rotation,Quaternion.LookRotation(rb.velocity, Vector3.up),Time.deltaTime);
    }
}
