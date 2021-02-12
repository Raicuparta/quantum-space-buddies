# Quantum Space Buddies - Outer Wilds Online Multiplayer Mod

Quantum Space Buddies (QSB) is a multiplayer mod for Outer Wilds. The mod uses the OWML mod loader and customized UNET code (internally referred to as QNet or QuantumUNET) for networking.

## License

Copyright (C) 2020 : Henry Pointer (_nebula or misternebula) - Aleksander Waage (AmazingAlek) - Ricardo Lopes (Raicuparta)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

<!-- TOC -->

- [FAQs](#frequently-asked-questions)
  - [What is synced?](#what-is-currently-synced)
  - [Why can't I connect?](#why-cant-i-connect-to-a-server)
- [Installation](#installation)
  - [Easy installation (recommended)](#easy-installation-recommended)
  - [Manual installation](#manual-installation)
- [Playing as a client](#playing-as-a-client)
- [Playing as a host](#playing-as-a-host)
- [Development Setup](#development-setup)
- [Authors](#authors)
- [Help / Discuss development / Whatever](#help--discuss-development--whatever)

<!-- /TOC -->

## Frequently Asked Questions

### What is currently synced?
QSB does not operate on a strict server-client relationship. Each player's game is left as a completely seperate game, and individual elemets are synced or patched so they can be controlled though network messages. Right now, the list of synced objects are :
- Geysers
- Elevators
- Nomai interface orbs
- Player animations
- Player tools
- Player ships
- Player probes
- NPC heads in conversations

### Why can't I connect to a server?
#### For the host :
- Open port 7777 TCP and UDP on your router. If access the internet through multiple layers of routers, the port will need to be opened on every router.
- Open port 7777 TCP and UDP in and out on your firewall. Some AVs might block you editing firewall settings, so check with your specific software.
- Make sure you are giving your public IPv4 address to your clients.
#### For the client :
- Open port 7777 TCP and UDP in and out on your firewall. Some AVs might block you editing firewall settings, so check with your specific software.
- Sometimes, it has helped to change your network profile to "private".
- Make sure you are putting the right address into the address box.

If nothing here works, many people have got QSB working through programs such as Hamachi. Also make sure you are not running through a VPN while trying to connect.

## Installation

### Easy installation (recommended)

- [Install the Outer Wilds Mod Manager](https://github.com/Raicuparta/ow-mod-manager#how-do-i-use-this);
- Install Quantum Space Buddies from the mod list displayed in the application;
- If you can't get the mod manager to work, follow the instructions for manual installation.

### Manual installation

- [Install OWML](https://github.com/amazingalek/owml#installation);
- [Download the latest Quantum Space Buddies release](https://github.com/misternebula/quantum-space-buddies/releases/latest);
- Extract the `QSB` directory to the `OWML/Mods` directory;
- Run `OWML.Launcher.exe` to start the game.

## Playing as a client

- Run the game;
- You'll see some new buttons on the top left;
- Replace `localhost` with the server's public IP address;
- Press "Connect". You can join servers in the menu or in-game, but it is recommended to join in the main menu.";
- If you see "Stop", you are connected.
- If it stops at "Connecting to..." then you or the host has issues with their firewall/router/other.

## Playing as a host

- Open port `7777` on your router.;
- Run the game.;
- You'll see some new buttons on the top left.;
- Press "Host". This can be done in-game or in the menu, but it is recommened to start servers in the menu.;
- If you now see the "Stop" button, you are hosting.;
- Give your external IPv4 address to your clients ([like what you see here](http://whatismyip.host/)).

## Development Setup

- [Download the Outer Wilds Mod Manager](https://github.com/misternebula/ow*mod*manager) and install it anywhere you like;
- Install OWML using the Mod Manager;
- Clone QSB's source;
- Open the file `QSB/QSB.csproj.user` in your favorite text editor;
- Edit the entry `<GameDir>` to point to the directory where Outer Wilds is installed;
- Edit the entry `<OwmlDir>` to point to your OWML directory (it is installed inside the Mod Manager directory);
- Open the project solution file `QSB.sln` in Visual Studio;
- If needed, right click `References` in the Solution Explorer > Manage NuGet Packages > Update OWML to fix missing references;
- Run this to stop tracking QSB.csproj.user: ```git update-index --skip-worktree QSB/QSB.csproj.user```

After doing this, the project references should be working. When you build the solution, the dll and json files will be copied to `[Mod Manager directory]/OWML/QSB`. If this process is successful, you should see the mod show up in the Mod Manager.

If for some reason none of this is working, you might have to set everything manually:

- To fix the references, right*click "References" in the Solution Explorer > "Add Reference", and add all the missing DLLs (references with yellow warning icon). You can find these DLLs in the game's directory (`OuterWilds\OuterWilds_Data\Managed`);
- If Visual Studio isn't able to automatically copy the files, you'll have to copy the built dlls manually to OWML.

## Authors

- [\_nebula](https://github.com/misternebula) - Developer of v0.3 onwards
- [AmazingAlek](https://github.com/amazingalek) - On-and-off developer and sometimes code tidyer
- [Raicuparta](https://github.com/Raicuparta) - Developer of v0.1 - v0.2

## Help / Discuss development / Whatever

[Join the unofficial Outer Wilds Discord](https://discord.gg/Sftcc9Z), we have a nice `#modding` channel where you can discuss all types of things.
