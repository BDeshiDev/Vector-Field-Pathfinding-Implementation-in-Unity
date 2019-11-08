using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flowSeek : SteerTowarsdDesiredVel
{
    public FlowMap flowMap;
    public float seekForce = 2;

    public override Vector3 calcDesiredVel(Vector3 curVel, float maxVel)
    {
        return flowMap.evaluateAt(transform.position) * seekForce;
    }
}
