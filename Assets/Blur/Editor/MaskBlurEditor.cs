using KuanMi.Blur;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blur.Editor
{
    
    [CustomEditor(typeof(MaskBlur))]
    public class MaskBlurEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_isMask;
        SerializedDataParameter m_maskType;
        SerializedDataParameter m_areaSize;
        SerializedDataParameter m_areaSmooth;
        SerializedDataParameter m_offset;
        
        SerializedDataParameter m_center;
        SerializedDataParameter m_radius;


        public override void OnEnable()
        {
            var o = new PropertyFetcher<MaskBlur>(serializedObject);
            m_isMask = Unpack(o.Find(x => x.isMask));
            m_maskType = Unpack(o.Find(x => x.maskType));
            m_areaSize = Unpack(o.Find(x => x.areaSize));
            m_areaSmooth = Unpack(o.Find(x => x.areaSmooth));
            m_offset = Unpack(o.Find(x => x.offset));
            m_center = Unpack(o.Find(x => x.center));
            m_radius = Unpack(o.Find(x => x.radius));
        }


        public override void OnInspectorGUI()
        {
            PropertyField(m_isMask);
            PropertyField(m_maskType);
            
            if(m_maskType.value.intValue == (int)MaskBlur.MaskType.Rectangle)
            {
                PropertyField(m_offset);
                PropertyField(m_areaSize);
                PropertyField(m_areaSmooth);
            }
            else if(m_maskType.value.intValue == (int)MaskBlur.MaskType.Circle)
            {
                PropertyField(m_center);
                PropertyField(m_radius);
                PropertyField(m_areaSmooth);
            }
        }
    }
}