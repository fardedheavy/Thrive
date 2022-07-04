﻿using System;
using Godot;

/// <summary>
///   A single patch in PatchMapDrawer
/// </summary>
public class PatchMapNode : MarginContainer
{
    [Export]
    public NodePath IconPath = null!;

    // Selected patch node
    [Export]
    public NodePath HighlightPanelPath = null!;

    // Player patch node
    [Export]
    public NodePath MarkPanelPath = null!;

    // for patches adjacent to the selected one
    [Export]
    public NodePath AdjacentPanelPath = null!;

    // TODO: Move this to Constants.cs
    private const float HalfBlinkInterval = 0.5f;

    private TextureRect iconRect = null!;
    private Panel highlightPanel = null!;
    private Panel markPanel = null!;
    private Panel adjacentHighlightPanel = null!;

    /// <summary>
    ///   True if _Ready() has been called
    /// </summary>
    private bool ready;

    /// <summary>
    ///   True if mouse is hovering on this node
    /// </summary>
    private bool highlighted;

    /// <summary>
    ///   True if the current node is selected
    /// </summary>
    private bool selected;

    /// <summary>
    ///   True if player is in the current node
    /// </summary>
    private bool marked;

    /// <summary>
    ///   True if player can move to this patch
    /// </summary>
    private bool enabled = true;

    private bool adjacentToSelectedPatch;

    private Texture? patchIcon;

    private float currentBlinkTime;

    /// <summary>
    ///   This object does nothing with this, this is stored here to make other code simpler
    /// </summary>
    public Patch Patch { get; set; } = null!;

    public ShaderMaterial? MonochromeMaterial { get; set; }

    public Action<PatchMapNode>? SelectCallback { get; set; }

    /// <summary>
    ///   Display the icon in color and make it highlightable/selectable.
    ///   Setting this to false removes current selection.
    /// </summary>
    public bool Enabled
    {
        get => enabled;
        set
        {
            Selected = Selected && value;

            enabled = value;

            if (ready)
            {
                UpdateSelectHighlightRing();
                UpdateGreyscale();
            }
        }
    }

    public Texture? PatchIcon
    {
        get => patchIcon;
        set
        {
            if (patchIcon == value)
                return;

            patchIcon = value;

            if (ready)
                UpdateIcon();
        }
    }

    public bool Highlighted
    {
        get => highlighted;
        set
        {
            highlighted = value;

            if (ready)
                UpdateSelectHighlightRing();
        }
    }

    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;

            if (ready)
                UpdateSelectHighlightRing();
        }
    }

    public bool Marked
    {
        get => marked;
        set
        {
            marked = value;

            if (ready)
                UpdateMarkRing();
        }
    }

    public bool AdjacentToSelectedPatch
    {
        get => adjacentToSelectedPatch;
        set
        {
            adjacentToSelectedPatch = value;
            UpdateSelectHighlightRing();
        }
    }

    public override void _Ready()
    {
        if (Patch == null!)
            GD.PrintErr($"{nameof(PatchMapNode)} should have {nameof(Patch)} set");

        iconRect = GetNode<TextureRect>(IconPath);
        highlightPanel = GetNode<Panel>(HighlightPanelPath);
        markPanel = GetNode<Panel>(MarkPanelPath);
        adjacentHighlightPanel = GetNode<Panel>(AdjacentPanelPath);

        ready = true;

        UpdateSelectHighlightRing();
        UpdateMarkRing();
        UpdateIcon();
        UpdateGreyscale();
    }

    public override void _Process(float delta)
    {
        currentBlinkTime += delta;
        if (currentBlinkTime > HalfBlinkInterval)
        {
            currentBlinkTime = 0;
            if (Marked)
                markPanel.Visible = !markPanel.Visible;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Enabled)
            return;

        if (@event is InputEventMouseButton
            {
                Pressed: true, ButtonIndex: (int)ButtonList.Left or (int)ButtonList.Right,
            })
        {
            ((PatchMapDrawer)GetParent()).MarkDirty();
            OnSelect();
            GetTree().SetInputAsHandled();
        }
    }

    public void OnSelect()
    {
        Selected = true;

        SelectCallback?.Invoke(this);
    }

    public void OnMouseEnter()
    {
        Highlighted = true;
    }

    public void OnMouseExit()
    {
        Highlighted = false;
    }

    private void UpdateSelectHighlightRing()
    {
        if (Enabled)
        {
            highlightPanel.Visible = Highlighted || Selected;
            adjacentHighlightPanel.Visible = AdjacentToSelectedPatch;
        }
        else
        {
            highlightPanel.Visible = false;
            adjacentHighlightPanel.Visible = false;
        }
    }

    private void UpdateMarkRing()
    {
        markPanel.Visible = Marked;
    }

    private void UpdateIcon()
    {
        if (PatchIcon == null)
            return;

        iconRect.Texture = PatchIcon;
    }

    private void UpdateGreyscale()
    {
        iconRect.Material = Enabled ? null : MonochromeMaterial;
    }
}
