Draft Twitch Viewers
v2.2: Release

This software is provided "as-is" with no warranties.

Presented under the GPL v3 license.

Creation and/or publication of media (images, videos, etc.) while using this software is authorized.

Created by: Nifty255

Copyright 2015-2016 All rights reserved.


This mod is in RELEASE. However, bugs can still happen. If you have a bug, or a suggestion, please leave it in a mature manner.


FEATURES:

Draft Twitch Viewers (DTV) uses web requests to connect to Twitch, and can pick a random user from any channel, and create a Kerbal in-game with that viewer's name.

- Easy to use interface.
- Draft from any channel, specified in the GUI. Or launch a viewer drawing without adding the winner to the game!
- While getting the channel viewer list, DTV can remove bots (specified by the player/streamer) and viewers with distasteful names.
- Upon attempted draft, an alert is displayed in-game indicating success or failure.
- Customize the draft success message and the drawing success message.
- Fully compatible with both Crew Manifest and Ship Manifest.
- Players can add the viewer directly to the current vessel.
- Rescue your viewers or take them on tours with DTV modified Career Mode Contracts!

CHANGELOG:

v2.2:
- All compatible contracts can now be retroactively modified to include drafted viewers.
- Careeer Mode Tourism Contracts are now modified by DTV to replace stock Kerbals with drafted viewers.
- When a tourism contract is offered, DTV silently drafts and replaces the old Kerbals with the new.
- The DTV contract system will deactivate after 5 consecutive failures and notify the player.
- The DTV contract system will skip a contract if there is no channel name and notify the player.
- NOTE: DTV can have up to 4 times the usual delay because tourism contracts can have up to 4 tourists.
- NOTE: Unknown results can occur if a contract is accepted before it can be modified. A fix is in the works.
- NOTE: To alleviate the above issue, modified contracts show a pre-completed "Modified By DTV" objective.

v2.1.3:
- Fixed the Draft Manager App not saving at all.

v2.1.2:
- Fixed improper saving of the "Add Kerbal to Craft" setting.

v2.1.1:
- DTV's Draft App now toggles visibility with the game UI.

v2.1:
- Careeer Mode Rescue Contracts are now modified by DTV to replace stock Kerbals with drafted viewers.
- When a rescue contract is offered, DTV silently drafts and replaces the old Kerbal with the new.
- The DTV contract system will deactivate after 5 consecutive failures and notify the player.
- The DTV contract system will skip a contract if there is no channel name and notify the player.
- NOTE: Unknown results can occur if a contract is accepted before it can be modified. A fix is in the works.
- NOTE: To alleviate the above issue, modified contracts show a pre-completed "Modified By DTV" objective.

v2.0.1:
- Downgraded target framework from .NET 4.5 to 3.5 to fix mod integration issues.
- Added parameter which can suppress drafts being saved.
- Suppressed saves allow for situations in which it is unclear whether or not the drafted viewer will be used.
- If an unsaved Kerbal will be used, the draft caller can save the name manually through "SaveSuppressedDraft".

v2.0:
- Large code refactor to allow for third-party mod integration.
- Settings reworked for consolidation and added stability.
- Added error handling to the web side of the draft system.
- Removed unnecessary "using"s to clean up code.
- Performing any draft or drawing saves the current settings.
- Made the alert window slightly larger.

v1.1.4:
- Added ability to add drafted Kerbals directly into the craft.
- The option to add directly into crafts is togglable in the Customize menu.
- Kerbals can be added into any part with available seating, simply by clicking it.

v1.1.3:
- Fixed bug which prevented the DTV App from appearing in the flight scene.

v1.1.2:
- Added version label to the bottom of the App window.

v1.1.1.1:
- Fixed "Kerman" toggle and custom messages not loading from file.

v1.1.1:
- Added toggle for adding "Kerman" to the end of Kerbal names.

v1.1:
- Added "Do a Viewer Drawing" which picks a random viewer independent of the draft.
- Viewers pulled for a drawing are stored in its own list to prevent repeat pulls.
- Added "Empty Drawn User List" button which resets the list and allows repeat pulls.
- Added ability to draft viewers of specific jobs.
- Users can still draft viewers, accepting any job.
- NOTE: Drafting for specific jobs may take longer and may fail on low-viewer channels.
- NOTE: The default action for right clicking is to draft with any job.
- Added fund requirement for users in career mode just like normal hiring.
- NOTE: KSP 1.0.2 displays a false hire cost in the Astronaut Complex. DTV shows the correct amount.

v1.0.5:
- Users already drafted are now removed before randomly drafting, skipping them completely.
- The list of users already drafted is stored in individual files for each game save.

v1.0.4.1:
- Fixed icon duplication bug.

v1.0.4:
- KSP 1.0 "Kompatibility" update ;3

v1.0.3:
- Fixed weird audio panning effects.
- Set draft audio to the game UI level.

v1.0.2:
- Removed twitch login requirement.
- Mod no longer posts to twitch chat.
- Mod now requests a user list, parses the list, and then drafts as usual.
- Mod now plays sounds on draft button click, draft success, and failure.
- Added username filtering which removes vulgar usernames form the draft.
- Right clicking the app launcher button will automatically draft a Kerbal.
- The draft alert now shows the drafted user's skill type.


v1.0.1:
- Fixed App window bug on resolutions other than 1080p.
- Fixed Draft alert window always showing "Draft Failed".

v1.0:
- INITIAL RELEASE