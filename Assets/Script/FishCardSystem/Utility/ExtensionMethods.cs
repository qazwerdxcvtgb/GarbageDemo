using UnityEngine;

namespace FishCardSystem
{
    /// <summary>
    /// 静态扩展方法工具类
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// 数值重映射：将值从一个范围映射到另一个范围
        /// </summary>
        /// <param name="value">要映射的值</param>
        /// <param name="from1">源范围最小值</param>
        /// <param name="to1">源范围最大值</param>
        /// <param name="from2">目标范围最小值</param>
        /// <param name="to2">目标范围最大值</param>
        /// <returns>映射后的值</returns>
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}
