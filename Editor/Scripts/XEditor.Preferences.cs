// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Editor
{
    public partial class XEditor
    {
        /// <summary>
        /// XEditor.Preferences 提供了编辑器首选项的加载和应用功能，支持自动收集和组织首选项面板、配置持久化和构建预处理。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 面板管理：基于 Unity SettingsProvider 组织首选项面板，提供可视化的配置管理界面
        /// - 构建预处理：在构建时处理和验证首选项，支持变量求值和编辑器配置清理
        /// 
        /// 使用手册
        /// 1. 打开界面
        /// - 通过菜单：Tools/EFramework/Preferences
        /// - 代码调用：XEditor.Preferences.Open()
        /// 
        /// 2. 配置操作
        /// - 保存配置：点击底部工具栏的"Save"按钮
        /// - 应用配置：点击底部工具栏的"Apply"按钮
        /// - 克隆配置：点击顶部工具栏的"Clone"按钮
        /// - 删除配置：点击顶部工具栏的"Delete"按钮
        /// 
        /// 3. 面板导航
        /// - 区域折叠：点击区域标题前的折叠箭头
        /// - 配置切换：使用顶部下拉列表切换不同配置文件
        /// - 文件定位：点击配置文件右侧的"定位"按钮
        /// 
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Preferences : SettingsProvider
        {
            #region 静态成员
            /// <summary>
            /// RootAttribute 用于标记首选项根目录路径属性的特性。
            /// 被此特性标记的静态属性将被用作首选项根目录路径。
            /// </summary>
            [AttributeUsage(AttributeTargets.Property)]
            public class RootAttribute : Attribute { }

            /// <summary>
            /// root 标记是否已初始化首选项根目录路径。
            /// </summary>
            internal static bool root;

            /// <summary>
            /// rootProp 存储首选项根目录路径属性信息。
            /// </summary>
            internal static PropertyInfo rootProp;

            /// <summary>
            /// Root 获取首选项根目录路径。
            /// 如果未通过 <see cref="RootAttribute"/> 自定义，则返回默认路径：项目目录/ProjectSettings/Preferences。
            /// </summary>
            public static string Root { get => Constants.GetCoustom<RootAttribute, string>(ref root, ref rootProp, XFile.PathJoin(XEnv.ProjectPath, "ProjectSettings", "Preferences")); }

            /// <summary>
            /// Extension 是配置的后缀，用于标识首选项文件类型。
            /// </summary>
            public const string Extension = ".json";

            /// <summary>
            /// MenuPath 是菜单栏的路径，定义了在 Unity 主菜单中的位置。
            /// </summary>
            internal const string MenuPath = "Tools/EFramework/Preferences";

            /// <summary>
            /// ProjMenu 是项目设置菜单栏的路径，定义了在 Project Settings 窗口中的位置。
            /// </summary>
            internal const string ProjMenu = "Project/EFramework/Preferences";

            /// <summary>
            /// Open 打开首选项设置窗口。
            /// </summary>
            [MenuItem(MenuPath)]
            public static void Open() { SettingsService.OpenProjectSettings(ProjMenu); }

            internal static Preferences Instance = new();

            /// <summary>
            /// Provider 提供了首选项提供者数组，用于 Unity 编辑器设置系统的注册。
            /// </summary>
            /// <returns>包含本首选项提供者的数组。</returns>
            [SettingsProviderGroup]
            internal static SettingsProvider[] Provider() { return new SettingsProvider[] { Instance }; }

            internal Preferences() : base(ProjMenu, SettingsScope.Project) { }
            #endregion

            #region 类型成员
            /// <summary>
            /// activeTarget 是当前活动的首选项目标对象。
            /// </summary>
            internal XPrefs.IBase activeTarget;

            /// <summary>
            /// activeIndex 是当前选中的首选项索引。
            /// </summary>
            internal int activeIndex = -1;

            /// <summary>
            /// sections 是按区域分组的首选项编辑器列表。
            /// </summary>
            internal List<List<XPrefs.IEditor>> sections;

            /// <summary>
            /// visualElement 是首选项窗口的根视觉元素。
            /// </summary>
            internal VisualElement visualElement;

            /// <summary>
            /// editors 是所有首选项的编辑器列表。
            /// </summary>
            internal List<XPrefs.IEditor> editors;

            /// <summary>
            /// editorCache 是首选项的编辑器缓存，按类型索引。
            /// </summary>
            internal readonly Dictionary<Type, XPrefs.IEditor> editorCache = new();

            /// <summary>
            /// Reload 重新加载所有首选项编辑器。
            /// 收集所有实现了 XPrefs.IEditor 接口的类型，并创建编辑器实例。
            /// 将编辑器按区域分组并排序。
            /// </summary>
            /// <param name="searchContext">搜索上下文字符串</param>
            internal void Reload(string searchContext = "")
            {
                editors = new List<XPrefs.IEditor>();
                var types = TypeCache.GetTypesDerivedFrom<XPrefs.IEditor>();

                foreach (var type in types)
                {
                    try
                    {
                        if (!editorCache.TryGetValue(type, out var obj) || (obj is ScriptableObject sobj && sobj == null))
                        {
                            if (type.IsSubclassOf(typeof(ScriptableObject)))
                            {
                                obj = ScriptableObject.CreateInstance(type) as XPrefs.IEditor;
                            }
                            else
                            {
                                obj = Activator.CreateInstance(type) as XPrefs.IEditor;
                            }
                            if (obj != null)
                            {
                                editorCache[type] = obj;
                            }
                        }

                        if (obj != null) editors.Add(obj);
                    }
                    catch (Exception e) { XLog.Panic(e); }
                }

                editors.Sort((e1, e2) => e1.Priority.CompareTo(e2.Priority));

                sections = new List<List<XPrefs.IEditor>>();
                for (var i = 0; i < editors.Count; i++)
                {
                    var target = editors[i];
                    List<XPrefs.IEditor> group = null;
                    for (var j = 0; j < sections.Count; j++)
                    {
                        var temp = sections[j];
                        if (temp != null && temp.Count > 0 && temp[0].Section == target.Section)
                        {
                            group = temp;
                            break;
                        }
                    }
                    if (group == null)
                    {
                        group = new List<XPrefs.IEditor>();
                        sections.Add(group);
                    }
                    group.Add(target);
                }

                foreach (var editor in editors)
                {
                    try { editor.OnActivate(searchContext, visualElement, activeTarget); }
                    catch (Exception e) { XLog.Panic(e); }
                }
            }

            /// <summary>
            /// OnActivate 当首选项面板被激活时调用。
            /// 初始化根视觉元素并重新加载面板。
            /// </summary>
            /// <param name="searchContext">搜索上下文字符串</param>
            /// <param name="rootElement">根视觉元素</param>
            public override void OnActivate(string searchContext, VisualElement rootElement)
            {
                visualElement = rootElement;
                Reload(searchContext);
            }

            /// <summary>
            /// OnTitleBarGUI 绘制首选项面板的标题栏界面。
            /// 显示可用的首选项文件列表，并提供克隆、删除等操作。
            /// </summary>
            public override void OnTitleBarGUI()
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var names = new List<string>();
                var files = new List<string>();
                Utility.CollectFiles(Root, files);
                for (var i = 0; i < files.Count;)
                {
                    var file = files[i];
                    if (file.EndsWith(Extension))
                    {
                        files[i] = XFile.NormalizePath(Path.GetRelativePath(XEnv.ProjectPath, files[i])); i++;
                    }
                    else files.RemoveAt(i);
                }
                files.Sort((e1, e2) =>
                {
                    var s1 = e1.Contains(".template", StringComparison.OrdinalIgnoreCase);
                    var s2 = e2.Contains(".template", StringComparison.OrdinalIgnoreCase);
                    if (!s1 && s2) return -1;
                    if (s1 && !s2) return 1;
                    return StringComparer.OrdinalIgnoreCase.Compare(e1, e2);
                });
                names = files.Select(ele => Path.GetFileName(ele)).ToList();

                if (activeTarget != null && activeTarget.File == XPrefs.IAsset.Uri && !activeTarget.Dirty && !activeTarget.Equals(XPrefs.Asset))
                {
                    activeIndex = -1; // 当前的内置配置已经被修改，重置后重新读取
                    activeTarget = null;
                }
                if (activeIndex >= files.Count) activeIndex = -1;
                if (activeIndex == -1)
                {
                    if (activeTarget == null)
                    {
                        var currentIndex = files.IndexOf(XPrefs.IAsset.Uri);
                        if (currentIndex >= 0)
                        {
                            activeIndex = currentIndex;
                            activeTarget = new XPrefs.IBase();
                            activeTarget.Read(XPrefs.IAsset.Uri);
                        }
                        else activeTarget = new XPrefs.IBase();
                    }
                    else activeIndex = files.IndexOf(activeTarget.File);
                }

                GUILayout.BeginHorizontal();
                var invalidTarget = activeIndex == -1;
                var ocolor = GUI.color;
                if (invalidTarget) GUI.color = Color.gray;

                var lastIndex = activeIndex;
                activeIndex = EditorGUILayout.Popup(activeIndex, names.ToArray());
                if (lastIndex != activeIndex)
                {
                    activeTarget = new XPrefs.IBase();
                    activeTarget.Read(files[activeIndex]);
                }
                if (GUILayout.Button(new GUIContent("", EditorGUIUtility.FindTexture("UnityEditor.ConsoleWindow"))))
                {
                    if (invalidTarget) return;
                    EditorApplication.delayCall += () => Utility.ShowInExplorer(files[activeIndex]);
                }
                if (GUILayout.Button(new GUIContent("Delete", EditorGUIUtility.FindTexture("TreeEditor.Trash"))))
                {
                    if (invalidTarget) return;
                    EditorApplication.delayCall += () =>
                    {
                        if (files[activeIndex].Contains(".template") && EditorUtility.DisplayDialog("Warning", $"Delete the template preferences of {files[activeIndex]} is not allowed. Please continue with explorer.", "Explorer", "Dismiss"))
                        {
                            Utility.ShowInExplorer(files[activeIndex]);
                        }
                        else if (EditorUtility.DisplayDialog("Warning", $"You are deleting the preferences of {files[activeIndex]}. Do you want to proceed?", "Delete", "Cancel"))
                        {
                            XFile.DeleteFile(files[activeIndex]);
                            activeIndex = -1;
                            activeTarget = null;
                        }
                    };
                }
                if (GUILayout.Button(new GUIContent("Clone", EditorGUIUtility.FindTexture("TreeEditor.Duplicate"))))
                {
                    if (invalidTarget) return;
                    EditorApplication.delayCall += () =>
                    {
                        if (!XFile.HasDirectory(Root)) XFile.CreateDirectory(Root);
                        var path = EditorUtility.SaveFilePanel("Clone Preferences", Root, Environment.UserName, Extension.TrimStart('.'));
                        if (!string.IsNullOrEmpty(path))
                        {
                            path = XFile.NormalizePath(Path.GetRelativePath(XEnv.ProjectPath, path));
                            var raw = XFile.OpenText(files[activeIndex]);
                            if (XFile.HasFile(path)) XFile.DeleteFile(path);
                            XFile.SaveText(path, raw);
                            activeTarget = new XPrefs.IBase();
                            activeTarget.Read(path);
                            activeIndex = -1;
                        }
                    };
                }
                GUI.color = ocolor;
                GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            /// <summary>
            /// OnGUI 绘制首选项面板的主体界面。
            /// 按区域分组显示各个首选项面板。
            /// </summary>
            /// <param name="searchContext">搜索上下文字符串</param>
            public override void OnGUI(string searchContext)
            {
                if (sections != null && sections.Count > 0)
                {
                    GUILayout.Space(5);
                    foreach (var section in sections)
                    {
                        var sectionName = section[0].Section;
                        if (string.IsNullOrEmpty(sectionName)) continue;
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        var expanded = true;
                        if (section[0].Foldable)
                        {
                            expanded = EditorPrefs.GetBool(XFile.PathJoin(XEnv.ProjectPath, "Preferences", sectionName), true);
                            var newExpanded = EditorGUILayout.Foldout(expanded, new GUIContent(sectionName, section[0].Tooltip));
                            if (expanded != newExpanded) EditorPrefs.SetBool(XFile.PathJoin(XEnv.ProjectPath, "Preferences", sectionName), newExpanded);
                        }
                        else EditorGUILayout.Foldout(expanded, new GUIContent(sectionName, section[0].Tooltip));

                        if (expanded)
                        {
                            foreach (var editor in section)
                            {
                                if (editor is ScriptableObject sobj && sobj == null) { Reload(); return; } // 构建后ScriptableObject为空，故重载之
                                try { editor.OnVisualize(searchContext, activeTarget); }
                                catch (Exception e) { XLog.Panic(e); }
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            /// <summary>
            /// OnFooterBarGUI 绘制首选项面板的底部界面。
            /// 提供保存和应用首选项的按钮。
            /// </summary>
            public override void OnFooterBarGUI()
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                var ocolor = GUI.color;
                if (activeTarget.File != XPrefs.IAsset.Uri && activeTarget.Dirty) GUI.color = Color.yellow;
                if (GUILayout.Button(new GUIContent("Save", EditorGUIUtility.FindTexture("SaveActive")))) EditorApplication.delayCall += () => Save();
                GUI.color = ocolor;

                ocolor = GUI.color;
                if (activeTarget.File == XPrefs.IAsset.Uri && activeTarget.Dirty) GUI.color = Color.yellow;
                if (string.IsNullOrEmpty(XPrefs.IAsset.Uri) || !XFile.HasFile(XPrefs.IAsset.Uri) || XPrefs.IAsset.Uri != activeTarget.File) GUI.color = Color.cyan;
                if (GUILayout.Button(new GUIContent("Apply", EditorGUIUtility.FindTexture("SaveFromPlay"), "Press Ctrl/⌘ + Enter to apply preferences."))) EditorApplication.delayCall += () => Save(true);
                GUI.color = ocolor;
                GUILayout.EndHorizontal();
                GUILayout.Space(2f);

                var evt = UnityEngine.Event.current;
                if (evt.type == EventType.KeyDown &&
                    (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) &&
                    (evt.control || evt.command))
                {
                    evt.Use();
                    EditorApplication.delayCall += () => Save(true);
                }
            }

            /// <summary>
            /// OnDeactivate 当首选项面板被关闭时调用，对所有面板执行停用操作。
            /// </summary>
            public override void OnDeactivate()
            {
                if (editors != null && editors.Count > 0)
                {
                    foreach (var editor in editors)
                    {
                        if (editor == null) continue;
                        try { editor.OnDeactivate(activeTarget); }
                        catch (Exception e) { XLog.Panic(e); }
                    }
                }
            }

            /// <summary>
            /// Save 保存首选项设置。
            /// 如果 apply 为 true，则同时应用设置到当前编辑器会话。
            /// </summary>
            /// <param name="apply">是否应用设置到当前编辑器会话</param>
            internal void Save(bool apply = false)
            {
                var doSave = new Action(() =>
                {
                    if (activeTarget.Save())
                    {
                        if (apply)
                        {
                            XPrefs.IAsset.Uri = activeTarget.File;
                            XPrefs.Asset.Read(XPrefs.IAsset.Uri);
                            Event.Notify<Event.Internal.OnPreferencesApply>();
                        }
                        EditorWindow.focusedWindow.ShowNotification(new GUIContent("Save {0}preferences succeeded.".Format(apply ? "and apply " : "")), 1f);
                    }
                });
                if (string.IsNullOrEmpty(activeTarget.File))
                {
                    if (!XFile.HasDirectory(Root)) XFile.CreateDirectory(Root);
                    var path = EditorUtility.SaveFilePanel("Save Preferences", Root, Environment.UserName, Extension.TrimStart('.'));
                    if (string.IsNullOrEmpty(path))
                    {
                        XLog.Error("XEditor.Preferences: save preferences error caused by nil file path.");
                        return;
                    }
                    activeTarget.File = XFile.NormalizePath(Path.GetRelativePath(XEnv.ProjectPath, path));
                }
                if (activeTarget.File.Contains(".template"))
                {
                    if (EditorUtility.DisplayDialog("Warning", $"You are saving the template preferences. Do you want to proceed?", "Save", "Cancel"))
                    {
                        doSave.Invoke();
                    }
                }
                else doSave.Invoke();
            }
            #endregion
        }
    }
}
