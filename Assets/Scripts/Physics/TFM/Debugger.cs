using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Debugger : MonoBehaviour
{
    [SerializeField] private TMP_Text[] texts;
    

    public void Line1(float data)
    {
        texts[0].text = "EngineRpm = " + data.ToString();
    }

    public void Line2(float data)
    {
        texts[1].text = "Clutch Torque = " + data.ToString();
    }

    public void Line3(float data)
    {
        texts[2].text = "Clutch Lock = " + data.ToString();
    }

    public void Line4(float data)
    {
        if (data == 0)
        {
            texts[3].text = "CurrentGear = " + "R";
        }
        else if (data == 1)
        {
            texts[3].text = "CurrentGear = " + "N";
        }
        else
        {
            texts[3].text = "CurrentGear = " + (data - 1).ToString();
        }
        

    }

    public void Line5(float data)
    {
        texts[4].text = "Spd = " + data.ToString() + " KM/H";
    }
}
