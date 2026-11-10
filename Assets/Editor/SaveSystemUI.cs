using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static JSONSaveSystem;

[CustomEditor(typeof(JSONSaveSystem))]

// ensure class initializer is called whenever scripts recompile
[InitializeOnLoadAttribute]
public class SaveSystemUI : Editor
{
    //Creates buttons in the inspector
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Load Training Data"))
        {
            JSONSaveSystem.ReadFromJson();
        }

        if (GUILayout.Button("Save Training Data"))
        {
            JSONSaveSystem.SaveIntoJson();
        }

        if (GUILayout.Button("Delete Training Data"))
        {
            JSONSaveSystem.DeleteJson();
        }

        if (GUILayout.Button("Clear Training Data (From Editor)"))
        {
            JSONSaveSystem.ClearTrainingDataList();
        }
    }

    // register an event handler when the class is initialized
    //anytime these 'event's get triggered, it will call a method, for example: HandleOnPlayModeChanged(...)
    static SaveSystemUI()
    {
        EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
        EditorApplication.pauseStateChanged += HandleOnPauseModeChanged;
    }

    //anytime the editor 'pause' state changes
    private static void HandleOnPauseModeChanged(PauseState state)
    {
        if (state.Equals(PauseState.Paused))
        {
            //We paused!
            Debug.Log("We paused!");
        }
    }

    //anytime the editor 'play' mode changes
    private static void HandleOnPlayModeChanged(PlayModeStateChange state)
    {
        if (state.Equals(PlayModeStateChange.ExitingPlayMode))
        {
            //We stopped playing!
            Debug.Log("We stopped playing!");
            JSONSaveSystem.SaveIntoJson();
        }
    }
}
