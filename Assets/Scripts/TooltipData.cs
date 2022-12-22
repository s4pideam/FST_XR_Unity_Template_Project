using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TooltipData : MonoBehaviour
{
    public string metrik;
    public String fileName;
    public float value;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public String show()
    {
        return fileName + "\n \n" + metrik + ": " + value;
    }
}
