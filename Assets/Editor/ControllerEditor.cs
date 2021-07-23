using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(Controller))]
public class ControllerEditor : Editor
{
	private Controller script;

	private void OnEnable()
	{
		// Method 1
		script = (Controller)target;
	}

	public override void OnInspectorGUI()
	{
		if (GUILayout.Button("Randomize :)"))
		{
			script.randomizeSettings();
		}

		// Draw default inspector after button...
		base.OnInspectorGUI();
	}
}
