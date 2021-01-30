//credits :https://github.com/madsbangh/EasyButtons/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Linq;
using System.Reflection;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class ButtonAttribute : Attribute { }

#if UNITY_EDITOR
public static class EasyButtonsEditorExtensions
{
    public static void DrawEasyButtons(this Editor editor)
    {
        var methods = editor.target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetParameters().Length == 0);
        foreach (var method in methods)
        {
            var ba = (ButtonAttribute)Attribute.GetCustomAttribute(method, typeof(ButtonAttribute));
            if (ba != null)
            {
                GUILayout.Space(10);
                var buttonName = ObjectNames.NicifyVariableName(method.Name);
                if (GUILayout.Button(buttonName))
                {
                    foreach (var t in editor.targets)
                    {
                        method.Invoke(t, null);
                    }
                }
                GUILayout.Space(10);
            }
        }
    }
}
[CanEditMultipleObjects]
[CustomEditor(typeof(UnityEngine.Object), true)]
public class ObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        this.DrawEasyButtons();
        DrawDefaultInspector();
    }
}
#endif