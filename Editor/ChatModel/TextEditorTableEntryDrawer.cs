using UnityEngine;
using UnityEditor;
namespace UniChat.Editor.ChatModel
{
    [CustomPropertyDrawer(typeof(TextEditorTable.AudioInfo))]
    public class TextEditorTableAudioInfoDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty infoTextProperty = property.FindPropertyRelative("infoText");
            SerializedProperty fileNameProperty = property.FindPropertyRelative("fileName");
            EditorGUI.LabelField(position, $"{fileNameProperty.stringValue}  {infoTextProperty.stringValue}");
            EditorGUI.EndProperty();
        }
    }
    [CustomPropertyDrawer(typeof(TextEditorTable.Entry))]
    public class TextEditorTableEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty stringValueProp = property.FindPropertyRelative("stringValue");
            float height = EditorGUIUtility.singleLineHeight * 2;
            GUIContent content = new(stringValueProp.stringValue);
            float textHeight = EditorStyles.textArea.CalcHeight(content, EditorGUIUtility.currentViewWidth);
            height += textHeight;
            height += EditorGUIUtility.standardVerticalSpacing * 3;
            return height;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty uniqueIdProp = property.FindPropertyRelative("uniqueId");
            SerializedProperty stringValueProp = property.FindPropertyRelative("stringValue");
            EditorGUI.BeginProperty(position, label, property);
            Rect uniqueIdRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(uniqueIdRect, $"ID {uniqueIdProp.uintValue}");
            Rect textRect = new(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, position.height - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);
            stringValueProp.stringValue = EditorGUI.TextArea(textRect, stringValueProp.stringValue, new GUIStyle(GUI.skin.textArea) { wordWrap = true });
            EditorGUI.EndProperty();
        }
    }
}
