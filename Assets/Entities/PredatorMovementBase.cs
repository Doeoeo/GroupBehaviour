using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

/* SystemBase for adjusting fish positions
 *  TO DO:
 *      - Might be worth doing parallel. Check!
 */
// FishMovementBase must wait for velocities to be computed or we repeat previous move
[UpdateAfter(typeof(PredatorVelocityBase))]
public class PredatorMovementBase : SystemBase {
    protected int b = 0;
    protected override void OnUpdate() {
        float dt = Time.DeltaTime;
        Entities
            .WithReadOnly(dt)
            .ForEach((ref Translation translation, ref PredatorPropertiesComponent predatorProperties) => {

                predatorProperties.position += predatorProperties.speed * dt;
                translation.Value = predatorProperties.position;
                Debug.DrawLine(predatorProperties.position, predatorProperties.position + predatorProperties.speed, Color.red);
        }).Run();
        

    }
}

