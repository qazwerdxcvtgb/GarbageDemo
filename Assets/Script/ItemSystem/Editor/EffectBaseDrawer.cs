#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ItemSystem
{
    /// <summary>
    /// EffectBase 的自定义 PropertyDrawer
    /// 支持 [SerializeReference] 多态类型选择
    /// </summary>
    [CustomPropertyDrawer(typeof(EffectBase), true)]
    public class EffectBaseDrawer : PropertyDrawer
    {
        // 缓存所有 EffectBase 子类
        private static Type[] effectTypes;
        private static string[] effectDisplayNames;
        
        static EffectBaseDrawer()
        {
            // 查找所有非抽象的 EffectBase 子类
            effectTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => 
                {
                    try { return assembly.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => t.IsSubclassOf(typeof(EffectBase)) && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToArray();
            
            // 获取显示名称
            effectDisplayNames = new string[effectTypes.Length + 1];
            effectDisplayNames[0] = "(无)";
            
            for (int i = 0; i < effectTypes.Length; i++)
            {
                try
                {
                    var instance = Activator.CreateInstance(effectTypes[i]) as EffectBase;
                    effectDisplayNames[i + 1] = instance?.DisplayName ?? effectTypes[i].Name;
                }
                catch
                {
                    effectDisplayNames[i + 1] = effectTypes[i].Name;
                }
            }
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float currentY = position.y;
            
            // 获取当前类型索引
            int currentIndex = 0;
            object currentValue = property.managedReferenceValue;
            if (currentValue != null)
            {
                Type currentType = currentValue.GetType();
                currentIndex = Array.IndexOf(effectTypes, currentType) + 1;
            }
            
            // 绘制折叠标题和类型选择
            Rect headerRect = new Rect(position.x, currentY, position.width, lineHeight);
            
            // 绘制折叠箭头
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, currentY, 15, lineHeight),
                property.isExpanded,
                GUIContent.none,
                true
            );
            
            // 绘制类型选择下拉菜单
            Rect popupRect = new Rect(position.x + 15, currentY, position.width - 15, lineHeight);
            
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, label.text, currentIndex, effectDisplayNames);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                {
                    // 选择"(无)"
                    property.managedReferenceValue = null;
                }
                else if (newIndex > 0 && newIndex <= effectTypes.Length)
                {
                    // 创建新实例
                    Type selectedType = effectTypes[newIndex - 1];
                    try
                    {
                        property.managedReferenceValue = Activator.CreateInstance(selectedType);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"无法创建 {selectedType.Name} 实例: {e.Message}");
                    }
                }
            }
            
            currentY += lineHeight + spacing;
            
            // 如果展开且有值，绘制属性
            if (property.isExpanded && property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                
                // 遍历所有子属性
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();
                
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;
                    
                    Rect propRect = new Rect(position.x, currentY, position.width, EditorGUI.GetPropertyHeight(iterator, true));
                    EditorGUI.PropertyField(propRect, iterator, true);
                    currentY += propRect.height + spacing;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float height = lineHeight + spacing; // 类型选择行
            
            // 如果展开且有值，计算子属性高度
            if (property.isExpanded && property.managedReferenceValue != null)
            {
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();
                
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;
                    height += EditorGUI.GetPropertyHeight(iterator, true) + spacing;
                }
            }
            
            return height;
        }
    }
}
#endif
