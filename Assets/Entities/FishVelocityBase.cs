using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;



/* SystemBase for adjusting fish velocities 
 *  TO DO: 
 *      - This can probably be parallel
 *      - When predator is added uncoment its contribution
 *      - Should probably fix the hardcoded roosting radius 
 *      - Roosting area check could probably be moved to FishDriveBase
 *  NEW:
 *      - Added [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] for fixed update calling
 *      - dt is now hardcoded to 0.2f because delta time is not very low (this is roughly equal to the old Time.deltaTIme)
 */

// FishVelocityBase has to wait for drives to be computed or we repeat previous state
[UpdateAfter(typeof(FishDriveBase))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FishVelocityBase : SystemBase {
    static uint seed = (uint) (UnityEngine.Random.value * 10000);
    static Unity.Mathematics.Random rnd = new Unity.Mathematics.Random(seed);
    private FishAgentCreator controller;

    protected int b = 0;
    protected override void OnUpdate() {
        if (!controller) {
            controller = FishAgentCreator.Instance;
        }

        float dt = 0.2f;
        Entities
            .WithoutBurst()
            .WithAll<FishPropertiesComponent>()
            .ForEach((ref Translation position, ref FishPropertiesComponent fishProperties) => {
                // This segment checks if the agent is out of roosting area and adds a border drive component
                // with a direction parallel to its current speed wighted by distance and angle from the roosting area
                // The ange ensures that the agent doesn't spin in circles
                float distance = ((Vector3)position.Value).magnitude;

                fishProperties.sW = controller.seperationWeight;
                fishProperties.aW = controller.alignmentWeight;
                fishProperties.cW = controller.cohesionWeight;
                fishProperties.eW = controller.escapeWeight;
                fishProperties.mA = controller.maxAcceleration;
                fishProperties.vM = controller.maxVelocity * fishProperties.len;
                fishProperties.vC = controller.cruisingVelocity * fishProperties.len;

                float v = controller.borderRadius;
                if (distance > v) {
                    // Vector3.forward shoud change to something else for 3D :)
                    fishProperties.bD = Vector3.Cross(fishProperties.speed, Vector3.forward).normalized;
                    fishProperties.bW = math.pow(distance - v, 2) * math.sin(math.min(math.radians(Vector3.Angle(-fishProperties.position, fishProperties.speed)), math.PI / 2));
                } 
                else {
                    fishProperties.bW = 0;
                }

                // Adjust acceleration and add it to agent's velocity 
                float3 a = fishProperties.sW * fishProperties.sD + fishProperties.aW * fishProperties.aD + fishProperties.cW * fishProperties.cD
                         + fishProperties.bW * fishProperties.bD + fishProperties.eW * fishProperties.eD;
                fishProperties.speed += a * dt;

                // Cut the speed to agent's maximum speed
                if(((Vector3)fishProperties.speed).magnitude > fishProperties.vM) {
                    fishProperties.speed = ((float3)((Vector3)fishProperties.speed).normalized) * fishProperties.vM;
                }

                // Draw all forces affecting the agent. With wrong Unity settings this can slow performance
                if(controller.fishDebug) {
                    Debug.DrawLine(fishProperties.position, fishProperties.position + fishProperties.bD * fishProperties.bW, Color.blue);
                    Debug.DrawLine(fishProperties.position, fishProperties.position + fishProperties.sD, Color.red);
                    Debug.DrawLine(fishProperties.position, fishProperties.position + fishProperties.aD, Color.green);
                    Debug.DrawLine(fishProperties.position, fishProperties.position + fishProperties.cD, Color.yellow);
                }
            }).Run();

    }
}
