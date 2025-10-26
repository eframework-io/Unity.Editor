// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using EFramework.Unity.Editor;
using EFramework.Unity.Utility;
using UnityEngine;

/// <summary>
/// TestXEditorMinIO 是 XEditor.MinIO 的单元测试。
/// </summary>
public class TestXEditorMinIO
{
    private string testDir;

    [OneTimeSetUp]
    public void Setup()
    {
        testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorMinIO");
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
        XFile.CreateDirectory(testDir);

        var mcBin = XFile.PathJoin(XEnv.ProjectPath, "Library", Application.platform == RuntimePlatform.WindowsEditor ? "mc.exe" : "mc");
        if (XFile.HasFile(mcBin)) XFile.DeleteFile(mcBin);

        XFile.SaveText(XFile.PathJoin(testDir, "test.txt"), $"test content {XTime.GetMillisecond()}");
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
    }

    [Test]
    public void Execute()
    {
        var mc = new XEditor.MinIO
        {
            ID = "TestXEditorMinIO/my-upload",
            Endpoint = "http://localhost:9000",
            Bucket = "default",
            Access = "admin",
            Secret = "adminadmin",
            Local = testDir,
            Remote = "TestXEditorMinIO",
            Batchmode = Application.isBatchMode
        };

        var report = XEditor.Tasks.Execute(mc);

        Assert.That(report.Result, Is.EqualTo(XEditor.Tasks.Result.Succeeded), "MinIO 上传任务应该成功执行完成。");

        // 验证上传的文件内容
        var localContent = XFile.OpenText(XFile.PathJoin(testDir, "test.txt"));
        var tempFile = XFile.PathJoin(testDir, "temp.txt");

        // 下载远端文件
        var task = XEditor.Command.Run(
            bin: mc.Bin,
            args: new string[] {
                    "get",
                    $"\"{mc.Alias}/{mc.Bucket}/{mc.Remote}/test.txt\"",
                    tempFile,
                    "--config-dir",
                    mc.Temp
            }
        );
        task.Wait();

        Assert.That(task.Result.Code, Is.EqualTo(0), "MinIO 客户端下载命令应该成功执行。");

        // 比较文件内容
        var remoteContent = XFile.OpenText(tempFile);
        Assert.That(remoteContent, Is.EqualTo(localContent), "上传后的远端文件内容应该与本地文件完全一致。");

        Assert.That(XFile.HasDirectory(mc.Temp), Is.False, "任务完成后临时目录应该被清理。");
    }
}
