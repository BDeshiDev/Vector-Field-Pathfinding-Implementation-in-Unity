﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowMapTester : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform goal;
    private FlowMap flowMap;
    void Start()
    {
        flowMap = GetComponent<FlowMap>();
        flowMap.calcDistToTarget(goal.position);
    }

    private void Update()
    {
        flowMap.calcDistToTarget(goal.position);
    }
}
