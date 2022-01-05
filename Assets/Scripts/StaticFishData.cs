using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;


/* A static class that contains Initializing data for fish.
 * It makes it easier to create another entity by just duplicating this class
 *  TO DO:
 *      - For evolution to occur Set methods are needed to change this data
 *      - It might be useful to be able to load this data from a file to resume evolution
 */      
public static class StaticFishData {

    private static int floatIndex = 0, float3Index = 0, fishIndex = -1;
    private static float bl = 0.1f;

    // TODO(miha): Set UnityEngine.Random seed. reset this seed (set it to the same it was) for every simulation. When going into next generation set new seed & repeat.

    private static EntityArchetype entityArchetype = World.DefaultGameObjectInjectionWorld.EntityManager.CreateArchetype(
            typeof(FishPropertiesComponent),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(Scale),
            typeof(Rotation)
        );

    private static float[] floatData = { 
        4f * bl,                                                                                    // Max speed
        2f * bl,                                                                                    // Crusing speed 
        330f,                                                                                       // Field of view 
        5,                                                                                          // Separation weight
        0.3f,                                                                                       // Alignment weight     
        1,                                                                                          // Cohesion weight
        5f,                                                                                         // Escape weight
        0,                                                                                          // Border weight
        0,                                                                                          // Max acceleration
    };

    private static float3[] float3Data = {
        new float3(0, 0, 0),                                                                        // Separation drive
        new float3(0, 0, 0),                                                                        // Alignment drive 
        new float3(0, 0, 0),                                                                        // Cohesion drive
        new float3(0, 0, 0),                                                                        // Escape drive
        new float3(0, 0, 0),                                                                        // Border drive
        new float3(0, 0, 0),                                                                        // Direction
        new float3(0, 0, 0),                                                                        // Speed
        new float3(0, 0, 0),                                                                        // Position
    };

    public static float getNextFloat() {return floatData[floatIndex++];}
    public static float3 getNextFloat3() {return float3Data[float3Index++];}

    public static float3 getNoIncFloat3() {return float3Data[float3Index - 1];}

    public static int getIndex() { return fishIndex; }

    public static float getBl() {return bl;}

    public static float3 getRandom() {return float3Data[float3Index++] = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);}
    public static EntityArchetype getArchetype() {return entityArchetype;}

    public static void reset() {floatIndex = 0; float3Index = 0; fishIndex++; }
}
