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
                    
                    predator.status = -2;
                //else we have the fish and swim towards it
                } else {
                    float3 speedToFish = positions[targetFishArrayIndex].position - predatorPosition;
                    //change the vector speed magnitude to max speed
                    predator.speed = ((Vector3) speedToFish).normalized * predator.vM;

                    //if the fish is less than lock on distance away from the fish, we go to find the most isolated fish
                    if (math.distance(predatorPosition, positions[targetFishArrayIndex].position) < predator.lockOnDistance) {
                        //we stop hunting the center fish and move to the status 0
                        predator.status = 0;
                    }
                }

            //status = 0 means the predator is finding a new target
            } else if (predator.status == 0) {
                
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
                    predator.status = -2;

                //else we have the fish and we eat it if we are close
                } else {
                    float3 speedToFish = positions[targetFishArrayIndex].position - predatorPosition;
                    //change the vector speed magnitude to max speed
                    predator.speed = ((Vector3) speedToFish).normalized * predator.vM;

                    //if the fish is less than 1 bl away from the fish, we say the prey ate the fish
                    if (math.distance(predatorPosition, positions[targetFishArrayIndex].position) < 0.1f) {
                        // Debug.Log("I caught the fish!");
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
    private FishAgentCreator controller;

    static uint seed = (uint) (UnityEngine.Random.value * 10000);
    static Unity.Mathematics.Random rnd = new Unity.Mathematics.Random(seed);
    static float c = rnd.NextFloat(360f);
    static float s = rnd.NextFloat(360f);

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

        if(distance < 1f)
        {
            float m = Mathf.Lerp(0, p.vM, distance);
            desired *= m;
        }
        else
        {
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
    }

    // NOTE(miha): returns preyCenter (struct of position and speed) and
    // minIndex (nearest fish at the current frame to the preddator).
    static SimpleTacticInfo nearestTactic(in PredatorSTPropertiesComponent predator, ref NativeArray<FishPropertiesComponent> positions) {
        SimpleTacticInfo result = default(SimpleTacticInfo);

        float minDistance = ((Vector3)positions[0].position - (Vector3)predator.position).magnitude;
        int minIndex = 0;
        float3 preyCenterPosition = new float3(0.0f, 0.0f, 0.0f);
        float3 preyCenterSpeed = new float3(0.0f, 0.0f, 0.0f);

        for(int i = 1; i < positions.Length; i++) {
            float blindAngle = Vector3.Angle((Vector3)predator.position, (Vector3)positions[i].position);
            bool preyInSight = (blindAngle > 60 || blindAngle < 120);

            float distance = ((Vector3)positions[i].position - (Vector3)predator.position).magnitude;


            if(preyInSight && distance < minDistance) {
                minDistance = distance;
                minIndex = i;
            }

            preyCenterPosition += positions[i].position;
            preyCenterSpeed += positions[i].speed;
        }

        preyCenterPosition /= positions.Length;
        preyCenterSpeed /= positions.Length;
        float3 followGroup = preyCenterPosition + (preyCenterSpeed * -10.0f);

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
            float blindAngle = Vector3.Angle((Vector3)predator.position, (Vector3)positions[i].position);
            bool preyInSight = (blindAngle > 60 || blindAngle < 120);
            float peripherality = positions[i].peripherality;
            float distance = ((Vector3)positions[i].position - (Vector3)predator.position).magnitude;

            if(preyInSight && peripherality < minPeripherality) {
                minPeripherality = peripherality;
                minIndex = i;
            }

            if(distance < minDistance)
                minDistance = distance;

            preyCenterPosition += positions[i].position;
            preyCenterSpeed += positions[i].speed;
        }

        preyCenterPosition /= positions.Length;
        preyCenterSpeed /= positions.Length;
        float3 followGroup = preyCenterPosition + (preyCenterSpeed * -10.0f);

        result.preyCenterPosition = preyCenterPosition;
        result.preyCenterSpeed = preyCenterSpeed;
        result.index = minIndex;
        result.distance = minDistance;
        result.followGroup = followGroup;
        
        return result;
    }

    static SimpleTacticInfo peripheralTactic(in PredatorSTPropertiesComponent predator, NativeArray<FishPropertiesComponent> positions) {
        SimpleTacticInfo result = default(SimpleTacticInfo);

        int maxIndex = 0;
        float maxPeripherality = positions[0].peripherality;
        float minDistance = ((Vector3)positions[0].position - (Vector3)predator.position).magnitude;
        float3 preyCenterPosition = new float3(0.0f, 0.0f, 0.0f);
        float3 preyCenterSpeed = new float3(0.0f, 0.0f, 0.0f);

        for(int i = 1; i < positions.Length; i++) {
            float peripherality = positions[i].peripherality;
            float distance = ((Vector3)positions[i].position - (Vector3)predator.position).magnitude;
            float blindAngle = Vector3.Angle(predator.speed, predator.position - positions[i].position);
            bool preyInSight = (blindAngle > 60 || blindAngle < 120);

            if(preyInSight && peripherality > maxPeripherality) {
                Debug.DrawLine(predator.position, positions[i].position, Color.green);
                Debug.Log("ANGLE: " + blindAngle);
                maxPeripherality = peripherality;
                maxIndex = i;
            }

            if(distance < minDistance)
                minDistance = distance;

            preyCenterPosition += positions[i].position;
            preyCenterSpeed += positions[i].speed;
        }

        preyCenterPosition /= positions.Length;
        preyCenterSpeed /= positions.Length;
        Debug.DrawLine(predator.position, preyCenterPosition, Color.green);
        float3 followGroup = preyCenterPosition + (preyCenterSpeed * -10.0f);

        result.preyCenterPosition = preyCenterPosition;
        result.preyCenterSpeed = preyCenterSpeed;
        result.index = maxIndex;
        result.distance = minDistance;
        result.followGroup = followGroup;
        
        return result;
    }

    protected override void OnUpdate() {
            //Query to get all fish components
            m_Group = GetEntityQuery(ComponentType.ReadOnly<FishPropertiesComponent>());
            NativeArray <FishPropertiesComponent> positions = m_Group.ToComponentDataArray<FishPropertiesComponent>(Allocator.TempJob);
            //NativeArray <Entity> fishes = m_Group.ToEntityArray(Allocator.TempJob);

            //main for each for all predators
            Entities.WithAll<PredatorSTPropertiesComponent>()
                .WithoutBurst()
                .WithReadOnly(positions)
                .WithNativeDisableContainerSafetyRestriction(positions)
                .ForEach((Entity selectedEntity, ref Translation predatorTranslation, 
                          ref PredatorSTPropertiesComponent predator) => {
                     float3 predatorPosition = new float3(predatorTranslation.Value);

                     if(!controller)
                        controller = FishAgentCreator.Instance;

                     SimpleTacticInfo tacticInfo = default(SimpleTacticInfo);
                     // NOTE(miha): Choose which tactit predator will use.
                     if(controller.simpleTactic == SimpleTactic.Nearest) {
                         tacticInfo = nearestTactic(predator, ref positions);
                     }
                     if(controller.simpleTactic == SimpleTactic.Center) {
                         tacticInfo = centerTactic(predator, positions);
                     }
                     if(controller.simpleTactic == SimpleTactic.Peripheral) {
                         tacticInfo = peripheralTactic(predator, positions);
                     }
                     Debug.DrawLine(predator.position, tacticInfo.preyCenterPosition, Color.green);

                     Debug.DrawLine(predator.position, (Quaternion.Euler(0, 0, -30) * (Vector3)predator.speed) * 10f, Color.green);
                     Debug.DrawLine(predator.position, (Quaternion.Euler(0, 0, 30) * (Vector3)predator.speed) * 10f, Color.green);
                     if(predator.state != State.Resting) {
                         if(tacticInfo.distance < 5.0f) {
                             predator.state = State.Hunting;
                         }
                         else {
                             predator.state = State.Cruising;
                         }
                     }

                     if(predator.state == State.Hunting) {
                         Debug.DrawLine(predator.position, positions[tacticInfo.index].position, Color.red);

                         if(tacticInfo.distance < predator.catchDistance) {
                             predator.fishToEat = tacticInfo.index;
                             predator.remainingRest = predator.restTime;
                             predator.state = State.Resting;
                         }

                         predator.nearestFish = tacticInfo.index;
                         float3 speedToFish = positions[tacticInfo.index].position - predator.position;
                         predator.speed += (float3) Seek(predator, positions[tacticInfo.index].position);
                     }

                     // NOTE(miha): Predators follows the group if it is
                     // resting/cruising state.
                     if(predator.state == State.Resting || predator.state == State.Cruising) {
                         predator.speed += (float3)(Arrive(predator, tacticInfo.followGroup));
                         predator.remainingRest--;
                     }

                     if(predator.remainingRest == 0)
                         predator.state = State.Cruising;

                }).Run();

            //Query to get predator components
            EntityQuery m_Group_p = GetEntityQuery(ComponentType.ReadOnly<PredatorSTPropertiesComponent>());
            NativeArray <PredatorSTPropertiesComponent> predatorPositions = m_Group_p.ToComponentDataArray<PredatorSTPropertiesComponent>(Allocator.TempJob);

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
