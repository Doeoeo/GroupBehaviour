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

    public int closestFish;                                       //Index of the fish that the predator chooses
    public int status;        
    public int restTime;                                          //0 - choosing; 1 - hunting; 2 - resting
    public int remainingRest;                                     //Remaining frames for idling after catching a fish
    public int fishToEat;                                         //Fish soft deletion
}
