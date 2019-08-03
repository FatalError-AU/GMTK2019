using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

public class LanternIntensityTweaker : MonoBehaviour
{
    public float minDistance = 10F;
    public float maxDistance = 30F;
    public float minLumen = 300F;
    public float maxLumen = 600F;
    
    [Header("Light flicker")]
    public float varyAmount = 2F;
    public float varyFrequency = 2F;
    private float angle;

    private new HDAdditionalLightData light;
    
    private void Awake()
    {
        light = GetComponent<HDAdditionalLightData>();
    }

    private void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, LayerMask.GetMask("Ground")))
        {
            light.intensity = Utility.MathRemap(Mathf.Clamp(hit.distance, minDistance, maxDistance), minDistance, maxDistance, minLumen, maxLumen);

            angle += Time.deltaTime * varyFrequency;
            light.intensity += Mathf.Sin(angle) * varyAmount;

            if (angle > 2f * Mathf.PI)
                angle -= 2f * Mathf.PI;
        }
    }
}