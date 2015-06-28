Draft Twitch Viewers
v1.1.2: Release

This software is provided "as-is" with no warranties.

Presented under the GPL v3 license.

Creation and/or publication of media (images, videos, etc.) while using this software is authorized.

Created by: Nifty255

Copyright 2015 All rights reserved.


This mod is in RELEASE. However, bugs can still happen. If you have a bug, or a suggestion, please leave it in a mature manner.


FEATURES:

Draft Twitch Viewers (DTV) uses web requests to connect to Twitch, and can pick a random user from any channel, and create a Kerbal in-game
with that viewer's name.

- Easy to use interface.
- Draft from any channel, specified in the GUI.
- While getting the channel viewer list, DTV can remove bots specified by the player/streamer and viewers with distasteful names.
- Customize the draft message, the already drafted message, and the roster full message.
- Upon attempted draft, an alert is displayed in-game indicating success or failure.
- Fully compatible with both Crew Manifest and Ship Manifest.

CHANGELOG:

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