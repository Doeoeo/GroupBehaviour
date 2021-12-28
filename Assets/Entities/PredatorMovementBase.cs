using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

/* SystemBase for adjusting fish positions
 *  TO DO:
 *      - Might be worth doing parallel. Check!
 */
// FishMovementBase must wait for velocities to be computed or we repeat previous move
[UpdateAfter(typeof(PredatorVelocityBase))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class PredatorMovementBase : SystemBase {
    protected int b = 0;
    protected override void OnUpdate() {
        float dt = 0.2f;
        Entities
            .WithReadOnly(dt)
            .ForEach((ref Translation translation, ref PredatorPropertiesComponent predatorProperties, ref Rotation rotation) => {

                predatorProperties.position += predatorProperties.speed * dt;
                translation.Value = predatorProperties.position;
                Debug.DrawLine(predatorProperties.position, predatorProperties.position + predatorProperties.speed, Color.red);
                rotation.Value = quaternion.LookRotation(predatorProperties.speed,Vector3.up);
        }).Run();
        

    }
}

[UpdateAfter(typeof(PredatorSTVelocityBase))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class PredatorSTMovementBase : SystemBase {
    protected override void OnUpdate() {
        float dt = 0.2f;
        Entities
            .WithReadOnly(dt)
            .ForEach((ref Translation translation, ref PredatorSTPropertiesComponent predatorProperties, ref Rotation rotation) => {

                predatorProperties.position += predatorProperties.speed * dt;
                translation.Value = predatorProperties.position;
                // Debug.DrawLine(predatorProperties.position, predatorProperties.position + predatorProperties.speed, Color.red);
                rotation.Value = quaternion.LookRotation(predatorProperties.speed,Vector3.up);
        }).Run();
        

    }
}
