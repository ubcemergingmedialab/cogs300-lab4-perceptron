using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Perceptron {

    // An array storing all the weights the perceptron has
    private float[] weights;
    
    // The threshold of the perceptron. The output should be 1 if the net input is greater than this value
    private float threshold = 0.5f;

    // TODO: set the learning rate for the perceptron
    // A constant that determines how big of an amount to increase/decrease the weights when training
    private float learningRate = 0f;
    
    

    // TODO
    // Return the perceptron's predicted output given the array of inputs
    public int GetPerceptronOutput(float[] inputs)
    {
        // the net input is the sum of the products between the weights and their corresponding inputs,
        // also known as the activation summation
        float netInput = 0;  
        
        // TODO: calculate the net input given the inputs and weights
        




        // TODO: implement the threshold function (i.e. determine the output value to return (1 or 0))
        



        return 0; //REMOVE THIS after you have finished implementing this function
    }


    // TODO
    // Train the perceptron (i.e. learning) given an array of inputs and the target output
    public void Train(float[] inputs, float targetOutput)
    {
        // TODO: set the actual output 
        // This is what the perceptron thinks the output is given its current weights
        int actualOutput = 0;
        
        // TODO: calculate the error
        // This is the difference between the target output and the actual output
        float error = 0;

        // TODO: modify the weights using the perceptron learning rule
        // Note: for improved performance, first use the error variable to determine
        // when it is necessary to modify the weights, and only modify them under
        // those conditions
        



    }

    
    // --------------------IGNORE ME--------------------

    // initialize perceptron with numWeights
    public Perceptron(int numWeights)
    {
        weights = new float[numWeights];
        for (int weight = 0; weight < weights.Length; weight++)
        {
            weights[weight] = 0f;
        }
    }

    
    // Return all the weights as an array
    public float[] GetWeights
    {
        get { return weights; }
    }


    // Manually set the value of one weight at a given index (0-based) in the weight array 
    public void SetWeight(int index, float value)
    {
        if (index >= weights.Length)
            return;

        weights[index] = value;
    }
}
