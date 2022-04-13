using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearBoxComponent : MonoBehaviour
{

    [SerializeField] private float[] gearboxRatio;
    [SerializeField] private float shiftTime;
    private int currentGear = 1;
    private bool inGear = true;
    private int nextGear = 1;

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

    public float GetOutputTorque(float inputTorque)
    {
        float outputTorque = inputTorque * gearboxRatio[currentGear];
        return outputTorque; 
    }

    public float GetInputShaftVelocity(float outputShaftVelocity)
    {
        float inputShaftVelocity = outputShaftVelocity * gearboxRatio[currentGear];
        return inputShaftVelocity;
    }

    IEnumerator GearChange(int _nextGear, float shiftTime)
    {
        inGear = false;
        currentGear = 1; //Sets to neutral for shfitTime seconds
        yield return new WaitForSeconds(shiftTime);
        currentGear = _nextGear;
        inGear = true;
    }

    public float GetGearBoxRatio()
    {
        return gearboxRatio[currentGear];
    }

    public int GetCurrentGear()
    {
        return currentGear;
    }
}
