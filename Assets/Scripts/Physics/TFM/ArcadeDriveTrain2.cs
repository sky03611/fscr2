using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcadeDriveTrain2 : MonoBehaviour
{
    private float engineAcceleration;
    [Header("Engine Settings")]
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField] private float engineMultiplier;
    [SerializeField] private float startFriction = 50f;
    [SerializeField] private float frictionCoefficient = 0.02f;
    [SerializeField] private float engineInertia = 0.2f;
    [SerializeField] private float engineIdleRpm;
    [SerializeField] private float engineMaxRpm;
    [SerializeField] private float clutchedEngineTorqueMultiplier = 100f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private Vector3 sEngineOrientation = Vector3.right;
    private float clutchNPEngineRpm;
    private float clutchCPEngineRpm;
    private float dt;
    private float rpmToRad;
    private float radToRpm;
    private float engineRpm;
    private float engineFriction;
    private float engineTorque;
    private float driveTorque;
    private float throttle;
    [Header("GearBox and Diff")]
    [SerializeField] private float clutch;
    [SerializeField] private float[] gearboxRatio;
    [SerializeField] private float shiftTime = 0.2f;
    [SerializeField] private bool diffLocked;
    [SerializeField] private float differentialRatio = 3.23f;
    [SerializeField] private bool autoShifter;
    private float[] angularVelocity = new float[2];
    private float[] outputTorque = new float[2];
    private int currentGear = 1;
    private bool inGear = true;
    private int nextGear = 1;
    private float transmissionTorque;
    private float accel;
    private Rigidbody rb;

    [Header("Other")]
    private float wheelInertia;

    public void InitializeArcadeDT(float _wheelInertia, Rigidbody _rb)
    {
        rb = _rb;
        rpmToRad = Mathf.PI * 2 / 60;
        radToRpm = 1 / rpmToRad;
        wheelInertia = _wheelInertia;
    }

    public void PhysicsUpdate(float _dt, float input, float wheelVelocityL, float wheelVelocityR, float clutchInput)
    {
        angularVelocity[0] = wheelVelocityL;
        angularVelocity[1] = wheelVelocityR;
        dt = _dt;
        //Data from wheels for engine
        var angularData = GetDifferentialInputShaftVelocity(wheelVelocityL, wheelVelocityR) * gearboxRatio[currentGear] * differentialRatio;
        CalculateEngine(input, angularData);
        TransmissionTorque();
        AutomaticShifter();

    }

    private void CalculateEngine(float input, float diffShaftSpeed)
    {
        if (gearboxRatio[currentGear] == 0)
        {
            clutch = 1f;
            rb.AddTorque(-sEngineOrientation * engineTorque * 3);
        }
        else
        {
            clutch = MapRangeClamped(engineRpm, 1000f, 1300f, 1f, 0f);
        }
        float torqueMult = MapRangeClamped(engineRpm, 5500f, engineMaxRpm + 100f, 0, -torqueCurve.Evaluate(engineRpm) * engineMultiplier);
        float vel = 0;
        if (!inGear)
        {
            throttle = 0;
        }
        else
        {
            throttle = input;
        }
        engineFriction = (engineRpm * frictionCoefficient) + startFriction;
        float engineFriction2 = (accel * frictionCoefficient) + startFriction;
        clutchNPEngineRpm = diffShaftSpeed *  radToRpm;
        //accel += ((engineTorque - engineFriction) / engineInertia * Time.fixedDeltaTime)* radToRpm;
        accel = (engineTorque * clutchedEngineTorqueMultiplier)/engineInertia + torqueMult;
        clutchCPEngineRpm =  accel + engineRpm;
        var engineRpmTmp = (clutch * clutchCPEngineRpm + (1 - clutch) * clutchNPEngineRpm) ;
        engineRpm = Mathf.SmoothDamp(engineRpm, 150f + engineRpmTmp, ref vel, smoothTime);
        engineRpm -= engineFriction;
        //engineRpm -= engineFriction;
        engineRpm = Mathf.Clamp(engineRpm, engineIdleRpm, engineMaxRpm);
        engineTorque = throttle * torqueCurve.Evaluate(engineRpm) * engineMultiplier;
        driveTorque = (1 - clutch) * engineTorque + torqueMult;
        
    }

    private void TransmissionTorque()
    {
        transmissionTorque = driveTorque * gearboxRatio[currentGear];
    }

    private void AutomaticShifter()
    {
        if (!autoShifter) return;
        if(engineRpm >= engineMaxRpm - 300f && inGear)
        {
            ChangeGearUp();
        }
        if(engineRpm<=2500 && inGear && currentGear > 2)
        {
            ChangeGearDown();
        }
    }

    public void ChangeGearUp()
    {
        if (inGear && currentGear < gearboxRatio.Length - 1)
        {

            nextGear++;
            StartCoroutine(GearChange(nextGear, shiftTime));
        }
        else
        {
            currentGear = nextGear;
        }
    }

    public void ChangeGearDown()
    {
        if (inGear && currentGear != 0)
        {
            if (currentGear != 0)
            {
                nextGear--;
                StartCoroutine(GearChange(nextGear, shiftTime));
            }
            else
            {
                currentGear = nextGear;
            }
        }
    }

    IEnumerator GearChange(int _nextGear, float shiftTime)
    {
        inGear = false;
        currentGear = 1; //Sets to neutral for shfitTime seconds
        yield return new WaitForSeconds(shiftTime);
        currentGear = _nextGear;
        inGear = true;
    }

    public float GetDifferentialInputShaftVelocity(float outputShaftVelocityLeft, float outputShaftVelocityRight)
    {
        var inputShaftVelocity = (outputShaftVelocityLeft + outputShaftVelocityRight) * 0.5f * differentialRatio ;
        return inputShaftVelocity;
    }

    public float[] GetDifferentialTorque()
    {

        if (diffLocked)
        {
            var vel = (angularVelocity[0] - angularVelocity[1]) * 0.5f / Time.fixedDeltaTime * wheelInertia;
            outputTorque[0] = (transmissionTorque * 0.5f * differentialRatio) - vel;
            outputTorque[1] = (transmissionTorque * 0.5f * differentialRatio) + vel;
            return outputTorque;
        }
        else
        {
            outputTorque[0] = transmissionTorque * 0.5f * differentialRatio;
            outputTorque[1] = transmissionTorque * 0.5f * differentialRatio;
            return outputTorque;
        }
    }
    

    public float GetRpm()
    {
        
        return engineRpm;
    }

    public int GetCurrentGear()
    {
        return currentGear;
    }


    private float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB)
    {
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }
}
