using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(CameraShifter))]
public class CameraShifterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        string help = @"This is for shifting camera position instantly
by the key to change the view position in runtime.
Push the following keys to move camera position with 'Ctrl'(fast move) or 'Shift'(slow move).
up/down arrow : Go forward/backward
right/left arrow : Go left/right
+ / - in num pad : Go up/down 
Page up / Page down : Look up/down
< / > : Look left / right

Note : This keep inital offset in start up.
Please use 'SetCameraShift' of CameraShifter.cs to change the base offset from camera master.
";

        EditorGUILayout.HelpBox(help, MessageType.Info);
    }
}
