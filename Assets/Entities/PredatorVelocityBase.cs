using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class PredatorVelocityBase : SystemBase {


    private EntityQuery m_Group;
    private FishAgentCreator controller;

    protected override void OnUpdate() {
        if (!controller) {
            controller = FishAgentCreator.Instance;
        }
        if (controller) {
            m_Group = GetEntityQuery(ComponentType.ReadOnly<FishPropertiesComponent>());
            NativeArray <FishPropertiesComponent> positions = m_Group.ToComponentDataArray<FishPropertiesComponent>(Allocator.TempJob);

            Entities.WithAll<PredatorPropertiesComponent>()
                .WithoutBurst()
                .WithReadOnly(positions)
                .WithNativeDisableContainerSafetyRestriction(positions)
                .ForEach((Entity selectedEntity, ref Translation predatorTranslation, ref PredatorPropertiesComponent predator) => {

                    float3 predatorPosition = new float3(predatorTranslation.Value);

                    float3 closestFish = new float3(predatorTranslation.Value);

                    for (int i = 0; i < positions.Length; i++) {

                        float comparedDistance = math.distance(predatorPosition, positions[i].position);

                    }

                }).Run();
        }
    }
    
}
