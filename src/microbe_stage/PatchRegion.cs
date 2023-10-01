﻿using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A region is a something like a continent/ocean that contains multiple patches.
/// </summary>
[UseThriveSerializer]
[JsonObject(IsReference = true)]
public class PatchRegion
{
    public PatchRegion(int id, string name, RegionType regionType, Vector2 screenCoordinates)
    {
        ID = id;
        Patches = new List<Patch>();
        Name = name;
        Height = 0;
        Width = 0;
        Type = regionType;
        ScreenCoordinates = screenCoordinates;
    }

    [JsonConstructor]
    public PatchRegion(int id, string name, RegionType type, Vector2 screenCoordinates, MapElementVisibility visibilityState,
        float height, float width)
    {
        ID = id;
        Name = name;
        Type = type;
        ScreenCoordinates = screenCoordinates;
        VisibilityState = visibilityState;
        Height = height;
        Width = width;
    }

    public enum RegionType
    {
        Sea = 0,
        Ocean = 1,
        Continent = 2,
        Predefined = 3,
    }

    [JsonProperty]
    public MapElementVisibility VisibilityState { get; set; }

    [JsonProperty]
    public RegionType Type { get; }

    [JsonProperty]
    public int ID { get; }

    /// <summary>
    ///   Regions this is next to
    /// </summary>
    [JsonIgnore]
    public HashSet<PatchRegion> Adjacent { get; } = new();

    /// <summary>
    ///   The IDs of adjacent regions, and the patches this region connects to
    /// </summary>
    [JsonIgnore]
    public Dictionary<int, List<(Patch To, Patch From)>> ConnectingPatches { get; set; } = new();

    [JsonProperty]
    public float Height { get; set; }

    [JsonProperty]
    public float Width { get; set; }

    [JsonIgnore]
    public bool Explored => VisibilityState == MapElementVisibility.Explored;

    [JsonIgnore]
    public int DiscoveredPatches
    {
        get
        {
            var count = 0;

            foreach (var patch in Patches)
            {
                if (patch.Discovered)
                    count++;
            }

            return count;
        }
    }

    [JsonIgnore]
    public Vector2 Size
    {
        get => new(Width, Height);
        set
        {
            Width = value.x;
            Height = value.y;
        }
    }

    /// <summary>
    ///   The name of the region / continent
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is not translatable as this is just the output from the name generator, which isn't language specific
    ///     currently. And even once it is a different approach than <see cref="LocalizedString"/> will be needed to
    ///     allow randomly generated names to translate.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public string Name { get; private set; }

    /// <summary>
    ///   Coordinates this region is to be displayed at in the GUI
    /// </summary>
    [JsonProperty]
    public Vector2 ScreenCoordinates { get; set; }

    /// <summary>
    ///   The patches in this region. This is last because other constructor params need to be loaded from JSON first
    ///   and also this can't be a JSON constructor parameter because the patches refer to this so we couldn't
    ///   construct anything to begin with.
    /// </summary>
    [JsonProperty]
    public List<Patch> Patches { get; private set; } = null!;

    /// <summary>
    ///   Adds a connection to region
    /// </summary>
    public void AddNeighbour(PatchRegion region)
    {
        Adjacent.Add(region);
    }

    public void SetConnectingPatch(PatchRegion otherRegion, Patch to, Patch from)
    {
        if (ConnectingPatches.TryGetValue(otherRegion.ID, out var patches))
        {
            if (patches.Contains((to, from)))
                return;

            patches.Add((to, from));
        }
        else
        {
            ConnectingPatches[otherRegion.ID] = new()
            {
                (to, from),
            };
        }
    }
}
