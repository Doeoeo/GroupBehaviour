using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Threading;
//using System.Runtime.Remoting.Metadata.W3cXsd2001;
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class PredatorVelocityBase : SystemBase {


    private EntityQuery m_Group;
    private FishAgentCreator controller;

    protected override void OnUpdate() {

        float lockOnDistance = 1.5f;

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

            //status -2 means the predator needs to find group center
            float leastPeripheral = positions[0].peripherality;
            if (predator.status == -2) {
                Debug.Log("Calculating flock center ... ");
                //reset the group center
                predator.centerFish = positions[0].id;
                for (int i = 1; i < positions.Length; i++) {
                    //compare peripherality to leastPeripheral and find the least peripheral
                    if(leastPeripheral > positions[i]. peripherality) {
                        leastPeripheral = positions[i]. peripherality;
                        //predator.centerFish is id of the fish that has the lowest peripherality value
                        predator.centerFish = positions[i].id;
                    }
                }
                
                Debug.Log("Hunting central fish.");
                predator.status = -1;
            //status -1 means the predator needs to move to the center of the group
            //this stops when the predator is less than lock on distance away from the centre and goes to status 0
            } else if (predator.status == -1) {
                int targetFishArrayIndex = -1;

                //find the fish with the target id
                for(int i = 0; i < positions.Length; i++) {
                    if (positions[i].id == predator.centerFish) {
                        targetFishArrayIndex = i;
                        break;
                    } 
                }

                //if the fish was not found we need to select another
                if(targetFishArrayIndex == -1) {
                    Debug.Log("Targeted fish number " + predator.closestFish + " no longer exists, finding new target.");
                    predator.status = -2;
                //else we have the fish and swim towards it
                } else {
                    float3 speedToFish = positions[targetFishArrayIndex].position - predatorPosition;
                    //change the vector speed magnitude to max speed
                    predator.speed = ((Vector3) speedToFish).normalized * predator.vM;

                    //if the fish is less than lock on distance away from the fish, we go to find the most isolated fish
                    if (math.distance(predatorPosition, positions[targetFishArrayIndex].position) < lockOnDistance) {
                        //we stop hunting the center fish and move to the status 0
                        predator.status = 0;
                    }
                }

            //status = 0 means the predator is finding a new target
            } else if (predator.status == 0) {
                Debug.Log("Aquiring most isolatedtarget ... ");

                float biggestPeripherality = 0;
                int j = -1;
                for(int i = 0; i < positions.Length; i++) {
                    //finding the most peripheral fish that also has a speed angle less than 90 degrees diffrent from predator
                    if (positions[i].peripherality > biggestPeripherality && Vector3.Angle(predator.speed, positions[i].peripheralityVector) < 90) {
                        biggestPeripherality = positions[i].peripherality;
                        //updating the id of most isolated fish for the predator
                        predator.mostIsolated = positions[i].id;
                        j = i;
                    } 
                }

                Debug.Log("Targeting fish number " + predator.mostIsolated + " ... angle change: " + Vector3.Angle(predator.speed, positions[j].peripheralityVector));
                predator.status = 1;

            //status 1 means that hunting the target
            } else if (predator.status == 1) {

                int targetFishArrayIndex = -1;

                //find the fish with the target id
                for(int i = 0; i < positions.Length; i++) {
                    if (positions[i].id == predator.mostIsolated) {
                        targetFishArrayIndex = i;
                        break;
                    } 
                }

                //if the fish was not found we need to select another
                if(targetFishArrayIndex == -1) {
                    Debug.Log("Targeted fish number " + predator.mostIsolated + " no longer exists, finding new center.");
                    predator.status = -2;

                //else we have the fish and we eat it if we are close
                } else {
                    float3 speedToFish = positions[targetFishArrayIndex].position - predatorPosition;
                    //change the vector speed magnitude to max speed
                    predator.speed = ((Vector3) speedToFish).normalized * predator.vM;

                    //if the fish is less than 1 bl away from the fish, we say the prey ate the fish
                    if (math.distance(predatorPosition, positions[targetFishArrayIndex].position) < 0.1f) {
                        Debug.Log("I caught the fish!");
                        //setup the fish with the id to be eaten
                        predator.fishToEat = predator.mostIsolated;
                        //maybe we need a critical section
                        predator.status = 2;
                    }
                }

            //is the predator status is 2 then the predator is just drifting in the direction
            //he was last hunting for the fish, however it now has cruising speed
            } else if (predator.status == 2) {
                //reset soft deleted fish, so we don't try to delete it again
                predator.fishToEat = -1;

                //speed should now cruizing speed
                predator.speed = ((Vector3) predator.speed).normalized * predator.vC;
                if (predator.remainingRest == predator.restTime) {
                    Debug.Log("Resting..." + predator.remainingRest);
                    predator.remainingRest--;
                //counting down the frames of resting time
                } else if (predator.remainingRest > 0) {
                    predator.remainingRest --;
                    //Debug.Log("Resting..." + predator.remainingRest);
                } else {
                    predator.status = -2;
                    //reset rest time
                    predator.remainingRest = predator.restTime;
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
