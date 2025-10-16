# Flaw's Fight Night (v0.2.0)

**Flaw's Fight Night** is a powerful Discord bot designed to manage and automate competitive tournaments of multiple types — including **Round Robin**, **Open Round Robin**, **Ladder**, and more. Built for flexibility, speed, and reliability, it provides a complete tournament lifecycle experience from registration to victory.

---

## 🌟 Features Overview

### 🏆 Tournament Management
- Create, start, and end tournaments with full lifecycle control  
- Support for multiple tournament types and formats  
- Lock/unlock teams and rounds for competition integrity  
- Configure tournament-specific settings (match type, length, tie-breaker rules)

### 👥 Team Management
- Register, edit, and remove teams dynamically  
- Track team stats, standings, and rankings 
- Supports 1v1 up to 20-player teams  

### ⚔️ Match Management
- Report wins, edit post-match results, and track full match history  
- Supports Round Robin and Ladder match rules  
- Includes score validation and automatic leaderboard updates
- Send and cancel challenges between teams in Ladder tournaments

### 📺 Live Tournament Views
- Auto-updating **LiveView** messages for:
  - Match logs
  - Team standings
  - Registered teams
- Real-time display inside designated Discord channels

### 🧠 Admin & Advanced Tools
- Fine-grained admin permissions and debug controls  
- Full GitHub backup integration for data redundancy  
- Quick restore support for offline recovery  

---

## 🚀 Quick Start

### 🔧 Setup Requirements
Before launching the bot, you’ll need:
- A **Discord Bot Token** from the [Discord Developer Portal](https://discord.com/developers)
- A **GitHub Personal Access Token (Fine-Grained)** with:
  - Repository Access → Selected Repository
  - Permissions → `Contents: Read & Write` (auto-enables Metadata)
- A **Git Repository HTTPS URL** for backup storage
- The **Guild ID** of your Discord server

### ⚙️ Running the Bot
1. Download the latest release from the [Releases Page](https://github.com/Theinfection91/FlawsFightNight/releases)
2. Extract the `.zip` contents and run `FlawsFightNight.exe`
3. Follow the on-screen prompts to:
   - Enter your Discord Token  
   - Enter GitHub Token & HTTPS Repo URL (optional but recommended)  
   - Select your Guild ID for slash commands  
4. Once setup completes, your bot will appear online and ready to use!

---

## 💬 Core Slash Commands

| Category | Example Commands | Description |
|-----------|------------------|-------------|
| **Tournament** | `/tournament create`, `/tournament start`, `/tournament end` | Manage tournaments and progress rounds |
| **Team** | `/team register`, `/team delete`, `/team set_rank` | Handle team creation, removal, and ranking |
| **Match** | `/match report-win`, `/match edit` | Report and adjust match outcomes |
| **Settings** | `/settings standings_channel_id set`, `/settings add_debug_admin` | Configure LiveViews and admin settings |

> For a full list of commands and detailed usage examples, see the [📖 Documentation](./Documentation.md).

---

## 🗂️ Git Backup Integration

All tournament data (excluding sensitive credentials) can automatically sync to your GitHub repository.  
This ensures long-term data persistence and seamless migration between systems.

- Auto-sync after data changes  
- Manual push & pull available  
- Offline protection for tournament state

---

## 🧩 Tournament Types

| Type | Description |
|------|--------------|
| **Normal Round Robin** | Everyone plays everyone, twice by default |
| **Open Round Robin** | Flexible scheduling and open-ended matches |
| **Ladder** | Teams challenge up the ladder for rank changes |
| *(Elimination planned)* | Classic knockout format (coming soon) |

---

## 🧰 Development Info

- **Language:** C# (.NET 8.0)
- **Platform:** Discord.NET API
- **Backup:** Git Integration (via `GitBackupManager`)
- **Status:** Active development — next update adds Ladder and Elimination tournaments

---

## 📈 Current Progress

✅ Round Robin tournaments fully functional  
✅ 20+ Slash Commands implemented  
✅ Real-time LiveViews working  
✅ Git backup stable 
🔄 Individual player stats coming soon
🔄 Elimination support coming soon  

---
