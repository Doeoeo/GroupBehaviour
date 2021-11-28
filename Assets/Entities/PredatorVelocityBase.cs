using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
//using System.Runtime.Remoting.Metadata.W3cXsd2001;

public class PredatorVelocityBase : SystemBase {


    private EntityQuery m_Group;
    private FishAgentCreator controller;

    protected override void OnUpdate() {

        if (!controller) {
            controller = FishAgentCreator.Instance;
        }

        if (controller) {
            //Query to get all fish components
            m_Group = GetEntityQuery(ComponentType.ReadOnly<FishPropertiesComponent>());
            NativeArray <FishPropertiesComponent> positions = m_Group.ToComponentDataArray<FishPropertiesComponent>(Allocator.TempJob);
            //NativeArray <Entity> fishes = m_Group.ToEntityArray(Allocator.TempJob);

            //main for each for all predators
            Entities.WithAll<PredatorPropertiesComponent>()
            .WithoutBurst()
            .WithReadOnly(positions)
            .WithNativeDisableContainerSafetyRestriction(positions)
            .ForEach((Entity selectedEntity, ref Translation predatorTranslation, ref PredatorPropertiesComponent predator) => {

                float3 predatorPosition = new float3(predatorTranslation.Value);

                //status = 0 means the predator is finding a new target
                if (predator.status == 0) {
                    //reset rest time
                    predator.remainingRest = predator.restTime;
                    predator.fishToEat = -1;

                    Debug.Log("Aquiring target... ");

                    //distance between predator and current fish
                    float currentDistance = math.distance(predatorPosition, positions[0].position);

                    //find closest fish (currently)
                    for (int i = 1; i < positions.Length; i++) {
                        float comparedDistance = math.distance(predatorPosition, positions[i].position);
                        //closest fish so far, so save it
                        if(comparedDistance < currentDistance) {
                            currentDistance = comparedDistance;
                            //predator.closestFish is id of the fish that is selected
                            predator.closestFish = positions[i].id;
                        }
                    }

                    Debug.Log("Targeted fish number " + predator.closestFish + " aquired!");
                    predator.status = 1;

                //status 1 means that hunting the target
                } else if (predator.status == 1) {

                    int targetFishArrayIndex = -1;

                    //find the fish with the target id
                    for(int i = 0; i < positions.Length; i++) {
                        if (positions[i].id == predator.closestFish) {
                            targetFishArrayIndex = i;
                            break;
                        } 
                    }

                    //if the fish was not found we need to select another
                    if(targetFishArrayIndex == -1) {
                        Debug.Log("Targeted fish number " + predator.closestFish + " no longer exists, finding new target.");
                        predator.status = 0;

                    //else we have the fish and we eat it if we are close
                    } else {
                        float3 speedToFish = positions[targetFishArrayIndex].position - predatorPosition;
                        //change the vector speed magnitude to max speed
                        predator.speed = ((Vector3) speedToFish).normalized * predator.vM;

                        //if the fish is less than 1 bl away from the fish, we say the prey ate the fish
                        if (math.distance(predatorPosition, positions[targetFishArrayIndex].position) < 0.1f) {
                            Debug.Log("I caught the fish!");
                            //setup the fish with the id to be eaten
                            predator.fishToEat = predator.closestFish;
                            //maybe we need a critical section
                            predator.status = 2;
                        }
                    }

                //is the predator status is 2 then the predator is just drifting in the direction
                //he was last hunting for the fish, however it now has cruising speed
                } else if (predator.status == 2) {
                    predator.fishToEat = predator.closestFish;
                    //speed should now cruizing speed
                    predator.speed = ((Vector3) predator.speed).normalized * predator.vC;
                    if (predator.remainingRest == predator.restTime) {
                        Debug.Log("Resting..." + predator.remainingRest);
                        predator.remainingRest--;
                    //counting down the frames of resting time
                    } else if (predator.remainingRest > 0) {
                        predator.remainingRest --;
                        Debug.Log("Resting..." + predator.remainingRest);
                    } else {
                        predator.status = 0;
                    }
                }
            }).Run();

            //Query to get predator components
            EntityQuery m_Group_p = GetEntityQuery(ComponentType.ReadOnly<PredatorPropertiesComponent>());
            NativeArray <PredatorPropertiesComponent> predatorPositions = m_Group_p.ToComponentDataArray<PredatorPropertiesComponent>(Allocator.TempJob);

            //go through all the predators to check if any of them has a fish to delete
            for (int i = 0; i < predatorPositions.Length; i++) {
                if (predatorPositions[i].fishToEat != -1) {
                    //there is a fish we need to delete so let's find it
                    Entities.WithAll<FishPropertiesComponent>()
                    .WithoutBurst()
                    .WithStructuralChanges()
                    .ForEach((Entity selectedEntity, ref FishPropertiesComponent fish) => {
                        //this is the fish the predator has eaten, so let's delete it
                        if (fish.id == predatorPositions[i].fishToEat) {
                            EntityManager.DestroyEntity(selectedEntity);
                        }
                    }).Run();
                }
            }
            
            predatorPositions.Dispose();
            positions.Dispose();
        }
    }
    
}
