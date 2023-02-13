using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class P2PlayerActionTrain : MonoBehaviour
{
    public Rigidbody rb;
    public float speed;
    public AudioSource eatingSound;
    public AudioSource avoidingSound;
    public Button resetBtn;

    public Text w1View;
    public Text w2View;
    public Text w3View;
    public Text w4View;

    private bool onGround;
    private bool moving;
    
    private Perceptron perceptron;
    

    // call resetWeight() when reset button is clicked
    void Awake() {
        resetBtn.onClick.AddListener(resetWeight);
    }


    // Start is called before the first frame update
    void Start()
    {
        onGround = false;
        moving = false;
        
        perceptron = new Perceptron(4);

        // Set weights of perceptron to last saved weights
        perceptron.SetWeight(0, PlayerPrefs.GetFloat("w1"));
        perceptron.SetWeight(1, PlayerPrefs.GetFloat("w2"));
        perceptron.SetWeight(2, PlayerPrefs.GetFloat("w3"));
        perceptron.SetWeight(3, PlayerPrefs.GetFloat("w4"));

        updateWeightViewAndSave();
    }

    // Update is called once per frame
    void Update()
    {
        if (onGround & moving) {
            transform.Translate(0, 0, 5f * speed * Time.deltaTime);
        }
        
    }


    // TODO
    void OnCollisionEnter(Collision collision) {
        if (collision.collider.tag == "Ground") {
            // Start moving when you land on the ground
            onGround = true;
            moving = true;
        }
        else if (collision.collider.tag == "Wall") {
            // Stop moving when you reach the wall
            moving = false;
        }
        else if (collision.collider.tag == "Interactable") {
            GameObject interactable = collision.gameObject;
            
            // These are the inputs and target output obtained from the object we collided with
            float[] inputs = interactable.GetComponent<InputAndExpected>().inputs;
            float targetOutput = interactable.GetComponent<InputAndExpected>().expected;
            
            Destroy(interactable);
            transform.rotation = Quaternion.identity;

            
            int perceptronOutput = perceptron.GetPerceptronOutput(inputs);
            perceptron.Train(inputs, targetOutput);

            
            if (perceptronOutput == 0) {
                Avoid(inputs);               
            }
            else if (perceptronOutput == 1) {
                Eat(inputs);
            }

            updateWeightViewAndSave();
        }
    }

    // Update the textboxes and save file with current weights
    private void updateWeightViewAndSave() {
        float[] weights = perceptron.GetWeights;

        w1View.text = weights[0].ToString();
        w2View.text = weights[1].ToString();
        w3View.text = weights[2].ToString();
        w4View.text = weights[3].ToString();

        PlayerPrefs.SetFloat("w1", weights[0]);
        PlayerPrefs.SetFloat("w2", weights[1]);
        PlayerPrefs.SetFloat("w3", weights[2]);
        PlayerPrefs.SetFloat("w4", weights[3]);
    }

    // Eat object. Turns green if it's the right action, else turns red
    private void Eat(float[] inputs) {
        eatingSound.Play();
        Debug.Log("Eat");

        if (inputs[0] == 1 && inputs[1] == 1 && inputs[2] == 0) {
            StartCoroutine(ChangeColor("green"));
        }
        else {
            StartCoroutine(ChangeColor("red"));
        }
    }

    // Avoid object. Turns green if it's the right action, else turns red
    private void Avoid(float[] inputs) {
        avoidingSound.Play();
        Debug.Log("Avoid");

        if (inputs[0] == 1 && inputs[1] == 1 && inputs[2] == 0) {
            StartCoroutine(ChangeColor("red"));
        }
        else {
            StartCoroutine(ChangeColor("green"));
        }
    }

    // Change color of player based on input (only "green" and "red" are valid)
    private IEnumerator ChangeColor(string color) {
        Material playerMat = gameObject.GetComponent<Renderer>().material;
        Color originalColor = playerMat.color;

        if (color == "green") {
            playerMat.SetColor("_Color", Color.green);
            yield return new WaitForSeconds(0.5f);
        }
        else if (color == "red") {
            playerMat.SetColor("_Color", Color.red);
            yield return new WaitForSeconds(0.5f);
        }

        playerMat.SetColor("_Color", originalColor);
    }


    // Reset all weights to 0
    private void resetWeight() {
        perceptron.SetWeight(0, 0);
        perceptron.SetWeight(1, 0);
        perceptron.SetWeight(2, 0);
        perceptron.SetWeight(3, 0);

        updateWeightViewAndSave();
    }
}

