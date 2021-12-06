using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Mathematics;


/* Inicialization of fish agents
 *  TO DO:
 *      - Probably best to initialize predator here to avoid any racing conditions not sure
 *      - Set spawn area to match the roosting area 
 *      - Play with weights to possibly avoid collisions
 */

public class FishAgentCreator : MonoBehaviour{

    [SerializeField] private Mesh fishMesh;
    [SerializeField] private Material fishMaterial;
    [SerializeField] private int fishNumber;

    [SerializeField] private Mesh predatorMesh;
    [SerializeField] private Material predatorMaterial;
    [SerializeField] private int predatorNumber;

    public static FishAgentCreator Instance;
    // Start is called before the first frame update
    private void Awake() {
        Instance = this;
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype fishArchetype = entityManager.CreateArchetype(
            typeof(FishPropertiesComponent),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(Scale),
            typeof(Rotation)
        );

        EntityArchetype predatorArchetype = entityManager.CreateArchetype(
            typeof(PredatorPropertiesComponent),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(Scale),
            typeof(Rotation)
        );

        NativeArray<Entity> fishArray = new NativeArray<Entity>(fishNumber, Allocator.Temp);
        NativeArray<Entity> predatorArray = new NativeArray<Entity>(predatorNumber, Allocator.Temp);

        entityManager.CreateEntity(fishArchetype, fishArray);
        entityManager.CreateEntity(predatorArchetype, predatorArray);

        float bl = 0.1f;

        for(int i = 0; i < fishArray.Length; i++) {
            Entity fish = fishArray[i];

            Vector3 pos = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);
            Vector3 sp = new float3(1, 1, 0);


            entityManager.SetComponentData(fish, new FishPropertiesComponent {
                id = i,
                vM = 4 * bl,
                vC = 2 * bl,
                foV = 0,
                sD = new float3(0, 0, 0),
                aD = new float3(0, 0, 0),
                cD = new float3(0, 0, 0),
                eD = new float3(0, 0, 0),
                bD = new float3(0, 0, 0),
                sW = 5,
                aW = 0.3f,
                cW = 1,
                eW = 5,
                bW = 0,
                mA = 0,
                len = bl,
                direction = new float3(0, 0, 0),
                position = new float3(pos),
                speed = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0),
            });

            entityManager.SetComponentData(fish, new Translation {
                Value = new float3(pos)
            });

            entityManager.SetSharedComponentData(fish, new RenderMesh {
                mesh = fishMesh,
                material = fishMaterial
            });

            entityManager.SetComponentData(fish, new Scale {
                Value = bl / 2
            });
        }

        for(int i = 0; i < predatorArray.Length; i++) {
            Entity predator = predatorArray[i];

            Vector3 pos = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);

            entityManager.SetComponentData(predator, new PredatorPropertiesComponent {
                vM = 6 * bl,
                vC = 3 * bl,
                mA = 0,
                len = bl * 6,
                status = -2,
                closestFish = -1,
                fishToEat = -1,
                centerFish = -1,
                mostIsolated = -1,
                restTime = 400,
                remainingRest = 400,
                direction = new float3(0, 0, 0),
                position = new float3(pos),
                speed = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0),
            });

            entityManager.SetComponentData(predator, new Translation {
                Value = new float3(pos)
            });

            entityManager.SetSharedComponentData(predator, new RenderMesh {
                mesh = predatorMesh,
                material = predatorMaterial
            });

            entityManager.SetComponentData(predator, new Scale {
                Value = bl
            });
        }

        fishArray.Dispose();
        predatorArray.Dispose();
    }


}
