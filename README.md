# KeyWe Archipelago

This mod is a client side implementation for Archipelago.gg

For the APWorld for KeyWe see [KeyWe APWorld]([https://github.com/xMcacutt/Archipelago-KeyWe/releases/latest])

## Setup

#### Mod Manager

COMING SOON

#### Manual Installation

First, download the latest release from the releases page and extract.

To Install MANUALLY, you'll need to install [BepinEx](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.4). Navigate to your KeyWe folder, which should be in steamapps/common/KeyWe, and copy the Bepin files into this folder. Then run KeyWe.exe. this will create the necessary subdirectories for Bepin to function and for mods to load.

Next, close the game and navigate to the plugins folder in the BepInEx folder, and copy the Multiclient.Net dll and KeyWe_AP_Client dll from the latest release into it.

Once you launch the game, the client should be running.

## Connecting

Upon loading into the main menu, you will be greeted by the connection screen, simply fill the text fields with the Server, Port, Slot Name, and Password, and click Connect.  Both players can (and are recommended to) be connected to the archipelago server on the same slot at the same time, then connect to each other through the online menu, though it is safe for only one player to be using the Archipelago mod.

## Log Window

The log in the bottom left of the game can be enabled/disabled by pressing F7. This will show items collected and received in the Archipelago world as they are sent/received.

## How This Rando Works

### Locations

In KeyWe AP, the following locations are considered checks by default

- Each individual objective in a level

- Each level completion (Rank requirement can be set to Bronze, Silver, or Gold)

- Hidden collectibles

#### Challenges

There are 9 challenges in the base calendar levels (1 for each week). These challenges introduce a task you must complete while also completing the level in order to check the location.

#### Overtime

There are 9 Overtime levels, separate from the main calendar. The Overtime levels are divided into 3 seasons, Summer, Fall, and Winter. Each Season has 1 challenge, for a total of 3 challenges total. The Overtime levels are significantly different from the calendar levels in how they play, and the grades for them are based on achieving a minimum score, as opposed to a time limit.

### Telepost Tournament

Telepost Tournament is a DLC for the game, which adds an additional 3 levels separate both from the main calendar and Overtime levels. These levels are a series of challenges where you "race" against other teams, and the grade is determined by the position you finish in, as opposed to time limit or score. Each Tournament race has 3 challenges that are enabled in-level which are also checks.

Attempts to bypass the game's DLC protection are strictly prohibited and won't work anyway... Nice try I guess. [SUPPORT THE DEVS!!!](https://store.steampowered.com/app/1873760/KeyWe__The_100th_Grand_Ol_Telepost_Tournament/?snr=1_5_9__405)

### Items

The following Items can be sent to the KeyWe world

#### Weeks/Level Unlocks

Each week on a calendar is an item. There are 3 weeks per Season, and 3 Seasons. Additionally, Overtime levels are split into 3 seasons, each containing 3 levels. Telepost Tournament, if enabled, is all enabled by a single item

#### Attributes

Certain attributes about your kiwi will begin at a set value. These values include:

- Walk Speed

- Jump Height

- Swim Speed

- Dash Speed

- Chirp Cooldown

- Peck Cooldown

- Respawn Time

- Respawn Move Speed

When you receive an item for one of these attributes, it will slightly boost that attribute, allowing you to maneuver the levels a bit easier/faster.

#### Cosmetics

Each category of cosmetics is a filler item that can be sent. When received, a random cosmetic of the specified type will be applied to your kiwi. The following cosmetic types are available:

- Facewear

- Hat

- Skin

- Backwear

- Footwear

- Hairstyle

- Arms (Disabled unless you own the DLC)

Certain cosmetics will not be automatically equipped in some levels to avoid conflicts with the levels' mechanics.
