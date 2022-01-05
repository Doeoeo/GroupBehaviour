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
//using System.Diagnostics;


//using System.Runtime.Remoting.Metadata.W3cXsd2001;
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class PredatorVelocityBase : SystemBase {


    private EntityQuery m_Group;
    private FishAgentCreator controller;

    protected override void OnUpdate() {
        if (!controller) {
            controller = FishAgentCreator.Instance;
        }

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

                //find the closest fish first
                float3 closestFishPos = positions[0].position;
                float closestFishDist = math.distance(predatorPosition, positions[0].position);
                
                for (int i = 1; i < positions.Length; i++) {
                    float currentDist = math.distance(predatorPosition, positions[i].position);
                    //we found a closer fish
                    if(currentDist < closestFishDist) {
                        closestFishPos = positions[i].position;
                        closestFishDist = currentDist;
                    }
                }
                //Debug.Log("Found closest fish!");

                //reset the group center
                predator.centerFish = positions[0].id;
                for (int i = 1; i < positions.Length; i++) {
                    //compare peripherality to leastPeripheral and find the least peripheral within closest group of closestfish
                    if(leastPeripheral > positions[i].peripherality && math.distance(closestFishPos, positions[i].position) < predator.closestGroupRadius) {
                        leastPeripheral = positions[i].peripherality;
                        //predator.centerFish is id of the fish that has the lowest peripherality value
                        predator.centerFish = positions[i].id;
                    }
                }
                //Debug.Log("Found least peripheral fish in group.");
                //Debug.Log("Hunting central fish.");
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
                    //Debug.Log("Fish not found :(, finding another target.");
                    predator.status = -2;
                //else we have the fish and swim towards it
                } else {
                    float3 speedToFish = positions[targetFishArrayIndex].position - predatorPosition;
                    //change the vector speed magnitude to max speed
                    predator.speed = ((Vector3) speedToFish).normalized * predator.vM;

                    //if the fish is less than lock on distance away from the fish, we go to find the most isolated fish
                    if (math.distance(predatorPosition, positions[targetFishArrayIndex].position) < predator.lockOnDistance) {
                        //Debug.Log("predator.lockOnDistance: " + predator.lockOnDistance);
                        //Debug.Log("Within lockon distance ... finding most peripheral fish in lockon radius ...");
                        //we stop hunting the center fish and move to the status 0
                        predator.status = 0;
                    }
                }

            //status = 0 means the predator is finding a new target
            } else if (predator.status == 0) {
                //Debug.Log("predator.lockOnRadius: " + predator.lockOnRadius);
                float biggestPeripherality = 0;
                for(int i = 0; i < positions.Length; i++) {
                    float distToPredator = math.distance(predatorPosition, positions[i].position);
                    //Debug.Log("distToPredator: " + distToPredator);
                    float angleDifference = Vector3.Angle(predator.speed, positions[i].peripheralityVector);
                    //Debug.Log("angleDifference: " + angleDifference);
                    //finding the most peripheral fish that also has a speed angle less than 90 degrees diffrent from predator and is less than lockOnRadius away from predator
                    if (positions[i].peripherality > biggestPeripherality && angleDifference < 90 && distToPredator < predator.lockOnRadius) {
                        biggestPeripherality = positions[i].peripherality;
                        //updating the id of most isolated fish for the predator
                        predator.mostIsolated = positions[i].id;
                    } 
                }

                predator.status = 1;
                //Debug.Log("Hunting the most peripheral fish.");

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
                    predator.status = -2;
                    //Debug.Log("Fish not found #2 :(, finding another target.");
                //else we have the fish and we eat it if we are close
                } else {
                    float3 speedToFish = positions[targetFishArrayIndex].position - predatorPosition;
                    //change the vector speed magnitude to max speed
                    predator.speed = ((Vector3) speedToFish).normalized * predator.vM;

                    //if the fish is less than 1 bl away from the fish, we say the prey ate the fish
                    if (math.distance(predatorPosition, positions[targetFishArrayIndex].position) < 0.1f) {
                        //Debug.Log("I caught the fish!");
                        predator.numOfFishCaught++;
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

            // NOTE(miha): Cinemachine camera follows this empty game object,
            // which have the same coordinates as predator.
            if(controller.isActive)
                controller.predatorCamera.position = predatorTranslation.Value;

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

public enum State {
    Cruising,
    Hunting,
    LockedPrey,
    Resting
}

public enum SimpleTactic {
    Random = 0,
    Nearest = 1,
    Center = 2,
    Peripheral = 3
} 

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class PredatorSTVelocityBase : SystemBase {
    private EntityQuery m_Group;
    private static FishAgentCreator controller;

    static uint seed = (uint) (UnityEngine.Random.value * 10000);
    static Unity.Mathematics.Random rnd = new Unity.Mathematics.Random(seed);

    static public SimpleTacticInfo tacticInfo = default(SimpleTacticInfo);
    static public bool chooseNewTarget = true;
    static public int currentPreyIndex = 0;
    static public SimpleTactic currentTactic = 0;

    static Vector3 Seek(in PredatorSTPropertiesComponent p, Vector3 target) {
        Vector3 desired = target - (Vector3)p.position;
        desired = desired.normalized;
        desired *= p.vM;
        Vector3 steer = desired - (Vector3)p.speed;
        return steer;
    }

    static Vector3 Arrive(in PredatorSTPropertiesComponent p, Vector3 target) {
        Vector3 desired = target - (Vector3)p.position;
        float distance = desired.magnitude;
        desired = desired.normalized;

        if(distance < 1f) {
            float m = Mathf.Lerp(0, p.vM, distance);
            desired *= m;
        }
        else {
            desired *= p.vM;
        }

        Vector3 steer = desired - (Vector3)p.speed;
        steer = Vector3.ClampMagnitude(steer, p.vM);

        return steer;
    }

    public struct SimpleTacticInfo {
        public float3 preyCenterPosition;
        public float3 preyCenterSpeed;
        public int index;
        public float distance;
        public float3 followGroup;
        public Entity fish;
    }

    // NOTE(miha): returns preyCenter (struct of position and speed) and
    // minIndex (nearest fish at the current frame to the preddator).
    static SimpleTacticInfo nearestTactic(in PredatorSTPropertiesComponent predator, NativeArray<FishPropertiesComponent> positions) {
        SimpleTacticInfo result = default(SimpleTacticInfo);

        float minDistance = ((Vector3)positions[0].position - (Vector3)predator.position).magnitude;
        int minIndex = 0;
        float3 preyCenterPosition = new float3(0.0f, 0.0f, 0.0f);
        float3 preyCenterSpeed = new float3(0.0f, 0.0f, 0.0f);

        for(int i = 1; i < positions.Length; i++) {
            float distance = ((Vector3)positions[i].position - (Vector3)predator.position).magnitude;

            // NOTE(miha): blindAngle gets smaller and smaller as the predator is closer to the prey.
            float blindAngle = Vector3.Angle(predator.speed, positions[i].speed);
            // blindAngle /= 10f;
            // blindAngle *= distance;
            bool preyInSight = (blindAngle < 90f && distance < 20f);

            if(controller.predatorDebug && preyInSight) {
                Debug.DrawLine(predator.position, predator.position + predator.speed, Color.red);
                Debug.DrawLine(predator.position, positions[i].position, Color.green);
            }

            if(preyInSight && distance < minDistance) {
                minDistance = distance;
                minIndex = i;
            }

            preyCenterPosition += positions[i].position;
            preyCenterSpeed += positions[i].speed;
        }

        preyCenterPosition /= positions.Length;
        preyCenterSpeed /= positions.Length;
        float3 followGroup = preyCenterPosition + (preyCenterSpeed * -6.0f);

        if(minIndex != 0) {
            Debug.DrawLine(predator.position, positions[minIndex].position);
            Debug.DrawLine(preyCenterPosition, followGroup, Color.red);
            // Debug.DrawLine(new float3(0f, 0f, 0f) , predator.speed, Color.red);
            // Debug.DrawLine(new float3(0f, 0f, 0f) , positions[maxIndex].speed, Color.green);
        }


        result.preyCenterPosition = preyCenterPosition;
        result.preyCenterSpeed = preyCenterSpeed;
        result.index = minIndex;
        result.distance = minDistance;
        result.followGroup = followGroup;
        
        return result;
    }

    static SimpleTacticInfo centerTactic(in PredatorSTPropertiesComponent predator, NativeArray<FishPropertiesComponent> positions) {
        SimpleTacticInfo result = default(SimpleTacticInfo);

        int minIndex = 0;
        float minPeripherality = positions[0].peripherality;
        float minDistance = ((Vector3)positions[0].position - (Vector3)predator.position).magnitude;

        float3 preyCenterPosition = new float3(0.0f, 0.0f, 0.0f);
        float3 preyCenterSpeed = new float3(0.0f, 0.0f, 0.0f);

        for(int i = 1; i < positions.Length; i++) {
            float distance = ((Vector3)positions[i].position - (Vector3)predator.position).magnitude;
            float blindAngle = Vector3.Angle(predator.speed, positions[i].speed);
            blindAngle /= 10f;
            blindAngle *= distance;
            bool preyInSight = (blindAngle < 90f);
            float peripherality = positions[i].peripherality;

            if(preyInSight && peripherality < minPeripherality) {
                minPeripherality = peripherality;
                minIndex = i;
            }

            if(distance < minDistance)
                minDistance = distance;

            preyCenterPosition += positions[i].position;
            preyCenterSpeed += positions[i].speed;
        }

        if(minIndex != 0) {
            Debug.DrawLine(predator.position, positions[minIndex].position);
            // Debug.DrawLine(new float3(0f, 0f, 0f) , predator.speed, Color.red);
            // Debug.DrawLine(new float3(0f, 0f, 0f) , positions[maxIndex].speed, Color.green);
        }

        preyCenterPosition /= positions.Length;
        preyCenterSpeed /= positions.Length;
        float3 followGroup = preyCenterPosition + (preyCenterSpeed * -7.0f);

        result.preyCenterPosition = preyCenterPosition;
        result.preyCenterSpeed = preyCenterSpeed;
        result.index = minIndex;
        result.distance = minDistance;
        result.followGroup = followGroup;
        
        return result;
    }

    public int preyInConfusionRadius(in PredatorSTPropertiesComponent predator,  NativeArray<FishPropertiesComponent> positions) {
        int confusionCount = 0;

        for(int i = 1; i < positions.Length; i++) {
            float distance = ((Vector3)positions[i].position - (Vector3)predator.position).magnitude;
            float blindAngle = Vector3.Angle(predator.speed, positions[i].speed);
            blindAngle /= 10f;
            blindAngle *= distance;
            bool preyInSight = (blindAngle < 90f);

            if(preyInSight && distance < controller.confusionRadius) {
                confusionCount++;
            }
        }

        return confusionCount;
    }

    static SimpleTacticInfo peripheralTactic(in PredatorSTPropertiesComponent predator, NativeArray<FishPropertiesComponent> positions) {
        SimpleTacticInfo result = default(SimpleTacticInfo);

        int maxIndex = 0;
        float maxPeripherality = positions[0].peripherality;
        float minDistance = ((Vector3)positions[0].position - (Vector3)predator.position).magnitude;
        float3 preyCenterPosition = new float3(0.0f, 0.0f, 0.0f);
        float3 preyCenterSpeed = new float3(0.0f, 0.0f, 0.0f);

        float a = 0f;
        float b = 0f;

        for(int i = 1; i < positions.Length; i++) {
            float peripherality = positions[i].peripherality;
            float distance = ((Vector3)positions[i].position - (Vector3)predator.position).magnitude;
            float blindAngle = Vector3.Angle(predator.speed, positions[i].speed);
            blindAngle /= 10f;
            blindAngle *= distance;
            bool preyInSight = (blindAngle < 90f);

            if(preyInSight && peripherality > maxPeripherality) {
                if(controller.predatorDebug)
                    Debug.DrawLine(predator.position, positions[i].position, Color.green);

                maxPeripherality = peripherality;
                maxIndex = i;
                b = Vector3.Angle(predator.speed, positions[i].speed);
                a = blindAngle;
            }

            if(distance < minDistance)
                minDistance = distance;

            preyCenterPosition += positions[i].position;
            preyCenterSpeed += positions[i].speed;
        }

        if(controller.predatorDebug) {
            // NOTE(miha): Draw line from predator to the targeted prey.
            if(maxIndex != 0) 
                Debug.DrawLine(predator.position, positions[maxIndex].position);
        }

        preyCenterPosition /= positions.Length;
        preyCenterSpeed /= positions.Length;
        if(controller.predatorDebug)
            Debug.DrawLine(predator.position, preyCenterPosition, Color.green);
        float3 followGroup = preyCenterPosition + (preyCenterSpeed * -7.0f);

        result.preyCenterPosition = preyCenterPosition;
        result.preyCenterSpeed = preyCenterSpeed;
        result.index = maxIndex;
        result.distance = minDistance;
        result.followGroup = followGroup;
        
        return result;
    }

    public void shuffle(int[] array) {
         for (int i = 0; i < array.Length - 1; i++) {
             int rndIndex = rnd.NextInt(i, array.Length);
             int temp = array[rndIndex];
             array[rndIndex] = array[i];
             array[i] = temp;
         }
    }

    protected override void OnUpdate() {
        if (!controller) {
            controller = FishAgentCreator.Instance;
        }

        //Query to get all fish components
        m_Group = GetEntityQuery(ComponentType.ReadOnly<FishPropertiesComponent>());
        NativeArray<FishPropertiesComponent> positions = m_Group.ToComponentDataArray<FishPropertiesComponent>(Allocator.TempJob);
        NativeArray<Entity> fishes = m_Group.ToEntityArray(Allocator.TempJob);

        //main for each for all predators
        // NOTE(miha): Added WithStructuralChanges, so we can delete Entity in this forEach!
        Entities.WithAll<PredatorSTPropertiesComponent>()
            .WithoutBurst()
            .WithStructuralChanges()
            .WithReadOnly(positions)
            .WithNativeDisableContainerSafetyRestriction(positions)
            .ForEach((Entity selectedEntity, ref Translation predatorTranslation, 
                      ref PredatorSTPropertiesComponent predator) => {

                 // NOTE(miha): So we always have one prey that predator can
                 // chase. Useful for not dealing with edge cases, where there
                 // is no more prey in the world. CAN BE REMOVED!
                 if(positions.Length == 1) {
                    predator.confusionProbability = 1.0f;
                 }

                 // TODO(miha): LockOnTarget not working correctly :(
                 // NOTE(miha): Choose if predator switches targeted fish every frame or not.
                 if(controller.predatorLockOnTarget) {
                     // NOTE(miha): Choose predator's tactic once per attack.
                     if(chooseNewTarget && controller.simpleTactic == SimpleTactic.Nearest) {
                         tacticInfo = nearestTactic(predator, positions);
                         Debug.Log("tacticinfo.index: " + tacticInfo.index);
                         currentPreyIndex = tacticInfo.index;
                         chooseNewTarget = false;
                     } else {
                         tacticInfo = nearestTactic(predator, positions);
                     }

                     if(chooseNewTarget && controller.simpleTactic == SimpleTactic.Center) {
                         tacticInfo = centerTactic(predator, positions);
                         Debug.Log("tacticinfo.index: " + tacticInfo.index);
                         currentPreyIndex = tacticInfo.index;
                         chooseNewTarget = false;
                     } else {
                         tacticInfo = centerTactic(predator, positions);
                     }

                     if(chooseNewTarget && controller.simpleTactic == SimpleTactic.Peripheral) {
                         tacticInfo = peripheralTactic(predator, positions);
                         Debug.Log("tacticinfo.index: " + tacticInfo.index);
                         currentPreyIndex = tacticInfo.index;
                         chooseNewTarget = false;
                     } else {
                         tacticInfo = peripheralTactic(predator, positions);
                     }
                 } else {
                     // NOTE(miha): Choose predator's tactic every frame.
                     if(controller.simpleTactic == SimpleTactic.Nearest) {
                         tacticInfo = nearestTactic(predator, positions);
                     }
                     if(controller.simpleTactic == SimpleTactic.Center) {
                         tacticInfo = centerTactic(predator, positions);
                     }
                     if(controller.simpleTactic == SimpleTactic.Peripheral) {
                         tacticInfo = peripheralTactic(predator, positions);
                     }
                 }

                 // CARE(miha): Random tactic changes once per attack!
                 if(controller.simpleTactic == SimpleTactic.Random) {
                     if(chooseNewTarget) {
                         float rand = rnd.NextFloat(1f);
                         chooseNewTarget = false;

                         // TODO(miha): Do we calculate indicies only once per attack?
                         // TODO(miha): Do we shuffle only at the start?
                         int[] randIndicies = {1, 2, 3};
                         shuffle(randIndicies);

                         //Debug.Log("random tactic: " + (SimpleTactic)randIndicies[0]);
                         // Debug.Log("shuffeled array: " + randIndicies[0] + ", " + randIndicies[1] + ", " + randIndicies[2]);

                         // TODO(miha): Do we need this in second if? -> rand > firstRandomTacticBarrier && 
                         // TODO(miha): Is it better to always have secondRandomTacticBarrier be greater than firstRandomTacticBarrier?
                         //
                         if(rand < predator.firstRandomTacticBarrier) {
                             Debug.Log("random tactic: nearest");
                             tacticInfo = nearestTactic(predator, positions);
                             currentPreyIndex = tacticInfo.index;
                             currentTactic = SimpleTactic.Nearest;
                         } else if(rand < predator.secondRandomTacticBarrier) {
                             Debug.Log("random tactic: center");
                             tacticInfo = centerTactic(predator, positions);
                             currentPreyIndex = tacticInfo.index;
                             currentTactic = SimpleTactic.Center;
                         } else {
                             Debug.Log("random tactic: peripheral");
                             tacticInfo = peripheralTactic(predator, positions);
                             currentPreyIndex = tacticInfo.index;
                             currentTactic = SimpleTactic.Peripheral;
                         }
                     } else {
                         if(currentTactic == SimpleTactic.Nearest) {
                             tacticInfo = nearestTactic(predator, positions);
                         }
                         if(currentTactic == SimpleTactic.Center) {
                             tacticInfo = centerTactic(predator, positions);
                         }
                         if(currentTactic == SimpleTactic.Peripheral) {
                             tacticInfo = peripheralTactic(predator, positions);
                         }
                     }
                 }

                 if(controller.predatorDebug) {
                     // Debug.Log("Choosing target:" + tacticInfo.index);
                     Debug.Log("Tactic info:" + tacticInfo);
                     // Debug.Log("targeted prey: " + currentPreyIndex);
                 }


                 if(predator.state != State.Resting) {
                     if(tacticInfo.distance < 100.0f) {
                         predator.state = State.Hunting;
                     }
                     else {
                         // TODO(miha): Cruising should implement some sort
                         // of a wanderer behaviour, not just following the
                         // tail of the group!
                         predator.state = State.Cruising;
                     }
                 }

                 if(predator.state == State.Hunting) {
                     if(controller.predatorDebug)
                         Debug.DrawLine(predator.position, positions[tacticInfo.index].position, Color.red);

                     if(tacticInfo.distance < predator.catchDistance) {
                         // NOTE(miha): Check if the predator was confused and
                         // prey got away. If we roll higher number than there
                         // is for confusionProbability, we caught the prey
                         // (increment caugh counter and delete prey entity).
                         //int confusionPreyIndex;
                         //if(controller.predatorLockOnTarget) {
                         //    confusionPreyIndex = positions[currentPreyIndex].confusionCount;
                         //} else {
                         //    confusionPreyIndex = positions[tacticInfo.index].confusionCount;
                         //}

                         int confusionCount = preyInConfusionRadius(predator, positions);
                         if(confusionCount == 0) confusionCount = 1;
                         // Debug.Log("confusion count: " + confusionCount);

                         if(rnd.NextFloat(1.0f) < (1/confusionCount)) {
                             predator.numOfFishCaught += 1;
                             EntityManager.DestroyEntity(fishes[tacticInfo.index]);
                         }
                         
                         // NOTE(miha): If prey was or was not caught, predator
                         // need some time before hunting again...
                         predator.remainingRest = predator.restTime;
                         predator.state = State.Resting;
                     }

                     //float3 speedToFish = positions[tacticInfo.index].position - predator.position;

                     if(controller.predatorLockOnTarget) {
                         predator.speed += (float3) Seek(predator, positions[currentPreyIndex].position);
                     } else {
                         predator.speed += (float3) Seek(predator, positions[tacticInfo.index].position);
                     }
                 }

                 // NOTE(miha): Predators follows the group if it is
                 // resting/cruising state.
                 if(predator.state == State.Resting || predator.state == State.Cruising) {
                     // TODO(miha): Maybe implement some sort of diffrent rest
                     // tactic for the first few cycles of the rest time?
                     // if(predator.remainingRest > (predator.restTime - 10)) {
                     //     predator.speed += (float3)(Arrive(predator, tacticInfo.followGroup));
                     // }
                     // else {
                     //     // TODO(miha): Implement some sort of a wanderer behaviour!
                     //     predator.speed += (float3)(Arrive(predator, tacticInfo.followGroup));
                     // }

                     predator.speed += (float3)(Arrive(predator, tacticInfo.followGroup));
                     predator.remainingRest--;
                 }


                 if(predator.remainingRest == 0) {
                     // NOTE(miha): When predator finish resting, it can choose
                     // another target.
                     chooseNewTarget = true;

                     // TODO(miha): Fix this hacky way?
                     predator.remainingRest = -1;
                     predator.state = State.Cruising;
                 }

                if(controller.isActive)
                    controller.predatorCamera.position = predatorTranslation.Value;

            }).Run();

        positions.Dispose();
        fishes.Dispose();
    }
}
