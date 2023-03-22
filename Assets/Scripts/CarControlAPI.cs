using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControlAPI : MonoBehaviour
{
    // Start is called before the first frame update

    #region Variables
    PrometeoCarController controlScript;

    public TrainingData[] trainingDataList = { };

    public bool perceptronPre, perceptronTrained;

    public float[] sensorDistances;


    public float sum;
    public float[] preSetWeights = { 1.5f, 1, 1, 1, 0.1f, -1, -1, -1, -1.5f };
    public float[] trainedWeights = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    public int maxIterations = 10;





    #endregion

    #region Built In
    void Awake()
    {
        controlScript = GetComponent<PrometeoCarController>();
    }

    private void Start()
    {
        if (perceptronTrained)
        {
            TrainPerceptron();
        }
    }

    // Update is called once per frame
    void Update()
    {
        float[] sensorAngles = { -70, -45, -20, -5, 0, 5, 20, 45, 70 };
        sensorDistances = GetSensorDistances(sensorAngles);
        if (perceptronPre)
        {
            PerceptronMovement(sensorDistances, preSetWeights);
        }
        else if (perceptronTrained)
        {
            PerceptronMovement(sensorDistances, trainedWeights);
        }
    }

    float[] GetSensorDistances(float[] sensorAngles)
    {
        float[] distances = new float[sensorAngles.Length];

        for (int i = 0; i < sensorAngles.Length; i++)
        {
            distances[i] = (float)System.Math.Round(1 / Raycast(sensorAngles[i]), 2);
        }

        return distances;
    }

    void TrainPerceptron()
    {
        trainingDataList = new TrainingData[]{
            new TrainingData(new float[]{0.09f, 0.06f, 0.03f, 0.07f, 0.07f, 0.07f, 0.06f, 0.09f, 0.11f}, -1),
            new TrainingData(new float[]{0.12f, 0.11f, 0.08f, 0.06f, 0.05f, 0.04f, 0.03f, 0.07f, 0.07f}, 1),
            new TrainingData(new float[]{0.07f, 0.03f, 0.07f, 0.07f, 0.07f, 0.06f, 0.08f, 0.11f, 0.12f}, -1),
            new TrainingData(new float[]{0.12f, 0.11f, 0.08f, 0.05f, 0.04f, 0.03f, 0.03f, 0.07f, 0.07f}, 1),
            new TrainingData(new float[]{0.03f, 0.08f, 0.07f, 0.1f, 0.11f, 0.12f, 0.14f, 0.14f, 0.12f}, -1),
            new TrainingData(new float[]{0.11f, 0.07f, 0.09f, 0.09f, 0.02f, 0.04f, 0.04f, 0.07f, 0.09f}, 1),
            new TrainingData(new float[]{0.15f, 0.12f, 0.06f, 0.02f, 0.02f, 0.02f, 0.11f, 0.06f, 0.08f}, 1),
        };

        //Updates the Training data (so we can save in a future button click...or unpause, etc).
        JSONSaveSystem.Instance.SetTrainingData(trainingDataList);

        float learningRate = 0.1f;
        for (int i = 0; i < maxIterations; i++)
        {
            int totalError = 0;
            for (int j = 0; j < trainingDataList.Length; j++)
            {
                int output = caluculatePerceptronOutput(trainingDataList[j].Inputs, trainedWeights);
                int error = trainingDataList[j].Output - output;

                totalError += Mathf.Abs(error);
                Debug.Log("totalError: " + totalError);
                if (error != 0)
                {
                    for (int k = 0; k < trainedWeights.Length; k++)
                    {
                        float result = learningRate * error * trainingDataList[j].Inputs[k];
                        trainedWeights[k] += result;
                        Debug.Log("weightNum: " + k + ", output:" + output + ", expected output:" + trainingDataList[j].Output + ", error: " + error + ", result: " + result + ", finalWeight: " + trainedWeights[k]);
                    }

                }

            }

        }
    }

    #endregion

    #region Utilities

    //Takes in an angle as degrees, where 0 is the front of the car, and 180,-180 are the back.
    //Returns a float (number with decimals) representing the distance to the nearest object, or 1000 if it doesn't hit anything.
    float Raycast(float yAngleOffset)
    {


        var direction = Quaternion.Euler(0, yAngleOffset, 0) * transform.forward;
        var position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);

        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(position, direction, out hit, Mathf.Infinity))
        {
            Debug.DrawRay(position, direction * hit.distance, Color.yellow);
            return hit.distance;
        }
        else
        {
            Debug.DrawRay(position, direction * 50, Color.red);
            return 1000f;
        }
    }

    #endregion

    int caluculatePerceptronOutput(float[] inputs, float[] weights)
    {
        sum = 0;

        for (int i = 0; i < inputs.Length; i++)
        {
            sum += inputs[i] * weights[i];
        }

        if (sum > 0)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }

    void PerceptronMovement(float[] sensorDistances, float[] weights)
    {
        controlScript.SetThrottle(1);

        controlScript.SetTurn(caluculatePerceptronOutput(sensorDistances, weights));

    }
}
