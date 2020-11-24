﻿using System;
using System.Linq;
using System.Threading.Tasks;
using H.NET.Core;
using H.NET.Core.Managers;
using H.NET.Core.Recorders;
using H.NET.Tests.Utilities;
using Nito.AsyncEx;
using Xunit;
using Xunit.Abstractions;

namespace H.NET.Tests
{
    public class BaseTests
    {
        protected ITestOutputHelper Output { get; }

        protected BaseTests()
        {
        }

        protected BaseTests(ITestOutputHelper output)
        {
            Output = output;
        }

        protected static void BaseDisposeTest(IDisposable obj)
        {
            // Check double disposing
            obj.Dispose();
            obj.Dispose();
        }

        protected static void BaseDataTest(byte[] bytes)
        {
            Assert.NotNull(bytes);
            Assert.InRange(bytes.Length, 1, int.MaxValue);
        }

        public static bool CheckPlatform(PlatformID? platformId) => 
            platformId == null || platformId == Environment.OSVersion.Platform;

        protected async Task BaseRecorderTest(IRecorder recorder, PlatformID? platformId = null, int timeout = 1000)
        {
            Assert.NotNull(recorder);

            if (!CheckPlatform(platformId))
            {
                Output?.WriteLine($"Recorder: {recorder} not support current system: {Environment.OSVersion}");
                return;
            }

            Assert.False(recorder.IsStarted);
            await recorder.StartAsync();
            Assert.True(recorder.IsStarted);

            await Task.Delay(timeout);

            await recorder.StopAsync();
            Assert.False(recorder.IsStarted);

            BaseDataTest(recorder.WavData.ToArray());
            BaseDisposeTest(recorder);

            Output?.WriteLine($"Recorder: {recorder} is good!");
        }

        protected static async Task BaseSynthesizerTest(string text, ISynthesizer synthesizer)
        {
            Assert.NotNull(text);
            Assert.NotNull(synthesizer);

            var bytes = await synthesizer.ConvertAsync(text);

            BaseDataTest(bytes);
            BaseDisposeTest(synthesizer);
        }

        protected static void BaseArgsTest(BaseManager manager, VoiceActionsEventArgs args)
        {
            Assert.Equal(manager.Converter, args.Converter);
            Assert.Equal(manager.Recorder, args.Recorder);
            Assert.Equal(manager.WavData, args.Data);
            Assert.Equal(manager.Text, args.Text);
        }

        protected async Task BaseManagerTest(BaseManager manager, PlatformID? platformId = null, TimeSpan? timeout = default, int waitEventTimeout = 20000)
        {
            timeout ??= TimeSpan.FromSeconds(1);

            Assert.NotNull(manager);
            if (!CheckPlatform(platformId))
            {
                Output?.WriteLine($"Manager: {manager} not support current system: {Environment.OSVersion}");
                return;
            }

            var startedEvent = new AsyncAutoResetEvent(false);
            var stoppedEvent = new AsyncAutoResetEvent(false);
            var newTextEvent = new AsyncAutoResetEvent(false);
            var actionEvent = new AsyncAutoResetEvent(false);
            manager.Started += (s, e) =>
            {
                startedEvent.Set();
                //BaseArgsTest(manager, e);
            };
            manager.Stopped += (s, e) =>
            {
                stoppedEvent.Set();
                //BaseArgsTest(manager, e);
            };
            manager.NewText += (sender, text) =>
            {
                newTextEvent.Set();
                Assert.Equal(manager.Text, text);

                if (string.Equals(manager.Text, "проверка", StringComparison.OrdinalIgnoreCase))
                {
                    actionEvent.Set();
                }
            };

            await manager.ChangeAsync();
            await Task.Delay(timeout.Value);
            await manager.ChangeAsync();

            await manager.ChangeWithTimeoutAsync(timeout.Value);
            await Task.Delay(timeout.Value);
            await manager.ChangeWithTimeoutAsync(timeout.Value);

            await manager.StartWithTimeoutAsync(timeout.Value);
            await Task.Delay(timeout.Value);
            await manager.StopAsync();

            await manager.StartAsync();
            await Task.Delay(timeout.Value);
            await manager.StopAsync();

            await manager.ProcessSpeechAsync(TestUtilities.GetRawSpeech("speech1.wav"));

            await startedEvent.WaitAsync();
            await stoppedEvent.WaitAsync();
            await newTextEvent.WaitAsync();
            await actionEvent.WaitAsync();

            await BaseRecorderTest(manager);

            Assert.Null(manager.Recorder);
            Assert.Null(manager.Converter);
        }
    }
}