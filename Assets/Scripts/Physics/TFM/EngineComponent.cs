using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineComponent : MonoBehaviour
{
    [SerializeField] private float engineAcceleration;
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField] private float startFriction = 50f;
    [SerializeField] private float frictionCoefficient = 0.02f;
    [SerializeField] private float engineInertia = 0.2f;
    [SerializeField] private float engineIdleRpm;
    [SerializeField] private float engineMaxRpm;
    [SerializeField] private float engineMul = 2f;
    private float rpmToRad;
    private float radToRpm;
    private float maxEffectiveTorque;
    private float engineRpm;
    private float engineFriction;
    private float engineTorque;
    private float engineAngularVelocity;


    public void InitializeEngine()
    {
        rpmToRad = Mathf.PI * 2 / 60;
        radToRpm = 1 / rpmToRad;
        engineIdleRpm *= rpmToRad;
        engineMaxRpm *= rpmToRad;
        engineAngularVelocity = 100;
    }

    public void PhysicsUpdate(float dt, float input, float loadTorque)
    {
        maxEffectiveTorque = torqueCurve.Evaluate(engineRpm) * engineMul;
        engineFriction = (engineRpm * frictionCoefficient) + startFriction;
        engineTorque = maxEffectiveTorque * input - engineFriction ;
        engineAcceleration = (engineTorque - loadTorque) / engineInertia ;
        engineAngularVelocity += engineAcceleration * dt;
        engineRpm = engineAngularVelocity * radToRpm;
        engineAngularVelocity = Mathf.Clamp(engineAngularVelocity, engineIdleRpm, engineMaxRpm);

    }

    public float GetAngularVelocity()
    {
        return engineAngularVelocity;
    }

    public float GetRpm()
    {
        return engineRpm;
    }

    public float GetTorque()
    {
        return engineTorque;
    }
}
