using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


/* SystemBase for adjusting computing drives
 *  TO DO:
 *      - Check if controller checking is necassary (probably not)
 *      - Maybe GetEntityQuerry can be on on Create but might cause racing conditions
 *      - Radiai should probably not be hard coded
 */
public class FishDriveBase : SystemBase {

    private EntityQuery m_Group;
    private FishAgentCreator controller;

    protected override void OnUpdate() {
        // Copied for initialisation order safety not sure if required
        if (!controller) {
            controller = FishAgentCreator.Instance;
        }
        if (controller) {
            // Querry over all entities to use in our forEach
            m_Group = GetEntityQuery(ComponentType.ReadOnly<FishPropertiesComponent>());
            NativeArray <FishPropertiesComponent> positions = m_Group.ToComponentDataArray<FishPropertiesComponent>(Allocator.TempJob);

            // Main forEach passing positions as ReadOnly didn't work.
            // Had to remove safety restrictions -> BE CAREFUL
            Entities.WithAll<FishPropertiesComponent>()
                .WithReadOnly(positions)
                .WithNativeDisableContainerSafetyRestriction(positions)
                .ForEach((Entity selectedEntity, ref Translation fishTranslation, ref FishPropertiesComponent fish) => {

                    // Might not be necassary
                    float3 fishPosition = new float3(fishTranslation.Value);

                    // Radiai for each drive
                    float seperationRadius = 5 * fish.len, alignmentRadius = 25 * fish.len, cohesionRadius = 100 * fish.len;

                    // Data for drive calculations
                    float3 seperationDrive = new float3(0, 0, 0), alignmentDrive = new float3(0, 0, 0), cohesionDrive = new float3(0, 0, 0), borderDrive = new float3(0, 0, 0);
                    int seperationCount = 0, alignmentCount = 0, cohesionCount = 0;

                    // Loop over entities
                    for (int i = 0; i < positions.Length; i++) {
                        // Compute distance and angle between fish agents
                        float comparedDistance = math.distance(fishPosition, positions[i].position);
                        float blindAngle = Vector3.Angle(fish.speed, fish.position - positions[i].position);

                        // Check if we are comparing to ourselves (could be safer to do with index) and if compared agent is behind the selected agent
                        if (comparedDistance != 0 && (blindAngle < 165 || blindAngle > 195)) {

                            // Check if compared agent is too close
                            if (comparedDistance < seperationRadius) {
                                seperationCount++;
                                Vector3 dJ = positions[i].position - fish.position;


                                seperationDrive += -1 * dJ * (1 - (float3)dJ.magnitude / seperationRadius);
                            }

                            // Check if agent is in accaptable distance
                            else if (comparedDistance < alignmentRadius) {
                                alignmentCount++;
                                alignmentDrive += positions[i].speed;

                            }

                            // Check if agent is too far
                            else if (comparedDistance < cohesionRadius) {
                                cohesionCount++;
                                cohesionDrive += positions[i].position - fish.position;
                            }

                        }

                    }

                    // Weight computed drives. Mind division by 0
                    if (seperationCount != 0) seperationDrive /= seperationCount;
                    if (alignmentCount != 0) alignmentDrive /= alignmentCount;
                    if (cohesionCount != 0) cohesionDrive /= cohesionCount;

                    // Set computed drives
                    fish.sD = seperationDrive;
                    fish.aD = alignmentDrive;
                    fish.cD = cohesionDrive;

            }).ScheduleParallel();

            // Cleanup
            positions.Dispose();
        }
    }

}

