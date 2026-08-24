using System;
using System.Linq;
using PvZ.Core.Entities;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameEntity.DetectorSlot))]
public sealed class DetectorSlotDrawer : PropertyDrawer
{
    private const int ExpandedLineCount = 4;

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

        return ExpandedLineCount * EditorGUIUtility.singleLineHeight +
               (ExpandedLineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var lineHeight = EditorGUIUtility.singleLineHeight;
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var line = new Rect(position.x, position.y, position.width, lineHeight);
        property.isExpanded = EditorGUI.Foldout(
            line,
            property.isExpanded,
            "Detector Binding",
            true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        var transformProperty = property.FindPropertyRelative("detectorTransform");
        var callbackProperty = property.FindPropertyRelative("callback");

        line.y += lineHeight + spacing;
        EditorGUI.PropertyField(line, transformProperty, new GUIContent("Transform"));

        var detectorTransform = transformProperty.objectReferenceValue as Transform;
        var detectorBehaviour = ResolveDetector(
            detectorTransform,
            out var detector,
            out var detectorStatus);

        line.y += lineHeight + spacing;
        using (new EditorGUI.DisabledScope(true))
        {
            if (detectorBehaviour != null)
            {
                EditorGUI.ObjectField(
                    line,
                    "Detector",
                    detectorBehaviour,
                    typeof(MonoBehaviour),
                    true);
            }
            else
            {
                EditorGUI.TextField(line, "Detector", detectorStatus);
            }
        }

        line.y += lineHeight + spacing;
        var callbackRect = EditorGUI.PrefixLabel(line, new GUIContent("Callback"));
        var callback = callbackProperty.managedReferenceValue as IDetectorCallback;
        var callbackName = callback == null
            ? "Select Callback"
            : GetTypeLabel(callback.GetType());

        if (GUI.Button(callbackRect, callbackName, EditorStyles.popup))
        {
            ShowCallbackMenu(property, detector, callback);
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private static MonoBehaviour ResolveDetector(
        Transform detectorTransform,
        out IDetector detector,
        out string status)
    {
        detector = null;
        status = "Select a Transform";
        if (detectorTransform == null) return null;

        MonoBehaviour detectorBehaviour = null;
        foreach (var behaviour in detectorTransform.GetComponents<MonoBehaviour>())
        {
            if (behaviour is not IDetector candidate) continue;
            if (detector != null)
            {
                detector = null;
                status = "Multiple detector components";
                return null;
            }

            detector = candidate;
            detectorBehaviour = behaviour;
        }

        if (detector == null)
        {
            status = "Missing detector component";
        }

        return detectorBehaviour;
    }

    private static void ShowCallbackMenu(
        SerializedProperty slotProperty,
        IDetector detector,
        IDetectorCallback currentCallback)
    {
        var menu = new GenericMenu();
        var serializedObject = slotProperty.serializedObject;
        var callbackPath = slotProperty.FindPropertyRelative("callback").propertyPath;

        menu.AddItem(
            new GUIContent("None"),
            currentCallback == null,
            () => SetCallback(serializedObject, callbackPath, null));
        menu.AddSeparator(string.Empty);

        var callbackContract = detector?.CallbackType ?? typeof(IDetectorCallback);
        var callbackTypes = TypeCache.GetTypesDerivedFrom<IDetectorCallback>()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                !type.IsGenericType &&
                type.IsSerializable &&
                callbackContract.IsAssignableFrom(type))
            .OrderBy(GetTypeLabel)
            .ToArray();

        if (callbackTypes.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("No compatible callbacks"));
        }

        foreach (var callbackType in callbackTypes)
        {
            var selectedType = callbackType;
            menu.AddItem(
                new GUIContent(GetMenuPath(selectedType)),
                currentCallback?.GetType() == selectedType,
                () => SetCallback(
                    serializedObject,
                    callbackPath,
                    Activator.CreateInstance(selectedType, true)));
        }

        menu.ShowAsContext();
    }

    private static void SetCallback(
        SerializedObject serializedObject,
        string callbackPath,
        object callback)
    {
        serializedObject.Update();
        var callbackProperty = serializedObject.FindProperty(callbackPath);
        if (callbackProperty == null) return;

        callbackProperty.managedReferenceValue = callback;
        serializedObject.ApplyModifiedProperties();
    }

    private static string GetTypeLabel(Type type)
    {
        return type.DeclaringType == null
            ? type.Name
            : $"{type.DeclaringType.Name}.{type.Name}";
    }

    private static string GetMenuPath(Type type)
    {
        return type.DeclaringType == null
            ? type.Name
            : $"{type.DeclaringType.Name}/{type.Name}";
    }
}
