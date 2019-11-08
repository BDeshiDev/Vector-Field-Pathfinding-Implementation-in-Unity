using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FlowMap : MonoBehaviour
{
    private VectorTile[,] heatMap = new VectorTile[4, 4];
    public Vector3 origin;
    public float tileScale = .5f;

    public bool showMap = true;
    public bool showArrow = true;
    public bool showDist = false;
    public LayerMask ObstacleMask;

    public int tileCountX = 60;
    public int tileCountZ = 60;
    int curTileCountX;
    int curTileCountZ;

    public Vector3 testTarget;
    public GridTile testTileTarget;
    public bool useTestTile = false;
    
    [Tooltip("Should be slightly smaller than 1, otherwise the collider may be considered part of nearby tile")]
    public float obstacleCheckBoxScale = .85f;
    [Tooltip("To stop physics based agents from getting stuck by trying to move diagonally while hugging a wall in same direction")]
    public bool avoidDiagonalMovementOnCorners = true;

    private void Awake()
    {
        resizeMap();
        calcObstacles();
    }


    public void calcDistToTarget()
    {
        if(!useTestTile)
            calcDistToTarget(testTarget);
        else
        {
            calcDistToTarget(testTileTarget);
        }
    }


    public void calcDistToTarget(Vector3 target)
    {
        if (getTileAtPoint(target, out var tileX, out var tileZ))
        {
            calcDistToTarget(tileX,tileZ);
        }
        else
        {
            Debug.Log("target outside");
            //do something or clamp the point
        }
    }

    public void calcDistToTarget(GridTile gt)
    {

        if (isValidTile(gt.x,gt.z) && doesTileOverlapObstacle(gt.x,gt.z))
        {
            calcDistToTarget(gt.x, gt.z);
        }
        else
        {
            Debug.Log("target outside");
            //do something or clamp the point
        }
    }

    private void resizeMap()
    {
        curTileCountX = tileCountX;
        curTileCountZ = tileCountZ;
        heatMap = new VectorTile[tileCountZ, tileCountX];
    }

    public void calcDistToTarget(int tileX, int tileZ)
    {
        origin = transform.position;

        for (int z = 0; z < curTileCountZ; z++)
        {
            for (int x = 0; x < curTileCountX; x++)
            {
                heatMap[z, x].encounteredInBFS = false;
                heatMap[z, x].flowVec = Vector3.zero;
            }
        }

        fillDistFromTargetWithBFS(tileZ, tileX);

        //createVecField(tileX,tileZ);
        createVecFieldNoLocalOptima(tileX,tileZ);
    }

    //has local optima problem
    private void createVecField(int goalX,int goalZ)
    {
        for (int z = 0; z < heatMap.GetLength(0); z++)
        {
            for (int x = 0; x < heatMap.GetLength(1); x++)
            {
                if (x == goalX && z == goalZ)
                {
                    heatMap[z, x].flowVec = Vector3.zero;
                }
                else
                {

                    heatMap[z, x].flowVec.x = ((isValidTile(x - 1, z) && !heatMap[z, x - 1].isObstacle)
                                                  ? heatMap[z, x - 1].distToGoal
                                                  : heatMap[z, x].distToGoal) - //left
                                              ((isValidTile(x + 1, z) && !heatMap[z, x + 1].isObstacle)
                                                  ? heatMap[z, x + 1].distToGoal
                                                  : heatMap[z, x].distToGoal); //right

                    heatMap[z, x].flowVec.y = 0;

                    heatMap[z, x].flowVec.z = ((isValidTile(x, z - 1) && !heatMap[z - 1, x].isObstacle)
                                                  ? heatMap[z - 1, x].distToGoal
                                                  : heatMap[z, x].distToGoal) - //bottom
                                              ((isValidTile(x, z + 1) && !heatMap[z + 1, x].isObstacle)
                                                  ? heatMap[z + 1, x].distToGoal
                                                  : heatMap[z, x].distToGoal); //top
                }
            }
        }
    }

    /*
     *We want each flowvec to flow from higher cost dist to lower dist ones
     *Hence we try to find neighbour with min dist to goal
     *And  this tile's world pos from it's world position 
     *The subtraction gives us a vector towards the smallest goal dist
     * This also avoids local optima since you are always choosing something insteaf of having a zero vector in the field
     *using world position does not matter, only direction does. So we use world pos
     * ALSO I'm an idiot for not being sure if iterators work for structs
     * If it did, we can access neighbours more easily with iterators and avoid copy paste
     */
    private void createVecFieldNoLocalOptima(int goalX, int goalZ)
    {
        for (int z = 0; z < heatMap.GetLength(0); z++)
        {
            for (int x = 0; x < heatMap.GetLength(1); x++)
            {
                heatMap[z, x].flowVec = Vector3.zero;
                if (x != goalX || z != goalZ)
                {
                    bool hasvalidNeighbour = false;
                    float minDist = 99999;

                    if (isValidTileAndNotObstacle(x + 1, z))//right
                    {
                        if (!hasvalidNeighbour || minDist > heatMap[z, x + 1].distToGoal)
                        {
                            hasvalidNeighbour = true;
                            minDist = heatMap[z, x + 1].distToGoal;
                            heatMap[z, x].flowVec = calcTileCenterWorldPos(x+1,z) - calcTileCenterWorldPos(x, z);
                        }
                    }

                    if (isValidTileAndNotObstacle(x, z + 1))//up
                    {
                        if (!hasvalidNeighbour || minDist > heatMap[z+1, x].distToGoal)
                        {
                            hasvalidNeighbour = true;
                            minDist = heatMap[z+1, x].distToGoal;
                            heatMap[z, x].flowVec = calcTileCenterWorldPos(x, z+1) - calcTileCenterWorldPos(x, z);
                        }
                    }

                    if (isValidTileAndNotObstacle(x - 1, z))//left
                    {
                        if (!hasvalidNeighbour || minDist > heatMap[z, x - 1].distToGoal)
                        {
                            hasvalidNeighbour = true;
                            minDist = heatMap[z, x - 1].distToGoal;
                            heatMap[z, x].flowVec = calcTileCenterWorldPos(x - 1, z) - calcTileCenterWorldPos(x, z);
                        }
                    }

                    if (isValidTileAndNotObstacle(x, z - 1))//down
                    {
                        if (!hasvalidNeighbour || minDist > heatMap[z-1, x ].distToGoal)
                        {
                            hasvalidNeighbour = true;
                            minDist = heatMap[z-1, x ].distToGoal;
                            heatMap[z, x].flowVec = calcTileCenterWorldPos(x, z-1) - calcTileCenterWorldPos(x, z);
                        }
                    }

                    if (isValidTileAndNotObstacle(x + 1, z + 1))//topRight
                    {
                        if (!hasvalidNeighbour || minDist > heatMap[z+1, x + 1].distToGoal &&
                            (!avoidDiagonalMovementOnCorners || (isValidTileAndNotObstacle(x,z+1) && isValidTileAndNotObstacle(x+1, z))))//top AND right tile has to be free
                        {
                            hasvalidNeighbour = true;
                            minDist = heatMap[z+1, x + 1].distToGoal;
                            heatMap[z, x].flowVec = calcTileCenterWorldPos(x + 1, z+1) - calcTileCenterWorldPos(x, z);
                        }
                    }

                    if (isValidTileAndNotObstacle(x + 1, z - 1))//bottomRight
                    {
                        if (!hasvalidNeighbour || minDist > heatMap[z-1, x + 1].distToGoal &&
                            (!avoidDiagonalMovementOnCorners || (isValidTileAndNotObstacle(x, z - 1) && isValidTileAndNotObstacle(x + 1, z))))//bottom AND right tile has to be free
                        {
                            hasvalidNeighbour = true;
                            minDist = heatMap[z-1, x + 1].distToGoal;
                            heatMap[z, x].flowVec = calcTileCenterWorldPos(x + 1, z-1) - calcTileCenterWorldPos(x, z);
                        }
                    }

                    if (isValidTileAndNotObstacle(x - 1, z + 1))//topLeft
                    {
                        if (!hasvalidNeighbour || minDist > heatMap[z+1, x - 1].distToGoal &&
                            (!avoidDiagonalMovementOnCorners || (isValidTileAndNotObstacle(x, z + 1) && isValidTileAndNotObstacle(x - 1, z))))//top AND left tile has to be free
                        {
                            hasvalidNeighbour = true;
                            minDist = heatMap[z+1, x - 1].distToGoal;
                            heatMap[z, x].flowVec = calcTileCenterWorldPos(x - 1, z+1) - calcTileCenterWorldPos(x, z);
                        }
                    }

                    if (isValidTileAndNotObstacle(x - 1, z - 1))//bottomLeft
                    {
                        if (!hasvalidNeighbour || minDist > heatMap[z-1, x - 1].distToGoal &&
                            (!avoidDiagonalMovementOnCorners || (isValidTileAndNotObstacle(x, z - 1) && isValidTileAndNotObstacle(x - 1, z))))//bottom AND left  tile has to be free
                        {
                            hasvalidNeighbour = true;
                            minDist = heatMap[z-1, x - 1].distToGoal;
                            heatMap[z, x].flowVec =  calcTileCenterWorldPos(x - 1, z-1) - calcTileCenterWorldPos(x, z);
                        }
                    }
                }
                heatMap[z,x].flowVec.Normalize();
            }
        }
    }

    public Vector3 calcTileCenterWorldPos(int x , int z)
    {
        return origin + new Vector3(x , 0, z ) * tileScale + new Vector3(1,0,1) * tileScale /2;
        //+1 cuz we want an offset * tileScale even on tile[0,0]
    }

    private void fillDistFromTargetWithBFS(int tileZ, int tileX)
    {
        heatMap[tileZ, tileX].distToGoal = 0;
        Queue<GridTile> BfsQ = new Queue<GridTile>();
        BfsQ.Enqueue(new GridTile(tileX, tileZ));


        while (BfsQ.Count > 0)
        {
            GridTile gt = BfsQ.Dequeue();
            heatMap[gt.z, gt.x].encounteredInBFS = true;

            if (!heatMap[gt.z, gt.x].isObstacle)
            {
                if (isValidTile(gt.x + 1, gt.z) && !heatMap[gt.z, gt.x + 1].encounteredInBFS)
                {
                    BfsQ.Enqueue(new GridTile(gt.x + 1, gt.z));
                    heatMap[gt.z, gt.x + 1].encounteredInBFS = true;
                    heatMap[gt.z, gt.x + 1].distToGoal = heatMap[gt.z, gt.x].distToGoal + 1;
                }

                if (isValidTile(gt.x, gt.z + 1) && !heatMap[gt.z + 1, gt.x].encounteredInBFS)
                {
                    BfsQ.Enqueue(new GridTile(gt.x, gt.z + 1));
                    heatMap[gt.z + 1, gt.x].encounteredInBFS = true;
                    heatMap[gt.z + 1, gt.x].distToGoal = heatMap[gt.z, gt.x].distToGoal + 1;
                }

                if (isValidTile(gt.x - 1, gt.z) && !heatMap[gt.z, gt.x - 1].encounteredInBFS)
                {
                    BfsQ.Enqueue(new GridTile(gt.x - 1, gt.z));
                    heatMap[gt.z, gt.x - 1].encounteredInBFS = true;
                    heatMap[gt.z, gt.x - 1].distToGoal = heatMap[gt.z, gt.x].distToGoal + 1;
                }

                if (isValidTile(gt.x, gt.z - 1) && !heatMap[gt.z - 1, gt.x].encounteredInBFS)
                {
                    BfsQ.Enqueue(new GridTile(gt.x, gt.z - 1));
                    heatMap[gt.z - 1, gt.x].encounteredInBFS = true;
                    heatMap[gt.z - 1, gt.x].distToGoal = heatMap[gt.z, gt.x].distToGoal + 1;
                }


                if (isValidTile(gt.x + 1, gt.z + 1) && !heatMap[gt.z + 1, gt.x + 1].encounteredInBFS)
                {
                    BfsQ.Enqueue(new GridTile(gt.x + 1, gt.z + 1));
                    heatMap[gt.z + 1, gt.x + 1].encounteredInBFS = true;
                    heatMap[gt.z + 1, gt.x + 1].distToGoal = heatMap[gt.z, gt.x].distToGoal + 1;
                }

                if (isValidTile(gt.x + 1, gt.z - 1) && !heatMap[gt.z - 1, gt.x + 1].encounteredInBFS)
                {
                    BfsQ.Enqueue(new GridTile(gt.x + 1, gt.z - 1));
                    heatMap[gt.z - 1, gt.x + 1].encounteredInBFS = true;
                    heatMap[gt.z - 1, gt.x + 1].distToGoal = heatMap[gt.z, gt.x].distToGoal + 1;
                }

                if (isValidTile(gt.x - 1, gt.z + 1) && !heatMap[gt.z + 1, gt.x - 1].encounteredInBFS)
                {
                    BfsQ.Enqueue(new GridTile(gt.x - 1, gt.z + 1));
                    heatMap[gt.z + 1, gt.x - 1].encounteredInBFS = true;
                    heatMap[gt.z + 1, gt.x - 1].distToGoal = heatMap[gt.z, gt.x].distToGoal + 1;
                }

                if (isValidTile(gt.x - 1, gt.z - 1) && !heatMap[gt.z - 1, gt.x - 1].encounteredInBFS)
                {
                    BfsQ.Enqueue(new GridTile(gt.x - 1, gt.z - 1));
                    heatMap[gt.z - 1, gt.x - 1].encounteredInBFS = true;
                    heatMap[gt.z - 1, gt.x - 1].distToGoal = heatMap[gt.z, gt.x].distToGoal + 1;
                }
            }
        }
    }

    private void calcObstacles()
    {
        for (int z = 0; z < curTileCountZ; z++)
        {
            for (int x = 0; x < curTileCountX; x++)
            {
                heatMap[z, x].isObstacle = doesTileOverlapObstacle(x, z);
                heatMap[z, x].encounteredInBFS = false;
                heatMap[z, x].flowVec = Vector3.zero;
            }
        }
    }


    public bool getTileAtPoint(Vector3 point,out int x, out int z)
    {
        x = Mathf.CeilToInt((point.x - origin.x) / tileScale-1);
        z = Mathf.CeilToInt((point.z - origin.z) / tileScale-1);
        
        return isValidTile(x,z);
    }

    public bool isValidTile(int x, int z)
    {
        return x >= 0 && x < curTileCountX && z >= 0 && z < curTileCountZ;
    }

    public bool isValidTileAndNotObstacle(int x, int z)
    {
        return isValidTile(x,z) && !heatMap[z,x].isObstacle;
    }

    public Vector3 evaluateAt(Vector3 position)
    {
        if (getTileAtPoint(position, out var x, out var z))
        {
            return heatMap[z, x].flowVec;
        }

        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (showMap)
        {
            for (int z = 0; z < heatMap.GetLength(0); z++)
            {
                Vector3 tileOrigin = origin + z * tileScale * Vector3.forward;
                for (int x = 0; x < heatMap.GetLength(1); x++)
                {
                    var topLeft = tileOrigin + Vector3.forward * tileScale;
                    var bottomRight = tileOrigin + Vector3.right * tileScale;
                    var topRight = tileOrigin + new Vector3(1, 0, 1) * tileScale;

                    if (x == testTileTarget.x && z == testTileTarget.z)
                    {
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine((tileOrigin + bottomRight)*.5f, (topLeft+topRight)*.5f);
                        Gizmos.DrawLine((tileOrigin + topLeft) * .5f, (bottomRight + topRight) * .5f);
                    }

                    if (heatMap[z, x].isObstacle)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(tileOrigin, topRight);
                        Gizmos.DrawLine(topLeft, bottomRight);
                    }

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(tileOrigin, topLeft); //left line
                    Gizmos.DrawLine(tileOrigin, bottomRight); //bottom line
                    Gizmos.DrawLine(topLeft, topRight); //Top line
                    Gizmos.DrawLine(bottomRight, topRight); //Right line

                    Gizmos.color = Color.yellow;
                    drawVecTile(tileOrigin, heatMap[z,x]);

                    tileOrigin += tileScale * Vector3.right;//aka bottomLeft
                }
            }
        }
    }


    public void drawVec(Vector3 origin, Vector3 vec)
    {
        Gizmos.DrawRay(origin + new Vector3(1, 0, 1) * tileScale / 2, tileScale * .5f * vec);
        Gizmos.DrawWireSphere(origin + new Vector3(1, 0, 1) * tileScale / 2 + tileScale * .5f * vec, .05f *tileScale);
    }
    public void drawVecTile(Vector3 drawOrigin,VectorTile vt)
    {
        if(showArrow)
            drawVec(drawOrigin,vt.flowVec);
#if UNITY_EDITOR
        if (showDist)
        {
            Handles.color =Color.magenta;
            Handles.Label(drawOrigin + new Vector3(1, 0, 1) * tileScale / 2, vt.distToGoal.ToString());
        }
#endif
    }

    public bool doesTileOverlapObstacle(int x ,int z)
    {
        return Physics.OverlapBox(calcTileCenterWorldPos(x,z), Vector3.one * obstacleCheckBoxScale * tileScale / 2,
                   Quaternion.identity, ObstacleMask).Length > 0;
    }

}
[System.Serializable]
public struct GridTile
{
    public int x;
    public int z;

    public GridTile(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}




