﻿using Godot;

/// <summary>
///   Environment HUD panel part of the microbe HUD
/// </summary>
public partial class EnvironmentPanel : BarPanelBase
{
    private readonly StringName vSeparationReference = new("v_separation");
    private readonly StringName hSeparationReference = new("h_separation");

    public override void AddPrimaryBar(CompoundProgressBar bar)
    {
        base.AddPrimaryBar(bar);
        bar.Narrow = true;
    }

    protected override void UpdatePanelState()
    {
        if (expandButton == null)
            return;

        base.UpdatePanelState();

        if (PanelCompressed)
        {
            primaryBarContainer.Columns = 2;
            primaryBarContainer.AddThemeConstantOverride(vSeparationReference, 20);
            primaryBarContainer.AddThemeConstantOverride(hSeparationReference, 17);

            foreach (var bar in primaryBars)
            {
                bar.Compact = true;
            }
        }
        else
        {
            primaryBarContainer.Columns = 1;
            primaryBarContainer.AddThemeConstantOverride(vSeparationReference, 4);
            primaryBarContainer.AddThemeConstantOverride(hSeparationReference, 0);

            foreach (var bar in primaryBars)
            {
                bar.Compact = false;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            vSeparationReference.Dispose();
            hSeparationReference.Dispose();
        }

        base.Dispose(disposing);
    }
}
