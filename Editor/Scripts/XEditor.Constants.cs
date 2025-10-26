// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;

namespace EFramework.Unity.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// ConstantsAttribute 是用于标记常量配置类的特性。
        /// 被此特性标记的类将被自动识别为常量配置类。
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
        public class ConstantsAttribute : Attribute { }

        /// <summary>
        /// XEditor.Constants 提供了编辑器常量配置的管理功能，支持通过特性标记的方式自定义常量值。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持特性标记自定义：使用 Constants 特性标记类来定义常量配置
        /// - 提供默认配置路径：为常用路径提供默认配置，无需手动设置
        /// - 支持运行时动态获取：在运行时自动检测并应用自定义配置值
        /// - 提供类型安全访问：通过泛型方法确保类型安全的配置获取
        /// - 实现配置覆盖机制：允许通过特性标记方式灵活覆盖默认配置
        /// 
        /// 使用手册
        /// 1. 常量配置类定义
        /// 
        /// 1.1 标记配置类
        /// 
        ///     [XEditor.Constants]
        ///     public static class MyConstants
        ///     {
        ///         // 常量配置属性
        ///     }
        /// 
        /// 2. 配置值获取
        /// 
        /// 2.1 获取自定义配置
        /// 
        ///     object value = XEditor.Constants.GetCustom(typeof(MyAttribute), ref sig, ref prop, defaultValue);
        /// 
        /// 2.2 泛型配置获取
        /// 
        ///     string value = XEditor.Constants.GetCoustom&lt;MyAttribute, string&gt;(ref sig, ref prop, "default");
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Constants
        {
            /// <summary>
            /// constantTypes 存储所有被 <see cref="Constants"/> 标记的类型列表。
            /// </summary>
            internal static List<Type> constantTypes;

            /// <summary>
            /// GetCustom 获取自定义属性值，如果未找到自定义值则返回默认值。
            /// </summary>
            /// <param name="attributeType">属性特性类型。</param>
            /// <param name="sig">标记是否已初始化。</param>
            /// <param name="prop">属性信息引用。</param>
            /// <returns>找到的属性信息。</returns>
            public static object GetCustom(Type attributeType, ref bool sig, ref PropertyInfo prop, object defval = null)
            {
                if (attributeType == null) return defval;
                if (sig == false && prop == null)
                {
                    sig = true;
                    constantTypes ??= TypeCache.GetTypesWithAttribute<ConstantsAttribute>().ToList();
                    foreach (var type in constantTypes)
                    {
                        var props = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var temp in props)
                        {
                            if (temp.GetCustomAttribute(attributeType) != null)
                            {
                                prop = temp;
                                break;
                            }
                        }
                    }
                }
                if (prop != null) return prop.GetValue(null);
                else return defval;
            }

            /// <summary>
            /// GetCoustom 获取自定义属性值，如果未找到自定义值则返回默认值。
            /// </summary>
            /// <typeparam name="TProp">属性特性类型。</typeparam>
            /// <typeparam name="TRet">返回值类型。</typeparam>
            /// <param name="sig">标记是否已初始化。</param>
            /// <param name="prop">属性信息引用。</param>
            /// <param name="defval">默认值。</param>
            /// <returns>自定义属性值或默认值。</returns>
            public static TRet GetCoustom<TProp, TRet>(ref bool sig, ref PropertyInfo prop, TRet defval)
                where TProp : Attribute
                where TRet : class
            {
                return GetCustom(typeof(TProp), ref sig, ref prop, defval) as TRet;
            }
        }
    }
}
