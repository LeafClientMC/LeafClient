# <img src="https://github.com/user-attachments/assets/31e58932-16eb-441c-900c-739672b50cc5" width="32"> LeafClient

A **Minecraft: Java Edition** client and launcher — **no cheats, no bypasses, no shady business**.
Built from scratch with a modern UI, first-class Microsoft authentication, built-in cosmetics, and a full mod manager.

> Bedrock Edition support is on the roadmap.

---

## ⚠️ Before You Do Anything

**Read the [LICENSE](https://github.com/LeafClientMC/LeafClient/blob/main/LICENSE.md) before using, modifying, or redistributing any part of LeafClient.**
Unauthorized redistribution or reuse of this code will result in a DMCA takedown.

---

## 🚀 Beta Status

**The beta is live.** Expect rough edges — please report every bug you find through the in-launcher **Feedback** system.

### What's in the client (20+ features)

| Category | Features |
|---|---|
| **HUDs** | ArmorHUD · CoordinatesHUD · CPSHUD · FPSHUD · ItemCounterHUD · KeystrokesHUD · MinimapHUD *(in progress)* · PingHUD · Performance Stats |
| **Gameplay** | Freelook · FullBright · ToggleSprint · ToggleCrouch · Zoom · Waypoints |
| **Quality of Life** | ChatMacros · Custom Crosshair · ServerInfo · HUDThemes *(in progress)* |
| **Branding** | Leaf Client logo badge next to your name in the tab list *(client-side only for now)* |

### What's been cut (for now)

- **🔁 Replay** — Removed. Building a full replay system (menu playback, packet simulation for replay worlds, editing/exporting) is weeks of work on its own and isn't worth blocking the first releases over. It'll come back once the core client is stable or the project has more contributors. The editor will be the easy part; faithful packet-replay simulation is the hard part.

---

## 📸 Preview

### In-game client
<img width="1919" alt="client-preview" src="https://github.com/user-attachments/assets/7a6ccc5d-05c6-408b-bcf2-db9fa77fdcd6" />
<img width="1413" alt="freelook-preview" src="https://github.com/user-attachments/assets/489cc80b-14d0-4ba8-9fb8-cef5fd595028" />
<img width="1919" alt="client-ui" src="https://github.com/user-attachments/assets/e516ed98-1240-4913-af70-4e620673a911" />

### Launcher
<img width="1745" alt="launcher-preview" src="https://github.com/user-attachments/assets/a237ceb4-748c-4353-866b-fab75d65bed6" />
<img width="1741" alt="cosmetics-editor" src="https://github.com/user-attachments/assets/daf91e64-1cfa-4687-833a-f28c057b9a5a" />
<img width="1747" alt="server-list" src="https://github.com/user-attachments/assets/45a4f7cd-3f7a-4e3e-9338-1caf5209187f" />
<img width="1744" alt="mods-list" src="https://github.com/user-attachments/assets/11778dba-3c7a-4750-abee-8bbc6fd7cc62" />
<img width="1746" alt="launcher-versions" src="https://github.com/user-attachments/assets/b2a91ec4-ac3f-4091-9b32-a5ce41fe256f" />
<img width="616" alt="accounts-preview" src="https://github.com/user-attachments/assets/21b3feaa-5bc7-42e5-a5e8-57ef44150615" />

---

## 🤔 Is it open source?

**Partially.** The backend logic (services, view-models, models, controls, utilities, and code-behind) is public. The UI design layer (AXAML views, themes, fonts, bundled assets, project files) and anything sensitive — cosmetics pipelines, backend secrets, in-game feature code — is kept private so the repo can't be cloned and rebranded as-is.

Anyone poking around the source can read how things work. They just can't clone-and-compile a drop-in clone.

Contributions are welcome on the public parts.

---

## 🛠️ Contributor

Solo project.
Everything you see — launcher UI, in-game client, the whole auth + cosmetics + mod-manager stack — is built by one person, with LLMs used to accelerate implementation (not replace design).

To make cross-platform Microsoft auth work inside an AOT-compiled launcher, I had to fork and patch CmlLib:
**[CmlLib-XboxAuthNet.AOTCompatible](https://github.com/voidZiAD/CmlLib-XboxAuthNet.AOTCompatible)**

The in-game feature code is 100% original. Third-party mods only enter the picture if *you* drop them into the mods folder yourself or install them through the launcher's Modrinth-powered mod page (which credits the original author by default).

---

## 💎 APIs Powering LeafClient

| Service | What it does |
|---|---|
| [StarlightSkins](https://starlightskins.lunareclipse.studio) | Primary in-launcher skin rendering with emotes |
| [Visage](https://vzge.me/) | Fallback skin rendering |
| [Modrinth API](https://api.modrinth.com) | In-launcher mod browse + install |
| [CmlLib](https://github.com/CmlLib) | Cross-platform Microsoft auth + Minecraft launching |
| [mcstatus.io](https://api.mcstatus.io) | Live Minecraft server status |

---

## 🎯 Project Goal

**Not-for-profit.** The vast majority of features will always be free.

The plan is to match what people expect from established clients like Lunar and Badlion — and then add the creative stuff those clients don't bother with — without locking the useful parts behind a paywall.

**Monetization (minimal and non-intrusive):**
- Static banner ads on the launcher screen — no pop-ups, no video pre-rolls, nothing in-game
- Optional cosmetics
- Possibly an optional subscription down the road

**No in-game paid features** beyond a handful of cosmetics. Everything that affects gameplay stays free.

Feedback shapes the roadmap. If you want something, suggest it.

---

## 🗃️ Project Status

**Beta.** Actively developed. Public release is happening version-by-version.

**Supported Minecraft versions (Fabric):** 1.21 → 1.21.11

**Launcher builds:**
- Windows — AOT, single-file native executable
- macOS — standard .NET build
- Linux — planned

---

## 🗨️ Want to contribute or just follow along?

Join the [Discord](https://discord.gg/4nQnTXjvJu).
