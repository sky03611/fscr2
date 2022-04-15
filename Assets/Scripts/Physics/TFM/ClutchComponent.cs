using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClutchComponent : MonoBehaviour
{
    [SerializeField] private float clutchStiffness = 30f;
    [SerializeField] private float clutchCapacity = 1.3f;
    [SerializeField] private float engineMaxTorque = 300f;
    [Range(0.1f,0.99f)]
    [SerializeField] private float clutchDamping;
    private EngineComponent engine;
    private float clutchTorque;
    private float clutchMaxTorque;
    private float outputShaftVelocity;
    private float engineAngularVelocity;
    private float gearBoxRatio;
    private float radToRpm;
    private float rpmToRad;
    private float clutchLock;

    public void InitializeClutch()
    {
        rpmToRad = Mathf.PI * 2 / 60;
        radToRpm = 1 / rpmToRad;
        clutchMaxTorque = engineMaxTorque * clutchCapacity;
        engine = GetComponent<EngineComponent>();
    }

    public void UpdatePhysics(float _outputShaftVelocity, float _engineAngularVelocity, float _gearBoxRatio)
    {
        outputShaftVelocity = _outputShaftVelocity;
        engineAngularVelocity = _engineAngularVelocity;
        gearBoxRatio = _gearBoxRatio;
        ClutchTorque();
        //SimpleClutchTorque();
    }

    public void ClutchTorque()
    {
        var clutchSlip = (engineAngularVelocity - outputShaftVelocity) * Mathf.Abs(Mathf.Sign(gearBoxRatio));
        clutchLock = gearBoxRatio == 0 ? 0: MapRangeClamped(engineAngularVelocity * radToRpm, 1000, 1300, 0, 1);
        var clt = Mathf.Clamp(clutchSlip * clutchLock * clutchStiffness, -clutchMaxTorque, clutchMaxTorque);
        clutchTorque = clt + ((clutchTorque - clt) * clutchDamping);
    }


    private float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB)
    {
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }

    public float GetLock()
    {
        return clutchLock;
    }

    public float GetClutchTorque()
    {
        return clutchTorque;
    }

    public float GetToEngine()
    {
        return outputShaftVelocity * 2 * Mathf.Abs(Mathf.Sign(gearBoxRatio));
    }


}
