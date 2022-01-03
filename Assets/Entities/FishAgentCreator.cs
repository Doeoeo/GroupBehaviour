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
 *      
 *  NEW:
 *      - Added an isActive toggle to avoid double spawning
 */

// TODO(miha): Create few helper functions - to create fishes, predators, ...
// TODO(miha): Maybe predator goes to the -1 * followGroup after catching fish.

public class FishAgentCreator : MonoBehaviour{

    [SerializeField] private Mesh fishMesh;
    [SerializeField] private Material fishMaterial;
    [SerializeField] private int fishNumber;

    [SerializeField] private Mesh predatorMesh;
    [SerializeField] private Material predatorMaterial;
    [SerializeField] private int predatorNumber;

    [SerializeField] public bool isActive;

    [SerializeField] public SimpleTactic simpleTactic;
    [SerializeField] public bool fishDebug;
    [SerializeField] public bool predatorDebug;

    [SerializeField] public float seperationWeight;
    [SerializeField] public float seperationRadius;
    [SerializeField] public float alignmentWeight;
    [SerializeField] public float alignmentRadius;
    [SerializeField] public float cohesionWeight;
    [SerializeField] public float cohesionRadius;
    [SerializeField] public float escapeWeight;
    [SerializeField] public float escapeRadius;
    [SerializeField] public float borderRadius = 100f;
    [SerializeField] public float maxAcceleration;
    [SerializeField] public float cruisingVelocity;
    [SerializeField] public float maxVelocity;

    public Transform predatorCamera;

    public static FishAgentCreator Instance;

    public void Awake() {
        Instance = this;
    }

    // Start is called before the first frame update
    public void Start() {
        if (!isActive) return;

        if(isActive)
            predatorCamera = gameObject.transform.Find("PredatorPosition");

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
            typeof(PredatorSTPropertiesComponent),
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

        float bl = 0.2f;

        // NOTE(miha): Every fish have the same speed vector, but diffrent
        // position. This way the fish are already aligned from the start of
        // teh simulation.
        float3 s = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0f);

        for(int i = 0; i < fishArray.Length; i++) {
            Entity fish = fishArray[i];

            Vector3 pos = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);

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
                sW = 1,
                aW = 100,
                cW = 100,
                eW = 100,
                mA = 100,
                len = bl,
                direction = new float3(0, 0, 0),
                position = new float3(pos),
                speed = s,
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

            int xSign = (UnityEngine.Random.Range(0,2)*2-1);
            int ySign = (UnityEngine.Random.Range(0,2)*2-1);
            float x = xSign * UnityEngine.Random.Range(0f, 2f) + (3 * xSign);
            float y = ySign * UnityEngine.Random.Range(0f, 2f) + (3 * ySign);

            Vector3 pos = new float3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);

            /*
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
                numOfFishCaught = 0,
                closestGroupRadius = 15f,
                lockOnDistance = 5f,
                lockOnRadius = 100f,
            });
            */

            entityManager.SetComponentData(predator, new PredatorSTPropertiesComponent {
                vM = 6 * bl,
                vC = 6 * bl,
                mA = 0,
                len = bl * 6,
                catchDistance = bl,
                fishToEat = -1,
                centerFish = -1,
                mostIsolated = -1,
                restTime = 100,
                remainingRest = 0,
                direction = new float3(0, 0, 0),
                position = new float3(pos),
                speed = new float3(0.1f, 0.0f, 0.0f), //new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0),
                tactic = SimpleTactic.Nearest,
                state = State.Cruising,
                confusionProbability = 0.25f,
                numOfFishCaught = 0,
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
