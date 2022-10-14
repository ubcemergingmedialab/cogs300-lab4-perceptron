using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class P1PlayerAction : MonoBehaviour
{
    public Rigidbody rb;
    public AudioSource eatingSound;
    public AudioSource avoidingSound;

    public InputField w1Input;
    public InputField w2Input;
    public InputField w3Input;
    public InputField w4Input;
    public Button moveButton;

    private bool onGround;
    private bool stop;
    private bool moving;

    private Vector3 originalPos;
    
    private Perceptron perceptron;
    
    
    void Awake() {
        moveButton.onClick.AddListener(MoveButtonClicked);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        onGround = false;
        moving = false;
        
        perceptron = new Perceptron(4);
        
        originalPos = transform.position;
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

    
    public void MoveButtonClicked() {
        try {
            float w1 = float.Parse(w1Input.text);
            float w2 = float.Parse(w2Input.text);
            float w3 = float.Parse(w3Input.text);
            float w4 = float.Parse(w4Input.text);

            perceptron.SetWeight(0, w1);
            perceptron.SetWeight(1, w2);
            perceptron.SetWeight(2, w3);
            perceptron.SetWeight(3, w4);

            moving = true;
            moveButton.interactable = false;
        }
        catch (ArgumentNullException e) {
            Debug.Log("please enter all weights");
        }
        catch (FormatException e) {
            Debug.Log("Please enter numeric values");
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

