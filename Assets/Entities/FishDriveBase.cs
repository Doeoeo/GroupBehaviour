using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Specialized;
//using System.Runtime.Remoting.Metadata.W3cXsd2001;


/* SystemBase for adjusting computing drives
 *  TO DO:
 *      - Check if controller checking is necassary (probably not)
 *      - Maybe GetEntityQuerry can be on on Create but might cause racing conditions
 *      - Radiai should probably not be hard coded
 *  NEW:
 *      - Added [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] for fixed update calling
 *      - Removed controller as it wasn't necassary
 */
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FishDriveBase : SystemBase {

    private EntityQuery m_Group;
    private EntityQuery m_Group_p;
    private static FishAgentCreator controller;

    protected override void OnUpdate() {
        if (!controller) {
            controller = FishAgentCreator.Instance;
        }

        // Copied for initialisation order safety not sure if required
        // Querry over all entities to use in our forEach
        m_Group = GetEntityQuery(ComponentType.ReadOnly<FishPropertiesComponent>());
        NativeArray <FishPropertiesComponent> positions = m_Group.ToComponentDataArray<FishPropertiesComponent>(Allocator.TempJob);

        m_Group_p = GetEntityQuery(ComponentType.ReadOnly<PredatorPropertiesComponent>());
        NativeArray <PredatorPropertiesComponent> predatorPositions = m_Group_p.ToComponentDataArray<PredatorPropertiesComponent>(Allocator.TempJob);

        // Main forEach passing positions as ReadOnly didn't work.
        // Had to remove safety restrictions -> BE CAREFUL
        Entities.WithAll<FishPropertiesComponent>()
            .WithoutBurst()
            .WithReadOnly(positions)
            .WithReadOnly(predatorPositions)
            .WithNativeDisableContainerSafetyRestriction(positions)
            .WithNativeDisableContainerSafetyRestriction(predatorPositions)
            .ForEach((Entity selectedEntity, ref Translation fishTranslation, ref FishPropertiesComponent fish) => {

                // Might not be necassary
                float3 fishPosition = new float3(fishTranslation.Value);

                // Radiai for each drive
                // NOTE(miha): fish.len is 0.2f
                float seperationRadius = controller.seperationRadius * fish.len;
                float alignmentRadius = controller.alignmentRadius * fish.len;
                float cohesionRadius = controller.cohesionRadius * fish.len;
                float escapeRadius = controller.escapeRadius * fish.len;
                float confusionRadius = controller.confusionRadius * fish.len;

                // Data for drive calculations
                float3 seperationDrive = new float3(0, 0, 0), alignmentDrive = new float3(0, 0, 0), 
                    cohesionDrive = new float3(0, 0, 0), borderDrive = new float3(0, 0, 0), 
                    escapeDrive = new float3(0, 0, 0);

                int seperationCount = 0, alignmentCount = 0, cohesionCount = 0, 
                    escapeCount = 0;

                float3 peripheralityVector = new float3(0, 0, 0);
                int peripheralityCount = 0;

                // Loop over entities
                for (int i = 0; i < positions.Length; i++) {
                    // Compute distance and angle between fish agents
                    float comparedDistance = math.distance(fishPosition, positions[i].position);
                    float blindAngle = Vector3.Angle(fish.speed, fish.position - positions[i].position);

                    // Check if we are comparing to ourselves (could be safer to do with index) and if compared agent is behind the selected agent
                    if (comparedDistance != 0 && (blindAngle < 165)) {

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

                            // NOTE(Miha): This is calculation of peripherality
                            // for mixture of simple tactics as in the paper.
                            peripheralityVector += (positions[i].position - fish.position) / comparedDistance;
                            peripheralityCount++;
                        }
                    }
                }

                //chech distance to all predators
                for (int i = 0; i < predatorPositions.Length; i++) { 
                    float blindAngle = Vector3.Angle(fish.speed, fish.position - predatorPositions[i].position);
                    float comparedDistance = math.distance(fishPosition, predatorPositions[i].position);
                    //find predators that are closer than 
                    if (comparedDistance < escapeRadius && (blindAngle < 165)) {
                        escapeCount++;
                        Vector3 vecToPredator = predatorPositions[i].position - fish.position;
                        escapeDrive += -1 * vecToPredator  * (1 - (float3)vecToPredator.magnitude / escapeRadius);
                    }
                }

                // Weight computed drives. Mind division by 0
                if (seperationCount != 0) seperationDrive /= seperationCount;
                if (alignmentCount != 0) alignmentDrive /= alignmentCount;
                if (cohesionCount != 0) cohesionDrive /= cohesionCount;
                if (escapeCount != 0) escapeDrive /= escapeCount;
                if (((Vector3)peripheralityVector).magnitude != 0) peripheralityVector /= peripheralityCount;

                // Set computed drives
                fish.sD = seperationDrive;
                fish.aD = alignmentDrive;
                fish.cD = cohesionDrive;
                fish.eD = escapeDrive; 
                fish.peripherality = ((Vector3)peripheralityVector).magnitude;
                fish.peripheralityVector = peripheralityVector;

        }).ScheduleParallel();

        // Cleanup
        positions.Dispose();
        predatorPositions.Dispose();
    }
    
}
