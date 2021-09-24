﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class CreditsScroll : Container
{
    private const bool ShowAssociationName = true;
    private const bool ShowFullLicenseTexts = true;

    private const string LicenseFile = "res://LICENSE.txt";
    private const string AssetsReadme = "res://assets/README.txt";
    private const string AssetsLicense = "res://assets/LICENSE.txt";
    private const string GodotLicense = "res://doc/GodotLicense.txt";
    private const string OFLLicense = "res://assets/OFL.txt";
    private const string GPLLicense = "res://gpl.txt";

    private const float GameNameEndOffset = 355;
    private const float GameNameInvisibleOffset = 1200;

    private const int OffsetBeforeNextDynamicPart = 65;
    private const int OffsetAfterSection = 50;
    private const int OffsetAfterTeamName = 10;
    private const int ExtraOffsetAfterTeam = 30;

    private const float LicenseTextWidthFraction = 0.85f;
    private const float LicenseTextSpeedMultiplier = 3;

    private const int DestroyTopThreshold = 10;

    // These are used to detect the team lead role names properly
    private const string ProgrammingTeamName = "Programming Team";
    private const string TheoryTeamName = "Theory Team";
    private const string GraphicsTeamName = "Graphics Team";
    private const string SoundTeamName = "Sound Team";
    private const string OutreachTeamName = "Outreach Team";
    private const string GameDesignTeamName = "Game Design Team";
    private const string TestingTeamName = "Testing Team";
    private const string ProjectManagementTeamName = "Project Management Team";

    private readonly List<DynamicPart> destroyedDynamicParts = new List<DynamicPart>();

    private CreditsPhase phase = CreditsPhase.NotRunning;
    private List<DynamicPart> dynamicParts;

    private bool scrolling = true;
    private float scrollOffset;
    private float smoothOffset;

    private GameCredits credits;

    private Control logo;
    private Control revolutionaryGames;
    private Control supportedBy;
    private Control developersHeading;

    private float normalScrollSpeed;

    [Signal]
    public delegate void OnFinishedSignal();

    private enum CreditsPhase
    {
        NotRunning,
        GameName,
        DynamicPartEarly,
        DynamicPart,
    }

    [Export]

    public float ScrollSpeed { get; set; } = 80;

    [Export]
    public bool AutoStart { get; set; } = true;

    [Export]
    public bool ShowGPLLicense { get; set; } = true;

    [Export]
    public NodePath LogoPath { get; set; }

    [Export]
    public NodePath RevolutionaryGamesPath { get; set; }

    [Export]
    public NodePath SupportedByPath { get; set; }

    [Export]
    public NodePath DevelopersHeadingPath { get; set; }

    [Export]
    public Font TeamNameFont { get; set; }

    [Export]
    public Font SectionNameFont { get; set; }

    public override void _Ready()
    {
        logo = GetNode<Control>(LogoPath);
        revolutionaryGames = GetNode<Control>(RevolutionaryGamesPath);
        supportedBy = GetNode<Control>(SupportedByPath);
        developersHeading = GetNode<Control>(DevelopersHeadingPath);

        if (TeamNameFont == null)
            throw new InvalidOperationException($"{nameof(TeamNameFont)} not set");

        if (SectionNameFont == null)
            throw new InvalidOperationException($"{nameof(SectionNameFont)} not set");

        credits = SimulationParameters.Instance.GetCredits();

        if (phase == CreditsPhase.NotRunning && AutoStart)
        {
            Setup();
        }
    }

    public override void _Process(float delta)
    {
        if (!scrolling || phase == CreditsPhase.NotRunning)
            return;

        scrollOffset += delta * ScrollSpeed;
        smoothOffset = Mathf.Round(scrollOffset);

        switch (phase)
        {
            case CreditsPhase.GameName:
            {
                UpdateStaticItemPositions();

                if (scrollOffset > GameNameEndOffset)
                {
                    phase = CreditsPhase.DynamicPartEarly;
                    LoadCurrentDevelopers();
                }

                break;
            }

            case CreditsPhase.DynamicPartEarly:
            {
                UpdateDynamicItems();
                UpdateStaticItemPositions();

                if (scrollOffset > GameNameInvisibleOffset)
                {
                    logo.Visible = false;
                    revolutionaryGames.Visible = false;
                    supportedBy.Visible = false;
                    developersHeading.Visible = false;

                    phase = CreditsPhase.DynamicPart;
                }

                break;
            }

            case CreditsPhase.DynamicPart:
            {
                UpdateDynamicItems();

                // End once all parts have scrolled off-screen
                if (dynamicParts.Count < 1)
                {
                    EmitSignal(nameof(OnFinishedSignal));
                    scrolling = false;
                }

                break;
            }

            default:
                throw new InvalidOperationException("invalid phase");
        }
    }

    public void Restart()
    {
        scrolling = true;

        if (scrollOffset == 0 && phase != CreditsPhase.NotRunning && phase == CreditsPhase.GameName)
            return;

        Setup();
    }

    public void Pause()
    {
        scrolling = false;
    }

    public void Resume()
    {
        scrolling = true;
    }

    private void Setup()
    {
        normalScrollSpeed = ScrollSpeed;
        scrollOffset = 0;
        smoothOffset = 0;
        phase = CreditsPhase.GameName;

        logo.Visible = true;
        revolutionaryGames.Visible = true;
        supportedBy.Visible = ShowAssociationName;
        developersHeading.Visible = true;

        UpdateStaticItemPositions();

        if (dynamicParts != null)
        {
            foreach (var part in dynamicParts)
            {
                part.DetachAndQueueFree();
            }
        }

        dynamicParts = new List<DynamicPart>();
    }

    private void LoadCurrentDevelopers()
    {
        int offset = GetNextDynamicSectionOffset();

        var leadLabel = CreateDynamicPart(offset, TranslationServer.Translate("LEAD_DEVELOPERS"), SectionNameFont);
        offset += (int)leadLabel.RectSize.y + OffsetAfterSection;

        // Team leads
        if (credits.Developers.Current.TryGetValue(ProgrammingTeamName, out var members))
        {
            offset = CreateTeamLeadsList(offset, TranslationServer.Translate("LEAD_PROGRAMMER"),
                TranslationServer.Translate("LEAD_PROGRAMMERS"), FindLeadsInTeam(members));
        }

        if (credits.Developers.Current.TryGetValue(TheoryTeamName, out members))
        {
            offset = CreateTeamLeadsList(offset, TranslationServer.Translate("LEAD_THEORIST"),
                TranslationServer.Translate("LEAD_THEORISTS"), FindLeadsInTeam(members));
        }

        if (credits.Developers.Current.TryGetValue(GraphicsTeamName, out members))
        {
            offset = CreateTeamLeadsList(offset, TranslationServer.Translate("LEAD_ARTIST"),
                TranslationServer.Translate("LEAD_ARTISTS"), FindLeadsInTeam(members));
        }

        if (credits.Developers.Current.TryGetValue(SoundTeamName, out members))
        {
            offset = CreateTeamLeadsList(offset, TranslationServer.Translate("SOUND_TEAM_LEAD"),
                TranslationServer.Translate("SOUND_TEAM_LEADS"), FindLeadsInTeam(members));
        }

        if (credits.Developers.Current.TryGetValue(OutreachTeamName, out members))
        {
            offset = CreateTeamLeadsList(offset, TranslationServer.Translate("LEAD_OUTREACH_PERSON"),
                TranslationServer.Translate("LEAD_OUTREACH_PEOPLE"), FindLeadsInTeam(members));
        }

        if (credits.Developers.Current.TryGetValue(GameDesignTeamName, out members))
        {
            offset = CreateTeamLeadsList(offset, TranslationServer.Translate("LEAD_GAME_DESIGNER"),
                TranslationServer.Translate("LEAD_GAME_DESIGNERS"), FindLeadsInTeam(members));
        }

        if (credits.Developers.Current.TryGetValue(TestingTeamName, out members))
        {
            offset = CreateTeamLeadsList(offset, TranslationServer.Translate("LEAD_TESTER"),
                TranslationServer.Translate("LEAD_TESTERS"), FindLeadsInTeam(members));
        }

        if (credits.Developers.Current.TryGetValue(ProjectManagementTeamName, out members))
        {
            offset = CreateTeamLeadsList(offset, TranslationServer.Translate("LEAD_PROJECT_MANAGER"),
                TranslationServer.Translate("LEAD_PROJECT_MANAGERS"), FindLeadsInTeam(members));
        }

        offset += OffsetBeforeNextDynamicPart;

        // Team members
        var currentLabel =
            CreateDynamicPart(offset, TranslationServer.Translate("CURRENT_DEVELOPERS"), SectionNameFont);
        offset += (int)currentLabel.RectSize.y + OffsetAfterSection;

        foreach (var pair in credits.Developers.Current)
        {
            offset = CreateTeamNameList(offset, pair.Key, pair.Value.Select(p => p.Name));
        }

        // Queue the next section to show once the last item becomes visible
        dynamicParts.Last().OnBecomeVisible += LoadPastDevelopers;
    }

    private void LoadPastDevelopers()
    {
        int offset = GetNextDynamicSectionOffset();

        var currentLabel = CreateDynamicPart(offset, TranslationServer.Translate("PAST_DEVELOPERS"), SectionNameFont);
        offset += (int)currentLabel.RectSize.y + OffsetAfterSection;

        foreach (var pair in credits.Developers.Past)
        {
            offset = CreateTeamNameList(offset, pair.Key, pair.Value.Select(p => p.Name));
        }

        dynamicParts.Last().OnBecomeVisible += LoadOutsideDevelopers;
    }

    private void LoadOutsideDevelopers()
    {
        int offset = GetNextDynamicSectionOffset();

        var currentLabel =
            CreateDynamicPart(offset, TranslationServer.Translate("OUTSIDE_CONTRIBUTORS"), SectionNameFont);
        offset += (int)currentLabel.RectSize.y + 15;

        var patreonPromptLabel = CreateDynamicPart(offset, TranslationServer.Translate("YOU_CAN_MAKE_PULL_REQUEST"));
        offset += (int)patreonPromptLabel.RectSize.y + OffsetAfterSection - 15;

        foreach (var pair in credits.Developers.Outside)
        {
            offset = CreateTeamNameList(offset, pair.Key, pair.Value.Select(p => p.Name));
        }

        dynamicParts.Last().OnBecomeVisible += LoadPatrons;
    }

    private void LoadPatrons()
    {
        int offset = GetNextDynamicSectionOffset();

        var currentLabel = CreateDynamicPart(offset, TranslationServer.Translate("PATRONS"), SectionNameFont);
        offset += (int)currentLabel.RectSize.y + 15;

        var patreonPromptLabel =
            CreateDynamicPart(offset, TranslationServer.Translate("YOU_CAN_SUPPORT_THRIVE_ON_PATREON"));
        offset += (int)patreonPromptLabel.RectSize.y + 35;

        offset = CreateTeamNameList(offset, TranslationServer.Translate("VIP_PATRONS"), credits.Patrons.VIPPatrons);
        offset = CreateTeamNameList(offset, TranslationServer.Translate("DEV_BUILD_PATRONS"),
            credits.Patrons.DevBuildPatrons);
        CreateTeamNameList(offset, TranslationServer.Translate("SUPPORTER_PATRONS"),
            credits.Patrons.SupporterPatrons);

        dynamicParts.Last().OnBecomeVisible += LoadDonators;
    }

    private void LoadDonators()
    {
        int offset = GetNextDynamicSectionOffset();

        var currentLabel = CreateDynamicPart(offset, TranslationServer.Translate("DONATIONS"), SectionNameFont);
        offset += (int)currentLabel.RectSize.y + OffsetAfterSection;

        foreach (var yearPair in credits.Donations)
        {
            var teamNameLabel = CreateDynamicPart(offset, yearPair.Key, TeamNameFont);
            offset += (int)teamNameLabel.RectSize.y + OffsetAfterSection;

            foreach (var monthPair in yearPair.Value)
            {
                offset = CreateTeamNameList(offset, monthPair.Key, monthPair.Value);
            }

            offset += OffsetAfterSection;
        }

        dynamicParts.Last().OnBecomeVisible += LoadTranslators;
    }

    private void LoadTranslators()
    {
        int offset = GetNextDynamicSectionOffset();

        var currentLabel = CreateDynamicPart(offset, TranslationServer.Translate("TRANSLATORS"), SectionNameFont);
        offset += (int)currentLabel.RectSize.y + OffsetAfterSection;

        // For now translators aren't separated by language as a lot of random people have done one or two edits
        // on most languages
        CreateDynamicPart(offset, string.Join("\n", credits.Translators));

        dynamicParts.Last().OnBecomeVisible += LoadLicenses;
    }

    private void LoadLicenses()
    {
        int offset = GetNextDynamicSectionOffset();

        var currentLabel = CreateDynamicPart(offset, TranslationServer.Translate("USED_LIBRARIES_LICENSES"),
            SectionNameFont);
        offset += (int)currentLabel.RectSize.y + OffsetAfterSection;

        var licenseTextLabel = CreateFileLoadedPart(offset, LicenseFile);
        offset += (int)licenseTextLabel.RectSize.y + ExtraOffsetAfterTeam;

        // This is purposefully not translatable
        var extraLicenseInfo = CreateDynamicPart(offset,
            "Godot and other license texts should have been provided along with this copy of Thrive\n" +
            "if not, please visit our Github at \nhttps://github.com/Revolutionary-Games/Thrive/tree/master/doc" +
            "to find the licenses.\n\nThrive assets are licensed under the\n" +
            "Creative Commons Attribution-ShareAlike 3.0 Unported License");

        offset += (int)extraLicenseInfo.RectSize.y + OffsetBeforeNextDynamicPart;

        var assetLicenseText = CreateFileLoadedPart(offset, AssetsReadme);
        offset += (int)assetLicenseText.RectSize.y + ExtraOffsetAfterTeam;

        // ReSharper disable HeuristicUnreachableCode ConditionIsAlwaysTrueOrFalse
#pragma warning disable 162
        if (ShowFullLicenseTexts)
        {
            // This is purposefully not translatable
            var fullLicensesHeading = CreateDynamicPart(offset, "Full license texts follow");
            fullLicensesHeading.OnBecomeVisible += LoadFullLicenseTexts;
        }
        else
        {
            assetLicenseText.OnBecomeVisible += LoadEndRemarks;
        }
#pragma warning restore 162

        // ReSharper restore HeuristicUnreachableCode ConditionIsAlwaysTrueOrFalse
    }

    private void LoadFullLicenseTexts()
    {
        var assetsLicenseLabel = CreateFileLoadedPart(GetNextDynamicSectionOffset(), AssetsLicense);
        assetsLicenseLabel.OnBecomeVisible += () =>
        {
            // As licenses are boring speed this up
            ScrollSpeed = normalScrollSpeed * LicenseTextSpeedMultiplier;

            if (ShowGPLLicense)
            {
                LoadGPLLicense();
            }
            else
            {
                LoadOFLLicense();
            }
        };
    }

    private void LoadGPLLicense()
    {
        var licenseLabel = CreateFileLoadedPart(GetNextDynamicSectionOffset(), GPLLicense);
        licenseLabel.OnBecomeVisible += LoadOFLLicense;
    }

    private void LoadOFLLicense()
    {
        var oflLicense = CreateFileLoadedPart(GetNextDynamicSectionOffset(), OFLLicense);
        oflLicense.OnBecomeVisible += LoadGodotLicense;
    }

    private void LoadGodotLicense()
    {
        // For some reason these really long texts seem to be a bit off in terms of the height, so even though we
        // don't add any height here, we still leave a pretty huge blank gap
        // To try to combat these the Godot license is last of the shown licenses
        var godotLicenseLabel =
            CreateFileLoadedPart(GetNextDynamicSectionOffset() + OffsetBeforeNextDynamicPart, GodotLicense);
        godotLicenseLabel.OnBecomeVisible += () =>
        {
            int offset = GetNextDynamicSectionOffset();

            // An empty text to detect when licenses are about to end
            var endOfLicensesMarker = CreateDynamicPart(offset, " ");

            endOfLicensesMarker.OnBecomeVisible += () =>
            {
                // Restore normal speed after licenses are pretty much over
                ScrollSpeed = normalScrollSpeed;
                LoadEndRemarks();
            };
        };
    }

    private void LoadEndRemarks()
    {
        int offset = GetNextDynamicSectionOffset() + OffsetBeforeNextDynamicPart;

        CreateDynamicPart(offset, TranslationServer.Translate("THANKS_FOR_PLAYING"));

        // This is the last section so when these items have all scrolled off the credits trigger the end signal
    }

    private string GetTranslatedHeading(string team)
    {
        // TODO: translate this dynamic data somehow. Probably need to have a switch with fixed strings that have
        // translations for them
        return team;
    }

    private DynamicPart CreateDynamicPart(int offset, string text, Font overrideFont = null)
    {
        var label = new DynamicPart(offset)
        {
            Text = text,
            RectMinSize = new Vector2(RectSize.x, 0),
            Align = Label.AlignEnum.Center,
        };

        if (overrideFont != null)
            label.AddFontOverride("font", overrideFont);

        AddDynamicItem(label);
        return label;
    }

    private int CreateTeamNameList(int offset, string team, IEnumerable<string> people)
    {
        var teamNameLabel = CreateDynamicPart(offset, GetTranslatedHeading(team), TeamNameFont);
        offset += (int)teamNameLabel.RectSize.y + OffsetAfterTeamName;

        // Combine the names to a single list for probably more efficiency
        // If past team leads need to be marked might need to use rich text label or some other approach there.
        // And this method might need to be split into separate implementations as not all places would need that.
        var memberLabel = CreateDynamicPart(offset, string.Join("\n", people));
        offset += (int)memberLabel.RectSize.y + ExtraOffsetAfterTeam;

        return offset;
    }

    private DynamicPart CreateFileLoadedPart(int offset, string file)
    {
        string text;
        using var reader = new File();

        if (reader.Open(file, File.ModeFlags.Read) == Error.Ok)
        {
            text = reader.GetAsText();
        }
        else
        {
            text = "Missing file to show here!";
            GD.PrintErr("Can't load file to show in credits: ", file);
        }

        var label = new DynamicPart(offset)
        {
            Text = text,
            RectMinSize = new Vector2(RectSize.x * LicenseTextWidthFraction, 0),
            RectPosition = new Vector2(Mathf.Round(RectSize.x * (1.0f - LicenseTextWidthFraction)), 0),
            Align = Label.AlignEnum.Fill,
            Autowrap = true,
        };
        AddDynamicItem(label);
        return label;
    }

    private IEnumerable<string> FindLeadsInTeam(IEnumerable<GameCredits.DeveloperPerson> people)
    {
        return people.Where(p => p.Lead).Select(p => p.Name);
    }

    private int CreateTeamLeadsList(int offset, string leadHeading, string leadHeadingPlural, IEnumerable<string> leads)
    {
        var leadsList = leads.ToList();

        if (leadsList.Count < 1)
            return offset;

        var teamNameLabel =
            CreateDynamicPart(offset, leadsList.Count > 1 ? leadHeadingPlural : leadHeading, TeamNameFont);
        offset += (int)teamNameLabel.RectSize.y + OffsetAfterTeamName;

        // Combine the names to a single list for probably more efficiency
        // If past team leads need to be marked might need to use rich text label or some other approach there.
        // And this method might need to be split into separate implementations as not all places would need that.
        var memberLabel = CreateDynamicPart(offset, string.Join("\n", leadsList));
        offset += (int)memberLabel.RectSize.y + ExtraOffsetAfterTeam;

        return offset;
    }

    private void UpdateStaticItemPositions()
    {
        logo.RectPosition = new Vector2(0, RectSize.y - smoothOffset);
        revolutionaryGames.RectPosition = new Vector2(0, RectSize.y - smoothOffset + 200);
        supportedBy.RectPosition = new Vector2(0, RectSize.y - smoothOffset + 250);
        developersHeading.RectPosition = new Vector2(0, RectSize.y - smoothOffset + 325);
    }

    private void AddDynamicItem(DynamicPart part)
    {
        AddChild(part);
        dynamicParts.Add(part);
        part.UpdatePosition(smoothOffset, RectSize.y);
    }

    private int GetNextDynamicSectionOffset()
    {
        int height = (int)RectSize.y;
        int offset = 0;

        foreach (var part in dynamicParts)
        {
            var y = (int)(part.RectPosition.y + part.RectSize.y) - height;

            if (y > offset)
                offset = y;
        }

        offset += (int)smoothOffset;

        return offset + OffsetBeforeNextDynamicPart;
    }

    private void UpdateDynamicItems()
    {
        var height = RectSize.y;

        foreach (var part in dynamicParts)
        {
            part.UpdatePosition(smoothOffset, height);

            if (!part.HasBeenVisible && part.RectPosition.y < height)
            {
                part.HasBeenVisible = true;

                if (part.OnBecomeVisible != null)
                    Invoke.Instance.Queue(part.OnBecomeVisible);
            }

            if (part.RectPosition.y + part.RectSize.y + DestroyTopThreshold < 0)
            {
                destroyedDynamicParts.Add(part);
            }
        }

        foreach (var part in destroyedDynamicParts)
        {
            part.OnEnded?.Invoke();

            part.DetachAndQueueFree();
            dynamicParts.Remove(part);
        }

        destroyedDynamicParts.Clear();
    }

    private class DynamicPart : Label
    {
        private readonly float startPosition;

        public DynamicPart(float startPosition)
        {
            this.startPosition = startPosition;
        }

        public bool HasBeenVisible { get; set; }

        public Action OnBecomeVisible { get; set; }

        // In the end nothing ended up using the set here, but it should work so it is kept here with a suppression
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public Action OnEnded { get; set; }

        public void UpdatePosition(float currentScroll, float containerHeight)
        {
            RectPosition = new Vector2(RectPosition.x, containerHeight + startPosition - currentScroll);
        }
    }
}
