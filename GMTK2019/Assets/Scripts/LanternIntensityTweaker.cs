using System;
using Player;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

public class LanternIntensityTweaker : MonoBehaviour
{
    public static LanternIntensityTweaker Light;
    
    public float minDistance = 10F;
    public float maxDistance = 30F;
    public float minLumen = 300F;
    public float maxLumen = 600F;
    public float heldLumen = 4F;
    
    [Header("Light flicker")]
    public float varyAmount = 2F;
    public float varyAmountHeld = 2F;
    public float varyFrequency = 2F;
    private float angle;

    private new HDAdditionalLightData light;
    private Light lightBasic;
    
    private void Awake()
    {
        Light = this;
        
        light = GetComponent<HDAdditionalLightData>();
        lightBasic = GetComponent<Light>();
    }

    private void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, LayerMask.GetMask("Ground")))
        {

            if (PlayerController.Instance.LanternHeld)
                light.intensity = heldLumen;
            else
                light.intensity = Utility.MathRemap(Mathf.Clamp(hit.distance, minDistance, maxDistance), minDistance, maxDistance, minLumen, maxLumen);

            angle += Time.deltaTime * varyFrequency;
            light.intensity += Mathf.Sin(angle) * (PlayerController.Instance.LanternHeld ? varyAmountHeld : varyAmount);

            if (angle > 2f * Mathf.PI)
                angle -= 2f * Mathf.PI;
        }
    }

    public float GetLightLevel()
        => LightUtils.ConvertPointLightLumenToCandela(light.intensity);

    public float GetLightRadius()
        => lightBasic.range;
}