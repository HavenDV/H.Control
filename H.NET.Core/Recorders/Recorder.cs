﻿using System;
using System.Collections.Generic;

namespace H.NET.Core.Recorders
{
    public class Recorder : Module, IRecorder
    {
        #region Properties

        public bool IsStarted { get; protected set; }
        public IReadOnlyCollection<byte> Data { get; protected set; }

        #endregion

        #region Events

        public event EventHandler<VoiceActionsEventArgs> Started;
        protected void OnStarted(VoiceActionsEventArgs args) => Started?.Invoke(this, args);

        public event EventHandler<VoiceActionsEventArgs> Stopped;
        protected void OnStopped(VoiceActionsEventArgs args) => Stopped?.Invoke(this, args);

        public event EventHandler<VoiceActionsEventArgs> NewData;
        protected void OnNewData(VoiceActionsEventArgs args) => NewData?.Invoke(this, args);

        protected virtual VoiceActionsEventArgs CreateArgs() => 
            new VoiceActionsEventArgs { Recorder = this, Data = Data };

        #endregion

        #region Public methods

        public virtual void Start()
        {
            if (IsStarted)
            {
                return;
            }

            IsStarted = true;
            Data = null;
            OnStarted(CreateArgs());
        }

        public virtual void Stop()
        {
            if (!IsStarted)
            {
                return;
            }

            IsStarted = false;
            OnStopped(CreateArgs());
        }

        #endregion
    }
}
