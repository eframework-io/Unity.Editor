// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using NUnit.Framework;
using EFramework.Unity.Editor;
using System.Collections.Generic;

/// <summary>
/// TestXEditorEvent 是 XEditor.Event 的单元测试。
/// </summary>
public class TestXEditorEvent
{
    /// <summary>
    /// 测试用事件接口。
    /// </summary>
    private interface ITestEvent : XEditor.Event.Callback
    {
        void Process(params object[] args);
    }

    /// <summary>
    /// 普通事件处理器。
    /// </summary>
    private class TestCallback : ITestEvent
    {
        public int Priority => 0;

        public bool Singleton => false;

        void ITestEvent.Process(params object[] args) { receivedEvents.Add(this); }
    }

    /// <summary>
    /// 单例事件处理器。
    /// </summary>
    private class TestSingletonCallback : ITestEvent
    {
        private static readonly TestSingletonCallback instance = new();
        public static TestSingletonCallback Instance => instance;

        public int Priority => 1;

        public bool Singleton => true;

        void ITestEvent.Process(params object[] args) { receivedEvents.Add(this); }
    }

    /// <summary>
    /// 记录已接收的事件回调。
    /// </summary>
    private static readonly List<ITestEvent> receivedEvents = new();

    [OneTimeTearDown]
    public void Cleanup() { receivedEvents.Clear(); }

    [Test]
    public void Register()
    {
        Assert.That(XEditor.Event.Callbacks.ContainsKey(typeof(ITestEvent)), "事件接口 ITestEvent 应该已注册到回调表中");
        Assert.That(XEditor.Event.Singletons.ContainsKey(typeof(TestSingletonCallback)), "单例处理器 TestSingletonCallback 应该已注册到单例表中");
        Assert.That(XEditor.Event.Singletons[typeof(TestSingletonCallback)], Is.EqualTo(TestSingletonCallback.Instance), "单例表中的实例应该与 TestSingletonCallback.Instance 相同");
    }

    [Test]
    public void Decode()
    {
        string testStr = "test";
        int testInt = 42;
        bool testBool = true;
        object[] args = new object[] { testStr, testInt, testBool };

        XEditor.Event.Decode(out string str, out int num, out bool flag, args);

        Assert.That(str, Is.EqualTo(testStr), $"字符串参数解析应得到 {testStr}");
        Assert.That(num, Is.EqualTo(testInt), $"整数参数解析应得到 {testInt}");
        Assert.That(flag, Is.EqualTo(testBool), $"布尔参数解析应得到 {testBool}");
    }

    [Test]
    public void Notify()
    {
        XEditor.Event.Notify<ITestEvent>();
        XEditor.Event.Notify<ITestEvent>();

        Assert.That(receivedEvents.Count, Is.EqualTo(4), "两次通知应产生4个事件回调（每次2个处理器）");
        Assert.That(receivedEvents[^1], Is.EqualTo(TestSingletonCallback.Instance), "最后一个触发的应该是优先级较低的单例处理器");
        Assert.Throws<ArgumentNullException>(() => XEditor.Event.Notify(null), "传入空事件类型应抛出 ArgumentNullException 异常");
    }
}
