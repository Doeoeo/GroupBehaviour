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

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private int agentNumberParam;
    [SerializeField] public bool isActive;

    public static int simulationFrames = 500;
    public static float timestep = 0.005f;
    private static int simulationNo;
    private int currentSimulationFrame;
    private EntityManager entityManager;
    FixedStepSimulationSystemGroup a;


    void Start() {
        Time.fixedDeltaTime = timestep;
        currentSimulationFrame = 0;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        generateEntity(StaticFishData.getArchetype(), agentNumberParam, constructFishComponent);
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
        //evaluate();
        cleanUp();
        generateEntity(StaticFishData.getArchetype(), agentNumberParam, constructFishComponent);
        currentSimulationFrame = 0;
    }

    // Kill every entity
    private void cleanUp() {
        NativeArray<Entity> entities = entityManager.GetAllEntities();
        entityManager.DestroyEntity(entities);
    }

    // Create new entities
    private void generateEntity(EntityArchetype entityArchetype, int agentNumber, Func<IComponentData> f) {
        NativeArray<Entity> entityArray = new NativeArray<Entity>(agentNumber, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entityArray);


        float bl = 0.1f;
        for (int i = 0; i < entityArray.Length; i++) {
            Entity entity = entityArray[i];

            entityManager.SetComponentData(entity, (FishPropertiesComponent)f());

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



}
