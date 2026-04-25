using System;
using System.Collections.Generic;
using LeafClient.Models;

namespace LeafClient.Services;

public sealed class TutorialService
{
    public static TutorialService Instance { get; } = new();

    public List<TutorialStep> Steps { get; } = new()
    {
        new TutorialStep
        {
            TargetElementName = "",
            Title = "Welcome to Leaf Client",
            Body = "Let's take a quick tour of everything available to you.",
            CenterTooltip = true
        },
        new TutorialStep
        {
            TargetElementName = "NewProfileButton",
            Title = "Create a Profile",
            Body = "Click New Profile to set up your first Minecraft profile.",
            NavigateToPage = 1,
            WaitForClickElement = "NewProfileButton",
            TooltipAnchor = TooltipAnchor.Left
        },
        new TutorialStep
        {
            TargetElementName = "ProfileEditorCard",
            Title = "Set Up Your Profile",
            Body = "Fill in a name and Minecraft version, then click Create.",
            WaitForClickElement = "PO_SaveButton",
            TooltipAnchor = TooltipAnchor.Right
        },
        new TutorialStep
        {
            TargetElementName = "ModsTab_Mods",
            Title = "Mods",
            Body = "This is the Mods page. Click the Mods tab to continue.",
            NavigateToPage = 3,
            WaitForClickElement = "ModsTab_Mods",
            TooltipAnchor = TooltipAnchor.Below
        },
        new TutorialStep
        {
            TargetElementName = "ModsBrowseBtn",
            Title = "Browse Mods",
            Body = "Click Browse Mods to discover and install mods from Modrinth.",
            WaitForClickElement = "ModsBrowseBtn",
            TooltipAnchor = TooltipAnchor.Below
        },
        new TutorialStep
        {
            TargetElementName = "ModBrowserPanel",
            Title = "Mod Browser",
            Body = "Search for mods and click Install to add them to your profile.",
            IsSkippable = true,
            SkipLabel = "Next →",
            TooltipAnchor = TooltipAnchor.Right
        },
        new TutorialStep
        {
            TargetElementName = "ModsTab_Rp",
            Title = "Resource Packs",
            Body = "Click the Resource Packs tab to manage textures and sounds.",
            WaitForClickElement = "ModsTab_Rp",
            TooltipAnchor = TooltipAnchor.Below
        },
        new TutorialStep
        {
            TargetElementName = "Tab_Browse",
            Title = "Browse Resource Packs",
            Body = "Click Browse to discover and install resource packs from Modrinth.",
            WaitForClickElement = "Tab_Browse",
            TooltipAnchor = TooltipAnchor.Below
        },
        new TutorialStep
        {
            TargetElementName = "BrowsePanel",
            Title = "Resource Pack Browser",
            Body = "Search for resource packs and click Install to add them to your profile.",
            IsSkippable = true,
            SkipLabel = "Next →",
            TooltipAnchor = TooltipAnchor.Right
        },
        new TutorialStep
        {
            TargetElementName = "AddServerButton",
            Title = "Quick Play",
            Body = "Save your favourite servers here for quick access — click Add Server to try it.",
            NavigateToPage = 2,
            WaitForClickElement = "AddServerButton",
            TooltipAnchor = TooltipAnchor.Left
        },
        new TutorialStep
        {
            TargetElementName = "AddServerModalCard",
            Title = "Add a Server",
            Body = "Fill in your server details and click Add Server. Skip if you don't have one.",
            WaitForClickElement = "AddServerModalSaveButton",
            IsSkippable = true,
            TooltipAnchor = TooltipAnchor.Right
        },
        new TutorialStep
        {
            TargetElementName = "LeafCapeStoreCard",
            Title = "Claim Your Free Cape",
            Body = "Every Leaf Client player gets a free Leaf Cape — click GET FREE to grab yours! Skip if you've already claimed it.",
            NavigateToPage = 7,
            OnEnter = TutorialOnEnter.SelectLeafCapeInStore,
            IsSkippable = true,
            TooltipAnchor = TooltipAnchor.Left
        },
        new TutorialStep
        {
            TargetElementName = "CosmeticsContentPanel",
            Title = "Equip Cosmetics",
            Body = "View and equip your cosmetics here. Take a moment to equip what you like.",
            NavigateToPage = 6,
            TooltipAnchor = TooltipAnchor.Left,
            SkipLabel = "Next →"
        },
        new TutorialStep
        {
            TargetElementName = "AccountPanel",
            Title = "Your Account",
            Body = "Manage your Minecraft and Leaf Client accounts from here.",
            OpenAccountPanel = true,
            TooltipAnchor = TooltipAnchor.Left,
            SkipLabel = "Next →"
        },
        new TutorialStep
        {
            TargetElementName = "EditSkinButton",
            Title = "Edit Your Skin",
            Body = "Click here to customise your Minecraft skin.",
            WaitForClickElement = "EditSkinButton",
            IsSkippable = false,
            TooltipAnchor = TooltipAnchor.Left,
            SkipIfCracked = true
        },
        new TutorialStep
        {
            TargetElementName = "",
            Title = "You're All Set!",
            Body = "That's everything! Enjoy Leaf Client — you can replay this tour anytime from the Settings page.",
            CenterTooltip = true,
            NavigateToPage = 0,
            CenterBtnLabel = "Let's go!"
        }
    };

    public int CurrentStepIndex { get; private set; } = 0;
    public bool IsRunning { get; private set; } = false;
    public bool IsHiddenForAction { get; private set; } = false;
    public bool IsCrackedAccount { get; private set; } = false;

    public void SetCrackedAccount(bool cracked) => IsCrackedAccount = cracked;

    public event Action? TutorialStarted;
    public event Action<TutorialStep>? StepChanged;
    public event Action? TutorialEnded;
    public event Action? TutorialHidden;
    public event Action? TutorialResumed;

    private TutorialService() { }

    public void StartTutorial()
    {
        CurrentStepIndex = 0;
        IsRunning = true;
        IsHiddenForAction = false;
        TutorialStarted?.Invoke();
        StepChanged?.Invoke(Steps[CurrentStepIndex]);
    }

    public void Next()
    {
        if (!IsRunning) return;
        CurrentStepIndex++;
        while (CurrentStepIndex < Steps.Count && IsCrackedAccount && Steps[CurrentStepIndex].SkipIfCracked)
            CurrentStepIndex++;
        if (CurrentStepIndex >= Steps.Count)
        {
            EndTutorial();
            return;
        }
        StepChanged?.Invoke(Steps[CurrentStepIndex]);
    }

    public void Skip()
    {
        EndTutorial();
    }

    public void HideForAction()
    {
        if (!IsRunning) return;
        IsHiddenForAction = true;
        TutorialHidden?.Invoke();
    }

    public void ResumeAfterAction()
    {
        if (!IsRunning || !IsHiddenForAction) return;
        IsHiddenForAction = false;
        TutorialResumed?.Invoke();
        Next();
    }

    public void EndTutorial()
    {
        IsRunning = false;
        IsHiddenForAction = false;
        TutorialEnded?.Invoke();
    }
}
