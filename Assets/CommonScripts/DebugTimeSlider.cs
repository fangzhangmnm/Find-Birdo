#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class DebugTimeSlider : EditorWindow
{
    [MenuItem("Window/DebugTimeSlider")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(DebugTimeSlider));
    }
    float timeScale = 1f;
    private void OnGUI()
    {
        timeScale = EditorGUILayout.Slider("Time Scale", timeScale, 0f, 2f);
    }
    bool prevP = false;
    private void Update()
    {
        if (Application.isPlaying)
        {
            if (Input.GetKey(KeyCode.P) && !prevP)
            {
                timeScale = timeScale != 1f ? 1f : 0.1f;
                Repaint();
            }
            prevP = Input.GetKey(KeyCode.P);
            Time.timeScale = timeScale;
        }
    }
}
#endif
