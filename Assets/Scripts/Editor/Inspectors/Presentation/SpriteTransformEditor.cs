using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEngine;

namespace PvZ.Editor.Inspectors.Presentation
{
[CustomEditor(typeof(SpriteTransform)), CanEditMultipleObjects]
public sealed class SpriteTransformEditor : UnityEditor.Editor
{
    private SerializedProperty _position;
    private SerializedProperty _scale;
    private SerializedProperty _skew;
    private SerializedProperty _brightness;
    private SerializedProperty _isBright;
    private SerializedProperty _alpha;
    private SerializedProperty _alphaCoef;
    private SerializedProperty _updatePosition;
    private SerializedProperty _nativeContent;
    private SerializedProperty _providesChildSpritePosition;
    private SerializedProperty _providesChildSpriteAffine;
    private SerializedProperty _spritePosition;
    private SerializedProperty _spriteScale;
    private SerializedProperty _spriteSkew;

    private void OnEnable()
    {
        _position = serializedObject.FindProperty("position");
        _scale = serializedObject.FindProperty("scale");
        _skew = serializedObject.FindProperty("skew");
        _brightness = serializedObject.FindProperty("brightness");
        _isBright = serializedObject.FindProperty("isBright");
        _alpha = serializedObject.FindProperty("alpha");
        _alphaCoef = serializedObject.FindProperty("alphaCoef");
        _updatePosition = serializedObject.FindProperty("updatePosition");
        _nativeContent = serializedObject.FindProperty("nativeContent");
        _providesChildSpritePosition = serializedObject.FindProperty(
            "providesChildSpritePosition");
        _providesChildSpriteAffine = serializedObject.FindProperty(
            "providesChildSpriteAffine");
        _spritePosition = serializedObject.FindProperty("spritePosition");
        _spriteScale = serializedObject.FindProperty("spriteScale");
        _spriteSkew = serializedObject.FindProperty("spriteSkew");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("FLA Transform");
        EditorGUILayout.PropertyField(_position);
        EditorGUILayout.PropertyField(_scale);
        EditorGUILayout.PropertyField(_skew);
        EditorGUILayout.PropertyField(_updatePosition);

        DrawHeader("Appearance");
        EditorGUILayout.PropertyField(_brightness);
        EditorGUILayout.PropertyField(_isBright);
        EditorGUILayout.PropertyField(_alpha);
        EditorGUILayout.PropertyField(_alphaCoef);

        DrawHeader("Native Hierarchy");
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(_nativeContent);
        }

        DrawHeader("Child Reference Pose");
        EditorGUILayout.PropertyField(
            _providesChildSpritePosition,
            new GUIContent(
                "Position Provider",
                "Provide a fixed FLA-global reference position to child animation tracks."));
        EditorGUILayout.PropertyField(
            _providesChildSpriteAffine,
            new GUIContent(
                "Affine Provider",
                "Provide a fixed FLA-global position, scale and skew reference pose to child animation tracks."));

        var providesPosition = IsEnabled(_providesChildSpritePosition);
        var providesAffine = IsEnabled(_providesChildSpriteAffine);
        if (providesPosition || providesAffine)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(
                _spritePosition,
                new GUIContent("Reference Position"));
            if (providesAffine)
            {
                EditorGUILayout.PropertyField(
                    _spriteScale,
                    new GUIContent("Reference Scale"));
                EditorGUILayout.PropertyField(
                    _spriteSkew,
                    new GUIContent("Reference Skew"));
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static bool IsEnabled(SerializedProperty property)
    {
        return property.hasMultipleDifferentValues || property.boolValue;
    }

    private static void DrawHeader(string title)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
}
}
