# XEditor.Preferences

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.editor?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.editor)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.editor?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.editor)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Editor)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

提供了编辑器首选项的加载和应用功能，支持自动收集和组织首选项面板、配置持久化和构建预处理。

## 功能特性

- 面板管理：基于 Unity SettingsProvider 组织首选项面板，提供可视化的配置管理界面
- 构建预处理：在构建时处理和验证首选项，支持变量求值和编辑器配置清理

## 使用手册

### 1. 打开界面
- 通过菜单：`Tools/EFramework/Preferences`
- 代码调用：`XEditor.Preferences.Open()`

### 2. 配置操作
- 保存配置：点击底部工具栏的"Save"按钮
- 应用配置：点击底部工具栏的"Apply"按钮
- 克隆配置：点击顶部工具栏的"Clone"按钮
- 删除配置：点击顶部工具栏的"Delete"按钮

### 3. 面板导航
- 区域折叠：点击区域标题前的折叠箭头
- 配置切换：使用顶部下拉列表切换不同配置文件
- 文件定位：点击配置文件右侧的"定位"按钮

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
