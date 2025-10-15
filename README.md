# Flaw's Fight Night v0.2.0

🏆 **The Ultimate Discord Tournament Management Bot** 🏆

Flaw's Fight Night is a comprehensive Discord bot being designed to manage tournaments of various types including Ladder, Round Robin, and Elimination.

## 🌟 Key Features

### Tournament Management
- Create and manage multiple tournament types (Just Normal and Open Round Robin in `v0.1.x`)
- Start and end tournaments with full lifecycle control
- Lock/unlock teams and rounds for competition integrity
- Custom tournament configurations and settings

### Team Management
- Track comprehensive team statistics
- Manage team standings and rankings

### Match Management
- Report match results and scores
- Edit post-match details for accuracy
- Complete match history tracking

### Live Tournament Views
- Real-time match updates in dedicated channels (LiveView)
- Live standings with automatic rankings
- Team displays with current status
- Auto-updating tournament progress

### Advanced Features
- Git backup integration for data safety
- Admin controls with permission management
- Multiple tournament types with different rules
- Comprehensive statistics and analytics

## 🚀 Quick Start

### Key Commands
- `/tournament create` - Create a new tournament
- `/team register` - Register a team for a tournament
- `/tournament lock-teams` - After having at least three teams registered, lock teams in.
- `/tournament unlock-teams` - If you need to add/delete teams before starting, unlock the teams.
- `/tournament start` - If teams are locked, start the tournament and build the match schedule.
- `/match report-win` - Report a match result.
- `/match edit` - Edit a post match if any user input error occurs.
- `/tournament lock-in-round` - Lock in a round after all matches, including if there was a bye match, has been reported.
- `/tournament unlock-round` - Unlock to make any changes to post matches again.
- `/tournament next-round` - Once a round is locked in, move to the next round.
- `/tournament end` - Once all matches for every round is reported, use this to end the tournament and crown the winner.
- `/settings` - Configure bot settings and tournament channel settings for the LiveView auto updating messages for Standings, Matches, and Teams.

## 📋 Available Commands

The bot includes 20+ slash commands organized into categories, with a lot more planned for the future:
- **Tournament Commands** - Create, manage, and control tournaments
- **Team Commands** - Register and manage teams
- **Match Commands** - Handle match reporting and challenges
- **Settings Commands** - Configure bot behavior and tournament LiveView channels

## 🔧 Installation
- Check the Releases page.

## 📊 Current Status

- ✅ Round Robin tournaments working
- ✅ 20+ slash commands
- ✅ Real-time live views
- ✅ Git backup integration
