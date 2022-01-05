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
public static class StaticPredatorSTData {

    private static int floatIndex = 0, float3Index = 0, intIndex = 0, evolveIndex = 0,
                       simpleTacticIndex = 0, stateIndex = 0;
    private static float bl = SimulationController.bodyLength;

    private static EntityArchetype entityArchetype = World.DefaultGameObjectInjectionWorld.EntityManager.CreateArchetype(
            typeof(PredatorSTPropertiesComponent),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(Scale),
            typeof(Rotation)
        );

    // NOTE(miha): We are evolving first and second randomTacticBarrier
    private static float[] toEvolve = {
        0.33f,  // firstRandomTacticBarrier
        0.66f,   // secondRandomTacticBarrier
    };

    private static float[] floatData = {
        6f * bl,    // vM                                                                                   // Max speed
        3f * bl,    // vC                                                                                    // Crusing speed 
        0,          // mA                                                                                          // Max acceleration
        3f * bl,    // catchDistance
        0.25f,      // confusionProbability
    };

    private static float3[] float3Data = {
        new float3(0, 0, 0),    // direction                                                         // Direction
        new float3(0, 0, 0),    // position                                                                        // Speed
        new float3(0, 0, 0),    // speed                                                                        // Position
    };

    // NOTE(miha): SectionTactic takes the number from the enum TacticType? TODO
    private static int[] intData = {
        50,     // restTime                                                                                        // Rest time
        50,     // remainingRest                                                                        // Remaining rest
        1,      // firstSectionTactic                                                                                         // Fish to eat
    };

    private static SimpleTactic[] simpleTacticData = {
        SimpleTactic.Random,
    };

    private static State[] stateData = {
        State.Hunting,
    };

    private static float numOfFishCaught = 0.0f;

    public static SimpleTactic getSimpleTactic() { return simpleTacticData[simpleTacticIndex]; }
    public static State getState() { return stateData[stateIndex]; }

    public static float getNextFloat() { return floatData[floatIndex++]; }
    public static float3 getNextFloat3() { return float3Data[float3Index++]; }

    public static float3 getNoIncFloat3() { return float3Data[float3Index - 1]; }

    public static int getNextInt() { return intData[intIndex++]; }

    public static float[] getEvolve() { return toEvolve; }
    public static float getNextEvolve() {return toEvolve[evolveIndex++];} 
    public static void setEvolve(float[] newGene) { toEvolve = newGene; }

    public static float getBl() { return bl; }

    public static float3 getRandom() { return float3Data[float3Index++] = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0); }
    public static EntityArchetype getArchetype() { return entityArchetype; }

    public static void reset() { floatIndex = 0; float3Index = 0; intIndex = 0; evolveIndex = 0; simpleTacticIndex = 0; stateIndex = 0; }

    public static float getNumOfFishCaught() { return numOfFishCaught; }
    public static void setNumOfFishCaught(float fishCaught) { numOfFishCaught = fishCaught; }
}
