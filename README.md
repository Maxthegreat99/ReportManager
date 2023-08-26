# ReportManager
A plugin for TShock giving servers easy-to-use report, warn and mute commands.

- Originally made by Rozen4334
- Patched and updated for to **TShock** `5.0` by [csmir](https://github.com/csmir) & [RenderBr](https://github.com/RenderBr)
- Updated for **TShock** `5.2` by Maxthegreat99

## How to Install
1. Put the .dll into the `\ServerPlugins\` folder.
2. Get the dependencies.
3. Restart the server.
4. Give your desired group the the permissions defined in the configs folder.

## User Instructions
### Commands and Usage
- `/warn` - base command for all the `warn` subcommands, when a user is warned they are webbed until they type the command `/warn read`, if the player is offline, the same behaviour shall happen but when they rejoin.
- `/mute` - base command for all the `mute` subcommands, muted users cannot talk in chat, even after rejoining. to unmute a player admins must type the command `/mute del <index>`.
- `/report` - base command for all the `report subcommands`, this commands allows players to report users, griefing and other things. This command also allows admins to teleport themselves to the location the report.
### Permissions
- `reportmanager.report` - gives access to the `report` command.
- `reportmanager.staff` - gives access to the staff level subcommands for the 3 commands mentioned above(ex. `mute`, `report teleport`, `warn del` etc...)
### Configs
- `WebHook` - the Webhook URL that the plugin will use to post reports to your desired discord channel. [*Heres a tutorial on how to create a webhook*](https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks)

## Dependencies
Report Manager uses Discord.Net dependencies to send reports through Webhooks, you can get them here:
- [Discord.Net.Webhooks](https://www.nuget.org/packages/Discord.Net.Webhook/#versions-body-tab)
- [Discord.Net.Rest](https://www.nuget.org/packages/Discord.Net.Rest/)
- [Discord.Net.Core](https://www.nuget.org/packages/Discord.Net.Core/)
### How To Download The Dependencies
- 1. On the webpages above(https://www.nuget.org), you can click on "Open in NuGet package explorer"
     
     ![image](https://github.com/Maxthegreat99/ReportManager/assets/100855415/813b74db-ff84-4936-b6a6-5a79d426113a)
     
- 2. In the NuGet package explorer you'll find the `.dll` of the dependency you want embed in the `lib > net6.0` directory
     
     ![image](https://github.com/Maxthegreat99/ReportManager/assets/100855415/8930e196-b304-4d11-b43c-5d5b2a77f62a)
     
- 3. Now all you need to do is download the `.dll` put it in the `\ServerPlugins\` folder and there you go! Task completed!


## Forked repository
https://github.com/RenderBr/ReportManager
