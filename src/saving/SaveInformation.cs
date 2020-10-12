using System;

/// <summary>
///   Info embedded in a save file
/// </summary>
public class SaveInformation
{
    public enum SaveType
    {
        /// <summary>
        ///   Player initiated save
        /// </summary>
        Manual,

        /// <summary>
        ///   Automatic save
        /// </summary>
        AutoSave,

        /// <summary>
        ///   Quick save, separate from manual to make it easier to keep a fixed number of quick saves
        /// </summary>
        QuickSave,

        /// <summary>
        ///   A broken save that cannot be loaded
        /// </summary>
        Broken,
    }

    /// <summary>
    ///   Version of the game the save was made with, used to detect incompatible versions
    /// </summary>
    public virtual string ThriveVersion { get; set; } = Constants.Version;

    public virtual string Platform { get; set; } = FeatureInformation.GetOS();

    public virtual string Creator { get; set; } = Environment.UserName;

    public virtual DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    ///   An extended description for this save
    /// </summary>
    public virtual string Description { get; set; } = string.Empty;

    /// <summary>
    ///   Unique ID of this save
    /// </summary>
    public Guid ID { get; set; } = Guid.NewGuid();

    public virtual SaveType Type { get; set; } = SaveType.Manual;
}
