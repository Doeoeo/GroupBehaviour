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

public struct PredatorSTPropertiesComponent : IComponentData
{
    public float vM;                                              //Max speed
    public float vC;                                              //Crusing speed
    public float mA;                                              //Max acceleration
    public float len;                                             //Body length 
    public float catchDistance;

    public float3 direction;                                      //Direction
    public float3 position;                                       //Position
    public float3 speed;                                          //Speed

    public int restTime;                                          //Frames dedicated to eating fish
    public int remainingRest;                                     //Remaining frames for eating after catching a fish
    
    // TODO(miha): Do we need these vars?
    // public int fishToEat;                                         //Fish soft deletion
    // public int mostIsolated;
    // public int centerFish;                                        //Central fish for hunting
    // public int targetFish;
    // public int peripheralFish;


    public SimpleTactic tactic;
    public State state;

    public float confusionProbability;

    public int numOfFishCaught;

    // NOTE(miha): TODO(miha): Add some comentarry.
    public float firstRandomTacticBarrier;
    public float secondRandomTacticBarrier;

    public int firstSectionTactic;
    public int secondSectionTactic;
    public int thirdSectionTactic;

    // TODO(miha): There is 25% for confusion... Don't need to keep statistic
    // how many attacks predator performed.. unless it is decided that
    // confusion is based on some other parameters (eg. angle of attack, speed
    // of attack, ect.).
    public int numOfAttacks;
}
