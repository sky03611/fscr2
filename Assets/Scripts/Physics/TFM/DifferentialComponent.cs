using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifferentialComponent : MonoBehaviour
{
    [SerializeField] private float differentialRatio = 3.23f;
    [SerializeField] private bool diffLocked;
    private WheelControllerTFM[] wheelControllers;
    private float[] outputTorque = new float[2];
    private float inputShaftVelocity;
    private float angularVelocityL;
    private float angularVelocityR;
    
    public void InitializeDifferential(WheelControllerTFM[] _wheels)
    {
        wheelControllers = _wheels;
    } 
    public float[] GetOutputTorque(float inputTorque)
    {
        if (diffLocked)
        {
            angularVelocityL = wheelControllers[2].GetWheelAngularVelocity();
            angularVelocityR = wheelControllers[3].GetWheelAngularVelocity();
            var vel = (angularVelocityL - angularVelocityR) * 0.5f / Time.fixedDeltaTime * wheelControllers[0].GetWheelInertia();
            outputTorque[0] = (inputTorque * 0.5f * differentialRatio) - vel;
            outputTorque[1] = (inputTorque * 0.5f * differentialRatio) + vel;
            return outputTorque;
        }
        else
        {
            outputTorque[0] = inputTorque * differentialRatio * 0.5f;
            outputTorque[1] = inputTorque * differentialRatio * 0.5f;
            return outputTorque;
        }
    }

    public float GetInputShaftVelocity(float outputShaftVelocityLeft, float outputShaftVelocityRight)
    {
        inputShaftVelocity = (outputShaftVelocityLeft + outputShaftVelocityRight) * 0.5f * differentialRatio;
        return inputShaftVelocity;
    }
}
