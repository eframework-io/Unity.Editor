// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using EFramework.Unity.Editor;
using EFramework.Unity.Utility;

/// <summary>
/// TestXEditorTitle 是 XEditor.Title 的单元测试。
/// </summary>
public class TestXEditorTitle
{
        [SetUp]
        public void Setup()
        {
                XEditor.Title.isRefreshing = false;
                XEditor.Title.preferencesLabel = "";
                XEditor.Title.gitBranch = "";
                XEditor.Title.gitPushCount = 0;
                XEditor.Title.gitPullCount = 0;
                XEditor.Title.gitDirtyCount = 0;
        }

        [TearDown]
        public void Cleanup()
        {
                XEditor.Title.isRefreshing = false;
                _ = XEditor.Title.Refresh();
        }

        [Obsolete]
        [TestCase("", "", 0, 0, 0, false, "Unity", Description = "无首选项和 Git 信息时的默认标题")]
        [TestCase("[Preferences: Test/Channel/1.0.0/Debug/Info]", "", 0, 0, 0, false, "Unity - [Preferences: Test/Channel/1.0.0/Debug/Info]", Description = "仅包含首选项标签的标题")]
        [TestCase("", "master", 1, 2, 3, false, "Unity - [Git*: master ↑2 ↓3]", Description = "仅包含 Git 信息的标题")]
        [TestCase("", "master", 0, 0, 0, true, "Unity - [Git: master ⟳]", Description = "刷新状态下的 Git 标题")]
        [TestCase("[Preferences: Test/Channel/1.0.0/Debug/Info]", "master", 1, 0, 0, false, "Unity - [Preferences: Test/Channel/1.0.0/Debug/Info] - [Git*: master]", Description = "首选项和 Git 信息组合的标")]
        public void SetTitle(string preferencesLabel, string gitBranch, int gitDirtyCount, int gitPushCount, int gitPullCount, bool isRefreshing, string expected)
        {
#if UNITY_6000_0_OR_NEWER
        var descriptor = new ApplicationTitleDescriptor("Unity", "Editor", "6000.0.32f1", "Personal", false) { title = "Unity" };
#else
                // 使用反射创建 ApplicationTitleDescriptor 实例
                var descriptorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ApplicationTitleDescriptor");
                var constructors = descriptorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#if UNITY_2022_1_OR_NEWER
        // 查找匹配的构造函数
        var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 5);
        var descriptor = constructor.Invoke(new object[] { "Unity", "Editor", "6000.0.32f1", "Personal", false });
#else
                var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 6);
                var descriptor = constructor.Invoke(new object[] { "Unity", "Editor", "6000.0.32f1", "", "Personal", false });
#endif
                // 使用反射设置 title 属性
                var titleProperty = descriptor.GetType().GetField("title", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                titleProperty.SetValue(descriptor, "Unity");
#endif
                XEditor.Title.preferencesLabel = preferencesLabel;
                XEditor.Title.gitBranch = gitBranch;
                XEditor.Title.gitDirtyCount = gitDirtyCount;
                XEditor.Title.gitPushCount = gitPushCount;
                XEditor.Title.gitPullCount = gitPullCount;
                XEditor.Title.isRefreshing = isRefreshing;
                XEditor.Title.SetTitle(descriptor);
#if UNITY_6000_0_OR_NEWER
        Assert.That(descriptor.title, Is.EqualTo(expected));
#else
                // 使用反射获取 title 属性值
                var actualTitle = titleProperty.GetValue(descriptor) as string;
                Assert.That(actualTitle, Is.EqualTo(expected));
#endif
        }

        [Test]
        public async Task Refresh()
        {
                XEditor.Title.isRefreshing = false;
                await XEditor.Title.Refresh();
                var preferencesName = string.IsNullOrEmpty(XPrefs.IAsset.Uri) ? "Unknown" : !XFile.HasFile(XPrefs.IAsset.Uri) ? $"{Path.GetFileName(XPrefs.IAsset.Uri)}(Deleted)" : Path.GetFileName(XPrefs.IAsset.Uri);
                var preferencesDirty = string.IsNullOrEmpty(XPrefs.IAsset.Uri) || !XFile.HasFile(XPrefs.IAsset.Uri) ? "*" : "";
                var expectedLabel = $"[Preferences{preferencesDirty}: {preferencesName}/{XEnv.Channel}/{XEnv.Version}/{XEnv.Mode}/{XLog.Level()}]";
                Assert.That(XEditor.Title.preferencesLabel, Is.EqualTo(expectedLabel), "Should update preferences label");
                var task = XEditor.Command.Run("git", print: false, args: new string[] { "rev-parse", "--git-dir" });
                if (task.Result.Code == 0) Assert.That(XEditor.Title.gitBranch, Is.Not.Empty); // 在 Git 仓库中
                else Assert.That(XEditor.Title.gitBranch, Is.Empty); // 不在 Git 仓库中
        }
}