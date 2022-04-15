using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraComputations : MonoBehaviour
{
    [Range(2, 1000000)]
    public int computations;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float num = 0;
        for (int i = 0; i < computations; i++)
        {
            num++;
            num /= 2;
            num *= 2;
        }
    }
}
