using UnityEngine;

namespace FishCardSystem
{
    /// <summary>
    /// 手牌弧线排布参数（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "CurveParameters", menuName = "FishCard/Curve Parameters")]
    public class CurveParameters : ScriptableObject
    {
        [Header("位置曲线")]
        [Tooltip("位置曲线：建议 (0,0)、(0.5,1)、(1,0)，中间凸起")]
        public AnimationCurve positioning;

        [Tooltip("位置影响系数：建议 0.02～0.1")]
        public float positioningInfluence = 0.1f;

        [Header("旋转曲线")]
        [Tooltip("旋转曲线：建议 (0,1)、(0.5,0)、(1,-1)，两端倾斜")]
        public AnimationCurve rotation;

        [Tooltip("旋转影响系数：建议 1～10")]
        public float rotationInfluence = 10f;
    }
}
