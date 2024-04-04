using UnityEngine;
using UnityEditor;
using System;
using static Kurisu.UniChat.TextEmbeddingTable;
namespace Kurisu.UniChat.Editor
{
    [Serializable]
    public class TextEmbeddingEditorEntry
    {
        public uint uniqueId;
        [TextArea]
        public string stringValue;
        public bool isEdit;
        public readonly TextEmbeddingEntry internalEntry;
        public TextEmbeddingEditorEntry(TextEmbeddingEntry internalEntry)
        {
            this.internalEntry = internalEntry;
            uniqueId = internalEntry.uniqueId;
            stringValue = internalEntry.stringValue;
        }
        public void Update()
        {
            internalEntry.stringValue = stringValue;
        }
    }
    [CustomPropertyDrawer(typeof(TextEmbeddingEditorEntry))]
    public class TextEmbeddingEditorEntryDrawer : PropertyDrawer
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
            EditorGUI.LabelField(uniqueIdRect, uniqueIdProp.uintValue.ToString());
            Rect textRect = new(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, position.height - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);
            stringValueProp.stringValue = EditorGUI.TextArea(textRect, stringValueProp.stringValue, new GUIStyle(GUI.skin.textArea) { wordWrap = true });
            EditorGUI.EndProperty();
        }
    }
}
