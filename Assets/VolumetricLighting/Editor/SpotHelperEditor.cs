using UnityEngine;

namespace Other.VolumetricLighting.Scripts.Editor
{
    using UnityEditor;
    
    [CustomEditor(typeof(SpotVolumeLight))]
    public class SpotHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var spotHelper = target as SpotVolumeLight;
            spotHelper.intensity = EditorGUILayout.FloatField(new GUIContent("Intensity","强度"), spotHelper.intensity);
            spotHelper.mieK = EditorGUILayout.Slider(new GUIContent("MieK","米氏散射系数"), spotHelper.mieK, -1, 1); }
    }
}