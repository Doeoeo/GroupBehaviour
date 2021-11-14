using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

/* Data for each fish agent
 *  TO DO:
 *      - Some variables aren't used. Use them :)
 *          (foV, direction, mA/can't remember why this is used/, eD, eW)
 */
public struct FishPropertiesComponent : IComponentData {
    public float vM;                                              //Max speed
    public float vC;                                              //Crusing speed 
    
    public float foV;                                             //Field of view 
    
    public float3 sD;                                             //Separation drive
    public float3 aD;                                             //Alignment drive 
    public float3 cD;                                             //Cohesion drive
    public float3 eD;                                             //Escape drive
    public float3 bD;                                             //Border drive
    
    public float sW;                                              //Separation weight
    public float aW;                                              //Alignment weight  
    public float cW;                                              //Cohesion weight 
    public float eW;                                              //Escape weight
    public float bW;                                              //Border weight

    public float mA;                                              //Max acceleration
    public float len;                                             //Body length

    public float3 direction;                                      //Direction
    public float3 position;                                       //Position
    public float3 speed;                                          //Speed
}
