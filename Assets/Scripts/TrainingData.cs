using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class TrainingData
{
    public float[] Inputs;
    public int Output;

    public TrainingData(float[] inputs, int output)
    {
        Inputs = inputs;
        Output = output;
    }
}
