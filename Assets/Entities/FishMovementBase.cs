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
[UpdateAfter(typeof(FishVelocityBase))]
public class FishMovementBase : SystemBase {
    protected int b = 0;
    protected override void OnUpdate() {
        float dt = Time.DeltaTime;
        Entities
            .WithReadOnly(dt)
            .ForEach((ref Translation translation, ref FishPropertiesComponent fishProperties, ref Rotation rotation) => {

                fishProperties.position += fishProperties.speed * dt;
                translation.Value = fishProperties.position;
                rotation.Value = quaternion.LookRotation(fishProperties.speed,Vector3.up);
        }).Run();
        

    }
}

