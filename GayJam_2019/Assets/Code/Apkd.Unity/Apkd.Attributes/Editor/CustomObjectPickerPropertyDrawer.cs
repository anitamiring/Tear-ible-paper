// using UnityEngine;
// using UnityEditor;
// using System.Collections;
// using System.Linq;
// using System.Collections.Generic;

// namespace Apkd
// {
//     [CustomPropertyDrawer(typeof(Internal.CustomObjectPickerAttribute))]
//     public sealed class SerializedInterfacePropertyDrawer : PropertyDrawer
//     {
//         static readonly Dictionary<System.Type, System.Type[]> interfaceImplementationsDict
//             = new Dictionary<System.Type, System.Type[]>();

//         static GUIStyle style;

//         int dropdownIndex = 0;

//         public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
//         {
//             style = style ?? new GUIStyle(EditorStyles.popup) { fontSize = 6 };
//             var data = attribute as Internal.CustomObjectPickerAttribute;

//             if (string.IsNullOrWhiteSpace(label.tooltip))
//                 label.tooltip = data.InterfaceType.Name;
//             else
//                 label.tooltip = $"({data.InterfaceType.Name}) {label.tooltip}";

//             System.Type[] implementations;
//             if (!interfaceImplementationsDict.TryGetValue(data.InterfaceType, out implementations))
//             {
//                 interfaceImplementationsDict[data.InterfaceType] = implementations = data.InterfaceType
//                     .Module
//                     .GetTypes()
//                     .Where(x => x.GetInterfaces().Contains(data.InterfaceType))
//                     .ToArray();
//             }

//             {
//                 var rect = pos;
//                 rect.x = rect.xMax - 64;
//                 rect.width = 64;
//                 dropdownIndex = EditorGUI.Popup(
//                     position: rect,
//                     selectedIndex: dropdownIndex,
//                     displayedOptions: implementations.Select(x => x.Name).ToArray(),
//                     style);
//             }

//             var selectedType = implementations[dropdownIndex];

//             {
//                 var rect = pos;
//                 rect.xMax -= 64;
//                 EditorGUI.ObjectField(rect, property, selectedType, label);
//             }
//         }
//     }
// }

#if UNITY_EDITOR && ODIN_INSPECTOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
namespace Apkd
{
    [CustomPropertyDrawer(typeof(Internal.CustomObjectPickerAttribute))]
    [DrawerPriority(0, 0, 99999999999)]
    public sealed class SerializedInterfacePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(pos, property, label, includeChildren: true);
        }
    }

    // public sealed class Drawer : OdinAttributeDrawer<Internal.CustomObjectPickerAttribute>
    // {
    //     protected override void DrawPropertyLayout(InspectorProperty property, Internal.CustomObjectPickerAttribute attribute, GUIContent label)
    //     {
    //         var rect = EditorGUILayout.GetControlRect();
    //         EditorGUI.PropertyField(rect, property.Info.proper)
    //     }
    // }
}
#endif