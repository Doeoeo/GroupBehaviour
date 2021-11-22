using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct PredatorPropertiesComponent : IComponentData
{
    public float vM;                                              //Max speed
    public float vC;                                              //Crusing speed

    public float mA;                                              //Max acceleration
    public float len;                                             //Body length 

    public float3 direction;                                      //Direction
    public float3 position;                                       //Position
    public float3 speed;                                          //Speed
}
