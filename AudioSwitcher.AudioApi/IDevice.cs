﻿using System;
using System.Threading.Tasks;

namespace AudioSwitcher.AudioApi
{
    public interface IDevice
    {
        IAudioController Controller { get; }

        Guid Id { get; }

        string Name { get; }

        string InterfaceName { get; }

        string FullName { get; }

        DeviceIcon Icon { get; }

        bool IsDefaultDevice { get; }

        bool IsDefaultCommunicationsDevice { get; }

        DeviceState State { get; }

        DeviceType DeviceType { get; }

        bool IsPlaybackDevice { get; }

        bool IsCaptureDevice { get; }

        bool IsMuted { get; }

        int Volume { get; set; }

        SpeakerConfiguration ActiveSpeakers { get; }

        bool SetAsDefault();

        Task<bool> SetAsDefaultAsync();

        bool SetAsDefaultCommunications();

        Task<bool> SetAsDefaultCommunicationsAsync();

        bool Mute(bool mute);

        Task<bool> MuteAsync(bool mute);

        bool ToggleMute();

        Task<bool> ToggleMuteAsync();

        event EventHandler<DeviceVolumeChangedEventArgs> VolumeChanged;

        int VolumeStepUp();

        int VolumeStepDown();
    }
}