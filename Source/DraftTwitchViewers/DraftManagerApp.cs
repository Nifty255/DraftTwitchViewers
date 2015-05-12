using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace DraftTwitchViewers
{
    /// <summary>
    /// The Draft Manager App. This app is used to connect to twitch and draft users into the game as Kerbals.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class DraftManagerApp : MonoBehaviour
    {
        #region Variables

        /// <summary>
        /// The instance of this object.
        /// </summary>
        private static DraftManagerApp instance;

        /// <summary>
        /// The App Launcher Button
        /// </summary>
        private ApplicationLauncherButton draftManagerButton;
        /// <summary>
        /// Was a right click found?
        /// </summary>
        private bool rightClickFound = false;
        /// <summary>
        /// The current number of frames since a right click was found.
        /// </summary>
        private float currentTimeToReset = 0;
        /// <summary>
        /// The max number of frames since a right click was found.
        /// </summary>
        private float maxTimeToReset = 0.25f;
        /// <summary>
        /// Is the app showing?
        /// </summary>
        private bool isShowing = false;
        /// <summary>
        /// Is the app button being hovered over?
        /// </summary>
        private bool isHovering = false;
        /// <summary>
        /// Is  the user customizing?
        /// </summary>
        private bool isCustomizing = false;
        /// <summary>
        /// The window bounds.
        /// </summary>
        private Rect windowRect;
        /// <summary>
        /// The alert bounds.
        /// </summary>
        private Rect alertRect;
        /// <summary>
        /// The width of the window.
        /// </summary>
        private float windowWidth = 250;
        /// <summary>
        /// The height of the window.
        /// </summary>
        private float windowHeight = 200f;
        /// <summary>
        /// Is the draft failure alert showing?
        /// </summary>
        private bool alertShowing = false;
        /// <summary>
        /// The message the alert is showing for.
        /// </summary>
        private string alertingMsg = "";
        /// <summary>
        /// Did the draft fail?
        /// </summary>
        private bool failedToDraft = false;
        /// <summary>
        /// Is the draft busy?
        /// </summary>
        private bool draftBusy = false;

        /// <summary>
        /// The AudioSource played when a draft is started.
        /// </summary>
        private AudioSource startClip;
        /// <summary>
        /// The AudioSource played when a draft succeeds.
        /// </summary>
        private AudioSource successClip;
        /// <summary>
        /// THe AudioSource played when a draft fails.
        /// </summary>
        private AudioSource failureClip;

        /// <summary>
        /// The twitch channel.
        /// </summary>
        private string channel = "";
        /// <summary>
        /// Remember the channel?
        /// </summary>
        private bool remember = false;
        /// <summary>
        /// The message used when a draft succeeds.
        /// </summary>
        private string draftMessage = "&user has been drafted as a &skill!";
        /// <summary>
        /// The massage used when a user is already drafted.
        /// </summary>
        private string thereMessage = "&user has already been drafted.";
        /// <summary>
        /// The message used when the crew limit is reached.
        /// </summary>
        private string cantMessage = "&user can't be drafted. Crew limit reached!";

        /// <summary>
        /// The settings save location.
        /// </summary>
        private string saveLocation = "GameData/DraftTwitchViewers/";

        /// <summary>
        /// The list of users currently in chat.
        /// </summary>
        private List<string> usersInChat;
        /// <summary>
        /// The list of mods in chat which shouldn't be drafted.
        /// </summary>
        private List<string> botsToRemove;

        #endregion

        #region Unity Functions

        /// <summary>
        /// Called when the MonoBehavior is awakened.
        /// </summary>
        private void Awake()
        {
            // If there's already an instance, delete this instance.
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            // Do not destroy this instance.
            DontDestroyOnLoad(gameObject);
            // Save this instance so others can detect it.
            instance = this;
            // If the player returns to the main menu, disconnect and destroy this isntance.
            //GameEvents.onGameSceneLoadRequested.Add((e) => { if (e == GameScenes.MAINMENU) { instance = null; Destroy(gameObject); } });

            SoundManager.LoadSound("DraftTwitchViewers/Sounds/Start", "Start");
            SoundManager.LoadSound("DraftTwitchViewers/Sounds/Success", "Success");
            SoundManager.LoadSound("DraftTwitchViewers/Sounds/Failure", "Failure");
            startClip = SoundManager.CreateSound("Start", false);
            successClip = SoundManager.CreateSound("Success", false);
            failureClip = SoundManager.CreateSound("Failure", false);

            // Load user settings.
            ConfigNode userSettings = ConfigNode.Load(saveLocation + "User.cfg");
            // If the file exists,
            if (userSettings != null)
            {
                // Get the USER node.
                userSettings = userSettings.GetNode("USER");

                // If the USER node exists,
                if (userSettings != null)
                {
                    // Get the user settings.
                    if (userSettings.HasValue("channel")) { channel = userSettings.GetValue("channel"); }
                    // These settings were remembered, so it should remember again.
                    remember = true;
                }
            }

            // Load message settings.
            ConfigNode msgSettings = ConfigNode.Load(saveLocation + "Messages.cfg");
            // If the file exists,
            if (msgSettings != null)
            {
                // Get the SETTINGS node.
                msgSettings = msgSettings.GetNode("SETTINGS");

                // If the SETTINGS node exists,
                if (msgSettings != null)
                {
                    // Get the message settings.
                    if (msgSettings.HasValue("draftMessage")) { draftMessage = msgSettings.GetValue("draftMessage"); }
                    if (msgSettings.HasValue("thereMessage")) { thereMessage = msgSettings.GetValue("thereMessage"); }
                    if (msgSettings.HasValue("cantMessage")) { cantMessage = msgSettings.GetValue("cantMessage"); }
                }
            }

            // Initialize the list.
            botsToRemove = new List<string>();

            // Load bot settings.
            ConfigNode botSettings = ConfigNode.Load(saveLocation + "Bots.cfg");
            // If the file exists,
            if (botSettings != null)
            {
                // Get the BOTS node.
                botSettings = botSettings.GetNode("BOTS");

                // If the BOTS node exists,
                if (botSettings != null)
                {
                    // Get the list of BOT nodes.
                    ConfigNode[] bots = botSettings.GetNodes("BOT");

                    // Iterate through and add bots to the list.
                    foreach (ConfigNode c in bots)
                    {
                        if (c.HasValue("name")) { botsToRemove.Add(c.GetValue("name")); }
                    }
                }
            }

            // Create the App Launcher button and add it.
            draftManagerButton = ApplicationLauncher.Instance.AddModApplication(
                       DisplayApp,
                       HideApp,
                       HoverApp,
                       UnhoverApp,
                       DummyVoid,
                       Disable,
                       ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT,
                       (Texture)GameDatabase.Instance.GetTexture("DraftTwitchViewers/Textures/Toolbar", false));

            // This app should be mutually exclusive. (It should disappear when the player clicks on another app.
            ApplicationLauncher.Instance.EnableMutuallyExclusive(draftManagerButton);

            // Set up the window bounds.
            windowRect = new Rect(Screen.width - windowWidth, 40f, windowWidth, windowHeight);
            // Set up the alert bounds.
            alertRect = new Rect(Screen.width / 2 - windowWidth / 2, Screen.height / 2 - windowHeight / 4, windowWidth, 1f);

            // Initialize filtering regexes.
            InitRegexes();
        }

        /// <summary>
        /// Called when Unity updates.
        /// </summary>
        void Update()
        {
            if (!rightClickFound)
            {
                rightClickFound = Input.GetKeyDown(KeyCode.Mouse1);
                currentTimeToReset = 0;
            }
            else if (currentTimeToReset < maxTimeToReset)
            {
                currentTimeToReset += Time.deltaTime;
            }
            else if (rightClickFound)
            {
                rightClickFound = false;
            }
        }

        #endregion

        #region App Functions

        /// <summary>
        /// Displays the app when the player clicks.
        /// </summary>
        private void DisplayApp()
        {
            if (rightClickFound)
            {
                // Lowercase the channel.
                channel = channel.ToLower();

                // If the channel is empty,
                if (channel == "")
                {
                    // Send a failure alert.
                    alertingMsg = "Can't draft! Please enter a channel!";
                    failedToDraft = true;
                    alertShowing = true;
                    failureClip.Play();
                }
                else
                {
                    // Else, begin the draft.
                    SaveUser();
                    StartCoroutine(DraftIntoKrew());
                }

                draftManagerButton.SetFalse(false);
            }
            else
            {
                isShowing = true;
            }
        }

        /// <summary>
        /// Displays the app while the player hovers.
        /// </summary>
        private void HoverApp()
        {
            isHovering = true;
        }

        /// <summary>
        /// Hides the app when the player clicks a second time.
        /// </summary>
        private void HideApp()
        {
            isShowing = false;
        }

        /// <summary>
        /// Hides the app when the player unhovers.
        /// </summary>
        private void UnhoverApp()
        {
            isHovering = false;
        }

        /// <summary>
        /// Hides the app when it is disabled.
        /// </summary>
        private void Disable()
        {
            isShowing = false;
            isHovering = false;
        }

        /// <summary>
        /// Repositions the app.
        /// </summary>
        private void Reposition()
        {
            // Gets the button's anchor in 3D space.
            float anchor = draftManagerButton.GetAnchor().x;

            // Adjusts the window bounds.
            windowRect = new Rect(Mathf.Min(anchor + 1210.5f - (windowWidth * (isCustomizing ? 2 : 1)), Screen.width - (windowWidth * (isCustomizing ? 2 : 1))), 40f, (windowWidth * (isCustomizing ? 2 : 1)), windowHeight);
        }

        /// <summary>
        /// A dummy method which returns nothing. 
        /// </summary>
        private void DummyVoid() { /* I do nothing!!! \('o')/ */ }

        #endregion

        #region GUI Functions

        /// <summary>
        /// Called when Unity reaches the GUI phase.
        /// </summary>
        private void OnGUI()
        {
            // If the app is showing ir hovered over,
            if (isShowing || isHovering)
            {
                // Display the window.
                GUILayout.Window(GetInstanceID(), windowRect, AppWindow, "Draft Twitch Viewers", HighLogic.Skin.window);
            }

            // If the alert is showing,
            if (alertShowing)
            {
                // Display the window.
                GUILayout.Window(GetInstanceID() + 1, alertRect, AlertWindow, "Twitch Draft: " + (failedToDraft ? "Failed!" : (draftBusy ? "Working..." : "Success!")), HighLogic.Skin.window);
            }

            Reposition();
        }

        /// <summary>
        /// Draws the app window.
        /// </summary>
        /// <param name="windowID">The windiw ID.</param>
        private void AppWindow(int windowID)
        {
            GUILayout.BeginVertical(HighLogic.Skin.box);

            // Channel
            GUILayout.Label("Channel (Lowercase):", HighLogic.Skin.label);
            channel = GUILayout.TextField(channel, HighLogic.Skin.textField);

            // Remember
            remember = GUILayout.Toggle(remember, "Remember Me", HighLogic.Skin.toggle);

            // Draft a twitch viewer as a crewmember
            if (GUILayout.Button("Draft a Viewer into Crew", HighLogic.Skin.button))
            {
                // Lowercase the channel.
                channel = channel.ToLower();

                // If the channel is empty,
                if (channel == "")
                {
                    // Send a failure alert.
                    alertingMsg = "Can't draft! Please enter a channel!";
                    failedToDraft = true;
                    alertShowing = true;
                    failureClip.Play();
                }
                else
                {
                    // Else, begin the draft.
                    SaveUser();
                    StartCoroutine(DraftIntoKrew());
                }
            }

            // Customize
            GUILayout.Label("", HighLogic.Skin.label);
            if (GUILayout.Button("Customize", HighLogic.Skin.button))
            {
                isCustomizing = !isCustomizing;
            }
            if (isCustomizing)
            {
                // On successful draft.
                GUILayout.Label("Successful Draft:", HighLogic.Skin.label);
                draftMessage = GUILayout.TextField(draftMessage, HighLogic.Skin.textField);

                // When already drafted.
                GUILayout.Label("Already Drafted:", HighLogic.Skin.label);
                thereMessage = GUILayout.TextField(thereMessage, HighLogic.Skin.textField);

                // If crew roster is full.
                GUILayout.Label("Full Roster:", HighLogic.Skin.label);
                cantMessage = GUILayout.TextField(cantMessage, HighLogic.Skin.textField);

                // $user Explanation
                GUILayout.Label("", HighLogic.Skin.label);
                GUILayout.Label("\"&user\" = The user drafted.", HighLogic.Skin.label);
                GUILayout.Label("\"&skill\" = The user's skill.", HighLogic.Skin.label);

                // Bots to remove
                GUILayout.Label("", HighLogic.Skin.label);
                GUILayout.Label("Bots to Remove (One per line, no commas):", HighLogic.Skin.label);
                string botsString = string.Join("\n", botsToRemove.ToArray());
                botsString = GUILayout.TextArea(botsString, HighLogic.Skin.textArea);
                botsToRemove = new List<string>();
                botsToRemove.AddRange(botsString.Split('\n'));

                // Save
                if (GUILayout.Button("Save", HighLogic.Skin.button))
                {
                    SaveMessages();
                    SaveBots();
                }
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the alert window.
        /// </summary>
        /// <param name="windowID">The windiw ID.</param>
        private void AlertWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Alert text.
            GUILayout.Label(alertingMsg, HighLogic.Skin.label);

            // The close button.
            GUILayout.Label("", HighLogic.Skin.label);
            if (GUILayout.Button("Close", HighLogic.Skin.button))
            {
                alertingMsg = "";
                alertShowing = false;
                failedToDraft = false;
            }

            GUILayout.EndVertical();
        }

        #endregion

        #region KSP Functions

        /// <summary>
        /// Creates a new Kerbal, picks a random name from twitch chat, and renames the Kerbal to the random name.
        /// This method is called via StartCoroutine and won't block the game thread.
        /// </summary>
        /// <returns>The coroutine IEnumerator.</returns>
        private IEnumerator DraftIntoKrew()
        {
            // Shows the alert as working.
            alertShowing = true;
            draftBusy = true;
            startClip.Play();

            // Creates a new Unity web request (WWW) using the provided channel.
            WWW getList = new WWW("http://tmi.twitch.tv/group/user/" + channel + "/chatters");

            // Waits for the web request to finish.
            yield return getList;

            // Parse the result into a list of users, still lowercased.
            usersInChat = new List<string>();
            usersInChat.AddRange(ParseIntoNameArray(getList.text, "moderators"));
            usersInChat.AddRange(ParseIntoNameArray(getList.text, "staff"));
            usersInChat.AddRange(ParseIntoNameArray(getList.text, "admins"));
            usersInChat.AddRange(ParseIntoNameArray(getList.text, "global_mods"));
            usersInChat.AddRange(ParseIntoNameArray(getList.text, "viewers"));

            // Remove any bots present.
            foreach (string bot in botsToRemove)
            {
                usersInChat.Remove(bot);
            }

            // Create a new list which will be used to remove from the user list.
            List<string> toRemove = new List<string>();

            // Iterate through the regexes.
            foreach(Regex r in regexes)
            {
                // Iterate through each username per regex.
                foreach(string u in usersInChat)
                {
                    // If the current regex matches the current username,
                    if (r.IsMatch(u))
                    {
                        // Mark the name for removal by adding it to the removal list.
                        toRemove.Add(u);
                    }
                }
            }

            // Iterate through the removal list and remove each entry from the user list.
            foreach(string r in toRemove)
            {
                usersInChat.Remove(r);
            }

            // If the user list is empty,
            if (usersInChat.Count == 0)
            {
                // Send a failure alert.
                alertingMsg = "Can't draft! Empty or invalid channel.";
                failedToDraft = true;
                alertShowing = true;
                failureClip.Play();
                yield break;
            }

            // Gets a random user from the list.
            string userDrafted = usersInChat[UnityEngine.Random.Range(0, usersInChat.Count)];

            // Creates a new Unity web request (WWW) using the user chosen.
            WWW getUser = new WWW("https://api.twitch.tv/kraken/users/" + userDrafted);

            // Waits for the web request to finish.
            yield return getUser;

            // Parses the real username of the chosen user.
            string realUsername = getUser.text.Substring(getUser.text.IndexOf("\"display_name\""));
            realUsername = realUsername.Substring(realUsername.IndexOf(":") + 2);
            realUsername = realUsername.Substring(0, realUsername.IndexOf(",") - 1);

            // Gets the roster system.
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            draftBusy = false;

            // Checks for the prior presence of the chosen user.
            foreach (ProtoCrewMember p in roster.Crew)
            {
                // If the user is present in the roster,
                if (p.name == realUsername + " Kerman")
                {
                    // The user is present. No need to add again.

                    // Alert in-game.
                    alertingMsg = thereMessage.Replace("&user", realUsername);
                    failedToDraft = true;
                    alertShowing = true;
                    failureClip.Play();

                    // Return (for coroutines).
                    yield break;
                }
            }

            // Checks for available roster space.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                // If the roster is full,
                if (roster.GetActiveCrewCount() >= GameVariables.Instance.GetActiveCrewLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)))
                {
                    // The roster is full. Can't add another.

                    // Alert in-game.
                    alertingMsg = cantMessage.Replace("&user", realUsername);
                    failedToDraft = true;
                    alertShowing = true;
                    failureClip.Play();

                    // Return (for coroutines).
                    yield break;
                }
            }

            // All checks have passed.

            // Create a new Kerbal and rename.
            ProtoCrewMember newKerbal = roster.GetNewKerbal();
            newKerbal.name = realUsername + " Kerman";
            KerbalRoster.SetExperienceTrait(newKerbal);

            // If the game mode is not Career, set the skill level to maximum possible.
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
            {
                newKerbal.experienceLevel = 5;
                newKerbal.experience = 9999;
            }

            // Alert in-game.
            alertingMsg = draftMessage.Replace("&user", realUsername).Replace("&skill", newKerbal.experienceTrait.Title);
            failedToDraft = false;
            alertShowing = true;
            successClip.Play();
        }

        /// <summary>
        /// Saves the user information set by the player.
        /// </summary>
        private void SaveUser()
        {
            ConfigNode root = new ConfigNode();
            if (remember)
            {
                ConfigNode settings = root.AddNode("USER");
                settings.AddValue("channel", channel);
            }
            root.Save(saveLocation + "User.cfg");
        }

        /// <summary>
        /// Saves the custom messages set by the player.
        /// </summary>
        private void SaveMessages()
        {
            ConfigNode root = new ConfigNode();
            ConfigNode settings = root.AddNode("SETTNGS");
            settings.AddValue("draftMessage", draftMessage);
            settings.AddValue("thereMessage", thereMessage);
            settings.AddValue("cantMessage", cantMessage);
            root.Save(saveLocation + "Messages.cfg");
        }

        /// <summary>
        /// Saves the list of bots to remove from the draft list.
        /// </summary>
        private void SaveBots()
        {
            ConfigNode root = new ConfigNode();
            ConfigNode bots = root.AddNode("BOTS");
            foreach(string bot in botsToRemove)
            {
                ConfigNode botNode = bots.AddNode("BOT");
                botNode.AddValue("name", bot);
            }
            root.Save(saveLocation + "Bots.cfg");
        }

        #endregion

        #region Misc Methods

        /// <summary>
        /// Converts a twitch chatter request into a list of users.
        /// </summary>
        /// <param name="toParse">The request to parse.</param>
        /// <param name="parsingFrom">The section of the request to parse.</param>
        /// <returns>A list of users.</returns>
        string[] ParseIntoNameArray(string toParse, string parsingFrom)
        {
            string toSplit = toParse.Substring(toParse.IndexOf("\"" + parsingFrom + "\": ["));
            toSplit = toSplit.Substring(toSplit.IndexOf('[') + 1);
            toSplit = toSplit.Substring(0, toSplit.IndexOf(']'));
            if (toSplit == "")
            {
                return new string[] {};
            }
            toSplit = toSplit.Replace(" ", "");
            toSplit = toSplit.Replace("\n", "");
            string[] toRet = toSplit.Split(',');

            for (int i = 0; i < toRet.Length; i++)
            {
                toRet[i] = toRet[i].Substring(1, toRet[i].Length - 2);
            }

            return toRet;
        }

        #endregion

        #region Regexes

        string[] regexStrings = new string[] {

            "(?<!c)(?:a|4)n(?:a|4)(?:l|i|1)",
            "(?:a|4)nu(?:s|5)",
            "(?:a|4)r(?:s|5)(?:e|3)",
            "(?:a|4)(?:s|5)(?:s|5)",
            "b(?:a|4)(?:l|i|1)(?:l|i|1)(?:s|5)",
            "b(?:a|4)(?:s|5)(?:t|7)(?:a|4)rd",
            "b(?:l|i|1)(?:t|7)ch",
            "b(?:l|i|1)(?:a|4)(?:t|7)ch",
            "b(?:l|i|1)(?:o|0)(?:o|0)dy",
            "b(?:l|i|1)(?:o|0)wj(?:o|0)b",
            "b(?:o|0)(?:l|i|1)(?:l|i|1)(?:o|0)ck",
            "b(?:o|0)(?:l|i|1)(?:l|i|1)(?:o|0)k",
            "b(?:o|0)n(?:e|3)r",
            "b(?:o|0)(?:o|0)b",
            "bum",
            "bu(?:t|7)(?:t|7)",
            "c(?:l|i|1)(?:l|i|1)(?:t|7)",
            "c(?:o|0)ck",
            "c(?:o|0)(?:o|0)n",
            "cr(?:a|4)p",
            "cun(?:t|7)",
            "d(?:a|4)mn",
            "d(?:l|i|1)ck",
            "d(?:l|i|1)(?:l|i|1)d(?:o|0)",
            "dyk(?:e|3)",
            "(?:e|3)r(?:o|0)(?:t|7)(?:l|i|1)c",
            "f(?:a|4)g",
            "f(?:e|3)ck",
            "f(?:e|3)(?:l|i|1)(?:l|i|1)(?:a|4)(?:t|7)",
            "f(?:e|3)(?:l|i|1)ch",
            "fuck",
            "fudg(?:e|3)p(?:a|4)ck",
            "f(?:l|i|1)(?:a|4)ng(?:e|3)",
            "h(?:e|3)(?:l|i|1)(?:l|i|1)",
            "h(?:l|i|1)(?:t|7)(?:l|i|1)(?:e|3)r",
            "h(?:o|0)m(?:o|0)",
            "j(?:e|3)rk",
            "j(?:l|i|1)zz",
            "kn(?:o|0)b(?:e|3)nd",
            "(?:l|i|1)(?:a|4)b(?:l|i|1)(?:a|4)",
            "(?:l|i|1)m(?:a|4)(?:o|0)",
            "(?:l|i|1)mf(?:a|4)(?:o|0)",
            "muff",
            "n(?:l|i|1)gg(?:(?:e|3)r|(?:a|4))",
            "(?:o|0)mg",
            "p(?:e|3)n(?:l|i|1)(?:s|5)",
            "p(?:l|i|1)(?:s|5)(?:s|5)",
            "p(?:o|0)(?:o|0)p",
            "pr(?:l|i|1)ck",
            "pub(?:e|3)",
            "pu(?:s|5)(?:s|5)y",
            "qu(?:e|3)(?:e|3)r",
            "(?:s|5)(?:a|4)(?:t|7)(?:a|4)n",
            "(?:s|5)cr(?:o|0)(?:t|7)um",
            "(?:s|5)(?:e|3)x",
            "(?:s|5)h(?:l|i|1)(?:t|7)",
            "(?:s|5)(?:l|i|1)u(?:t|7)",
            "(?:s|5)m(?:e|3)gm(?:a|4)",
            "(?:s|5)punk",
            "(?:t|7)(?:l|i|1)(?:t|7)",
            "(?:t|7)(?:o|0)(?:s|5)(?:s|5)(?:e|3)r",
            "(?:t|7)urd",
            "(?:t|7)w(?:a|4)(?:t|7)",
            "v(?:a|4)g(?:l|i|1)n(?:a|4)",
            "w(?:a|4)nk",
            "wh(?:o|0)r(?:e|3)",
            "w(?:t|7)f"
        };

        Regex[] regexes;

        void InitRegexes()
        {
            List<Regex> rList = new List<Regex>();

            foreach (string r in regexStrings)
            {
                Regex rNew = new Regex(r, RegexOptions.IgnoreCase);
                rList.Add(rNew);
            }

            regexes = rList.ToArray();
        }

        #endregion
    }
}