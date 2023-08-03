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

            volumeLight.MaxIntensity = EditorGUILayout.ColorField(new GUIContent("MaxIntensity", "最大强度"), volumeLight.MaxIntensity,true,false,true);
            
            volumeLight.DistanceAttenuation = EditorGUILayout.Slider(new GUIContent("DistanceAttenuation", "距离衰减"), volumeLight.DistanceAttenuation, 0, 1);
            volumeLight.ShadowAttenuation = EditorGUILayout.Slider(new GUIContent("ShadowAttenuation", "阴影反向衰减强度"), volumeLight.ShadowAttenuation, -1, 10);
            
            volumeLight.MieK = EditorGUILayout.Slider(new GUIContent("MieK", "米氏散射系数\n0：均匀散射\n1：仅向光线方向散射\n-1：仅向光线反向散射"), volumeLight.MieK, -1, 1);
            volumeLight.NumSteps = EditorGUILayout.Slider(new GUIContent("NumSteps", "采样次数"), volumeLight.NumSteps, 1, 16);
            volumeLight.Evenness = EditorGUILayout.FloatField(new GUIContent("Evenness", "越大越均匀"), volumeLight.Evenness);
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