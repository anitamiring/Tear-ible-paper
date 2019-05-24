#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Apkd
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!position.Contains(Event.current.mousePosition))
                GUI.enabled = false;

            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif