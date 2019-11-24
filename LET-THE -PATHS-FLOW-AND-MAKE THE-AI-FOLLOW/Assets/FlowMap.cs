using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
public class FlowMap : MonoBehaviour
{
    private VectorTile[,] seekHeatMap = new VectorTile[4, 4];
    private VectorTile[,] fleeHeatMap = new VectorTile[4, 4];
    public Vector3 origin;
    public float tileScale = .5f;

    public bool showGizmos = true;
    public bool showFleeMapInstead = false;
    public bool shouldUpdateFleeMap = true;
    public bool showArrow = true;
    public bool showDist = false;
    public LayerMask ObstacleMask;

    public int tileCountX = 60;
    public int tileCountZ = 60;
    public int gridMaxSize => curTileCountX * curTileCountZ;
    int curTileCountX;
    int curTileCountZ;

    public Vector3 testTarget;
    public GridTile testTileTarget;
    public bool useTestTile = false;

    [Tooltip("Should be slightly smaller than 1, otherwise the collider may be considered part of nearby tile")]
    public float obstacleCheckBoxScale = .85f;

    [Tooltip("To stop physics based agents from getting stuck by trying to move diagonally while hugging a wall in same direction")]
    public bool avoidDiagonalMovementOnCorners = true;

    public float fleeMoveCostFactor = -1.2f;

    private const int MaxDist = 999999;

    private void Awake()
    {
        resizeMap();
        calcObstacles();
    }


    public void calcDistToTarget()
    {
        if (!useTestTile)
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
            calcDistToTarget(tileX, tileZ);
        }
        else
        {
            Debug.Log("target outside");
            //do something or clamp the point
        }
    }

    public void calcDistToTarget(GridTile gt)
    {

        if (isValidTile(gt.x, gt.z) && doesTileOverlapObstacle(gt.x, gt.z))
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
        seekHeatMap = new VectorTile[tileCountZ, tileCountX];
        fleeHeatMap = new VectorTile[tileCountZ,tileCountX];
        for (int z = 0; z < tileCountZ; z++)
        {
            for (int x = 0; x < tileCountX; x++)
            {
                seekHeatMap[z,x] = new VectorTile(new GridTile(x,z));
                fleeHeatMap[z,x] = new VectorTile(new GridTile(x, z));
            }
        }
    }

    public void calcDistToTarget(int tileX, int tileZ)
    {
        origin = transform.position;

        for (int z = 0; z < seekHeatMap.GetLength(0); z++)
        {
            for (int x = 0; x < seekHeatMap.GetLength(1); x++)
            {
                fleeHeatMap[z,x].distToGoal = seekHeatMap[z, x].distToGoal = MaxDist;
            }
        }

        fillDistFromTargetWithDjikstra(tileZ, tileX);
        if(shouldUpdateFleeMap)
            updateFleeMap(tileZ,tileX);
    }

    private Vector3 calcDirTowardsAdjMinDistTile(int x, int z,VectorTile[,] heatMapToUse)
    {
        Vector3 bestDir = Vector3.zero;

        bool hasvalidNeighbour = false;
        float minDist = MaxDist;
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (!(i == 0 && j == 0) && isValidTileAndNotObstacle(x + i, z + j) && (minDist > heatMapToUse[z + j, x + i].distToGoal || !hasvalidNeighbour))//ignore tile itself
                {

                    if ((i == 0 || j == 0) || !avoidDiagonalMovementOnCorners ||
                        (isValidTileAndNotObstacle(x + i, z) && isValidTileAndNotObstacle(x, z + j)))
                    {
                        hasvalidNeighbour = true;
                        minDist = heatMapToUse[z + j, x + i].distToGoal;
                        bestDir = new Vector3(i, 0, j);
                    }
                }
            }
        }

        return bestDir;
    }

    public Vector3 calcTileCenterWorldPos(int x, int z)
    {
        return origin + new Vector3(x, 0, z) * tileScale + new Vector3(1, 0, 1) * tileScale / 2;

    }

    private void fillDistFromTargetWithDjikstra(int tileZ, int tileX)
    {
        seekHeatMap[tileZ, tileX].distToGoal = 0;
        Heap<VectorTile> closestTileQueue = new Heap<VectorTile>(gridMaxSize);
        HashSet<VectorTile> encountered = new HashSet<VectorTile>();
        closestTileQueue.Add(seekHeatMap[tileZ,tileX]);
        while (closestTileQueue.Count > 0)
        {
            VectorTile curTile = closestTileQueue.RemoveFirst();
            encountered.Add(curTile);
//            Debug.Log("deq " + curTile);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (!(i == 0 && j == 0) && isValidTileAndNotObstacle(curTile.tilePos.x + i, curTile.tilePos.z + j))//ignore tile itself
                    {
                        if (encountered.Contains(seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]))
                        {
                            continue;
                        }

                        float newDist = curTile.distToGoal + seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i].moveCost * ((i== 0 || j == 0)?1:1.4f);

                        if (newDist < seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i].distToGoal ||
                               !closestTileQueue.Contains(seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i])  )
                        {
//                            Debug.Log("update " + seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]);
                            seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i].distToGoal = newDist;
                            fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i].distToGoal=  fleeMoveCostFactor * newDist;

                            if (!closestTileQueue.Contains(seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]))
                            {
                                closestTileQueue.Add(seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]);
                            }
                            else
                            {
                                closestTileQueue.UpdateItem(seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]);
                            }
                        }
                    }
                }
            }
        }
    }

    private void updateFleeMap(int tileZ,int tileX)
    {
        fleeHeatMap[tileZ, tileX].distToGoal = 0;
        Heap<VectorTile> closestTileQueue = new Heap<VectorTile>(gridMaxSize);
        HashSet<VectorTile> encountered = new HashSet<VectorTile>();
        closestTileQueue.Add(fleeHeatMap[tileZ, tileX]);
        while (closestTileQueue.Count > 0)
        {
            VectorTile curTile = closestTileQueue.RemoveFirst();
            encountered.Add(curTile);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (!(i == 0 && j == 0) && isValidTileAndNotObstacle(curTile.tilePos.x + i, curTile.tilePos.z + j))//ignore tile itself
                    {
                        if (encountered.Contains(fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]))
                        {
                            continue;
                        }

                        float newDist = curTile.distToGoal + -fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i].moveCost * ((i == 0 || j == 0) ? 1 : 1.4f);

                        if (newDist < fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i].distToGoal ||
                            !closestTileQueue.Contains(fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]))
                        {
                            //                            Debug.Log("update " + seekHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]);
                            fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i].distToGoal = newDist;

                            if (!closestTileQueue.Contains(fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]))
                            {
                                closestTileQueue.Add(fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]);
                            }
                            else
                            {
                                closestTileQueue.UpdateItem(fleeHeatMap[curTile.tilePos.z + j, curTile.tilePos.x + i]);
                            }
                        }
                    }
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
                fleeHeatMap[z,x].isObstacle= seekHeatMap[z, x].isObstacle = doesTileOverlapObstacle(x, z);
            }
        }
    }


    public bool getTileAtPoint(Vector3 point, out int x, out int z)
    {
        x = Mathf.CeilToInt((point.x - origin.x) / tileScale - 1);
        z = Mathf.CeilToInt((point.z - origin.z) / tileScale - 1);

        return isValidTile(x, z);
    }

    public bool isValidTile(int x, int z)
    {
        return x >= 0 && x < curTileCountX && z >= 0 && z < curTileCountZ;
    }

    public bool isValidTileAndNotObstacle(int x, int z)
    {
        return isValidTile(x, z) && !seekHeatMap[z, x].isObstacle;
    }

    public Vector3 evaluateSeekAt(Vector3 position)
    {
        if (getTileAtPoint(position, out var x, out var z))
        {
            return calcDirTowardsAdjMinDistTile(x, z,seekHeatMap);
        }
        return Vector3.zero;
    }

    public Vector3 evaluateFleeAt(Vector3 position)
    {
        if (getTileAtPoint(position, out var x, out var z))
        {
            return calcDirTowardsAdjMinDistTile(x, z, fleeHeatMap);
        }
        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        var gridArr = showFleeMapInstead ? fleeHeatMap : seekHeatMap;
        if (showGizmos && gridArr != null)
        {
            for (int z = 0; z < gridArr.GetLength(0); z++)
            {
                Vector3 tileOrigin = origin + z * tileScale * Vector3.forward;
                for (int x = 0; x < gridArr.GetLength(1); x++)
                {
                    var topLeft = tileOrigin + Vector3.forward * tileScale;
                    var bottomRight = tileOrigin + Vector3.right * tileScale;
                    var topRight = tileOrigin + new Vector3(1, 0, 1) * tileScale;

                    if (x == testTileTarget.x && z == testTileTarget.z)
                    {
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine((tileOrigin + bottomRight) * .5f, (topLeft + topRight) * .5f);
                        Gizmos.DrawLine((tileOrigin + topLeft) * .5f, (bottomRight + topRight) * .5f);
                    }

                    if (gridArr[z, x].isObstacle)
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

                    drawVecTile(tileOrigin, gridArr[z,x],calcDirTowardsAdjMinDistTile(x,z, gridArr));
                    tileOrigin += tileScale * Vector3.right; //aka bottomLeft
                }
            }
        }
    }


    public void drawVec(Vector3 origin, Vector3 vec)
    {
        Gizmos.DrawRay(origin + new Vector3(1, 0, 1) * tileScale / 2, tileScale * .5f * vec);
        Gizmos.DrawWireSphere(origin + new Vector3(1, 0, 1) * tileScale / 2 + tileScale * .5f * vec, .05f * tileScale);
    }

    public void drawVecTile(Vector3 drawOrigin, VectorTile vt,Vector3 flowVec)
    {
        if (showArrow)
            drawVec(drawOrigin, flowVec);
#if UNITY_EDITOR
        if (showDist)
        {
            Handles.color = Color.magenta;
            Handles.Label(drawOrigin + new Vector3(1, 0, 1) * tileScale / 2, vt.distToGoal.ToString());
        }
#endif
    }

    public bool doesTileOverlapObstacle(int x, int z)
    {
        return Physics.OverlapBox(calcTileCenterWorldPos(x, z), Vector3.one * obstacleCheckBoxScale * tileScale / 2,
                   Quaternion.identity, ObstacleMask).Length > 0;

    }

}

public struct GridTile
{
    public int x;
    public int z;

    public GridTile(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public override string ToString()
    {
        return "{" + x + "," + z+"}";
    }
}




