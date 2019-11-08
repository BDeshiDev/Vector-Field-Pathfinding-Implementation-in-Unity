using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SteerTowarsdDesiredVel : SteeringBehaviour
{
    public abstract Vector3 calcDesiredVel(Vector3 curVel, float maxVel);

    public override Vector3 calcSteeringVector(Vector3 curVel, float maxVel, float maxSteerAmount = 5)
    {
        Vector3 steer = (calcDesiredVel(curVel, maxVel) - curVel);

        if (steer.sqrMagnitude > (maxSteerAmount * maxSteerAmount))
            steer = steer.normalized * maxSteerAmount;

        return steer;
    }
}
