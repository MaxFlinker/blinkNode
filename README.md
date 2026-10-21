# blinkNode

blinkNode starts as a workstation-only utility: it can show local activity as a visible blink without any server or network connection. The later networked blinkNode can connect computers and servers across a private Tailscale network. A NUC is simply one possible node. “Public” chat means visible to authorized members of that tailnet, not open to the public internet.

## Downloads

The current workstation release is **blinkNode Light 0.1.0 for Windows x64**. Download the ZIP from [the latest release](../../releases/latest). It requires the **.NET 8 Desktop Runtime**.

This release contains only the standalone desktop app. It has no AI providers, Grok integration, chat service, node agent, or Tailscale connection. See [the release notes](CHANGELOG.md).

## Changelog

Release history is maintained in [CHANGELOG.md](CHANGELOG.md). The current release, 0.1.0, is the first workstation-only version.

## First version

- Start with a standalone workstation mode (a possible `blinkNode Light` / prequel): one Windows desktop app, no server, no Tailscale, and no other machine required. It can display a virtual blink driven by selected local activity when no physical LED or supported activity source is available. Show clearly whether a blink represents measured activity or a simulation.
- Local activity collection is opt-in and limited to user-selected categories. Keep raw activity data on the workstation, provide pause and clear controls, and do not upload it. Define the initial indicators narrowly; do not collect message contents, keystrokes, or unrelated personal data.
- `blinkNode.Agent` runs on supported Windows or Unix-like machines and reports node status. Hardware-specific abilities such as LED control are optional plugins; a headless server can participate without any LED support.
- `blinkNode.App` runs on Windows desktops. It shows selected node activity in a compact always-on-top window, offers tray and taskbar presence, and provides the chat interface.
- The initial solution uses .NET 8: a cross-platform ASP.NET Core node agent and a Windows Forms desktop app. Linux is the first Unix-like target; keep the agent portable so macOS or other Unix-like systems can be added.
- Both sides communicate over the private Tailscale network. The agent does not expose a public internet port.
- The later networked chat supports IRC-style rooms, direct messages to a named user, and owner-owned AI companions such as Alice, Grok, and Mai. They are the user's companions, configured under that user's control, not shared service bots. The owner can give each a distinct name/persona and connect it only through a supported provider integration. Members can talk with a companion when its owner invites it into a room, ask for opinions, or address it by name; it should feel like a participant rather than a command interface. `#toall` and direct messages can raise a desktop popup; each user can mute popups.
- Each companion belongs to its owner, with that owner's identity, selected provider connection, persona, and private memory. The owner controls which rooms it can join, which room history it can use, and whether it answers when addressed or participates more freely. Room members can see which owner's companion is present and whose provider usage powers it.
- Make providers pluggable for the companion's owner. Start with **Continue with ChatGPT** using OpenAI's documented open-source sign-in flow; eligible users can authorize ChatGPT plan usage for Responses API requests without supplying an API key. Check authorization scopes and eligibility before sending prompts.
- Add GitHub Copilot, Grok, and Microsoft 365 Copilot as separate providers where their supported integrations fit. GitHub's Copilot SDK supports signed-in users and per-user GitHub OAuth with a Copilot subscription. Grok inference uses an xAI API key and has separate API billing from Grok subscriptions. Microsoft 365 Copilot's Chat API is a preview and requires a Microsoft 365 Copilot license for each user. Never use browser cookies or scrape consumer chat sites.
- Provider sign-in authorizes that provider's supported inference API; it does not import that provider's private chat history or memory. Let the owner explicitly import selected material into each companion's private profile and set audience permissions for it. Each assistant profile is separate from this Codex chat and does not inherit its history, identity, or tools.
- Keep provider credentials on the owner's participating device/node, never in the shared chat host or in logs. The owner can start, pause, or revoke each provider connection at any time; show the provider and owner while a companion participates in a room.
- Before sending a message that a local privacy check flags as possibly sensitive to an external AI provider, privately ask its author with a preview and **Send once**, **Edit**, and **Keep local** choices. Make the check local; do not send questionable text to another model to decide whether it is safe. If permission is denied or unanswered, keep it out of the AI request.
- Before using a companion's private profile details in a shared-room reply, privately ask its owner with a preview and **Allow once**, **Edit**, and **Don't share** choices. If permission is denied or unanswered, omit the detail. Keep private chats and room histories isolated.
- Keep AI integration behind a provider interface so a local model or another provider can be added later.
- Tailscale members can join chat from machines that do not expose LED controls. Node administration and any hardware commands require separate authorization roles.
- A configurable always-on node hosts the shared chat service; it need not be the machine with LEDs. The agent stores chat history locally with a configurable retention period.

## Design constraints

- DiskLED by Helge Klein is an Apache-2.0-licensed functional reference for Windows performance counters and animated tray indicators ([source repository](https://github.com/helgeklein/DiskLED)). This workstation starter uses .NET's Windows network counters and does not include DiskLED source. If DiskLED code is later reused or adapted, retain its copyright and license notices and include the Apache-2.0 license and required NOTICE material with distributions.
- Treat Tailscale as private network transport, not as application authorization. blinkNode must identify enrolled users/devices and enforce chat and LED permissions itself.
- Keep transport, node metrics, and hardware control behind interfaces so each platform exposes only capabilities it supports.
- Do not hard-code credentials or expose a public listener.

## Open implementation choices

- Implement and register supported provider sign-in flows; let each owner select which rooms Alice can join and which provider/model powers her.
- Choose which host runs the chat service and how it should be installed as a background service on Linux.
- Identify initial node metrics and any LED-capable hardware to support.
