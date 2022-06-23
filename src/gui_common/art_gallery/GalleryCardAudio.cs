﻿using System;
using Godot;

public class GalleryCardAudio : GalleryCard, IGalleryCardPlayback
{
    [Export]
    public NodePath PlaybackControlsPath = null!;

    private PlaybackControls? playbackControls;
    private AudioStreamPlayer? ownPlayer;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackStopped;

    /// <summary>
    ///   NOTE: Manipulating playback shouldn't be done directly through here, instead use the provided methods
    ///   so that controls could be updated accordingly.
    /// </summary>
    public AudioStreamPlayer Player
    {
        get
        {
            EnsurePlayerExist();
            return ownPlayer!;
        }
        set
        {
            ownPlayer = value;
            UpdatePlaybackBar();
        }
    }

    public bool Playing => playbackControls?.Playing ?? false;

    public override void _Ready()
    {
        base._Ready();

        playbackControls = GetNode<PlaybackControls>(PlaybackControlsPath);

        EnsurePlayerExist();
    }

    public void StartPlayback()
    {
        playbackControls?.StartPlayback();
    }

    public void StopPlayback()
    {
        playbackControls?.StopPlayback();
    }

    private void EnsurePlayerExist()
    {
        if (ownPlayer == null)
        {
            ownPlayer = new AudioStreamPlayer { Stream = GD.Load<AudioStream>(Asset.ResourcePath) };
            UpdatePlaybackBar();
            AddChild(ownPlayer);
        }
    }

    private void UpdatePlaybackBar()
    {
        if (playbackControls == null)
            return;

        playbackControls.AudioPlayer = ownPlayer;
    }

    private void OnStarted()
    {
        PlaybackStarted?.Invoke(this, EventArgs.Empty);
    }

    private void OnStopped()
    {
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }
}
