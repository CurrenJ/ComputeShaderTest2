using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(Controller))]
public class ControllerEditor : Editor
{
    private GUIStyle redTitle;
    private GUIStyle greenTitle;
    private GUIStyle blueTitle;
    private GUIStyle titleLabel;

    private Controller script;
    private bool teamSettingsExist;

    // Shader Properties
    private SerializedProperty computeShader;
    private SerializedProperty targetResolution;

    // Simulation Properties
    private SerializedProperty population;
    private SerializedProperty numberOfTeams;
    private SerializedProperty spawnMode;
    private SerializedProperty decayFactor;
    private SerializedProperty diffuseFactor;

    // Playback Properties
    private SerializedProperty stepsPerUpdate;
    private SerializedProperty showAgentsOnly;
    private SerializedProperty showObstructionsOnly;
    private SerializedProperty showRawColorChannels;
    private SerializedProperty useHSVColorRemapping;

    //Render Properties
    private SerializedProperty renderToPngSequence;
    private SerializedProperty saveRawChannelsPngSequence;
    private SerializedProperty filepath;
    private SerializedProperty frameNum;
    private SerializedProperty stepNum;
    private SerializedProperty renderTimescale;
    private SerializedProperty framesPerSecond;
    private SerializedProperty frameLimit;

    // Other Properties
    private SerializedProperty useCurrenPastelPackForRandomColors;

    // Red Team Properties
    private SerializedProperty r_TurnFactor;
    private SerializedProperty r_MoveSpeed;
    private SerializedProperty r_SensorAngleOffset;
    private SerializedProperty r_SensorPosOffset;
    private SerializedProperty r_SensorRadius;
    private SerializedProperty r_TeamColor;

    // Green Team Properties
    private SerializedProperty g_TurnFactor;
    private SerializedProperty g_MoveSpeed;
    private SerializedProperty g_SensorAngleOffset;
    private SerializedProperty g_SensorPosOffset;
    private SerializedProperty g_SensorRadius;
    private SerializedProperty g_TeamColor;

    // Blue Team Properties
    private SerializedProperty b_TurnFactor;
    private SerializedProperty b_MoveSpeed;
    private SerializedProperty b_SensorAngleOffset;
    private SerializedProperty b_SensorPosOffset;
    private SerializedProperty b_SensorRadius;
    private SerializedProperty b_TeamColor;

    private void OnEnable()
    {
        if (serializedObject.FindProperty("teamSettings") != null)
        {
            SerializedProperty teamSettings = serializedObject.FindProperty("teamSettings");
            SerializedProperty redTeam = teamSettings.GetArrayElementAtIndex(0);
            SerializedProperty greenTeam = teamSettings.GetArrayElementAtIndex(1);
            SerializedProperty blueTeam = teamSettings.GetArrayElementAtIndex(2);

            r_MoveSpeed = redTeam.FindPropertyRelative("turnFactor");
            r_TurnFactor = redTeam.FindPropertyRelative("moveSpeed");
            r_SensorAngleOffset = redTeam.FindPropertyRelative("sensorAngleOffset");
            r_SensorPosOffset = redTeam.FindPropertyRelative("sensorPosOffset");
            r_SensorRadius = redTeam.FindPropertyRelative("sensorRadius");
            r_TeamColor = redTeam.FindPropertyRelative("teamColor");

            g_MoveSpeed = greenTeam.FindPropertyRelative("turnFactor");
            g_TurnFactor = greenTeam.FindPropertyRelative("moveSpeed");
            g_SensorAngleOffset = greenTeam.FindPropertyRelative("sensorAngleOffset");
            g_SensorPosOffset = greenTeam.FindPropertyRelative("sensorPosOffset");
            g_SensorRadius = greenTeam.FindPropertyRelative("sensorRadius");
            g_TeamColor = greenTeam.FindPropertyRelative("teamColor");

            b_MoveSpeed = blueTeam.FindPropertyRelative("turnFactor");
            b_TurnFactor = blueTeam.FindPropertyRelative("moveSpeed");
            b_SensorAngleOffset = blueTeam.FindPropertyRelative("sensorAngleOffset");
            b_SensorPosOffset = blueTeam.FindPropertyRelative("sensorPosOffset");
            b_SensorRadius = blueTeam.FindPropertyRelative("sensorRadius");
            b_TeamColor = blueTeam.FindPropertyRelative("teamColor");

            // Shader Properties
            computeShader = serializedObject.FindProperty("computeShader");
            targetResolution = serializedObject.FindProperty("targetResolution");

            // Simulation Properties
            population = serializedObject.FindProperty("population");
            numberOfTeams = serializedObject.FindProperty("teams");
            spawnMode = serializedObject.FindProperty("spawnMode");
            decayFactor = serializedObject.FindProperty("decayFactor");
            diffuseFactor = serializedObject.FindProperty("diffuseFactor");

            // Playback Properties
            stepsPerUpdate = serializedObject.FindProperty("stepsPerUpdate");
            stepsPerUpdate = serializedObject.FindProperty("stepsPerUpdate");
            showAgentsOnly = serializedObject.FindProperty("showAgentsOnly");
            showObstructionsOnly = serializedObject.FindProperty("showObstructionsOnly");
            showRawColorChannels = serializedObject.FindProperty("useRawColorChannels");
            useHSVColorRemapping = serializedObject.FindProperty("useHsvForColorRemapping");

            //Render Properties
            renderToPngSequence = serializedObject.FindProperty("renderToPng");
            saveRawChannelsPngSequence = serializedObject.FindProperty("saveRawChannelsSeparately");
            filepath = serializedObject.FindProperty("path");
            frameNum = serializedObject.FindProperty("frame");
            stepNum = serializedObject.FindProperty("step");
            renderTimescale = serializedObject.FindProperty("renderTimeScale");
            framesPerSecond = serializedObject.FindProperty("framesPerSecond");
            frameLimit = serializedObject.FindProperty("frameLimit");

            // Other Properties
            useCurrenPastelPackForRandomColors = serializedObject.FindProperty("useCurrenPastelPack");
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //DrawDefaultInspector();
        if (serializedObject.FindProperty("teamSettings") != null)
        {
            titleLabel = new GUIStyle();
            titleLabel.normal.textColor = Color.white;
            titleLabel.fontSize = 14;
            titleLabel.fontStyle = FontStyle.Bold;

            redTitle = new GUIStyle(titleLabel);
            Color r_Color = r_TeamColor.colorValue;
            r_Color.a = 1f;
            redTitle.normal.textColor = r_Color;
            redTitle.fontStyle = FontStyle.Bold;

            greenTitle = new GUIStyle(titleLabel);
            Color g_Color = g_TeamColor.colorValue;
            g_Color.a = 1f;
            greenTitle.normal.textColor = g_Color;
            greenTitle.fontStyle = FontStyle.Bold;

            blueTitle = new GUIStyle(titleLabel);
            Color b_Color = b_TeamColor.colorValue;
            b_Color.a = 1f;
            blueTitle.normal.textColor = b_Color;
            blueTitle.fontStyle = FontStyle.Bold;



            // Shader Properties
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField("1. Shader Properties", titleLabel);

            EditorGUILayout.BeginVertical();

            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(computeShader);
            EditorGUILayout.PropertyField(targetResolution);

            EditorGUILayout.EndVertical();

            // Simulation Properties
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField("2. Simulation Properties", titleLabel);

            EditorGUILayout.BeginVertical();

            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(population);
            EditorGUILayout.PropertyField(numberOfTeams);
            EditorGUILayout.PropertyField(spawnMode);
            EditorGUILayout.PropertyField(decayFactor);
            EditorGUILayout.PropertyField(diffuseFactor);

            EditorGUILayout.EndVertical();

            // Playback Properties
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField("3. Playback Properties", titleLabel);

            EditorGUILayout.BeginVertical();

            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(stepsPerUpdate);
            EditorGUILayout.PropertyField(showAgentsOnly);
            EditorGUILayout.PropertyField(showObstructionsOnly);
            EditorGUILayout.PropertyField(showRawColorChannels);
            EditorGUILayout.PropertyField(useHSVColorRemapping);

            EditorGUILayout.EndVertical();

            // Render Properties
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField("4. Render Properties", titleLabel);

            EditorGUILayout.BeginVertical();

            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(renderToPngSequence);
            EditorGUILayout.PropertyField(saveRawChannelsPngSequence);
            EditorGUILayout.PropertyField(filepath);
            EditorGUILayout.PropertyField(frameNum);
            EditorGUILayout.PropertyField(stepNum);
            EditorGUILayout.PropertyField(renderTimescale);
            EditorGUILayout.PropertyField(framesPerSecond);
            EditorGUILayout.PropertyField(frameLimit);

            EditorGUILayout.EndVertical();

            // Red Settings
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField("Red Settings", redTitle);

            EditorGUILayout.BeginVertical();

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(r_TurnFactor);
            EditorGUILayout.PropertyField(r_MoveSpeed);

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Sensor", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(r_SensorAngleOffset);
            EditorGUILayout.PropertyField(r_SensorPosOffset);
            EditorGUILayout.PropertyField(r_SensorRadius);

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Color", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(r_TeamColor);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            // Green Settings
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField("Green Settings", greenTitle);

            EditorGUILayout.BeginVertical();

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(g_TurnFactor);
            EditorGUILayout.PropertyField(g_MoveSpeed);

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Sensor", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(g_SensorAngleOffset);
            EditorGUILayout.PropertyField(g_SensorPosOffset);
            EditorGUILayout.PropertyField(g_SensorRadius);

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Color", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(g_TeamColor);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);


            // Green Settings
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField("Blue Settings", blueTitle);

            EditorGUILayout.BeginVertical();

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(b_TurnFactor);
            EditorGUILayout.PropertyField(b_MoveSpeed);

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Sensor", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(b_SensorAngleOffset);
            EditorGUILayout.PropertyField(b_SensorPosOffset);
            EditorGUILayout.PropertyField(b_SensorRadius);

            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Color", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(b_TeamColor);
            EditorGUILayout.EndVertical();
        }
        serializedObject.ApplyModifiedProperties();
    }
}
