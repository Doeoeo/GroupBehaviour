using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using System;

/* A Monobehaviour that spawns iterations of entities for each evolution cycle
 * The class sets a custom FixedUpdate rate to speed up the behaviour
 * The Monobehaviour can be disabled by the isActive toggle in the edditor
 *  TO DO:
 *      - The generateEntity has to cast the function to a specific IComponent data this can be made more flexible with an array of types or something
 *      - Spawn predators (they will be killed automaticly)
 *      - Create an evaluating function for evolution
 */      

public class SimulationController : MonoBehaviour {

    [SerializeField] private Mesh fishMesh;
    [SerializeField] private Material fishMaterial;

    [SerializeField] private Mesh predatorMesh;
    [SerializeField] private Material predatorMaterial;

    [SerializeField] private int agentNumberParam;
    [SerializeField] public bool isActive;

    public static int simulationFrames = 500;
    public static float timestep = 0.005f;
    private static int simulationNo;
    private int currentSimulationFrame;

    public static float bodyLength = 0.1f;

    private EntityManager entityManager;
    FixedStepSimulationSystemGroup a;

    private GeneticAlgorithm evolution;

    void Start() {
        Time.fixedDeltaTime = timestep;
        currentSimulationFrame = 0;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        generateEntity(StaticFishData.getArchetype(), agentNumberParam, constructFishComponent, fishMaterial, fishMesh);
        generateEntity(StaticPredatorData.getArchetype(), 1, constructPredatorComponent, predatorMaterial, predatorMesh);

        // TODO GEN ALG
        // evolution = new GeneticAlgorithm( ... );
    }

    void FixedUpdate() {
        if (!isActive) return;
        // This disaster needs to be called every frame so that we reset the Timestep. If not we are stuck at 60fps.
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<FixedStepSimulationSystemGroup>().Timestep = Time.fixedDeltaTime;
        if (currentSimulationFrame == simulationFrames) wrapUp();
        currentSimulationFrame++;
    }

    // Finalize a generation
    private void wrapUp() {
        // if (evolution.endOfPop()) evolution.evolve();
        // StaticPredatorData.setEvolve(evolution.nextGene());
        cleanUp();
        Debug.Log("Cleared Entities");
        // Create fish
        generateEntity(StaticFishData.getArchetype(), agentNumberParam, constructFishComponent, fishMaterial, fishMesh);
        Debug.Log("Made Fish");

        // Create predator
        generateEntity(StaticPredatorData.getArchetype(), 1, constructPredatorComponent, predatorMaterial, predatorMesh);
        Debug.Log("Made Predator");

        currentSimulationFrame = 0;
    }

    // Kill every entity
    private void cleanUp() {
        NativeArray<Entity> entities = entityManager.GetAllEntities();
        entityManager.DestroyEntity(entities);
    }

    // Create new entities
    private void generateEntity(EntityArchetype entityArchetype, int agentNumber, Func<IComponentData> f, Material material, Mesh mesh) {
        NativeArray<Entity> entityArray = new NativeArray<Entity>(agentNumber, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entityArray);

        float bl = 0.1f;
        for (int i = 0; i < entityArray.Length; i++) {

            Entity entity = entityArray[i];
            var t = f();

            if (t is FishPropertiesComponent) entityManager.SetComponentData(entity, (FishPropertiesComponent)t);
            else if (t is PredatorPropertiesComponent) entityManager.SetComponentData(entity, (PredatorPropertiesComponent)t);

            entityManager.SetComponentData(entity, new Translation {Value = new float3(StaticFishData.getNoIncFloat3())});

            entityManager.SetSharedComponentData(entity, new RenderMesh {
                mesh = mesh,
                material = material
            });

            entityManager.SetComponentData(entity, new Scale {Value = bl / 2});
        }

        entityArray.Dispose();

    }

    // Predefined component for Fish
    private IComponentData constructFishComponent() {
        StaticFishData.reset();
        return new FishPropertiesComponent {
            id = StaticFishData.getIndex(),
            vM = StaticFishData.getNextFloat(),
            vC = StaticFishData.getNextFloat(),
            foV = StaticFishData.getNextFloat(),
            sD = StaticFishData.getNextFloat3(),
            aD = StaticFishData.getNextFloat3(),
            cD = StaticFishData.getNextFloat3(),
            eD = StaticFishData.getNextFloat3(),
            bD = StaticFishData.getNextFloat3(),
            sW = StaticFishData.getNextFloat(),
            aW = StaticFishData.getNextFloat(),
            cW = StaticFishData.getNextFloat(),
            eW = StaticFishData.getNextFloat(),
            bW = StaticFishData.getNextFloat(),
            mA = StaticFishData.getNextFloat(),
            len = StaticFishData.getBl(),
            direction = StaticFishData.getNextFloat3(),
            speed = StaticFishData.getRandom(),
            position = StaticFishData.getRandom(),
        };
    }

    // Predifined component for Predator
    private IComponentData constructPredatorComponent() {
        StaticPredatorData.reset();
        return new PredatorPropertiesComponent {
            vM = StaticPredatorData.getNextFloat(),
            vC = StaticPredatorData.getNextFloat(),
            mA = StaticPredatorData.getNextFloat(),
            len = StaticPredatorData.getBl(),

            direction = StaticPredatorData.getNextFloat3(),
            position = StaticPredatorData.getRandom(),
            speed = StaticPredatorData.getRandom(),

            closestFish = StaticPredatorData.getNextInt(),
            status = StaticPredatorData.getNextInt(),
            restTime = StaticPredatorData.getNextInt(),
            remainingRest = StaticPredatorData.getNextInt(),
            fishToEat = StaticPredatorData.getNextInt(),
            centerFish = StaticPredatorData.getNextInt(),
            mostIsolated = StaticPredatorData.getNextInt(),

            lockOnDistance = StaticPredatorData.getNextEvolve(),
        };
    }


}
