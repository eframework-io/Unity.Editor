// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Reflection;
using NUnit.Framework;
using EFramework.Unity.Editor;

/// <summary>
/// TestXEditorConstants 是 XEditor.Constants 的单元测试。
/// </summary>
public class TestXEditorConstants
{
    /// <summary>
    /// 用于测试的自定义属性特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    private class MyPropertyAttribute : Attribute { }

    /// <summary>
    /// 用于测试的常量类。
    /// </summary>
    [XEditor.Constants]
    private class MyConstants
    {
        /// <summary>
        /// 自定义的测试属性。
        /// </summary>
        [MyProperty]
        public static string CustomValue => "my_value";
    }

    [TestCase(typeof(MyPropertyAttribute), null, "my_value", Description = "验证存在的自定义特性 MyPropAttribute")]
    [TestCase(typeof(ObsoleteAttribute), null, null, Description = "验证不存在的特性 ObsoleteAttribute")]
    [TestCase(typeof(ObsoleteAttribute), "default", "default", Description = "验证不存在的特性 ObsoleteAttribute，使用默认值")]
    [TestCase(null, "default", "default", Description = "验证空特性类型 null，使用默认值")]
    [TestCase(typeof(SerializableAttribute), "default", "default", Description = "验证无效的特性类型 SerializableAttribute，使用默认值")]
    public void GetCustom(Type attributeType, object defaultValue, object expectedValue)
    {
        bool sig = false;
        PropertyInfo prop = null;

        // 第一次调用
        var result1 = XEditor.Constants.GetCustom(attributeType, ref sig, ref prop, defaultValue);
        Assert.That(result1, Is.EqualTo(expectedValue),
            "首次查找特性 {0} 应返回 {1}",
            attributeType?.Name ?? "null",
            expectedValue ?? "null");

        // 重置 sig 但保留 prop，测试缓存
        sig = false;
        var result2 = XEditor.Constants.GetCustom(attributeType, ref sig, ref prop, defaultValue);
        Assert.That(result2, Is.EqualTo(expectedValue),
            "使用缓存的属性信息查找特性 {0} 应返回 {1}",
            attributeType?.Name ?? "null",
            expectedValue ?? "null");

        // 完全重置，测试重新查找
        sig = false;
        prop = null;
        var result3 = XEditor.Constants.GetCustom(attributeType, ref sig, ref prop, defaultValue);
        Assert.That(result3, Is.EqualTo(expectedValue),
            "重新查找特性 {0} 应返回 {1}",
            attributeType?.Name ?? "null",
            expectedValue ?? "null");

        // 使用不同的默认值测试
        var differentDefault = "different";
        var result4 = XEditor.Constants.GetCustom(attributeType, ref sig, ref prop, differentDefault);
        var expectedResult4 = prop != null ? expectedValue : differentDefault;
        Assert.That(result4, Is.EqualTo(expectedResult4),
            "使用不同默认值 {0} 查找特性 {1} 应返回 {2}",
            differentDefault,
            attributeType?.Name ?? "null",
            expectedResult4 ?? "null");
    }
}
