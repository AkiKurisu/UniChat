using UnityEngine;
using UnityEditor;
using static Kurisu.UniChat.StateMachine.ChatStateMachineGraph;
namespace Kurisu.UniChat.StateMachine.Editor
{
    // Modified from https://gist.github.com/tomkail/ba4136e6aa990f4dc94e0d39ec6a058c
    [CustomPropertyDrawer(typeof(SerializedBehaviorWrapper), true)]
    public class GenericBehaviorWrapperDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return CalculatePropertyHeight(property);
        }
        public static float CalculatePropertyHeight(SerializedProperty property)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight;
            if (property.objectReferenceValue == null || !AreAnySubPropertiesVisible(property))
            {
                return totalHeight;
            }
            var data = property.objectReferenceValue as ScriptableObject;
            if (data == null) return EditorGUIUtility.singleLineHeight;
            SerializedObject serializedObject = new(data);
            try
            {
                SerializedProperty prop = serializedObject.GetIterator();
                if (prop == null)
                {
                    return EditorGUIUtility.singleLineHeight;
                }
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.name == "m_Script") continue;
                        if (prop.name == "m_Value") continue;
                        var subProp = serializedObject.FindProperty(prop.name);
                        float height = EditorGUI.GetPropertyHeight(subProp, null, true) + EditorGUIUtility.standardVerticalSpacing;
                        totalHeight += height;
                    }
                    while (prop.NextVisible(false));
                }
                return totalHeight;
            }
            finally
            {
                serializedObject.Dispose();
            }

        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
            {
                var data = (ScriptableObject)property.objectReferenceValue;
                SerializedObject serializedObject = new(data);
                SerializedProperty prop = serializedObject.FindProperty("m_Value");
                if (prop != null && prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.name == "m_Script") continue;
                        if (prop.name == "m_Value") continue;
                        EditorGUI.PropertyField(position, prop, true);
                        position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    }
                    while (prop.NextVisible(false));
                }
                if (GUI.changed)
                    serializedObject.ApplyModifiedProperties();
                serializedObject.Dispose();
            }
            property.serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }
        private static bool AreAnySubPropertiesVisible(SerializedProperty property)
        {
            var data = (ScriptableObject)property.objectReferenceValue;
            SerializedObject serializedObject = new(data);
            SerializedProperty prop = serializedObject.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.name == "m_Script") continue;
                if (prop.name == "m_Value") continue;
                return true;
            }
            serializedObject.Dispose();
            return false;
        }
    }
}