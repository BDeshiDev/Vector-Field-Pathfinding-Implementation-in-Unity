using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flowFlee : SteerTowarsdDesiredVel
{
    public FlowMap flowMap;
    public float seekForce = 2;

    public override Vector3 calcDesiredVel(Vector3 curVel, float maxVel)
    {
        return flowMap.evaluateFleeAt(transform.position) * seekForce;
    }
}
