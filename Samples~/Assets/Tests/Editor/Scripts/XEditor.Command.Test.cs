// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using UnityEngine;
using EFramework.Unity.Editor;
using EFramework.Unity.Utility;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

/// <summary>
/// TestXEditorCommand 是 XEditor.Command 的单元测试。
/// </summary>
public class TestXEditorCommand
{
    private string testDir;

    private string succeededCmdFile;

    private string succeededCmdName;

    private string failedCmdFile;

    private string failedCmdName;

    [OneTimeSetUp]
    public void Setup()
    {
        // 创建测试目录
        testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorCommand");
        if (!XFile.HasDirectory(testDir)) XFile.CreateDirectory(testDir);

        // 创建测试命令文件
        succeededCmdName = Application.platform == RuntimePlatform.WindowsEditor ? "succeeded.cmd" : "succeeded";
        succeededCmdFile = XFile.PathJoin(testDir, succeededCmdName);
        XFile.SaveText(succeededCmdFile, Application.platform == RuntimePlatform.WindowsEditor ?
            "@echo Hello World\r\n@exit 0" :  // Windows 命令格式
            "#!/bin/bash\necho Hello World\nexit 0");  // Unix 命令格式

        failedCmdName = Application.platform == RuntimePlatform.WindowsEditor ? "failed.cmd" : "failed";
        failedCmdFile = XFile.PathJoin(testDir, failedCmdName);
        XFile.SaveText(failedCmdFile, Application.platform == RuntimePlatform.WindowsEditor ?
            "@echo Hello World\r\n@exit 1" :  // Windows 命令格式
            "#!/bin/bash\necho Hello World\nexit 1");  // Unix 命令格式

        // 非 Windows 平台设置执行权限
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            XEditor.Command.Run("/bin/chmod", testDir, false, false, "+x", succeededCmdFile).Wait();
            XEditor.Command.Run("/bin/chmod", testDir, false, false, "+x", failedCmdFile).Wait();
        }
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
    }

    [Test]
    public void Find()
    {
        Assert.AreEqual(XEditor.Command.Find(""), "", "空命令名称应返回空字符串");
        Assert.AreEqual(XEditor.Command.Find("nonexistent"), "nonexistent", "不存在的命令应返回原字符串");
        Assert.AreEqual(XEditor.Command.Find(succeededCmdName, testDir), succeededCmdFile, "指定路径的命令应返回完整路径");
    }

    [TestCase(false, false, "succeeded", 0, TestName = "验证成功命令执行，无打印输出，无进度显示")]
    [TestCase(true, false, "succeeded", 0, TestName = "验证成功命令执行，启用打印输出，无进度显示")]
    [TestCase(false, true, "failed", 1, TestName = "验证失败命令执行，无打印输出，启用进度显示")]
    [TestCase(true, true, "failed", 1, TestName = "验证失败命令执行，启用打印输出和进度显示")]
    public void Run(bool print, bool progress, string cmd, int code)
    {
        if (print)
        {
            if (code != 0) LogAssert.Expect(LogType.Error, new Regex(@"XEditor\.Command\.Run: finish .* with code: .*"));
            else LogAssert.Expect(LogType.Log, new Regex(@"XEditor\.Command\.Run: finish .* with code: .*"));
        }
        var task = XEditor.Command.Run(bin: XEditor.Command.Find(cmd, testDir), print: print, progress: progress);
        task.Wait();
        Assert.That(task.Result.Code, Is.EqualTo(code), "命令应返回正确的退出码");
    }
}
