using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomPropertyDrawer(typeof(Output))]
    public class OutputDrawer : PropertyDrawer
    {
        private string[] names;
        private Output[] values;
        private bool cached;

        private void Cache ()
        {
            if (cached) return;

            var fields = typeof(Output)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(Output));

            names = fields.Select(f => f.Name).ToArray();
            values = fields.Select(f => (Output)f.GetValue(null)).ToArray();

            if (names.Length == 0)
            {
                names = new[] { "-----" };
                values = new[] { new Output(string.Empty) };
            }

            cached = true;
        }

        public override VisualElement CreatePropertyGUI (SerializedProperty property)
        {
            Cache();

            SerializedProperty stringProp = property.FindPropertyRelative("value");
            string current = stringProp.stringValue;
            int index = Array.IndexOf(values, new Output(current));
            if (index < 0) index = 0;

            var popup = new PopupField<string>(names.ToList(), index)
            {
                label = property.displayName
            };

            popup.RegisterValueChangedCallback(evt =>
            {
                int newIndex = Array.IndexOf(names, evt.newValue);
                if (newIndex >= 0)
                {
                    stringProp.stringValue = values[newIndex].ToString();
                    property.serializedObject.ApplyModifiedProperties();
                }
            });

            return popup;
        }

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            Cache();

            SerializedProperty stringProp = property.FindPropertyRelative("value");
            string current = stringProp.stringValue;
            int index = Array.IndexOf(values, new Output(current));
            if (index < 0) index = 0;

            int selected = EditorGUI.Popup(position, label.text, index, names);
            if (selected != index)
            {
                stringProp.stringValue = values[selected].ToString();
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}