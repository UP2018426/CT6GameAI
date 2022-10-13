using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieBB : Blackboard {

    public Vector3 MoveToLocation;
    public Vector3 PlayerLocation;

    public int PlayerHealth = 2;

    public GameObject Cube;

    void Update ()
    {
        PlayerLocation = Cube.transform.position;
    }
}
