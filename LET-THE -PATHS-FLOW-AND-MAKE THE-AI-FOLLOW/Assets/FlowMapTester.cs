using System.Collections;
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
    }

    // Update is called once per frame
    private void Update()
    {
        flowMap.calcDistToTarget(goal.position);
    }
}
