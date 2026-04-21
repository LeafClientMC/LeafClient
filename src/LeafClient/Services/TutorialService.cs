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
            TargetElementName = "GameButton",
            Title = "Create a Profile",
            Body = "Select your Minecraft version and create a profile to launch."
        },
        new TutorialStep
        {
            TargetElementName = "ModsButton",
            Title = "Browse Mods",
            Body = "Find and install mods directly from Modrinth."
        },
        new TutorialStep
        {
            TargetElementName = "ModsButton",
            Title = "Resource Packs",
            Body = "Customise textures and sounds via the Mods -> Resource Packs tab."
        },
        new TutorialStep
        {
            TargetElementName = "ServersButton",
            Title = "Quick Play",
            Body = "Save your favourite servers for quick access."
        },
        new TutorialStep
        {
            TargetElementName = "StoreButton",
            Title = "Get a Free Cape",
            Body = "Visit the store to claim your free Leaf Cape."
        },
        new TutorialStep
        {
            TargetElementName = "CosmeticsButton",
            Title = "Equip Your Cape",
            Body = "Equip cosmetics and preview them on your character."
        },
        new TutorialStep
        {
            TargetElementName = "GameButton",
            Title = "Your Account",
            Body = "Manage your Minecraft and Leaf Client accounts."
        },
        new TutorialStep
        {
            TargetElementName = "SettingsButton",
            Title = "Edit Your Skin",
            Body = "Customise your Minecraft skin.",
            IsSkippable = true
        }
    };

    public int CurrentStepIndex { get; private set; } = 0;
    public bool IsRunning { get; private set; } = false;

    public event Action? TutorialStarted;
    public event Action<TutorialStep>? StepChanged;
    public event Action? TutorialEnded;

    private TutorialService() { }

    public void StartTutorial()
    {
        CurrentStepIndex = 0;
        IsRunning = true;
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

    public void EndTutorial()
    {
        IsRunning = false;
        TutorialEnded?.Invoke();
    }
}
