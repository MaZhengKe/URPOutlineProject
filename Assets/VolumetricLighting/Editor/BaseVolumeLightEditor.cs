using UnityEngine;

namespace KuanMi.VolumetricLighting.Editor
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
                
            var t =EditorGUILayout.FloatField(new GUIContent("Intensity", "强度"), volumeLight.Intensity);
            volumeLight.Intensity =  Mathf.Max(0, t);
            volumeLight.MieK = EditorGUILayout.Slider(new GUIContent("MieK", "米氏散射系数\n0：均匀散射\n1：仅向光线方向散射\n-1：仅向光线反向散射"), volumeLight.MieK, -1, 1);
            volumeLight.NumSteps = EditorGUILayout.Slider(new GUIContent("NumSteps", "采样次数\nTAA下3次足矣"), volumeLight.NumSteps, 1, 16);
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