﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AudioSwitcher.AudioApi.CoreAudio.Interfaces;
using AudioSwitcher.AudioApi.CoreAudio.Threading;
using AudioSwitcher.AudioApi.Observables;
using AudioSwitcher.AudioApi.Session;

namespace AudioSwitcher.AudioApi.CoreAudio
{
    internal class CoreAudioSession : IAudioSession, IAudioSessionEvents
    {

        private readonly IAudioSessionControl2 _audioSessionControl;
        private readonly ISimpleAudioVolume _simpleAudioVolume;

        private readonly object _stateLock = new object();
        private readonly object _disconnectedLock = new object();
        private readonly List<IObserver<AudioSessionStateChanged>> _stateObservers;
        private readonly List<IObserver<AudioSessionDisconnected>> _disconnectedObservers;

        private string _fileDescription;
        private int _volume;
        private string _id;
        private int _processId;
        private string _displayName;
        private bool _isSystemSession;
        private AudioSessionState _state;
        private string _executablePath;

        public string Id
        {
            get
            {
                return _id;
            }
        }

        public int ProcessId
        {
            get
            {
                return _processId;
            }
        }

        public string DisplayName
        {
            get
            {
                return String.IsNullOrWhiteSpace(_displayName) ? _fileDescription : _displayName;
            }
        }

        public string ExecutablePath
        {
            get
            {
                return _executablePath;
            }
        }

        public bool IsSystemSession
        {
            get
            {
                return _isSystemSession;
            }
        }

        public int Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                ComThread.Invoke(() =>
                {
                    _simpleAudioVolume.SetMasterVolume(((float)value) / 100, Guid.Empty);
                });
            }
        }

        public bool IsMuted { get; set; }

        public AudioSessionState SessionState
        {
            get
            {
                return _state;
            }
        }

        public CoreAudioSession(IAudioSessionControl control)
        {
            ComThread.Assert();

            // ReSharper disable once SuspiciousTypeConversion.Global
            _audioSessionControl = control as IAudioSessionControl2;

            // ReSharper disable once SuspiciousTypeConversion.Global
            _simpleAudioVolume = control as ISimpleAudioVolume;

            if (_audioSessionControl == null || _simpleAudioVolume == null)
                throw new InvalidComObjectException("control");

            _stateObservers = new List<IObserver<AudioSessionStateChanged>>();
            _disconnectedObservers = new List<IObserver<AudioSessionDisconnected>>();

            _audioSessionControl.RegisterAudioSessionNotification(this);

            RefreshProperties();
            RefreshVolume();
        }

        private void RefreshVolume()
        {
            ComThread.Invoke(() =>
            {
                float vol;
                _simpleAudioVolume.GetMasterVolume(out vol);
                _volume = (int)(vol * 100);
            });
        }

        private void RefreshProperties()
        {
            ComThread.Invoke(() =>
            {
                _isSystemSession = _audioSessionControl.IsSystemSoundsSession() == 0;
                _audioSessionControl.GetDisplayName(out _displayName);

                EAudioSessionState state;
                _audioSessionControl.GetState(out state);
                _state = state.AsAudioSessionState();

                uint processId;
                _audioSessionControl.GetProcessId(out processId);
                _processId = (int)processId;

                _audioSessionControl.GetSessionIdentifier(out _id);

                try
                {
                    if (ProcessId > 0)
                    {
                        var proc = Process.GetProcessById(ProcessId);
                        _executablePath = proc.MainModule.FileName;
                        _fileDescription = proc.MainModule.FileVersionInfo.FileDescription;
                    }
                }
                catch
                {
                    _fileDescription = "";
                }
            });
        }

        int IAudioSessionEvents.OnDisplayNameChanged(string displayName, ref Guid eventContext)
        {
            _displayName = displayName;
            return 0;
        }

        int IAudioSessionEvents.OnIconPathChanged(string iconPath, ref Guid eventContext)
        {
            return 0;
        }

        int IAudioSessionEvents.OnSimpleVolumeChanged(float volume, bool isMuted, ref Guid eventContext)
        {
            _volume = (int)(volume * 100);
            return 0;
        }

        int IAudioSessionEvents.OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex, ref Guid eventContext)
        {
            return 0;
        }

        int IAudioSessionEvents.OnGroupingParamChanged(ref Guid groupingId, ref Guid eventContext)
        {
            return 0;
        }

        int IAudioSessionEvents.OnSessionDisconnected(EAudioSessionDisconnectReason disconnectReason)
        {
            FireDisconnected();
            return 0;
        }

        int IAudioSessionEvents.OnStateChanged(EAudioSessionState state)
        {
            FireStateChanged(state);
            return 0;
        }

        private void FireStateChanged(EAudioSessionState state)
        {
            Task.Factory.StartNew(() =>
            {
                lock (_stateLock)
                {
                    Parallel.ForEach(_stateObservers, x =>
                    {
                        try
                        {
                            x.OnNext(new AudioSessionStateChanged(this, state.AsAudioSessionState()));
                        }
                        catch (Exception e)
                        {
                            x.OnError(e);
                        }
                    });
                }
            });
        }

        private void FireDisconnected()
        {
            Task.Factory.StartNew(() =>
            {
                lock (_disconnectedLock)
                {
                    Parallel.ForEach(_disconnectedObservers, x =>
                    {
                        try
                        {
                            x.OnNext(new AudioSessionDisconnected(this));
                        }
                        catch (Exception e)
                        {
                            x.OnError(e);
                        }
                    });
                }
            });
        }

        public void Dispose()
        {
            lock (_stateLock)
            {
                _stateObservers.ForEach(x => x.OnCompleted());
                _stateObservers.Clear();
            }

            lock (_disconnectedLock)
            {
                _disconnectedObservers.ForEach(x => x.OnCompleted());
                _disconnectedObservers.Clear();
            }

            Marshal.FinalReleaseComObject(_audioSessionControl);
        }

        public IDisposable Subscribe(IObserver<AudioSessionStateChanged> observer)
        {
            lock (_stateLock)
            {
                _stateObservers.Add(observer);
            }

            return DelegateDisposable.Create(() =>
            {
                lock (_stateLock)
                {
                    _stateObservers.Remove(observer);
                }
            });
        }

        public IDisposable Subscribe(IObserver<AudioSessionDisconnected> observer)
        {
            lock (_disconnectedLock)
            {
                _disconnectedObservers.Add(observer);
            }

            return DelegateDisposable.Create(() =>
            {
                lock (_disconnectedLock)
                {
                    _disconnectedObservers.Remove(observer);
                }
            });
        }
    }
}
