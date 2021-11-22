using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;

public class PredatorVelocityBase : SystemBase {


    private EntityQuery m_Group;
    private FishAgentCreator controller;

    int frame;
    int status;
    int timeout;
    int iClosestFish;

    private float restingSpeed;

    protected override void OnCreate() {
        frame = 0;
        status = 0; //0 - choosing; 1 - hunting; 2 - resting
        timeout = 60;
    }

    protected override void OnUpdate() {
        if (!controller) {
            controller = FishAgentCreator.Instance;
        }
        if (controller) {
            m_Group = GetEntityQuery(ComponentType.ReadOnly<FishPropertiesComponent>());
            NativeArray <FishPropertiesComponent> positions = m_Group.ToComponentDataArray<FishPropertiesComponent>(Allocator.TempJob);
            if (status == 0) {

                Debug.Log("Aquiring target... ");
                Entities.WithAll<PredatorPropertiesComponent>()
                .WithoutBurst()
                //.WithReadOnly(positions)
                //.WithNativeDisableContainerSafetyRestriction(positions)
                .ForEach((Entity selectedEntity, ref Translation predatorTranslation, ref PredatorPropertiesComponent predator) => {

                    /*float3 predatorPosition = new float3(predatorTranslation.Value);

                    iClosestFish = 0;
                    float dis = math.distance(predatorPosition, positions[0].position);

                    for (int i = 1; i < positions.Length; i++) {

                        float comparedDistance = math.distance(predatorPosition, positions[i].position);

                        if(comparedDistance < dis) {
                            dis = comparedDistance;
                            iClosestFish = i;
                        }

                    }*/
                }).Run();

                Debug.Log("Target aquired!");
                status = 1;
            } else if (status == 1) {

                float3 speed = new float3();

                Entities.WithAll<PredatorPropertiesComponent>()
                .ForEach((Entity selectedEntity, ref Translation predatorTranslation, ref PredatorPropertiesComponent predator) => {
                    NativeArray <FishPropertiesComponent> positions = m_Group.ToComponentDataArray<FishPropertiesComponent>(Allocator.TempJob);
                    float3 predatorPosition = new float3(predatorTranslation.Value);
                    float3 speed = positions[iClosestFish].position - predatorPosition;
                    predator.speed = speed;//.normalized;
                }).Schedule();

                /*if (math.distance(predatorPosition, positions[iClosestFish].position) < 0.1f) {
                    Debug.Log("I caught the fish!");
                    status = 2;
                }*/

            } else if (status == 2) {
                if (timeout == 59) {
                    Debug.Log("Resting...");
                } else if (timeout > 0) {
                    timeout --;
                } else {
                    timeout = 60;
                    status = 0;
                }
            }
        }
    }
    
}
