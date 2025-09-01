# Flaw's Fight Night Documentation

## Getting Started

**NOTE: I BORROWED THIS STEP-BY-STEP DOCUMENTATION FROM MY OTHER DISCORD BOT (Ladderbot4) SO YOU MAY SEE SOME NAMES AND REFERENCES TO IT IN THE IMAGES AND STEPS FOR NOW, BUT IT IS STILL THE SAME PROCESS JUST FOLLOW ALONG. WILL UPDATE IN THE FUTURE.**

**2ND NOTE: THE GITHUB TOKEN PORTION OF THIS GUIDE WILL VISUALLY LOOK DIFFERENT ON GITHUB'S WEBSITE NOW COMPARED TO PICTURES, BUT IT IS STILL THE SAME PROCESS OF ONLY ALLOWING READ AND WRITE IN THE CONTENTS SECTION ONCE YOU GET TO IT, AND IT WILL TURN ON THE METADATA AUTOMATICALLY LIKE BEFORE AND IT'LL ONLY BE TWO PERMISSIONS ALLOWED.**

This documentation will guide you through the following essential setup steps:
1. **Configuring the Bot**:
   - Setting up the bot's Discord token.
   - Configuring the GitHub Personal Access Token (PAT).
   - Providing the Git HTTPS URL path for backups.
   - Setting the Guild ID to enable slash commands.

2. **Using Commands**:
   - Overview of user commands for interacting with tournaments, teams, matches, and setttings.
   - Explanation of admin commands for advanced configuration and management.

## Configuring The Bot
How to get/set the Discord Token, Git PAT Token, Git Repo HTTPS Url Path, and the Guild ID for SlashCommands:

## How to Setup Discord Developer Portal Correctly (Discord Token and Bot Invite)

Naivgate to the [Discord Developer Portal](https://discord.com/developers) and log in to your Discord account to follow these steps to correctly set up the bot invite link and generate the Discord Token you will need:

---

### Step 1: Create New Application
After logging into the Discord Developer Portal, while in the **Applications** tab click the `New Application` button and enter the name you desire for the new bot.

![Step 1: New Application](examples/DiscordDeveloperSetup/dds1.png)

---

### Step 2: Enable Intents
After entering a name you will be transfered into the General Information tab of the new application. On the left side of the screen click **Bot** (Click three lines if sidebar is not shown automatically).

![Step 2: Enable Intents1](examples/DiscordDeveloperSetup/dds2.png)

1. Enable each **Privileged Gateway Intent** for the Bot
2. Be sure to **Save the Changes**.

![Step 2: Enable Intents2](examples/DiscordDeveloperSetup/dds3.png)

**HINT** In this Bot tab above the Intents is also where you can change the icon for your bot. This will also change in Discord itself and looks way better than Discord's default avatar.
---

### Step 3: OAuth2 URL Generator
After being sure to save the changes to Intents, now on the left sidebar click the **OAuth2** tab.

![Step 3: OAuth2 URL Generator1](examples/DiscordDeveloperSetup/dds4.png)

Now under **Scopes** tick the box for `bot` and `applications.commands`. After you tick `bot` another box containing more choices will appear below called **Bot Permissions**. Only choose `Administator` from this box, after ticking it every other choice is greyed out anyway.
Your result should look similar to this so far:

![Step 3: OAuth2 URL Generator2](examples/DiscordDeveloperSetup/dds5.png)

Now scroll down and you'll see a dropdown box. Make sure **Guild Install** is selected and the **Generated URL** is now what you can copy and use to invite this bot to your personal Discord server!

![Step 3: OAuth2 URL Generator3](examples/DiscordDeveloperSetup/dds6.png)

---

### Step 4: Invite Bot To Server

Go to the link and it should cause a popup in your Discord. Make sure to have the correct server selected in the drop down to invite the bot to and hit **Continue**.

![Step 4: Invite Bot To Server1](examples/DiscordDeveloperSetup/dds7.png)

Another popup will show the Bot is being invited with Adminitrator Rights, hit **Authorize** to continue.

![Step 4: Invite Bot To Server2](examples/DiscordDeveloperSetup/dds8.png)

You should now see the Bot in the designated server.
![Step 4: Invite Bot To Server3](examples/DiscordDeveloperSetup/dds9.png)

---

### Step 5: Regenerate Discord Token

The last thing I do now is regenerate the Bot Token that we will need to give the bot. Navigate back to the **Bot** tab in the Discord Developer Portal. 
 
![Step 5: Regenerate Discord Token1](examples/DiscordDeveloperSetup/dds2.png)

Now click **Reset Token** and then click **Yes, do it!" in the following popup. Discord will have you re-enter your password to Reset the Token.

![Step 5: Regenerate Discord Token2](examples/DiscordDeveloperSetup/dds10.png)

You should now see something similar to what's below. You'll get a long string of numbers, letters and some periods in it. This is the Discord Bot Token and is required for the bot to work. Hold on to this, keep it safe. If you lose it, you'll need to Reset it again.

![Step 5: Regenerate Discord Token3](examples/DiscordDeveloperSetup/dds11.png)

This completes the Get instructions of the Discord Token, again hold on to it as we will need it once we run the bot.
---

## How To Create Private GitHub Repo And Find Git HTTPS URL Path (Backup Storage)

With my old bot came the ability to backup every .json file (besides credential files as they hold sensitive token information) directly to a GitHub Repo you own or have permission to. Creating a repo is free and easy. I recommend creating a private repo but it's up to you. 

---

### Step 1: Login and Create New Repo
First, navigate to the [GitHub Home Page](https://github.com/). Log in if needed, then click the `+` button and select `New repository`.

![Step 1: Login and Create New Repo](examples/GitRepoSetup/grs1.png)

Choose a **Repository name** and see if it's available. Again, I recommend making the repository private so click the **Private** tick circle and then click **Create repository**.

![Step 1: Login and Create New Repo2](examples/GitRepoSetup/grs2.png)

---

### Step 2: Find Git HTTPS Url Path

You'll now be moved inside of the repository and where you need to copy down the HTTPS Url it gives you. You will need this for the Git Backup Storage process as well when it asks for the **Git HTTPS URL Path**. You can always come back to this repo and find this link again, it will not change unless you rename the repo.

![Step 2: Find Git HTTPS Url Path](examples/GitRepoSetup/grs3.png)

**IMPORTANT** We must put a `.json` file inside the repo to get things started. I hope to patch this out eventually but for now this must be done for the Backup Storage to work properly. Near where you found the Git HTTPS Url, you'll see a hyper link to create a new file for the repo. Click this.

![Step 2: Find Git HTTPS Url Path2](examples/GitRepoSetup/grs4.png)

---

### Step 3: Adding An `init.json` File

1. Type `init.json` where it says Name your file...
2. Click the first Commit Changes button
3. Click the second Commit Chanes button in popup window

![Step 3: Adding An init.json File1](examples/GitRepoSetup/grs5.png)

You should now see the new `init.json` file in the repository. This will allow the initial push to the repo from the bot when commands are executed.

![Step 3: Adding An init.json File1](examples/GitRepoSetup/grs6.png)

**HINT** If you marked your Repo as private but want others you trust to see it, invite their GitHub account as a collaborator in the repo's Settings.
---

## How To Generate a GitHub PAT Token (Backup Storage)

To use your GitHub repository as a backup storage system for the database `.json` files, you need to generate a **GitHub PAT Token** that targets the repository of your choice. Follow the instructions below:

---

### Step 1: Access Settings
On the GitHub Desktop site, navigate to your **Settings**.

![Step 1: Access Settings](examples/GitRepoSetup/1.png)

---

### Step 2: Open Developer Settings
In the left-hand menu, scroll down and select **Developer settings**.

![Step 2: Developer Settings](examples/GitRepoSetup/2.png)

---

### Step 3: Generate a New Fine-Grained Token
Click on **Fine-grained tokens** and then select **Generate new token**.

![Step 3: Generate Token](examples/GitRepoSetup/3.png)

---

### Step 4: Configure Token Details
1. **Name**: Enter a name for the token. This can be any descriptive name and is not used in the program.
2. **Expiration**: Choose an expiration date. It is recommended to set **"No expiration"** since the token will be used for long-term backups. This is optional and depends on your security needs.
3. **Repository Access**: Under **Repository Access**, select **Only select repositories** and choose the specific repository the token will target.

![Step 4: Configure Token Details](examples/GitRepoSetup/4.png)

---

### Step 5: Set Repository Permissions
Under **Repository Permissions**, locate **Contents** and set it to **Read and Write**. This will automatically enable **Metadata** as a required permission. You should now have **2 permissions** selected: **Contents** and **Metadata**.

![Step 5: Set Permissions](examples/GitRepoSetup/5.png)

---

### Step 6: Generate the Token
Click **Generate Token** to finalize the process.

![Step 6: Generate Token](examples/GitRepoSetup/6.png)

---

### Step 7: Save Your Token
Once the token is generated, **copy the token string** and store it securely. If you lose this token, you will need to regenerate it.

![Step 7: Save Token](examples/GitRepoSetup/7.png)

> **Note:** The generated token is required for the program to connect to GitHub and perform backup operations. It must be stored in your program’s configuration file securely.   
---

## Running FlawsFightNight.exe and Initial Bot Setup
Now that we have a **Discord Bot Token**, the **Git PAT Token**, and the **Git HTTPS Url Path** we can fire up **FlawsFightNight** for the first time! Download the latest release of FlawsFightNight [HERE](https://github.com/Theinfection91/FlawsFightNight/releases) or from my Releases section in this Repo and extract the contents of the .zip file into a folder of your choosing. Run the `FlawsFightNight.exe` file and follow these instructions:

### Step 1: Enter Discord Bot Token

Upon running the `.exe` file you'll be welcomed immediately with Windows saying it protected your PC from my Bot. Since this is a small project it is not digitally signed and thus Windows does this. I can assure you there is nothing malicious about this program, but if this warning scares you then you're free to stop the setup now and not use this bot. To bypass this, just click **More Info** and then **Run anyway**

![Step 1: Enter Discord Bot Token1](examples/LB4Setup/lbsu1.png)

![Step 1: Enter Discord Bot Token2](examples/LB4Setup/lbsu2.png)

Once you are passed the Security Check you'll be greeted with a Command Prompt looking window, it might not look like much but this is **FlawsFightNight**. You also may or may not have noticed new folders were created in the folder you extracted the `.zip` to. A few different folders and files will be created while the bot finishes this set up process. In the `Credentials` folder you'll find a couple different json files that hold your discord and github token information; These files will not be uploaded to a Git Repo if Backup Storage is setup. 

Now, The first thing the Bot is going to ask you for is the **Discord Bot Token** that we acquired earlier. Copy and paste it into the program now and then press `Enter` on your keyboard.

![Step 2: Enter Discord Bot Token3](examples/LB4Setup/lbsu3.png)

---

### Step 2: Enter Git PAT Token and HTTPS Path

The second bit of information the bot asks for is the **Git PAT Token** we generated earlier as well. This option can be skipped for now by entering 0 but it is highly recommended to set this up and have backup storage available online in case the bot goes offline for some reason. Copy and paste the token or enter 0 now.

![Step 2: Enter Git PAT Token and HTTPS Path1](examples/LB4Setup/lbsu4.png)

Now it asks for that **Git HTTPS Url Path** that we copied once we made our private repository on GitHub earlier. Copy and paste this into the program now.

![Step 2: Enter Git PAT Token and HTTPS Path2](examples/LB4Setup/lbsu5.png)

You should now see the output say something about `GitBackupManager - No local repo found. Cloning repository...` and `GitBackupManager - Repository cloned successfully.`. If so, the Git PAT Token and Git HTTPS Url Path worked and are now linked together. You will now be asked if you want to copy the newly cloned data as your Database. In most cases, this answer will be Yes (Y). Only in niche situations will you NOT want the cloned data as your Database, like for some reason the local JSON files on the machine are more up to date than the JSON files in the online repo. This would be a rare occurence, but is always possible. To get back to this clone data question process again, **you must delete the 'BackupRepo' folder completely and restart the program itself. Deleting the contents of the folder will not initiate the cloning process again, only by deleting the folder itself will.**

![Step 2: Enter Git PAT Token and HTTPS Path3](examples/LB4Setup/lbsu13.png)

---

### Step 3: Copy and Paste Guild ID From List

Time to locate the Guild ID of the server and give it to the bot. Luckily, I got you on this one. The bot will now dynamically check every server its connected to and give you a list of ID's to choose from. Seeing as how only you invited this one, there should be only one on this list. **CAREFULLY** copy and paste the number. If you hit Ctrl+C twice in fast succession you will close the bot... This is a Windows feature and will look to work code around it one day but for now just be careful. Copy the ID and then paste it right back to the bot and hit `Enter` on your keyboard. This allows for the interactive SlashCommands to immediately show in the given server, instead of waiting up to an hour sometimes for Discord to populate them globally.

![Step 3: Copy and Paste Guild ID From List1](examples/LB4Setup/lbsu7.png)

---

### Step 4: Checking Commands and Backup Storage

Your window should now look similar to mine, with it saying the bot is logged in as the name you assigned it in the Discord Developer Portal and showing the Guild ID again as well as stating it is `Ready`. You will also see the bot online in your server now when the `.exe` is running. If you entered anything incorrectly, exit the bot and edit the `config.json` file in the Settings folder then save it and reload and check the bot again.

![Step 4: Checking Commands and Backup Storage](examples/LB4Setup/lbsu8.png)

Let's enter our first command and get things started. By pressing `/` on your keyboard in the text input bar of your Discord server the bot is assigned to you should see all of the interactive SlashCommands under a category by the name in which you assigned the bot in the Discord Developer Portal. If these did not appear then your Guild ID was entered incorrectly. 

If you receive any errors then please repeat this guide, or reach out directly to me for any questions or help.

---

## Using Commands
Extensive information and how to use the various commands in FlawsFightNight:

## Tournament Commands

### Create Tournament (`/tournament create`)

**Description**
Create a new tournament of the given tournament type and given team size format. Upon success, it will give back a Tournament ID# that is used in commands to target it. Tournament names are unique, can not have two tournaments with the same name.

**Usage**
```csharp
/tournament create tournamentName:<string> tournamentType:<TournamentType> teamSize<int>
```

---

### Delete Tournament (`/tournament delete`)

**Description**:
Upon invoking command, will load a modal box to receive input from user. This is same as Ladderbot but takes the Tournament ID#. Must enter with an uppercase 'T' as this input is case sensitive like Ladderbot4 was. Tournaments can only be deleted before they start or after they end for safety. Will patch in overrides later.

**Usage**
```csharp
/tournament delete
```

---

### Lock Teams In Tournament (`/tournament lock-teams`)

**Description**:
**This command will only be used for Round Robin and Elimination tournaments, no need in Ladders.**
In Round Robin, once at least three teams are registered to a tournament then an admin can lock the teams. This prevents any new teams from being added and also any exisiting from being deleted from it. This lock must be done before starting the tournament.

**Usage**
```csharp
/tournament lock-teams
```

---

### Unlock Teams In Tournament (`/tournament unlock-teams`)

**Description**:
**This command will only be used for Round Robin and Elimination tournaments, no need in Ladders.** 
Using this will unlock the teams before starting a tournament, allowing adding or removal of them. Lock again to be able to start.

**Usage**
```csharp
/tournament unlock-teams
```

---

### Start Tournament (`/tournament start`)

**Description**:
For a Round Robin tournament, once teams are locked you may use this command to load a modal confirmation box that will ask for you to input the tournament ID twice, with your input being case sensitive meaning the T must be uppercase. On success, it will initiate the starting procedures of the tournament like sending each player their teams match schedule by round.

**Usage**
```csharp
/tournament start
```

---

### Lock In Round Of Tournament (`/tournament lock-in-round`)

**Description**:
**This command will only be used for Round Robin and Elimination tournaments, no need in Ladders.**
Once all matches of a round have been played, an admin must lock in the round before being able to advance to the next round. Once locked, admins may not use `/match edit` to make any changes to a post match within the current round.

**Usage**
```csharp
/tournament lock-in-round
```

---

### Unlock Round In Tournament (`/tournament unlock-round`)

**Description**:
**This command will only be used for Round Robin and Elimination tournaments, no need in Ladders.**
If a round has been locked and an admin needs to make a change then use this. Once unlocked, admins may use `/match edit` to make any changes to a post match within the current round.

**Usage**
```csharp
/tournament unlock-round
```

---

### Next Round In Tournament (`/tournament next-round`)

**Description**:
**This command will only be used for Round Robin and Elimination tournaments, no need in Ladders.**
Once the round is locked in, an admin will use this to advance to the next round.

**Usage**
```csharp
/tournament next-round
```

---

### End Tournament (`/tournament end`)

**Description**:
Attempt to end the tournament if all conditions are met. Currently for Round Robin a tournament will only be able to end once the last match of the last round has been reported. Only then will this command work correctly. This too will load the modal confirmation system I designed and ask for the case sensitive input of the tournament ID to end.

**Usage**
```csharp
/tournament end
```

---

### Setup Tournament (`/tournament setup`)

**Description**:
Once a tournament is created, certain ones allow for customization. Round Robin tournaments will have different tie breaker logic you can choose from eventually. It will also allow for a Round Robin tournament to just be a single round robin and not double like default. This command must be used before starting the tournament to take effect.

**Usage**
```csharp
/tournament setup tournamentId:<string> tie_breaker_ruleset:<TieBreakerLogic> is_double_round_robin<bool>
```

---

## Team Commands

### Register Team To Tournament (`/team register`)

**Description**:
Using the tournament's ID#, create and register a new team to it. Team names are unique across all tournaments, so even if in different tournaments you may not have two team Alpha's. Can only register teams to tournaments where teams are unlocked where required like Round Robin. Up to 20 members may be added to a team currently. If a tournament is a 3v3, a team must be created with 3 members when giving the command. If any member is already on a team in the same tournament, the command will not proceed. Members can only be on one team per tournament, but be in as many tournaments as theyd like.

**Usage**
```csharp
/team register team_name:<string> tournament_id:<string> member1:<IUser> member2:<IUser> member3<IUser> etc.
```

---

### Delete Team From Tournament (`/team delete`)

**Description**:
Loads the modal confirmation box and asks for team name that is case sensitive and will delete a desired team from a tournament if conditions are right. Teams may only be removed from Round Robin tournaments before it starts and teams are unlocked, or after it ends and teams are unlocked. If a team can not finish the tournament, an admin should just use report win and declare the other team the winner and give both teams 0 points.

**Usage**
```csharp
/team delete 
```

---

## Match Commands

## Report Win For Match (`/match report-win`)

**Description**:
Using the winning teams name, as well as the score for winning team and losing team this will close out a match and turn it into a post match. This command is universal for admins and regular players. A player may only report wins for their team, but an admin can use this command and report for anyone. It also will accept a winner with a score of 0 to 0 in case a team forfeits or doesnt show that way the other team can win but also not gain any points. Will not accept losing team's score being higher than winning team's score.

**Usage**
```csharp
/match report-win winning_team_name:<string> winning_team_score:<int> losing_team_score:<int>
```

---

## Edit Post Match (`/match edit`)

**Description**:
If a round is unlocked and a match has been played, using the Match ID# that it is assigned an admin can edit the winner and scores of a post match. Once a round is locked or if the round has passed then a match may not be changed.

**Usage**
```csharp
/match edit match_id:<string> winning_team_name:<string> winning_team_score:<int> losing_team_score:<int>
```

---

### Settings Commands

## Set Matches Channel LiveView (`/settings matches_channel_id set`)

**Description**:
This command will take look for the given tournament ID and if found set the given discord channel to be the auto updating Match Log for that tournament. Once the tournament starts for Round Robin it will display all matches to be played for this round and then all previous matchs that have already been played with the result and match ID#.

**Usage**
```csharp
/settings matches_channel_id set tournament_id:<string> channel_id:<IMessageChannel>
```

---

## Remove Matches Channel LiveView (`/settings matches_channel_id remove`)

**Description**:
Stops the given tournament's task of sending the Matches LiveView to a channel

**Usage**
```csharp
/settings matches_channel_id remove tournament_id:<string>
```

---

## Set Standings Channel LiveView (`/settings standings_channel_id set`)

**Description**:
This command will take look for the given tournament ID and if found set the given discord channel to be the auto updating Standings for that tournament. Currently teams jump around when all at 0-0 because of tie breaker logic but will sort out when games are played. If two teams keep swapping ranks its cause they are equally tied and the logic is falling back to a coin flip of who would win. This will be patched out before I move on to Ladder tournaments.

**Usage**
```csharp
/settings standings_channel_id set tournament_id:<string> channel_id:<IMessageChannel>
```

---

## Remove Standings Channel LiveView (`/settings standings_channel_id remove`)

**Description**:
Stops the given tournament's task of sending the Standings LiveView to a channel

**Usage**
```csharp
/settings standings_channel_id remove tournament_id:<string>
```

---

## Set Teams Channel LiveView (`/settings teams_channel_id set`)

**Description**:
This command will take look for the given tournament ID and if found set the given discord channel to be the auto updating Teams information for that tournament like all it's members and rank. Currently you'll see rank jumping when teams are equally tied like I mentioned in Standings LiveView. This will be patched out eventually.

**Usage**
```csharp
/settings teams_channel_id set tournament_id:<string> channel_id:<IMessageChannel>
```

---

## Remove Teams Channel LiveView (`/settings teams_channel_id remove`)

**Description**:
Stops the given tournament's task of sending the Teams LiveView to a channel

**Usage**
```csharp
/settings teams_channel_id remove tournament_id:<string>
```

---

## Add Debug Admin (`/settings add_debug_admin`)

**Description**:
This is a nifty test command I use to bypass a player being on multiple teams within the same tournament. It could be useful for some people if they dont have enough people to fill a tournament and have someone play as two teams, or if admins just want to be nice and help me test sometime and give feedback.

**Usage**
```csharp
/settings add_debug_admin user:<IUser>
```

---

## Remove Debug Admin (`/settings remove_debug_admin`)

**Description**:
This just removes the given user from the debug admin list if they are on it.

**Usage**
```csharp
/settings remove_debug_admin user:<IUser>
```

---
