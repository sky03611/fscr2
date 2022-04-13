using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrakesComponent : MonoBehaviour
{
    private float[] brakeTorque = new float[2];
    [SerializeField] private float[] brakeBias = new float[2];
    [SerializeField] private AnimationCurve brakeTorqueCurve;
    [SerializeField] private float maxTorque = 2000f;
    public float[] GetBrakes(float brakeInput, float[] angularVelocities)
    {
        brakeTorque[0] = -brakeInput * brakeBias[0] * maxTorque * brakeTorqueCurve.Evaluate(Mathf.Abs((angularVelocities[0] + angularVelocities[2]) * 0.5f));
        brakeTorque[1] = -brakeInput * brakeBias[1] * maxTorque * brakeTorqueCurve.Evaluate(Mathf.Abs((angularVelocities[1] + angularVelocities[3]) * 0.5f));
        return brakeTorque;

    }
}
