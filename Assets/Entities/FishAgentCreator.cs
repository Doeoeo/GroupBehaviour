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

public class FishAgentCreator : MonoBehaviour{

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private int agentNumber;
    [SerializeField] public bool isActive;

    // Start is called before the first frame update
    private void Start() {
        if (!isActive) return;
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype fishArchetype = entityManager.CreateArchetype(
            typeof(FishPropertiesComponent),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(Scale)
        );

        NativeArray<Entity> fishArray = new NativeArray<Entity>(agentNumber, Allocator.Temp);

        entityManager.CreateEntity(fishArchetype, fishArray);
        float bl = 0.1f;
        for(int i = 0; i < fishArray.Length; i++) {
            Entity fish = fishArray[i];

            Vector3 pos = new float3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);
            Vector3 sp = new float3(1, 1, 0);


            entityManager.SetComponentData(fish, new FishPropertiesComponent {
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
                eW = 0.01f,
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
                mesh = mesh,
                material = material
            });

            entityManager.SetComponentData(fish, new Scale {
                Value = bl / 2
            });
        }

        fishArray.Dispose();
    }


}
