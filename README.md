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

**The beta is going to be live soon!** Expect rough edges — please report every bug you find through the in-launcher **Feedback** system.

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
<img width="1919" height="1079" alt="titlescreen" src="https://github.com/user-attachments/assets/eadebe97-b988-4e0e-84c3-27651fa7e2d6" />
<img width="1919" height="1079" alt="cosmetics" src="https://github.com/user-attachments/assets/c6f96d90-2284-447a-b969-182d86fd98c3" />
<img width="1920" height="1080" alt="client" src="https://github.com/user-attachments/assets/d6ecb64c-b04d-49c6-9c58-7ea08eecb5a4" />
<img width="546" height="216" alt="discordrpc" src="https://github.com/user-attachments/assets/38d3719e-df11-42fc-85f5-90d280e3b623" />


### Launcher
<img width="1742" height="926" alt="Screenshot 2026-05-02 030151" src="https://github.com/user-attachments/assets/78167a02-88cc-46b0-9663-a9c12283ffb8" />
<img width="1752" height="940" alt="Screenshot 2026-05-02 030037" src="https://github.com/user-attachments/assets/bb02b5e9-bcb1-4e5d-9089-3563abe34119" />
<img width="1743" height="931" alt="Screenshot 2026-05-02 030103" src="https://github.com/user-attachments/assets/34d8d58f-87dd-47f0-9e1e-755dcaf8122c" />
<img width="1746" height="927" alt="Screenshot 2026-05-02 030118" src="https://github.com/user-attachments/assets/d60e488f-a11e-4acb-8000-dc2879c5f1ad" />
<img width="1753" height="923" alt="Screenshot 2026-05-02 030130" src="https://github.com/user-attachments/assets/b135de6c-f26f-4b8e-9762-89265c1be0ce" />



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
- Windows — Single file Executable (.EXE)
- macOS — Single file Executable (.DMG/.APP)
- Linux — Planned

---

## 🗨️ Want to contribute or just follow along?

Join the [Discord](https://discord.gg/4nQnTXjvJu).
