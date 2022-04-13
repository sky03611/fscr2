using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteeringComponent : MonoBehaviour
{
    private float wheelBase;
    private float rearTrack;
    [SerializeField] private float turnRadius;
    private float ackermannAngleL;
    private float ackermannAngleR;
    private float[] steerAngle = new float[2];
    [SerializeField] private float steerForce = 2f;

    public void InitializeSteering(WheelControllerTFM[] wheelTransform)
    {
        InitializeAckermannParams(wheelTransform);
    }

    // Update is called once per frame
    public void PhysicsUpdate(float input)
    {
        CalculateAckermann(input);
    }

    private void InitializeAckermannParams(WheelControllerTFM[] wheelTransform)
    {
         wheelBase = Vector3.Distance(wheelTransform[0].transform.position, wheelTransform[2].transform.position);
         rearTrack = Vector3.Distance(wheelTransform[2].transform.position, wheelTransform[3].transform.position);  
    }

    private void CalculateAckermann(float inputData)
    {
        if (inputData > 0)
        {
            ackermannAngleL = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + rearTrack / 2)) * inputData * steerForce;
            ackermannAngleR = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - rearTrack / 2)) * inputData * steerForce;
        }
        else if (inputData < 0)
        {
            ackermannAngleL = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - rearTrack / 2)) * inputData * steerForce;
            ackermannAngleR = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + rearTrack / 2)) * inputData * steerForce;
        }
        else
        {
            ackermannAngleL = 0f;
            ackermannAngleR = 0f;
        }
        steerAngle[0] = ackermannAngleL;
        steerAngle[1] = ackermannAngleR;
    }

    public float[] GetSteerAngles()
    {
        return steerAngle;
    }
}
