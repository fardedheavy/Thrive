﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
///   The main tooltip class for the selections on the microbe editor's selection menu.
///   Contains list of processes and modifiers info.
/// </summary>
public class SelectionMenuToolTip : Control, ICustomToolTip
{
    [Export]
    public NodePath? NameLabelPath;

    [Export]
    public NodePath MpLabelPath = null!;

    [Export]
    public NodePath RequiresUnlockingPath = null!;

    [Export]
    public NodePath RequiresNucleusPath = null!;

    [Export]
    public NodePath DescriptionLabelPath = null!;

    [Export]
    public NodePath ProcessesDescriptionLabelPath = null!;

    [Export]
    public NodePath ModifierListPath = null!;

    [Export]
    public NodePath ProcessListPath = null!;

    /// <summary>
    ///   Hold reference of modifier info elements for easier access to change their values later
    /// </summary>
    private readonly List<ModifierInfoLabel> modifierInfos = new();

#pragma warning disable CA2213
    private PackedScene modifierInfoScene = null!;
    private Font latoBoldFont = null!;

    private Label? nameLabel;
    private Label? mpLabel;
    private Label? requiresNucleusLabel;
    private Label? requiresUnlockingLabel;
    private CustomRichTextLabel? descriptionLabel;
    private CustomRichTextLabel? processesDescriptionLabel;
    private VBoxContainer modifierInfoList = null!;
    private ProcessList processList = null!;
#pragma warning restore CA2213

    private string? displayName;
    private string? description;
    private string processesDescription = string.Empty;
    private int mpCost;
    private bool requiresNucleus;
    private string? requiresUnlocking;

    [Export]
    public string DisplayName
    {
        get => displayName ?? "SelectionMenuToolTip_unset";
        set
        {
            displayName = value;
            UpdateName();
        }
    }

    /// <summary>
    ///   Description of processes an organelle does if any.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: description string should only be set here and not directly on the rich text label node
    ///     as it will be overridden otherwise.
    ///   </para>
    /// </remarks>
    [Export]
    public string ProcessesDescription
    {
        get => processesDescription;
        set
        {
            processesDescription = value;
            UpdateProcessesDescription();
        }
    }

    /// <summary>
    ///   General description of the selectable.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: description string should only be set here and not directly on the rich text label node
    ///     as it will be overridden otherwise.
    ///   </para>
    /// </remarks>
    [Export]
    public string? Description
    {
        get => description;
        set
        {
            description = value;
            UpdateDescription();
        }
    }

    [Export]
    public int MutationPointCost
    {
        get => mpCost;
        set
        {
            mpCost = value;
            UpdateMpCost();
        }
    }

    [Export]
    public bool RequiresNucleus
    {
        get => requiresNucleus;
        set
        {
            requiresNucleus = value;
            UpdateRequiresNucleus();
        }
    }

    [Export]
    public string? RequiresUnlocking
    {
        get => requiresUnlocking;
        set
        {
            requiresUnlocking = value;
            UpdateRequiresUnlocking();
        }
    }

    [Export]
    public float DisplayDelay { get; set; }

    public ToolTipPositioning Positioning { get; set; } = ToolTipPositioning.ControlBottomRightCorner;

    public ToolTipTransitioning TransitionType { get; set; } = ToolTipTransitioning.Immediate;

    public bool HideOnMouseAction { get; set; }

    public Control ToolTipNode => this;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>(NameLabelPath);
        mpLabel = GetNode<Label>(MpLabelPath);
        requiresNucleusLabel = GetNode<Label>(RequiresNucleusPath);
        requiresUnlockingLabel = GetNode<Label>(RequiresUnlockingPath);
        descriptionLabel = GetNode<CustomRichTextLabel>(DescriptionLabelPath);
        processesDescriptionLabel = GetNode<CustomRichTextLabel>(ProcessesDescriptionLabelPath);
        modifierInfoList = GetNode<VBoxContainer>(ModifierListPath);
        processList = GetNode<ProcessList>(ProcessListPath);

        modifierInfoScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/tooltips/ModifierInfoLabel.tscn");
        latoBoldFont = GD.Load<Font>("res://src/gui_common/fonts/Lato-Bold-Smaller.tres");

        UpdateName();
        UpdateDescription();
        UpdateProcessesDescription();
        UpdateMpCost();
        UpdateRequiresNucleus();
        UpdateLists();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateDescription();
            UpdateProcessesDescription();
        }
    }

    /// <summary>
    ///   Instances the UI element for a modifier info
    /// </summary>
    public void AddModifierInfo(string name, float value)
    {
        var modifierInfo = (ModifierInfoLabel)modifierInfoScene.Instance();

        modifierInfo.DisplayName = name;
        modifierInfo.ModifierValue = value.ToString(CultureInfo.CurrentCulture);

        modifierInfoList.AddChild(modifierInfo);
        modifierInfos.Add(modifierInfo);
    }

    public ModifierInfoLabel GetModifierInfo(string nodeName)
    {
        return modifierInfos.Find(found => found.Name == nodeName);
    }

    /// <summary>
    ///   Creates UI elements for the processes info in a specific patch
    /// </summary>
    public void WriteOrganelleProcessList(List<ProcessSpeedInformation>? processes)
    {
        if (processes == null || processes.Count <= 0)
        {
            processList.QueueFreeChildren();

            var noProcessLabel = new Label();
            noProcessLabel.AddFontOverride("font", latoBoldFont);
            noProcessLabel.Text = TranslationServer.Translate("NO_ORGANELLE_PROCESSES");
            processList.AddChild(noProcessLabel);
            return;
        }

        processList.ShowSpinners = false;
        processList.ProcessesTitleColour = new Color(1.0f, 0.83f, 0.0f);
        processList.MarkRedOnLimitingCompounds = true;
        processList.ProcessesToShow = processes;
    }

    /// <summary>
    ///   Sets the value of all the membrane type modifiers on this tooltip relative
    ///   to the referenceMembrane. This currently only reads from the pre-added modifier
    ///   UI elements on this tooltip and doesn't actually create them on runtime.
    /// </summary>
    public void WriteMembraneModifierList(MembraneType referenceMembrane, MembraneType membraneType)
    {
        foreach (var modifier in modifierInfos)
        {
            float deltaValue;

            switch (modifier.Name)
            {
                case "baseMobility":
                    deltaValue = membraneType.MovementFactor - referenceMembrane.MovementFactor;
                    break;
                case "osmoregulationCost":
                    deltaValue = membraneType.OsmoregulationFactor - referenceMembrane.OsmoregulationFactor;
                    break;
                case "resourceAbsorptionSpeed":
                    deltaValue = membraneType.ResourceAbsorptionFactor - referenceMembrane.ResourceAbsorptionFactor;
                    break;
                case "health":
                    deltaValue = membraneType.Hitpoints - referenceMembrane.Hitpoints;
                    break;
                case "physicalResistance":
                    deltaValue = membraneType.PhysicalResistance - referenceMembrane.PhysicalResistance;
                    break;
                case "toxinResistance":
                    deltaValue = membraneType.ToxinResistance - referenceMembrane.ToxinResistance;
                    break;
                case "canEngulf":
                case "engulfInvulnerable":
                    deltaValue = 0;
                    break;
                default:
                    throw new Exception("Unhandled modifier type: " + modifier.Name);
            }

            // All stats with +0 value that are not part of the selected membrane is made hidden
            // on the tooltip so it'll be easier to digest and compare modifier changes
            if (Name != referenceMembrane.InternalName && modifier.ShowValue)
                modifier.Visible = deltaValue != 0;

            // Apply the value to the text labels as percentage (except for Health)
            if (modifier.Name == "health")
            {
                modifier.ModifierValue =
                    StringUtils.FormatPositiveWithLeadingPlus(deltaValue.ToString("F0", CultureInfo.CurrentCulture),
                        deltaValue);
            }
            else
            {
                modifier.ModifierValue = StringUtils.FormatPositiveWithLeadingPlus(TranslationServer
                    .Translate("PERCENTAGE_VALUE")
                    .FormatSafe((deltaValue * 100).ToString("F0", CultureInfo.CurrentCulture)), deltaValue);
            }

            if (modifier.Name == "osmoregulationCost")
            {
                modifier.AdjustValueColor(deltaValue, true);
            }
            else
            {
                modifier.AdjustValueColor(deltaValue);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NameLabelPath != null)
            {
                NameLabelPath.Dispose();
                MpLabelPath.Dispose();
                RequiresNucleusPath.Dispose();
                RequiresUnlockingPath.Dispose();
                DescriptionLabelPath.Dispose();
                ProcessesDescriptionLabelPath.Dispose();
                ModifierListPath.Dispose();
                ProcessListPath.Dispose();
                modifierInfoScene.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            return;

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = nameLabel.Text;
        }
        else
        {
            nameLabel.Text = displayName;
        }
    }

    private void UpdateDescription()
    {
        if (descriptionLabel == null)
            return;

        descriptionLabel.ExtendedBbcode = TranslationServer.Translate(Description);
    }

    private void UpdateProcessesDescription()
    {
        if (processesDescriptionLabel == null)
            return;

        processesDescriptionLabel.ExtendedBbcode = TranslationServer.Translate(ProcessesDescription);
        processesDescriptionLabel.Visible = !string.IsNullOrEmpty(ProcessesDescription);
    }

    private void UpdateMpCost()
    {
        if (mpLabel == null)
            return;

        string cost;

        if (mpCost < 0)
        {
            // Negative MP cost means it actually gives MP, to convey that to the player we need to explicitly
            // prefix the cost with a positive sign
            cost = "+" + Mathf.Abs(mpCost).ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            cost = mpCost.ToString(CultureInfo.CurrentCulture);
        }

        mpLabel.Text = cost;
    }

    private void UpdateRequiresNucleus()
    {
        if (requiresNucleusLabel == null)
            return;

        requiresNucleusLabel.Visible = requiresNucleus;
    }

    private void UpdateRequiresUnlocking()
    {
        if (requiresUnlockingLabel == null)
            return;

        requiresUnlockingLabel.Visible = requiresUnlocking != null;
        if (requiresUnlocking != null)
            requiresUnlockingLabel.Text = RequiresUnlocking;
    }

    private void UpdateLists()
    {
        foreach (ModifierInfoLabel item in modifierInfoList.GetChildren())
        {
            modifierInfos.Add(item);
        }
    }
}
