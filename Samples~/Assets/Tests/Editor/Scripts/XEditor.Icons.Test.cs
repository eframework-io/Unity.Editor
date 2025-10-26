// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using UnityEngine;
using EFramework.Unity.Editor;

/// <summary>
/// TestXEditorIcons 是 XEditor.Icons 的单元测试。
/// </summary>
public class TestXEditorIcons
{
    private XEditor.Icons iconsWindow;

    [SetUp]
    public void Setup()
    {
        iconsWindow = ScriptableObject.CreateInstance<XEditor.Icons>();
    }

    [TearDown]
    public void Cleanup()
    {
        if (iconsWindow != null)
        {
            Object.DestroyImmediate(iconsWindow);
        }
    }

    [Test]
    public void List()
    {
        Assert.That(XEditor.Icons.List, Is.Not.Empty, "图标列表不应为空，应包含系统预设图标。");
        Assert.That(XEditor.Icons.List.Length, Is.GreaterThan(0), "图标列表长度应大于 0，至少包含基础系统图标。");
    }

    [TestCase("Folder Icon", true)]
    [TestCase("NonExistentIcon_12345", false)]
    public void Find(string iconName, bool shouldExist)
    {
        var icon = XEditor.Icons.GetIcon(iconName);
        if (shouldExist)
        {
            Assert.That(icon, Is.Not.Null, $"查找已存在的图标 '{iconName}' 应返回有效的图标对象。");
            Assert.That(icon.image, Is.Not.Null, $"已存在图标 '{iconName}' 的纹理不应为空。");
        }
        else
        {
            Assert.That(icon, Is.Null, $"查找不存在的图标 '{iconName}' 应返回 null。");
        }
    }
}
