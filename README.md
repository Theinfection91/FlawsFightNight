# Flaw's Fight Night (v0.2.5)

**Flaw's Fight Night** is a powerful Discord bot designed to manage competitive tournaments while integrating **real-time UT2004 statistics tracking and advanced player analytics**. It supports multiple tournament types — **Normal Round Robin**, **Open Round Robin**, **Normal Ladder**, and **DSR (Dynamic Skill Rating) Ladder** — with complete lifecycle control, team management, and comprehensive player profiling through FTP integration with UT2004 game servers.

---

## ✨ New Features: UT2004 Stats & Analytics

### 📊 UT2004 Player Profiles & Leaderboards
- **Player Profile Tracking**: Register your UT2004 GUID to link your player profile with your Discord account
- **Multi-Mode Leaderboards**: Real-time player rankings across **iCTF**, **TAM**, **iBR**, and **General** game modes
- **FTP Server Integration**: Automatic stat syncing from UT2004 game servers for live player metrics
- **Elo Rating System**: Elo-based rating calculations per game mode with historical trace data (Inspired by UTStatsDB Elo System)

### ⚡ Advanced Player Analytics
- **Player Comparison**: Side-by-side comparison of two players' UT2004 statistics
- **Win Probability Calculator**: Predict match outcomes between tournament teams powered by OpenSkill
- **Team Suggestion Engine**: Balanced team recommendations for 2v2 up to 5v5 powered by OpenSkill
- **Match Summary Reports**: Detailed breakdowns of individual UT2004 matches with stat logs
- **Tournament Match History**: Track all UT2004 matches linked to tournament play with automatic tagging

### 🎮 Game Mode Support
- **iCTF** (Instagib Capture the Flag) - Fast-paced flag capture mode
- **TAM** (Team Arena Master) - Tactical team combat
- **iBR** (Instagib Bomb Run) - Bomb delivery gameplay
- **General** - Overall aggregate ratings across all modes

---

## Features Overview

### 🏆 Tournament Management
- Create, start, and end tournaments with full lifecycle control  
- Support for multiple tournament types and formats  
- Lock/unlock teams and rounds for competition integrity  
- Configure tournament-specific settings (tie-breaker rules, tournament length)

### 👥 Team Management
- Register, edit, and remove teams dynamically  
- Track team stats, standings, and rankings 
- Supports 1v1 up to 20-player teams  

### ⚔️ Match Management
- Report wins, edit post-match results, and track full match history  
- Supports Round Robin and Ladder match rules  
- Includes score validation and automatic leaderboard updates
- Send and cancel challenges between teams in Ladder tournaments
- Tag tournament matches with UT2004 stat log IDs for cross-reference

### 📊 Live Tournament Views
- Auto-updating LiveView messages for:
  - Match logs
  - Team standings
  - Registered teams
  - UT2004 player leaderboards (per game mode)
- Real-time display inside designated Discord channels

### 🔧 Admin & Advanced Tools
- Fine-grained admin permissions and debug controls  
- Full GitHub backup integration for data redundancy  
- Quick restore support for offline recovery  
- FTP server configuration and stat sync management

---

## Quick Start

### 📋 Setup Requirements
Before launching the bot, you'll need:
- A **Discord Bot Token** from the [Discord Developer Portal](https://discord.com/developers)
- A **GitHub Personal Access Token (Fine-Grained)** with:
  - Repository Access → Selected Repository
  - Permissions → `Contents: Read & Write` (auto-enables Metadata)
- A **Git Repository HTTPS URL** for backup storage
- *(Optional)* **UT2004 FTP Server credentials** for stat tracking (server IP, username, password)

### 🚀 Running the Bot
1. Download the latest release from the [Releases Page](https://github.com/Theinfection91/FlawsFightNight/releases)
2. Extract the `.zip` contents and run `FlawsFightNight.exe`
3. Follow the on-screen prompts to:
   - Enter your Discord Token  
   - Enter GitHub Token & HTTPS Repo URL (optional but recommended)  
   - Configure UT2004 FTP server details (optional but recommended for stats tracking)
   - Select your Guild ID for slash commands  
4. Once setup completes, your bot will appear online and ready to use.

---

## Core Slash Commands

All commands now feature **autocomplete** ✨ for easier use and fewer typos.

| Category | Example Commands | Description |
|-----------|------------------|-------------|
| **Tournament** | `/tournament create`, `/tournament start`, `/tournament end` | Manage tournaments and progress rounds |
| **Team** | `/team register`, `/team delete`, `/team set_rank` | Handle team creation, removal, and ranking |
| **Match** | `/match report-win`, `/match edit` | Report and adjust match outcomes |
| **Settings** | `/settings standings_channel_id set`, `/settings add_debug_admin` | Configure LiveViews and admin settings |
| **UT2004 Stats** | `/stats ut2004 register_guid`, `/stats ut2004 my_player`, `/stats ut2004 leaderboard` | Player profiles, leaderboards, and analytics |
| **Tournament Stats** | `/stats tournament my_profile` | Tournament-specific player achievements |

> For a full list of commands and detailed usage examples, see the [Documentation](./FlawsFightNightDoc.md).

---

## 💾 Git Backup Integration

All tournament data (excluding sensitive credentials) can automatically sync to your GitHub repository.  
This ensures long-term data persistence and seamless migration between systems.

- Auto-sync after data changes   
- Offline protection for tournament state

---

## 🎮 Tournament Types

| Type | Description |
|------|--------------|
| **Normal Round Robin** 🔄 | Traditional round-robin where everyone plays everyone, double by default |
| **Open Round Robin** 🔓 | Flexible scheduling and open-ended matches without round gates |
| **Normal Ladder** 🪜 | Standard ladder tournament where teams challenge each other for rank changes |
| **DSR Ladder** ⭐ | Ladder tournament with **Dynamic Skill Rating** mechanics — rank changes scale based on skill differential between teams for more competitive play |

---

## 💻 Development Info

- **Language:** C# (.NET 8.0)
- **Platform:** Discord.NET API
- **Backup:** Git Integration (via `GitBackupService`)
- **UT2004 Stats:** FTP-based stat parsing (newest parser for legacy UT2004 servers)
- **Status:** Active development

---
