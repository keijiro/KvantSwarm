//
// Custom editor class for Swarm2
//
using UnityEngine;
using UnityEditor;

namespace Kvant
{
    [CanEditMultipleObjects, CustomEditor(typeof(Swarm2))]
    public class Swarm2Editor : Editor
    {
        SerializedProperty _lineCount;
        SerializedProperty _historyLength;

        SerializedProperty _minAcceleration;
        SerializedProperty _maxAcceleration;
        SerializedProperty _damp;

        SerializedProperty _attractor;
        SerializedProperty _spread;
        SerializedProperty _flow;

        SerializedProperty _noiseAmplitude;
        SerializedProperty _noiseFrequency;
        SerializedProperty _noiseSpeed;
        SerializedProperty _noiseVariance;

        SerializedProperty _swirlStrength;
        SerializedProperty _swirlDensity;

        SerializedProperty _lineWidth;
        SerializedProperty _colorMode;
        SerializedProperty _color1;
        SerializedProperty _color2;
        SerializedProperty _metallic;
        SerializedProperty _smoothness;

        SerializedProperty _castShadows;
        SerializedProperty _receiveShadows;

        SerializedProperty _fixTimeStep;
        SerializedProperty _stepsPerSecond;
        SerializedProperty _randomSeed;

        static GUIContent _textAcceleration = new GUIContent("Acceleration");
        static GUIContent _textAmplitude    = new GUIContent("Amplitude");
        static GUIContent _textFrequency    = new GUIContent("Frequency");
        static GUIContent _textSpeed        = new GUIContent("Speed");
        static GUIContent _textVariance     = new GUIContent("Variance");

        void OnEnable()
        {
            _lineCount     = serializedObject.FindProperty("_lineCount");
            _historyLength = serializedObject.FindProperty("_historyLength");

            _minAcceleration = serializedObject.FindProperty("_minAcceleration");
            _maxAcceleration = serializedObject.FindProperty("_maxAcceleration");
            _damp            = serializedObject.FindProperty("_damp");

            _attractor = serializedObject.FindProperty("_attractor");
            _spread    = serializedObject.FindProperty("_spread");
            _flow      = serializedObject.FindProperty("_flow");

            _noiseAmplitude = serializedObject.FindProperty("_noiseAmplitude");
            _noiseFrequency = serializedObject.FindProperty("_noiseFrequency");
            _noiseSpeed     = serializedObject.FindProperty("_noiseSpeed");
            _noiseVariance  = serializedObject.FindProperty("_noiseVariance");

            _swirlStrength = serializedObject.FindProperty("_swirlStrength");
            _swirlDensity  = serializedObject.FindProperty("_swirlDensity");

            _lineWidth  = serializedObject.FindProperty("_lineWidth");
            _colorMode  = serializedObject.FindProperty("_colorMode");
            _color1     = serializedObject.FindProperty("_color1");
            _color2     = serializedObject.FindProperty("_color2");
            _metallic   = serializedObject.FindProperty("_metallic");
            _smoothness = serializedObject.FindProperty("_smoothness");

            _castShadows    = serializedObject.FindProperty("_castShadows");
            _receiveShadows = serializedObject.FindProperty("_receiveShadows");

            _fixTimeStep    = serializedObject.FindProperty("_fixTimeStep");
            _stepsPerSecond = serializedObject.FindProperty("_stepsPerSecond");
            _randomSeed     = serializedObject.FindProperty("_randomSeed");
        }

        public override void OnInspectorGUI()
        {
            var instance = target as Swarm2;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_lineCount);
            EditorGUILayout.PropertyField(_historyLength);

            if (EditorGUI.EndChangeCheck()) instance.NotifyConfigChange();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Dynamics", EditorStyles.boldLabel);
            MinMaxSlider(_textAcceleration, _minAcceleration, _maxAcceleration, 0.01f, 10.0f);
            EditorGUILayout.Slider(_damp, 0, 5);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("External Forces", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_attractor);
            EditorGUILayout.Slider(_spread, 0, 5);
            EditorGUILayout.PropertyField(_flow);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Turbulent Noise", EditorStyles.boldLabel);
            EditorGUILayout.Slider(_noiseAmplitude, 0.0f, 10.0f, _textAmplitude);
            EditorGUILayout.Slider(_noiseFrequency, 0.01f, 1.0f, _textFrequency);
            EditorGUILayout.Slider(_noiseSpeed, 0.0f, 5.0f, _textSpeed);
            EditorGUILayout.Slider(_noiseVariance, 0.0f, 10.0f, _textVariance);

            EditorGUILayout.Space();

            EditorGUILayout.Slider(_swirlStrength, 0.0f, 2.0f);
            EditorGUILayout.Slider(_swirlDensity, 0.01f, 5.0f);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
            EditorGUILayout.Slider(_lineWidth, 0, 0.5f);
            EditorGUILayout.PropertyField(_colorMode);
            EditorGUILayout.PropertyField(_color1);
            EditorGUILayout.PropertyField(_color2);
            EditorGUILayout.Slider(_metallic, 0, 1);
            EditorGUILayout.Slider(_smoothness, 0, 1);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_castShadows);
            EditorGUILayout.PropertyField(_receiveShadows);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_fixTimeStep);
            if (_fixTimeStep.hasMultipleDifferentValues || _fixTimeStep.boolValue)
                EditorGUILayout.PropertyField(_stepsPerSecond);
            EditorGUILayout.PropertyField(_randomSeed);

            serializedObject.ApplyModifiedProperties();
        }

        void MinMaxSlider(GUIContent label, SerializedProperty propMin, SerializedProperty propMax, float minLimit, float maxLimit)
        {
            var min = propMin.floatValue;
            var max = propMax.floatValue;

            EditorGUI.BeginChangeCheck();

            // Min-max slider.
            EditorGUILayout.MinMaxSlider(label, ref min, ref max, minLimit, maxLimit);

            var prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Float value boxes.
            var rect = EditorGUILayout.GetControlRect();
            rect.x += EditorGUIUtility.labelWidth;
            rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2 - 2;

            if (EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.labelWidth = 28;
                min = Mathf.Clamp(EditorGUI.FloatField(rect, "min", min), minLimit, max);
                rect.x += rect.width + 4;
                max = Mathf.Clamp(EditorGUI.FloatField(rect, "max", max), min, maxLimit);
                EditorGUIUtility.labelWidth = 0;
            }
            else
            {
                min = Mathf.Clamp(EditorGUI.FloatField(rect, min), minLimit, max);
                rect.x += rect.width + 4;
                max = Mathf.Clamp(EditorGUI.FloatField(rect, max), min, maxLimit);
            }

            EditorGUI.indentLevel = prevIndent;

            if (EditorGUI.EndChangeCheck()) {
                propMin.floatValue = min;
                propMax.floatValue = max;
            }
        }
    }
}
