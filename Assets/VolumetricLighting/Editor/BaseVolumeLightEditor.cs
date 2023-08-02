using UnityEngine;

namespace Other.VolumetricLighting.Scripts.Editor
{
    using UnityEditor;

    public class BaseVolumeLightEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var volumeLight = target as BaseVolumeLight;

            if (volumeLight.Light == null)
            {
                EditorGUILayout.HelpBox("Light is null", MessageType.Error);
                return;
            }

            if (!volumeLight.LightTypeIsSupported())
            {
                EditorGUILayout.HelpBox("Light type is not supported", MessageType.Error);
                return;
            }
                
            var t =EditorGUILayout.FloatField(new GUIContent("Intensity", "强度"), volumeLight.intensity);
            volumeLight.intensity =  Mathf.Max(0, t);
            volumeLight.mieK = EditorGUILayout.Slider(new GUIContent("MieK", "米氏散射系数"), volumeLight.mieK, -1, 1);
        }
    }

    [CustomEditor(typeof(PointVolumeLight))]
    public class SpotHelperEditor : BaseVolumeLightEditor
    {
    }

    [CustomEditor(typeof(SpotVolumeLight))]
    public class SpotVolumeLightEditor : BaseVolumeLightEditor
    {
    }
}