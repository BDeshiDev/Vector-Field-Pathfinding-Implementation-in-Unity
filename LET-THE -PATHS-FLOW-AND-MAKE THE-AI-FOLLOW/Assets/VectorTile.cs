using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct VectorTile
{
    public float distToGoal;
    public Vector3 flowVec;

    public bool encounteredInBFS;
    public bool isObstacle;

    public void resetvalues()
    {
        encounteredInBFS = isObstacle = false;
    }
}
