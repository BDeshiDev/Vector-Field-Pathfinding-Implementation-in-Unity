using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public abstract class SteeringBehaviour : MonoBehaviour
{
    public abstract Vector3 calcSteeringVector(Vector3 curVel, float maxVel, float maxSteerAmount = 5);

    public virtual Vector3 getNewVelWithSteerApplied(Vector3 curVel,float maxVel, float maxSteerAmount = 5) { 
        Vector3 steer = calcSteeringVector(curVel, maxVel, maxSteerAmount);
        return getNewVelWithSteerApplied(steer, curVel, maxVel, maxSteerAmount);
        //return Vector3.SmoothDamp(curVel, calcDesiredVel(target, curVel, maxVel), ref curVel, .1f);
    }

    public virtual Vector3 getNewVelWithSteerApplied(Vector3 steer, Vector3 curVel, float maxVel, float maxSteerAmount = 5)
    {
        if (steer.sqrMagnitude > (maxSteerAmount * maxSteerAmount))
            steer = steer.normalized * maxSteerAmount;
        curVel += steer;

        if (curVel.magnitude > (maxVel * maxVel))
            curVel = curVel.normalized * maxVel;

        return curVel;

    }
    
    

}
