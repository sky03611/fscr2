using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField] Debugger debug;
    [SerializeField] WheelControllerTFM[] wheelControllers;
    [SerializeField] EngineComponent engine;
    [SerializeField] SteeringComponent steering;
    [SerializeField] GearBoxComponent gearBox;
    [SerializeField] ClutchComponent clutchComponent;
    [SerializeField] DifferentialComponent differential;
    [SerializeField] BrakesComponent brakes;
    [SerializeField] ArcadeDriveTrain2 arcadeDriveTrain;
    [SerializeField] AntiRollBarComponent antirollBar;
    [SerializeField] Dashboard dashboard;
    [SerializeField] KeyCode shiftUpBtn;
    [SerializeField] KeyCode shiftDownBtn;
    [SerializeField] KeyCode clutchBtn;
    [SerializeField] KeyCode handBrakeBtn;
    enum DriveTrainType { Arcade, Sim };
    [SerializeField] DriveTrainType driveTrainType;
    private Rigidbody rb;
    private float downForce;
    private float deltaTime;
    private float inputThrottle;
    private float inputBrakes;
    private float inputSteering;
    private float angleL;
    private float angleR;
    private float clutch;
    private float btR; // brake torque rear
    private bool handBrake;
    private float[] wheelTorque = new float[2];
    private float[] angularVelocities = new float[4];


    private void Awake()
    {
        rb = transform.root.GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        engine.InitializeEngine();
        steering.InitializeSteering(wheelControllers);
        clutchComponent.InitializeClutch();
        arcadeDriveTrain.InitializeArcadeDT(wheelControllers[0].GetWheelInertia(), rb);
        dashboard.InitDashboard(rb, 10000f);
        antirollBar.InitializeAntirollBar(wheelControllers);
        differential.InitializeDifferential(wheelControllers);
    }

    private void Update()
    {
        if (driveTrainType == DriveTrainType.Sim) 
        {
            GearBoxShifterSim();
            dashboard.UpdateD(engine.GetRpm());
        }
        else 
        {
            GearBoxShifterArcade();
            dashboard.UpdateD(arcadeDriveTrain.GetRpm());
        }
    }

    private void GearBoxShifterArcade()
    {
        //ShiftUp
        if (Input.GetKeyDown(shiftUpBtn))
        {
            arcadeDriveTrain.ChangeGearUp();
        }
        //ShiftDown
        if (Input.GetKeyDown(shiftDownBtn))
        {
            arcadeDriveTrain.ChangeGearDown();
        }
    }

    private void GearBoxShifterSim()
    {
        //ShiftUp
        if (Input.GetKeyDown(shiftUpBtn))
        {
            gearBox.ChangeGearUp();
        }
        //ShiftDown
        if (Input.GetKeyDown(shiftDownBtn))
        {
            gearBox.ChangeGearDown();
        }
    }

    private void FixedUpdate()
    {
        deltaTime = Time.fixedDeltaTime;
        InputUpdate();
        UpdatePhysics();
    }

    private void InputUpdate()
    {
        inputThrottle = Input.GetAxis("Vertical") < 0 ? 0 : Input.GetAxis("Vertical");
        inputBrakes = Input.GetAxis("Vertical") > 0 ? 0 : Input.GetAxis("Vertical");
        inputSteering = Input.GetAxis("Horizontal");
        if (Input.GetKey(clutchBtn))
        {
            clutch = Mathf.Lerp(clutch, 1, Time.deltaTime);
        }
        else
        {
            clutch = Mathf.Lerp(clutch, 0, Time.deltaTime); ;
        }
        if (Input.GetKey(handBrakeBtn))
        {
            handBrake = true;
        }
        else 
        {
            handBrake = false;
        }
    }

    private void UpdatePhysics()
    {
        debug.Line5(rb.velocity.magnitude * 3.6f);
        UpdateSteering();
        UpdateDownForce();
        antirollBar.CalculateAntirollBar();
        if (driveTrainType == DriveTrainType.Sim)
        {
            SimDriveTrain();
            debug.Line1(engine.GetRpm());
            debug.Line2(clutchComponent.GetClutchTorque());
            debug.Line3(clutchComponent.GetLock());
            debug.Line4(gearBox.GetCurrentGear());
        }
        else
        {
            ArcadeDriveTrain();
            debug.Line1(arcadeDriveTrain.GetRpm());
            debug.Line4(arcadeDriveTrain.GetCurrentGear());
        }
    }

    private void ArcadeDriveTrain()
    {
        var whAVFL = wheelControllers[0].GetWheelAngularVelocity();
        var whAVFR = wheelControllers[1].GetWheelAngularVelocity();
        var whAVRL = wheelControllers[2].GetWheelAngularVelocity();
        var whAVRR = wheelControllers[3].GetWheelAngularVelocity();
        angularVelocities[0] = whAVFL;
        angularVelocities[1] = whAVFR;
        angularVelocities[2] = whAVRL;
        angularVelocities[3] = whAVRR;
        if (inputThrottle < 0.2f && Input.GetAxis("Vertical") >= 0 && inputBrakes <-0.3f) // dirty cheat
        {
            inputBrakes = -0.2f;
        }
        
        UpdateWheels(wheelTorque, brakes.GetBrakes(inputBrakes, angularVelocities));
        arcadeDriveTrain.PhysicsUpdate(deltaTime, inputThrottle, whAVRL, whAVRR, clutch);
        wheelTorque = arcadeDriveTrain.GetDifferentialTorque();
    }

    private void SimDriveTrain()
    {
        var gbxTorque = gearBox.GetOutputTorque(clutchComponent.GetClutchTorque());
        wheelTorque = differential.GetOutputTorque(gbxTorque);
        UpdateWheels(wheelTorque, brakes.GetBrakes(inputBrakes, angularVelocities));
        var whAVL = wheelControllers[2].GetWheelAngularVelocity();
        var whAVR = wheelControllers[3].GetWheelAngularVelocity();
        var dInputShaftVel = differential.GetInputShaftVelocity(whAVL, whAVR);
        var gBoxInShaftVel = gearBox.GetInputShaftVelocity(dInputShaftVel);
        clutchComponent.UpdatePhysics(gBoxInShaftVel, engine.GetAngularVelocity(), gearBox.GetGearBoxRatio());
        engine.PhysicsUpdate(deltaTime, inputThrottle, clutchComponent.GetClutchTorque());
    }

    private void UpdateSteering()
    {
        steering.PhysicsUpdate(inputSteering);
        angleL = steering.GetSteerAngles()[0];
        angleR = steering.GetSteerAngles()[1];
        wheelControllers[0].Steering(angleL);
        wheelControllers[1].Steering(angleR);
    }

    private void UpdateDownForce()
    {
        //Credits BlinkAChu
        Vector3 linearVelocity = transform.InverseTransformDirection(rb.GetPointVelocity(transform.position));
        downForce = 0.5f * 1.22f * Mathf.Pow((Mathf.Max(0, linearVelocity.z)), 2) * (5f * 2f);
        rb.AddForceAtPosition(-transform.up * downForce, transform.position);
    }

    private void UpdateWheels(float[] _driveTorque, float[] _brakeTorque)
    {
        if(handBrake)
        {
            btR = Mathf.Lerp(btR, 5000f, Time.deltaTime * 8f);
        }
        else
        {
            btR = _brakeTorque[1];
        }
        wheelControllers[0].PhysicsUpdate(0, _brakeTorque[0], deltaTime);
        wheelControllers[1].PhysicsUpdate(0, _brakeTorque[0], deltaTime);
        wheelControllers[2].PhysicsUpdate(_driveTorque[0], btR, deltaTime);
        wheelControllers[3].PhysicsUpdate(_driveTorque[1], btR, deltaTime);
    }

    public WheelControllerTFM[] GetWheels()
    {
        return wheelControllers;
    }
}
