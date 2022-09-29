﻿using Godot;

public abstract class ThriveopediaPage : PanelContainer
{
    [Export]
    public bool DisplayBackground = true;

    private PanelContainer backgroundPanel = null!;

    private GameProperties? currentGame = null!;

    public abstract string PageName { get; }
    public abstract string TranslatedPageName { get; }

    public Thriveopedia ThriveopediaMain = null!;

    public TreeItem? PageTreeItem;

    public GameProperties? CurrentGame
    {
        get => currentGame;
        set
        {
            currentGame = value;

            UpdateCurrentWorldDetails();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        backgroundPanel = GetNode<PanelContainer>(".");

        if (!DisplayBackground)
            backgroundPanel.AddStyleboxOverride("panel", new StyleBoxEmpty());
    }

    public void Init(Thriveopedia thriveopedia)
    {
        ThriveopediaMain = thriveopedia;
    }

    public abstract void UpdateCurrentWorldDetails();
}