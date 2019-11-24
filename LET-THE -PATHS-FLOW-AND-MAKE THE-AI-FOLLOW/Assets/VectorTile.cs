using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorTile:IHeapItem<VectorTile>
{
    public GridTile tilePos;
    public float distToGoal;   
    public bool isObstacle;
    public float moveCost = 1;

    public VectorTile(GridTile tilePos)
    {
        this.tilePos = tilePos;
    }

    public int CompareTo(VectorTile other)
    {
        return other.distToGoal.CompareTo(distToGoal);
    }

    public override string ToString()
    {
        return tilePos.ToString() + " D:" + distToGoal + " mc:" + moveCost;
    }

    public int HeapIndex { get; set; }
}
