using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

[UpdateAfter(typeof(PredatorVelocityBase))]
public class PredatorMovementBase : SystemBase {
    
    protected override void OnUpdate() {
        float dt = Time.DeltaTime;
        Entities
            .WithReadOnly(dt)
            .ForEach((ref Translation translation, ref PredatorPropertiesComponent predatorProperties) => {

                predatorProperties.position += predatorProperties.speed * dt;
                translation.Value = predatorProperties.position;
        }).Run();
        
    }
}

