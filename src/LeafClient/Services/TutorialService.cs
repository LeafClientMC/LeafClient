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
            TooltipAnchor = TooltipAnchor.Right
        },
        new TutorialStep
        {
            TargetElementName = "ProfileEditorCard",
            Title = "Set Up Your Profile",
            Body = "Choose a name and Minecraft version for your profile.",
            TooltipAnchor = TooltipAnchor.Right
        },
        new TutorialStep
        {
            TargetElementName = "PO_SaveButton",
            Title = "Save Your Profile",
            Body = "Click Create to save your new profile.",
            WaitForClickElement = "PO_SaveButton",
            TooltipAnchor = TooltipAnchor.Above
        },
        new TutorialStep
        {
            TargetElementName = "ModsTab_Mods",
            Title = "Browse Mods",
            Body = "Discover and install mods directly from Modrinth.",
            NavigateToPage = 3,
            TooltipAnchor = TooltipAnchor.Below
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
            TargetElementName = "AddServerButton",
            Title = "Quick Play",
            Body = "Save your favourite servers here for quick access.",
            NavigateToPage = 2,
            TooltipAnchor = TooltipAnchor.Left
        },
        new TutorialStep
        {
            TargetElementName = "StorePreviewActionBtn",
            Title = "Claim Your Free Cape",
            Body = "Every Leaf Client player gets a free Leaf Cape — click Claim to grab yours!",
            NavigateToPage = 7,
            OnEnter = TutorialOnEnter.SelectLeafCapeInStore,
            WaitForClickElement = "StorePreviewActionBtn",
            HideOverlayAfterAction = true,
            TooltipAnchor = TooltipAnchor.Left
        },
        new TutorialStep
        {
            TargetElementName = "CosTab_Capes",
            Title = "Equip Cosmetics",
            Body = "View and equip your cosmetics here.",
            NavigateToPage = 6,
            TooltipAnchor = TooltipAnchor.Below
        },
        new TutorialStep
        {
            TargetElementName = "AccountPanel",
            Title = "Your Account",
            Body = "Manage your Minecraft and Leaf Client accounts.",
            OpenAccountPanel = true,
            TooltipAnchor = TooltipAnchor.Left
        },
        new TutorialStep
        {
            TargetElementName = "EditSkinButton",
            Title = "Edit Your Skin",
            Body = "Click here to customise your Minecraft skin.",
            WaitForClickElement = "EditSkinButton",
            TooltipAnchor = TooltipAnchor.Left
        }
    };

    public int CurrentStepIndex { get; private set; } = 0;
    public bool IsRunning { get; private set; } = false;
    public bool IsHiddenForAction { get; private set; } = false;

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
