﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Directory = Godot.Directory;
using File = Godot.File;
using Path = System.IO.Path;

/// <summary>
///    A species saved by the user. Contains helper methods for saving/loading species on the disk.
/// </summary>
public class FossilisedSpecies
{
    public const string SAVE_FOSSIL_JSON = "fossil.json";
    public const string SAVE_INFO_JSON = "info.json";

    /// <summary>
    ///   General information about this saved species.
    /// </summary>
    private FossilisedSpeciesInformation info;

    /// <summary>
    ///   The species to be saved/loaded.
    /// </summary>
    private Species species;

    /// <summary>
    ///   Name of this saved species on disk.
    /// </summary>
    private string name;

    /// <summary>
    ///   A species saved by the user.
    /// </summary>
    /// <param name="info">Details about the species to save</param>
    /// <param name="species">The species to fossilise</param>
    /// <param name="name">The name of the species to use as the file name</param>
    public FossilisedSpecies(FossilisedSpeciesInformation info, Species species, string name)
    {
        this.info = info;
        this.species = species;
        this.name = name;
    }

    /// <summary>
    ///   Creates a list of existing fossilised species names to prevent unintended overwrites.
    /// </summary>
    /// <returns>A list of names of .thrivefossil files in the user's fossils directory</returns>
    /// <param name="orderByDate">Whether the returned list is ordered by date modified</param>
    public static List<string> CreateListOfFossils(bool orderByDate)
    {
        var result = new List<string>();

        using (var directory = new Directory())
        {
            if (!directory.DirExists(Constants.FOSSILISED_SPECIES_FOLDER))
                return result;

            directory.Open(Constants.FOSSILISED_SPECIES_FOLDER);
            directory.ListDirBegin(true, true);

            while (true)
            {
                var filename = directory.GetNext();

                if (string.IsNullOrEmpty(filename))
                    break;

                if (!filename.EndsWith(Constants.FOSSIL_EXTENSION, StringComparison.Ordinal))
                    continue;

                // Skip folders
                if (!directory.FileExists(filename))
                    continue;

                result.Add(filename);
            }

            directory.ListDirEnd();
        }

        using var file = new File();

        if (orderByDate)
        {
            result = result.OrderBy(item =>
                file.GetModifiedTime(Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, item))).ToList();
        }

        return result;
    }

    /// <summary>
    ///   Checks whether a species with the same name already exists.
    /// </summary>
    /// <param name="species">The species to check</param>
    /// <param name="existingFossilNames">A cached list of fossils if appropriate</param>
    /// <returns>True if a species with this name has already been fossilised and false otherwise</returns>
    public static bool IsSpeciesAlreadyFossilised(string name, List<string>? existingFossilNames = null)
    {
        existingFossilNames ??= CreateListOfFossils(false);
        return existingFossilNames.Any(n => n == name + Constants.FOSSIL_EXTENSION_WITH_DOT);
    }

    /// <summary>
    ///   Loads a fossilised species by its filename.
    /// </summary>
    /// <param name="fossilName">The name of the .thrivefossil file (including extension)</param>
    /// <returns>The species saved in the provided file</returns>
    public static Species LoadSpeciesFromFile(string fossilName)
    {
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, fossilName);
        var (_, species) = LoadFromFile(target);

        return species;
    }

    /// <summary>
    ///   Saves this species to disk.
    /// </summary>
    public void FossiliseToFile()
    {
        if (species is not MicrobeSpecies)
        {
            throw new NotImplementedException("Saving non-microbe species is not yet implemented");
        }

        WriteRawFossilDataToFile(info, species.StringCode, name + Constants.FOSSIL_EXTENSION_WITH_DOT);
    }

    private static void WriteRawFossilDataToFile(FossilisedSpeciesInformation speciesInfo, string fossilContent,
        string fossilName)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.FOSSILISED_SPECIES_FOLDER);
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, fossilName);

        var justInfo = ThriveJsonConverter.Instance.SerializeObject(speciesInfo);

        WriteDataToFossilFile(target, justInfo, fossilContent);
    }

    private static void WriteDataToFossilFile(string target, string justInfo, string serialized)
    {
        using var file = new File();
        if (file.Open(target, File.ModeFlags.Write) != Error.Ok)
        {
            GD.PrintErr("Cannot open file for writing: ", target);
            throw new IOException("Cannot open: " + target);
        }

        using Stream gzoStream = new GZipOutputStream(new GodotFileStream(file));
        using var tar = new TarOutputStream(gzoStream, Encoding.UTF8);

        OutputEntry(tar, SAVE_INFO_JSON, Encoding.UTF8.GetBytes(justInfo));
        OutputEntry(tar, SAVE_FOSSIL_JSON, Encoding.UTF8.GetBytes(serialized));
    }

    private static void OutputEntry(TarOutputStream archive, string name, byte[] data)
    {
        var entry = TarEntry.CreateTarEntry(name);

        entry.TarHeader.Mode = Convert.ToInt32("0664", 8);

        // TODO: could fill in more of the properties

        entry.Size = data.Length;

        archive.PutNextEntry(entry);

        archive.Write(data, 0, data.Length);

        archive.CloseEntry();
    }

    private static (FossilisedSpeciesInformation Info, Species Species) LoadFromFile(string file)
    {
        using (var directory = new Directory())
        {
            if (!directory.FileExists(file))
                throw new ArgumentException("fossil with the given name doesn't exist");
        }

        var (infoStr, fossilStr) = LoadDataFromFile(file);

        if (string.IsNullOrEmpty(infoStr))
        {
            throw new IOException("couldn't find info content in fossil");
        }

        if (string.IsNullOrEmpty(fossilStr))
        {
            throw new IOException("couldn't find fossil content in fossil file");
        }

        var infoResult = ThriveJsonConverter.Instance.DeserializeObject<FossilisedSpeciesInformation>(infoStr!) ??
            throw new JsonException("FossilisedSpeciesInformation is null");

        // Use the info file to deserialize the species to the correct type
        Species? speciesResult;
        switch (infoResult.Type)
        {
            case FossilisedSpeciesInformation.SpeciesType.Microbe:
                speciesResult = ThriveJsonConverter.Instance.DeserializeObject<MicrobeSpecies>(fossilStr!) ??
                    throw new JsonException("Fossil data is null");
                break;
            default:
                throw new NotImplementedException("Unable to load non-microbe species");
        }

        return (infoResult, speciesResult);
    }

    private static (string? Info, string? Fossil) LoadDataFromFile(string file)
    {
        string? infoStr = null;
        string? fossilStr = null;

        using var reader = new File();
        reader.Open(file, File.ModeFlags.Read);

        if (!reader.IsOpen())
            throw new ArgumentException("couldn't open the file for reading");

        using var stream = new GodotFileStream(reader);
        using Stream gzoStream = new GZipInputStream(stream);
        using var tar = new TarInputStream(gzoStream, Encoding.UTF8);

        TarEntry tarEntry;
        while ((tarEntry = tar.GetNextEntry()) != null)
        {
            if (tarEntry.IsDirectory)
                continue;

            if (tarEntry.Name == SAVE_INFO_JSON)
            {
                infoStr = ReadStringEntry(tar, (int)tarEntry.Size);
            }
            else if (tarEntry.Name == SAVE_FOSSIL_JSON)
            {
                fossilStr = ReadStringEntry(tar, (int)tarEntry.Size);
            }
            else
            {
                GD.PrintErr("Unknown file in fossil: ", tarEntry.Name);
            }
        }

        return (infoStr, fossilStr);
    }

    private static string ReadStringEntry(TarInputStream tar, int length)
    {
        // Pre-allocate storage
        var buffer = new byte[length];
        {
            using var stream = new MemoryStream(buffer);
            tar.CopyEntryContents(stream);
        }

        return Encoding.UTF8.GetString(buffer);
    }
}
