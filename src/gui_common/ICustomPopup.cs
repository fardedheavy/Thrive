using Godot;

/// <summary>
///   Interface for any custom popups inheriting Popup.
/// </summary>
public interface ICustomPopup
{
    /// <summary>
    ///   If true and <see cref="Popup.PopupExclusive"/> is true, pressing ESC key will close
    ///   the popup.
    /// </summary>
    bool ExclusiveAllowCloseOnEscape { get; }

    /// <summary>
    ///   Custom hide action to be implemented by custom popup.
    /// </summary>
    void ClosePopup();
}
