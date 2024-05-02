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
            if (property.objectReferenceValue == null || !AreAnySubPropertiesVisible(property))
            {
                return EditorGUIUtility.singleLineHeight;
            }
            var data = property.objectReferenceValue as ScriptableObject;
            if (data == null) return EditorGUIUtility.singleLineHeight;
            SerializedObject serializedObject = new(data);
            try
            {
                SerializedProperty prop = serializedObject.FindProperty("m_Value");
                if (prop == null)
                {
                    return EditorGUIUtility.singleLineHeight;
                }
                float totalHeight = 0;
                if (prop.NextVisible(true))
                {
                    do
                    {
                        float height = EditorGUI.GetPropertyHeight(prop, null, true) + EditorGUIUtility.standardVerticalSpacing;
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
                        float height = EditorGUI.GetPropertyHeight(prop, new GUIContent(prop.displayName), true);
                        position.height = height;
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
                return true;
            }
            serializedObject.Dispose();
            return false;
        }
    }
}