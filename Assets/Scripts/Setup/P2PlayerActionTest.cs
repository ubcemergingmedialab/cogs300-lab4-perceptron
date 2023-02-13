using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class P2PlayerActionTest : MonoBehaviour
{
    public Rigidbody rb;
    public AudioSource eatingSound;
    public AudioSource avoidingSound;

    private bool onGround;
    private bool moving;
    
    private Perceptron perceptron;
    
    
    // Start is called before the first frame update
    void Start()
    {
        onGround = false;
        moving = false;
        
        perceptron = new Perceptron(4);

        perceptron.SetWeight(0, PlayerPrefs.GetFloat("w1"));
        perceptron.SetWeight(1, PlayerPrefs.GetFloat("w2"));
        perceptron.SetWeight(2, PlayerPrefs.GetFloat("w3"));
        perceptron.SetWeight(3, PlayerPrefs.GetFloat("w4"));
    }

    // Update is called once per frame
    void Update()
    {
        if (onGround & moving) {
            transform.Translate(0, 0, 5f * Time.deltaTime);
        }
        
    }

    void OnCollisionEnter(Collision collision) {
        if (collision.collider.tag == "Ground") {
            onGround = true;
            moving = true;
        }
        else if (collision.collider.tag == "Wall") {
            moving = false;
        }
        else if (collision.collider.tag == "Interactable") {
            GameObject interactable = collision.gameObject;
            float[] inputs = interactable.GetComponent<InputInfo>().inputs;

            int perceptronOutput = perceptron.GetPerceptronOutput(inputs);


            Destroy(interactable);
            transform.rotation = Quaternion.identity;

            if (perceptronOutput == 0) {
                Avoid(inputs);               
            }
            else if (perceptronOutput == 1) {
                Eat(inputs);
            }

        }
    }

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
}

