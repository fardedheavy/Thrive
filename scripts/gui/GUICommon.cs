using Godot;

/// <summary>
///   Common helpers for the GUI to work with.
///   This singleton class is placed on AutoLoad for
///   global access while still inheriting from Node.
/// </summary>
public class GUICommon : Node
{
    private static GUICommon instance;

    private AudioStream buttonPressSound;

    private Tween tween;

    private GUICommon()
    {
        instance = this;

        AudioSource = new AudioStreamPlayer();
        tween = new Tween();

        AddChild(AudioSource);
        AddChild(tween);

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;

        buttonPressSound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");
    }

    public static GUICommon Instance
    {
        get
        {
            return instance;
        }
    }

    /// <summary>
    ///   The audio player for UI sound effects.
    /// </summary>
    public AudioStreamPlayer AudioSource { get; private set; }

    /// <summary>
    ///   Play the button click sound effect.
    /// </summary>
    public void PlayButtonPressSound()
    {
        AudioSource.Stream = buttonPressSound;
        AudioSource.Play();
    }

    /// <summary>
    ///   Plays the given sound non-positionally.
    /// </summary>
    public void PlayCustomSound(AudioStream sound)
    {
        AudioSource.Stream = sound;
        AudioSource.Play();
    }

    /// <summary>
    ///   Smoothly interpolates TextureProgress bar value.
    /// </summary>
    public void TweenBarValue(TextureProgress bar, float targetValue, float maxValue, float speed)
    {
        var percentage = (targetValue / maxValue) * 100;
        tween.InterpolateProperty(bar, "value", bar.Value, percentage, speed,
            Tween.TransitionType.Linear, Tween.EaseType.Out);
        tween.Start();
    }
}
