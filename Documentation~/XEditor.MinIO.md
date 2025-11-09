# XEditor.MinIO

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.editor?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.editor)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.editor?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.editor)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Editor)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

提供了基于 MinIO 客户端的存储服务集成，支持资源上传和下载，简化了云存储操作流程，适用于资源分发和远端部署场景。

## 功能特性

- 支持主流云存储平台：基于任务系统实现的云存储接口，易于扩展
- 提供资源上传功能：支持批量资源上传，适用于构建产物部署
- 实现资源下载功能：支持资源下载和验证，适用于远端资源获取
- 集成任务系统：自动处理上传下载任务，支持进度显示和错误处理

## 使用手册

### 1. 基本配置

#### 1.1 创建任务
```csharp
// 创建并配置 MinIO 任务实例
var mc = new XEditor.MinIO {
    ID = "my-upload",                    // 任务标识
    Endpoint = "http://localhost:9000",  // 存储服务地址
    Bucket = "default",                  // 存储分区名称
    Access = "admin",                    // 存储服务凭据
    Secret = "adminadmin"                // 存储服务密钥
};
```

### 2. 文件操作

#### 2.1 上传文件
```csharp
// 配置上传路径
mc.Local = "/path/to/local/file";      // 本地文件路径
mc.Remote = "path/in/bucket";          // 远端存储路径

// 执行上传任务
var report = XEditor.Tasks.Execute(mc);
```

#### 2.2 上传目录
```csharp
// 配置目录路径
mc.Local = "/path/to/local/directory"; // 本地目录路径
mc.Remote = "path/in/bucket";          // 远端存储路径

// 执行上传任务
var report = XEditor.Tasks.Execute(mc);
```

#### 2.3 路径处理
```csharp
// 1. 基本路径
mc.Remote = "path/in/bucket";          // 基本路径格式

// 2. 目录上传时的路径处理
mc.Local = "/path/to/MyFolder";        // 本地目录
mc.Remote = "remote/MyFolder";         // 如果远端路径末尾包含目录名，会自动去除重复，实际存储路径为：remote/MyFolder/*

// 3. 路径规范化
mc.Remote = "path/with/trailing/";     // 末尾斜杠会被自动移除
mc.Remote = "path\\with\\backslash";   // 反斜杠会被转换为正斜杠
```

#### 2.4 检查结果
```csharp
if (report.Result == XEditor.Tasks.Result.Succeeded) {
    Debug.Log("上传成功");
} else {
    Debug.LogError($"上传失败: {report.Error}");
}
```

## 常见问题

### 1. MinIO 客户端下载失败
- 现象：无法自动下载 MinIO 客户端
- 原因：网络连接问题或权限不足
- 解决：
  1. 检查网络连接是否正常
  2. 确保有足够的磁盘权限
  3. 尝试手动下载并放置到 Library 目录

### 2. 上传失败
- 现象：文件上传返回错误
- 原因：认证信息错误或存储分区配置问题
- 解决：
  1. 验证 Access 和 Secret 是否正确
  2. 确认存储分区是否存在且有写入权限
  3. 检查网络连接是否稳定

### 3. 目录上传不完整
- 现象：部分文件未能成功上传
- 原因：文件权限或网络不稳定
- 解决：
  1. 检查文件访问权限
  2. 确保网络稳定
  3. 尝试分批上传大目录

### 4. 远端路径问题
- 现象：文件上传位置不符合预期
- 原因：路径格式问题或目录名重复
- 解决：
  1. 确保使用正斜杠（/）作为路径分隔符
  2. 注意远端路径中是否已包含目标目录名
  3. 检查路径中是否有多余的斜杠

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
