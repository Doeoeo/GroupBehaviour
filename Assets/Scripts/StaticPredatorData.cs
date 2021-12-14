using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;


/* A static class that contains Initializing data for a predator.
 * It makes it easier to create another entity by just duplicating this class
 *  TO DO:
 *      - For evolution to occur Set methods are needed to change this data
 *      - It might be useful to be able to load this data from a file to resume evolution
 */
public static class StaticPredatorData {

    private static int floatIndex = 0, float3Index = 0, intIndex = 0, evolveIndex;
    private static float bl = SimulationController.bodyLength;

    private static EntityArchetype entityArchetype = World.DefaultGameObjectInjectionWorld.EntityManager.CreateArchetype(
            typeof(PredatorPropertiesComponent),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(Scale),
            typeof(Rotation)
        );

    private static float[] toEvolve = {
        1.5f
    };

    private static float[] floatData = {
        6f * bl,                                                                                    // Max speed
        3f * bl,                                                                                    // Crusing speed 
        0,                                                                                          // Max acceleration
    };

    private static float3[] float3Data = {
        new float3(0, 0, 0),                                                                        // Direction
        new float3(0, 0, 0),                                                                        // Speed
        new float3(0, 0, 0),                                                                        // Position
    };

    private static int[] intData = {
        -1,                                                                                         // Closest fish
        -2,                                                                                         // Status
        400,                                                                                        // Rest time
        400,                                                                                        // Remaining rest
        -1,                                                                                         // Fish to eat
        -1,                                                                                         // Center fish
        -1                                                                                          // Most isolated  
    };

    public static float getNextFloat() { return floatData[floatIndex++]; }
    public static float3 getNextFloat3() { return float3Data[float3Index++]; }

    public static float3 getNoIncFloat3() { return float3Data[float3Index - 1]; }

    public static int getNextInt() { return intData[intIndex++]; }

    public static float getNextEvolve() { return toEvolve[evolveIndex++]; }

    public static void setEvolve(float[] newGene) { toEvolve = newGene; }

    public static float getBl() { return bl; }

    public static float3 getRandom() { return float3Data[float3Index++] = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0); }
    public static EntityArchetype getArchetype() { return entityArchetype; }

    public static void reset() { floatIndex = 0; float3Index = 0; intIndex = 0; evolveIndex = 0; }
}
