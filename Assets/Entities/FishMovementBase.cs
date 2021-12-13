using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

/* SystemBase for adjusting fish positions
 *  TO DO:
 *      - Might be worth doing parallel. Check!
 *  NEW:
 *      - Added [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] for fixed update calling
 *      - dt is now hardcoded to 0.2f because delta time is not very low (this is roughly equal to the old Time.deltaTIme)
 */
// FishMovementBase must wait for velocities to be computed or we repeat previous move
[UpdateAfter(typeof(FishVelocityBase))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FishMovementBase : SystemBase {
    protected int b = 0;

    protected override void OnUpdate() {
        float dt = 0.2f;
        Debug.Log("doing " + dt);
        Entities
            .WithReadOnly(dt)
            .ForEach((ref Translation translation, ref FishPropertiesComponent fishProperties, ref Rotation rotation) => {

                fishProperties.position += fishProperties.speed * dt;
                translation.Value = fishProperties.position;
                rotation.Value = quaternion.LookRotation(fishProperties.speed,Vector3.up);
        }).Run();
        

    }
}

