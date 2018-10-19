﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using H.NET.Storages;
using H.NET.Storages.Extensions;
using H.NET.Utilities;
using HomeCenter.NET.Properties;
using HomeCenter.NET.Utilities;

namespace HomeCenter.NET.Services
{
    public class HookService : IDisposable
    {
        #region Properties

        public Settings Settings { get; }
        public CommandsStorage Storage { get; }
        public RunnerService RunnerService { get; }

        public LowLevelMouseHook MouseHook { get; set; } = new LowLevelMouseHook();
        public LowLevelKeyboardHook KeyboardHook { get; set; } = new LowLevelKeyboardHook();

        public Dictionary<KeysCombination, Command> Combinations { get; } = new Dictionary<KeysCombination, Command>();

        public Keys RecordKey => Hook.FromString(Settings.RecordKey);

        public List<string> HookIgnoredApps {
            get => Settings.HookIgnoredApps.Split(';').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            set => Settings.HookIgnoredApps = string.Join(";", value);
        }


        #endregion

        #region Constructors

        public HookService(RunnerService runnerService, Settings settings, CommandsStorage storage)
        {
            RunnerService = runnerService ?? throw new ArgumentNullException(nameof(runnerService));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        #endregion

        #region Public methods

        public void UpdateCombinations()
        {
            Combinations.Clear();
            foreach (var pair in Storage.UniqueValues(i => i.Value).Where(i => i.Value.HotKey != null))
            {
                var command = pair.Value;
                var hotKey = command.HotKey;
                var combination = KeysCombination.FromString(hotKey);
                if (combination.IsEmpty)
                {
                    continue;
                }

                Combinations[combination] = command;
            }
        }

        public bool RunCombination(KeysCombination combination)
        {
            if (!Combinations.TryGetValue(combination, out var command))
            {
                return false;
            }

            RunnerService.Run(command.Keys.FirstOrDefault()?.Text);
            return true;
        }

        public async Task<KeysCombination> CatchKey()
        {
            if (KeyboardHook == null || MouseHook == null)
            {
                return null;
            }

            var keyboardHookState = KeyboardHook.IsStarted;
            var mouseHookState = MouseHook.IsStarted;

            // Starts if not started
            KeyboardHook.Start();
            MouseHook.Start();

            KeysCombination combination = null;
            var isCancel = false;

            void OnKeyboardHookOnKeyDown(object sender, KeyboardHookEventArgs args)
            {
                args.Handled = true;
                if (args.Key == Keys.Escape)
                {
                    isCancel = true;
                    return;
                }

                combination = new KeysCombination(args.Key, args.IsCtrlPressed, args.IsShiftPressed, args.IsAltPressed);
            }

            void OnMouseHookOnMouseDown(object sender, MouseEventExtArgs args)
            {
                if (args.SpecialButton == 0)
                {
                    return;
                }

                args.Handled = true;
                combination = KeysCombination.FromSpecialData(args.SpecialButton);
            }

            KeyboardHook.KeyDown += OnKeyboardHookOnKeyDown;
            MouseHook.MouseDown += OnMouseHookOnMouseDown;

            while (!isCancel && (combination == null || combination.IsEmpty))
            {
                await Task.Delay(1);
            }

            KeyboardHook.KeyDown -= OnKeyboardHookOnKeyDown;
            MouseHook.MouseDown -= OnMouseHookOnMouseDown;

            KeyboardHook.SetEnabled(keyboardHookState);
            MouseHook.SetEnabled(mouseHookState);

            return isCancel ? null : combination;
        }

        public bool IsIgnoredApplication()
        {
            try
            {
                if (User32Utilities.AreApplicationFullScreen())
                {
                    return true;
                }

                var process = User32Utilities.GetForegroundProcess();
                var appExePath = process.MainModule.FileName;

                //var appProcessName = process.ProcessName;
                //var appExeName = appExePath.Substring(appExePath.LastIndexOf(@"\") + 1);

                return HookIgnoredApps.Contains(appExePath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            KeyboardHook?.Dispose();
            KeyboardHook = null;

            MouseHook?.Dispose();
            MouseHook = null;
        }

        #endregion
    }
}
