// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.MinIO 提供了基于 MinIO 客户端的存储服务集成，支持资源上传和下载，简化了云存储操作流程，适用于资源分发和远端部署场景。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持主流云存储平台：基于任务系统实现的云存储接口，易于扩展
        /// - 提供资源上传功能：支持批量资源上传，适用于构建产物部署
        /// - 实现资源下载功能：支持资源下载和验证，适用于远端资源获取
        /// - 集成任务系统：自动处理上传下载任务，支持进度显示和错误处理
        /// 
        /// 使用手册
        /// 1. 基本配置
        /// 
        /// 1.1 创建任务
        ///     // 创建并配置 MinIO 任务实例
        ///     var mc = new XEditor.MinIO {
        ///         ID = "my-upload",                    // 任务标识
        ///         Endpoint = "http://localhost:9000",  // 存储服务地址
        ///         Bucket = "default",                  // 存储分区名称
        ///         Access = "admin",                    // 存储服务凭据
        ///         Secret = "adminadmin"                // 存储服务密钥
        ///     };
        /// 
        /// 2. 文件操作
        /// 
        /// 2.1 上传文件
        ///     // 配置上传路径
        ///     mc.Local = "/path/to/local/file";      // 本地文件路径
        ///     mc.Remote = "path/in/bucket";          // 远端存储路径
        ///     
        ///     // 执行上传任务
        ///     var report = XEditor.Tasks.Execute(mc);
        /// 
        /// 2.2 上传目录
        ///     // 配置目录路径
        ///     mc.Local = "/path/to/local/directory"; // 本地目录路径
        ///     mc.Remote = "path/in/bucket";          // 远端存储路径
        ///     
        ///     // 执行上传任务
        ///     var report = XEditor.Tasks.Execute(mc);
        /// 
        /// 2.3 路径处理
        ///     // 1. 基本路径
        ///     mc.Remote = "path/in/bucket";          // 基本路径格式
        ///     
        ///     // 2. 目录上传时的路径处理
        ///     mc.Local = "/path/to/MyFolder";        // 本地目录
        ///     mc.Remote = "remote/MyFolder";         // 如果远端路径末尾包含目录名，会自动去除重复，实际存储路径为：remote/MyFolder/*
        ///     
        ///     // 3. 路径规范化
        ///     mc.Remote = "path/with/trailing/";     // 末尾斜杠会被自动移除
        ///     mc.Remote = "path\\with\\backslash";   // 反斜杠会被转换为正斜杠
        /// 
        /// 2.4 检查结果
        ///     if (report.Result == XEditor.Tasks.Result.Succeeded) {
        ///         Debug.Log("上传成功");
        ///     } else {
        ///         Debug.LogError($"上传失败: {report.Error}");
        ///     }
        /// 
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class MinIO : Tasks.Worker
        {
            /// <summary>
            /// Endpoint 是存储服务的地址。
            /// </summary>
            public string Endpoint;

            /// <summary>
            /// Bucket 是存储分区的名称。
            /// </summary>
            public string Bucket;

            /// <summary>
            /// Access 是存储服务的凭证。
            /// </summary>
            public string Access;

            /// <summary>
            /// Secret 是存储服务的密钥。
            /// </summary>
            public string Secret;

            /// <summary>
            /// Bin 是 MinIO 客户端可执行文件路径。
            /// 如果未指定，将自动下载对应平台的客户端，Windows 平台为 mc.exe，其他平台为 mc。
            /// </summary>
            public virtual string Bin { get; set; }

            /// <summary>
            /// alias 是 MinIO 客户端配置的别名。
            /// </summary>
            private string alias;

            /// <summary>
            /// Alias 获取或设置 MinIO 客户端配置的别名。
            /// </summary>
            public virtual string Alias
            {
                get
                {
                    if (string.IsNullOrEmpty(alias))
                    {
                        alias = GetType().FullName.Replace("+", ".").Replace(".", "-");
                    }
                    return alias;
                }
                set => alias = value;
            }

            /// <summary>
            /// temp 是临时目录路径，用于存储 MinIO 客户端配置和临时文件。
            /// </summary>
            private string temp;

            /// <summary>
            /// Temp 获取或设置临时目录路径。
            /// </summary>
            public virtual string Temp
            {
                get
                {
                    if (string.IsNullOrEmpty(temp))
                    {
                        temp = XFile.PathJoin(XEnv.ProjectPath, "Temp", $"MinIO-{XTime.GetMillisecond()}");
                        if (XFile.HasDirectory(temp)) XFile.DeleteDirectory(temp);
                        XFile.CreateDirectory(temp);
                    }
                    return temp;
                }
                set => temp = value;
            }

            /// <summary>
            /// Local 是本地文件或目录路径。
            /// </summary>
            public virtual string Local { get; set; }

            /// <summary>
            /// Remote 是远端存储路径。
            /// </summary>
            public virtual string Remote { get; set; }

            /// <summary>
            /// Preprocess 是预处理阶段。
            /// </summary>
            /// <param name="report">任务执行报告，用于记录处理结果和错误信息</param>
            /// <remarks>
            public override void Preprocess(Tasks.Report report)
            {
                // 根据平台确定MinIO客户端可执行文件名
                var name = Application.platform == RuntimePlatform.WindowsEditor ? "mc.exe" : "mc";

                // 尝试从环境变量中查找MinIO客户端
                Bin = Command.Find(name);

                // 如果未找到，则设为项目Library目录下的路径
                if (string.IsNullOrEmpty(Bin) || XFile.HasFile(Bin) == false) Bin = XFile.PathJoin(XEnv.ProjectPath, "Library", name);

                // 如果MinIO客户端不存在，则尝试下载
                if (!XFile.HasFile(Bin))
                {
                    var url = "";
                    var isCN = System.Globalization.RegionInfo.CurrentRegion.Name == "CN" ||
                                  System.Threading.Thread.CurrentThread.CurrentCulture.Name.StartsWith("zh-CN");
                    var baseUrl = isCN ? "https://dl.minio.org.cn/client/mc/release/" : "https://dl.min.io/client/mc/release/";

                    // 根据平台选择下载地址
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                        url = baseUrl + "windows-amd64/mc.exe";
                    else if (Application.platform == RuntimePlatform.LinuxEditor)
                        url = baseUrl + "linux-amd64/mc";
                    else if (Application.platform == RuntimePlatform.OSXEditor)
                        url = baseUrl + "darwin-amd64/mc";

                    XLog.Debug($"XEditor.MinIO.Process: start to download minio client from <a href=\"file:///{url}\">{url}</a>");

                    if (!string.IsNullOrEmpty(url))
                    {
                        // 在主线程中执行下载操作
                        XLoom.RunInMain(() =>
                        {
                            using var req = UnityWebRequest.Get(url);
                            req.SendWebRequest();
                            while (!req.isDone)
                            {
                                // 显示下载进度
                                if (EditorUtility.DisplayCancelableProgressBar("Download MinIO",
                                    $"Downloading MinIO Client...({XString.ToSize((long)req.downloadedBytes)})", req.downloadProgress))
                                {
                                    XLog.Error($"XEditor.MinIO.Process: download minio client from <a href=\"file:///{url}\">{url}</a> has been canceled.");
                                    break;
                                }
                            }
                            if (req.responseCode != 200 || !string.IsNullOrEmpty(req.error))
                            {
                                XLog.Error($"XEditor.MinIO.Process: download minio client from <a href=\"file:///{url}\">{url}</a> failed({req.responseCode}): {req.error}");
                            }
                            else if (req.isDone)
                            {
                                // 保存下载的客户端文件
                                XFile.SaveFile(Bin, req.downloadHandler.data);
#if !UNITY_EDITOR_WIN
                                // 非Windows平台设置可执行权限
                                Command.Run(bin: "chmod", args: new string[] { "755", Bin }).Wait();
#endif
                                XLog.Debug($"XEditor.MinIO.Process: download minio client from <a href=\"file:///{url}\">{url}</a> succeeded.");
                            }
                            EditorUtility.ClearProgressBar();
                        }).Wait();
                    }
                }

                // 如果MinIO客户端仍不存在，则报错
                if (XFile.HasFile(Bin) == false) report.Error = "MinIO Client was not found.";
                else
                {
                    // 设置MinIO客户端别名配置
                    var task = Command.Run(bin: Bin, args: new string[] { "alias", "set", Alias, Endpoint, Access, Secret, "--config-dir", Temp });
                    task.Wait();
                    if (task.Result.Code != 0)
                    {
                        report.Error = $"Run set alias with error: {task.Result.Error}";
                    }
                }
            }

            /// <summary>
            /// Process 是处理阶段。
            /// </summary>
            /// <param name="report">任务执行报告，用于记录处理结果和错误信息</param>
            public override void Process(Tasks.Report report)
            {
                // 验证远端路径
                if (string.IsNullOrEmpty(Remote))
                {
                    report.Error = "Remote uri is null.";
                    return;
                }

                // 验证本地路径是否存在
                if (!XFile.HasDirectory(Local) && !XFile.HasFile(Local))
                {
                    report.Error = "Local uri doesn't existed.";
                    return;
                }

                // 检查本地目录是否为空，若为空则无需上传
                if (XFile.HasDirectory(Local) && Directory.GetFiles(Local).Length == 0)
                {
                    XLog.Debug("XEditor.MinIO.Process: local uri was empty, no need to cp.");
                    return;
                }
                else
                {
                    // 处理本地和远端路径，确保格式正确
                    var local = Local;
                    if (local.EndsWith("/")) local = local[..^1];
                    var remote = Remote;
                    if (remote.EndsWith("/")) remote = remote[..^1];

                    // 处理目录名重复问题
                    var dir = Path.GetFileName(local);
                    if (remote.EndsWith(dir)) remote = remote[..^dir.Length];

                    // 执行上传命令
                    var task = Command.Run(bin: Bin, args: new string[] { "cp", "--recursive", $"\"{local}\"", $"\"{Alias}/{Bucket}/{remote}\"" });
                    task.Wait();
                    if (task.Result.Code != 0)
                    {
                        report.Error = $"Run cp with error: {task.Result.Error}";
                    }
                }
            }

            /// <summary>
            /// Postprocess 是后处理阶段。
            /// </summary>
            /// <param name="report">任务执行报告，用于记录处理结果和错误信息</param>
            public override void Postprocess(Tasks.Report report)
            {
                // 删除临时目录
                if (XFile.HasDirectory(Temp)) XFile.DeleteDirectory(Temp);
            }
        }
    }
}
