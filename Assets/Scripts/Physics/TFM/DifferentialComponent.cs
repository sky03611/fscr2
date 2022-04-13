using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifferentialComponent : MonoBehaviour
{
    [SerializeField] private float differentialRatio = 3.23f;
    private float[] outputTorque = new float[2];
    private float inputShaftVelocity;
    
    public float[] GetOutputTorque(float inputTorque)
    {
         outputTorque[0] = inputTorque * differentialRatio * 0.5f;
         outputTorque[1] = inputTorque * differentialRatio * 0.5f;
        return outputTorque;
    }

    public float GetInputShaftVelocity(float outputShaftVelocityLeft, float outputShaftVelocityRight)
    {
        inputShaftVelocity = (outputShaftVelocityLeft + outputShaftVelocityRight) * 0.5f * differentialRatio;
        return inputShaftVelocity;
    }
}
