// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using EFramework.Unity.Editor;
using EFramework.Unity.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// TestXEditorNpm 是 XEditor.Npm 的单元测试。
/// </summary>
public class TestXEditorNpm
{
    internal string testDir;

    [OneTimeSetUp]
    public void Setup()
    {
        // 创建测试目录和package.json
        testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorNpm");
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
        XFile.CreateDirectory(testDir);

        // 创建my-task.js
        var jsContent =
@"const args = process.argv.slice(2);
console.log(JSON.stringify({
    args: args.reduce((acc, arg) => {
        const [key, value] = arg.replace(/^--/, '').split('=');
        acc[key] = value;
        return acc;
    }, {})
}));";
        XFile.SaveText(XFile.PathJoin(testDir, "my-task.js"), jsContent);

        // 创建package.json，定义一个简单的测试命令
        var packageJson = $@"{{
            ""scripts"": {{
                ""my-task"": ""node my-task.js""
            }}
        }}";
        XFile.SaveText(XFile.PathJoin(testDir, "package.json"), packageJson);
    }

    [OneTimeTearDown]
    public void Reset()
    {
        if (XFile.HasDirectory(testDir))
        {
            XFile.DeleteDirectory(testDir);
        }
    }

    [Test]
    public void Execute()
    {
        // 有些 npm 版本会通过 stderr 输出 npm notice... 信息
        // 这里忽略这些信息
        LogAssert.ignoreFailingMessages = true;

        // 创建npm任务
        var npm = new XEditor.Npm(id: "my-task", script: "my-task", runasync: false, cwd: testDir, batchmode: Application.isBatchMode);

        // 执行任务并传递参数
        var args = new Dictionary<string, string>
        {
            { "param1", "value1" },
            { "param2", "value2" }
        };
        var report = XEditor.Tasks.Execute(npm, args);
        report.Task.Wait();

        // 验证执行结果
        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded), "NPM 任务应该成功执行完成。");

        Assert.That(report.Extras, Is.Not.Null, "任务报告的附加信息不应为空。");

        var cmdResult = report.Extras as XEditor.Command.Result;
        Assert.That(cmdResult, Is.Not.Null, "任务报告应包含命令执行结果。");

        Assert.That(cmdResult.Data, Contains.Substring("\"args\":{"), "输出应包含参数对象。");

        Assert.That(cmdResult.Data, Contains.Substring("\"param1\":\"value1\""), "输出应包含第一个测试参数。");

        Assert.That(cmdResult.Data, Contains.Substring("\"param2\":\"value2\""), "输出应包含第二个测试参数。");

        LogAssert.ignoreFailingMessages = false;
    }
}
