using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

public class JSONSaveSystem : MonoBehaviour
{
    public static JSONSaveSystem Instance { get; private set; }

    private static string NAME_OF_FILE = "/TrainingDataList.json";

    //--This is the training data that gets loaded, saved and cleared from the system
    //any modification of training data that we want to save (say, in CarControlAPI) needs to update *this* TrainingDataList object!
    [SerializeField]
    private static TrainingDataList trainingDataList = new TrainingDataList();

    private void Awake()
    {
        //if there is an instance, and it's not me, destroy myself!!
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    //saves the file as a JSON (check debug log to file location)
    public static void SaveIntoJson()
    {
        try
        {
            string data = JsonUtility.ToJson(trainingDataList);
            System.IO.File.WriteAllText(Application.persistentDataPath + NAME_OF_FILE, data);
            Debug.Log("File saved in: " + Application.persistentDataPath + NAME_OF_FILE);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error Saving: " + e);
        }
    }

    //Gets the TrainingDataList from file save
    public static TrainingDataList ReadFromJson()
    {
        try
        {
            string data = System.IO.File.ReadAllText(Application.persistentDataPath + NAME_OF_FILE);
            trainingDataList = JsonUtility.FromJson<TrainingDataList>(data);
            if(trainingDataList == null)
            {
                trainingDataList = new TrainingDataList();
            }
            Debug.Log("Fie loaded");
            return trainingDataList;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error Reading: " + e);
        }
        return null;
    }

    public static void DeleteJson()
    {
        try
        {
            System.IO.File.Delete(Application.persistentDataPath + NAME_OF_FILE);
            Debug.Log("Fie Deleted");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error Deleting JSON file");
        }
    }

    //Gets the training data as an array from file save
    public TrainingData[] ReadFromJsonAsArray()
    {
        trainingDataList = ReadFromJson();
        return trainingDataList.td.ToArray();
    }

    //Add a TrainingData object to the list
    public void AddTrainingData(TrainingData td)
    {
        trainingDataList.td.Add(td);
    }

    //Override the list with the input array
    public void SetTrainingData(TrainingData[] array)
    {
        trainingDataList.td = new List<TrainingData>(array);
    }

    //Clear training data
    public static void ClearTrainingDataList()
    {
        trainingDataList.td = new List<TrainingData>();
    }

    public List<TrainingData> GetTrainingDataList()
    {
        return trainingDataList.td;
    }

    [Serializable]
    public class TrainingDataList
    {
        public List<TrainingData> td = new List<TrainingData>();
    }
}
