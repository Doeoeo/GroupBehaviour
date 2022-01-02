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
    public int status;                                            //-2 finding centre; -1 charging to centre; 0 - choosing least peripheral prey; 1 - hunting; 2 - resting
    public int restTime;                                          //Frames dedicated to eating fish
    public int remainingRest;                                     //Remaining frames for eating after catching a fish
    public int fishToEat;                                         //Fish soft deletion
    public int centerFish;                                        //Central fish for hunting
    public int mostIsolated;                                      //least peripheral fish in lockOnRadius
    public float numOfFishCaught;

    public float closestGroupRadius;                              //Possible to evolve
    public float lockOnDistance;                                  //Value to evolve
    public float lockOnRadius;                                    //Value to evolve
}
