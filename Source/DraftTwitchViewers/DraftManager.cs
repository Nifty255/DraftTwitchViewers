using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DraftTwitchViewers
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class DraftManager : MonoBehaviour
    {
        #region Instance

        /// <summary>
        /// The instance of this class.
        /// </summary>
        private static DraftManager instance;

        /// <summary>
        /// The public instance of this class.
        /// </summary>
        public static DraftManager Instance
        {
            get { return instance; }
        }

        #endregion

        #region Settings Strings

        /// <summary>
        /// The settings save location.
        /// </summary>
        private string settingsLocation = "GameData/DraftTwitchViewers/";
        /// <summary>
        /// The individual game save location.
        /// </summary>
        private string saveLocation = "";

        #endregion

        #region Global Settings

        /// <summary>
        /// The twitch channel.
        /// </summary>
        public string Channel = "";
        /// <summary>
        /// Add "Kerman" to every name?
        /// </summary>
        public bool AddKerman = true;
        /// <summary>
        /// The list of mods in chat which shouldn't be drafted.
        /// </summary>
        public List<string> BotsToRemove;
        /// <summary>
        /// The list of users pulled for a drawing.
        /// </summary>
        public List<string> DrawnUsers;

        #endregion

        #region Per-Save (Local) Settings

        /// <summary>
        /// The list of users already drafted.
        /// </summary>
        public List<string> AlreadyDrafted;

        #endregion

        #region Misc Variables

        /// <summary>
        /// The last known GameScene. Used to determine whether or not to load local settings.
        /// </summary>
        private GameScenes lastKnownScene = GameScenes.MAINMENU;

        #endregion

        #region Unity Functions

        /// <summary>
        /// Called when the MonoBehaviour is first created.
        /// </summary>
        void Awake()
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            #region Global Settings Load

            // Create an empty bot list.
            BotsToRemove = new List<string>();
            // Create an empty drawing list.
            DrawnUsers = new List<string>();

            // Load global settings.
            ConfigNode globalSettings = ConfigNode.Load(settingsLocation + "GlobalSettings.cfg");
            // If the file exists,
            if (globalSettings != null)
            {
                // Used to save if any corrupt nodes were found.
                bool doSave = false;

                #region Draft Settings Load

                // Get the DRAFT node.
                ConfigNode draftSettings = globalSettings.GetNode("DRAFT");

                // If the DRAFT node exists,
                if (draftSettings != null)
                {
                    // Get the global settings.
                    if (draftSettings.HasValue("channel")) { Channel = draftSettings.GetValue("channel"); }
                    if (draftSettings.HasValue("addKerman")) { try { AddKerman = bool.Parse(draftSettings.GetValue("addKerman")); } catch { } }
                }
                // If the DRAFT node doesn't exist,
                else
                {
                    // Log a warning that is wasn't found.
                    Logger.DebugWarning("GlobalSettings.cfg WAS found, but the DRAFT node was not. Using defaults.");
                }

                #endregion

                #region Bot Settings Load

                // Get the list of bots to remove from drafts and drawings.
                ConfigNode botsSettings = globalSettings.GetNode("BOTS");

                // If the BOTS node exists,
                if (botsSettings != null)
                {
                    // Get a list of BOT nodes.
                    ConfigNode[] botNodes = botsSettings.GetNodes("BOT");

                    // Iterate through,
                    foreach (ConfigNode c in botNodes)
                    {
                        // If the node has a name value,
                        if (c.HasValue("name"))
                        {
                            // Add the string name to the list of bots to remove.
                            BotsToRemove.Add(c.GetValue("name"));
                        }
                        // If the node doesn't have a name value,
                        else
                        {
                            // Log a warning that this node is corrupt and is being removed.
                            Logger.DebugWarning("Corrupt BOT node. Removing.");
                            // Remove the corrupt node.
                            botsSettings.RemoveNode(c);
                            // Set doSave to true so the corrupt nodes remain gone.
                            doSave = true;
                        }
                    }
                }
                // If the BOTS node doesn't exist,
                else
                {
                    // Log a warning that is wasn't found.
                    Logger.DebugWarning("GlobalSettings.cfg WAS found, but the BOTS node was not. Using empty list.");
                }

                #endregion

                #region Drawing Settings Load

                // Get the list of users which have already won a drawing.
                ConfigNode drawnSettings = globalSettings.GetNode("DRAWN");

                // If the DRAWN node exists,
                if (drawnSettings != null)
                {
                    // Get a list of USER nodes.
                    ConfigNode[] drawnNodes = drawnSettings.GetNodes("USER");

                    // Iterate through,
                    foreach (ConfigNode c in drawnNodes)
                    {
                        // If the node has a name value,
                        if (c.HasValue("name"))
                        {
                            // Add the string name to the list of drawn users.
                            DrawnUsers.Add(c.GetValue("name"));
                        }
                        // If the node doesn't have a name value,
                        else
                        {
                            // Log a warning that this node is corrupt and is being removed.
                            Logger.DebugWarning("Corrupt DRAWN.USER node. Removing.");
                            // Remove the corrupt node.
                            drawnSettings.RemoveNode(c);
                            // Set doSave to true so the corrupt nodes remain gone.
                            doSave = true;
                        }
                    }
                }
                // If the DRAWN node doesn't exist,
                else
                {
                    // Log a warning that is wasn't found.
                    Logger.DebugWarning("GlobalSettings.cfg WAS found, but the DRAWN node was not. Using empty list.");
                }

                #endregion

                // If corrupt nodes were found and removed,
                if (doSave)
                {
                    // Save the settings file so they remain gone.
                    globalSettings.Save(settingsLocation + "GlobalSettings.cfg");
                }
            }
            // If the file doesn't exist,
            else
            {
                // Log a warning that it wasn't found.
                Logger.DebugWarning("GlobalSettings.cfg wasn't found. Using defaults.");
            }

            #endregion

            // Preload this list (which will be populated when local settings are loaded) to avoid errors.
            AlreadyDrafted = new List<string>();

            // Register for scene changes.
            GameEvents.onGameSceneLoadRequested.Add(ClearLocalSettings);

            // Initialize the regex array.
            InitRegexes();
        }

        #endregion

        #region Draft function

        /// <summary>
        /// Drafts a Kerbal, invoking the suplied success Action if the draft succeeds, or the failure Action if the draft fails.
        /// </summary>
        /// <param name="success">The Action to invoke on draft success.</param>
        /// <param name="failure">The Action to invoke on draft failure.</param>
        /// <param name="forDrawing">Whether the draft is for a drawing, or for an actual draft.</param>
        /// <param name="job">The job for the Kerbal. Optional and defaults to "Any" and is not needed if forDrawing is true.</param>
        /// <returns>The IEnumerator (used for making the draft asynchronously).</returns>
        public static IEnumerator DraftKerbal(Action<string> success, Action<string> failure, bool forDrawing, string job = "Any")
        {
            // If a channel hasn't been input yet,
            if (string.IsNullOrEmpty(instance.Channel))
            {
                // Invoke failure.
                failure.Invoke("Please specify a channel!");
            }
            // Else, continue.
            else
            {
                // Creates a new Unity web request (WWW) using the provided channel.
                WWW getList = new WWW("http://tmi.twitch.tv/group/user/" + instance.Channel + "/chatters");

                // Waits for the web request to finish.
                yield return getList;

                // Check for errors.
                if (string.IsNullOrEmpty(getList.error))
                {
                    // Parse the result into a list of users, still lowercased.
                    List<string> usersInChat = new List<string>();
                    usersInChat.AddRange(instance.ParseIntoNameArray(getList.text, "moderators"));
                    usersInChat.AddRange(instance.ParseIntoNameArray(getList.text, "staff"));
                    usersInChat.AddRange(instance.ParseIntoNameArray(getList.text, "admins"));
                    usersInChat.AddRange(instance.ParseIntoNameArray(getList.text, "global_mods"));
                    usersInChat.AddRange(instance.ParseIntoNameArray(getList.text, "viewers"));

                    // Remove any bots present.
                    foreach (string bot in instance.BotsToRemove)
                    {
                        usersInChat.Remove(bot);
                    }

                    // If it's for a drawing, remove drawn users. If it's for drafting, remove drafted users.
                    if (forDrawing)
                    {
                        // Remove any users who were already drafted.
                        foreach (string drawn in instance.DrawnUsers)
                        {
                            usersInChat.Remove(drawn);
                        }
                    }
                    else
                    {
                        // Remove any users who were already drafted.
                        foreach (string drafted in instance.AlreadyDrafted)
                        {
                            usersInChat.Remove(drafted);
                        }
                    }

                    // Create a new list which will be used to remove from the user list.
                    List<string> toRemove = new List<string>();

                    // Iterate through the regexes.
                    foreach (Regex r in instance.regexes)
                    {
                        // Iterate through each username per regex.
                        foreach (string u in usersInChat)
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
                    foreach (string r in toRemove)
                    {
                        usersInChat.Remove(r);
                    }

                    // If for drawing, perform drawing code.
                    if (forDrawing)
                    {
                        // If the user list is empty,
                        if (usersInChat.Count == 0)
                        {
                            // Invoke the failure Action, allowing the caller to handle this error.
                            failure.Invoke("Can't draw! No more valid users.");
                        }
                        else
                        {
                            // Gets a random user from the list.
                            string userDrawn = usersInChat[UnityEngine.Random.Range(0, usersInChat.Count)];

                            instance.DrawnUsers.Add(userDrawn);

                            // Creates a new Unity web request (WWW) using the user chosen.
                            WWW getUser = new WWW("https://api.twitch.tv/kraken/users/" + userDrawn);

                            // Waits for the web request to finish.
                            yield return getUser;

                            // Check for errors.
                            if (string.IsNullOrEmpty(getUser.error))
                            {
                                // Parses the real username of the chosen user.
                                string realUsername = getUser.text.Substring(getUser.text.IndexOf("\"display_name\""));
                                realUsername = realUsername.Substring(realUsername.IndexOf(":") + 2);
                                realUsername = realUsername.Substring(0, realUsername.IndexOf(",") - 1);

                                // Save save the new user in the drawing file.
                                instance.SaveDrawn();

                                // Invoke the success Action, allowing the caller to continue.
                                success.Invoke(realUsername);
                            }
                            // If there is an error,
                            else
                            {
                                // Invoke failure, stating web error.
                                failure.Invoke("Web error: " + getUser.error);
                            }
                        }
                    }
                    // Else, perform draft code.
                    else
                    {
                        // Set up variables used to exit the search loop.
                        bool foundProperKerbal = false;
                        bool failedToFindOne = false;
                        int searchAttempts = 0;

                        // Set up Kerbal data variables.
                        string realUsername = null;

                        // Perform the search loop at least once, and repeat until success or failure.
                        do
                        {
                            // Incremet attempts.
                            searchAttempts++;

                            // If the user list is empty,
                            if (usersInChat.Count == 0)
                            {
                                // No viewers left.
                                failedToFindOne = true;
                            }
                            // Otherwise, continue attempting to draft.
                            else
                            {
                                // Gets a random user from the list.
                                string userDrafted = usersInChat[UnityEngine.Random.Range(0, usersInChat.Count)];

                                // Creates a new Unity web request (WWW) using the user chosen.
                                WWW getUser = new WWW("https://api.twitch.tv/kraken/users/" + userDrafted);

                                // Waits for the web request to finish.
                                yield return getUser;

                                // Check for errors.
                                if (string.IsNullOrEmpty(getUser.error))
                                {
                                    // Parses the real username of the chosen user.
                                    realUsername = getUser.text.Substring(getUser.text.IndexOf("\"display_name\""));
                                    realUsername = realUsername.Substring(realUsername.IndexOf(":") + 2);
                                    realUsername = realUsername.Substring(0, realUsername.IndexOf(",") - 1);

                                    // Create a new Kerbal prototype and rename.
                                    ProtoCrewMember newKerbal = CrewGenerator.RandomCrewMemberPrototype(ProtoCrewMember.KerbalType.Crew);
                                    newKerbal.name = realUsername + (instance.AddKerman ? " Kerman" : "");
                                    KerbalRoster.SetExperienceTrait(newKerbal);

                                    // If the kerbal satisfies the job requirements, wait... We actually search for qualifications?
                                    if (job == "Any" || newKerbal.experienceTrait.Title == job)
                                    {
                                        // We found a proper Kerbal, so we can add him to the Already Drafted list and exit the loop.
                                        instance.AlreadyDrafted.Add(userDrafted);
                                        foundProperKerbal = true;
                                    }
                                    // Otherwise,
                                    else
                                    {
                                        // The Kerbal lacks the required job. Remove them from the list and go again.
                                        usersInChat.Remove(userDrafted);
                                    }
                                }
                                // If there is an error,
                                else
                                {
                                    // Invoke failure, stating web error.
                                    failure.Invoke("Web error: " + getUser.error);
                                }
                            }
                        }
                        while (!foundProperKerbal && !failedToFindOne && searchAttempts < 25);

                        // If we found a Kerbal with the right job,
                        if (foundProperKerbal)
                        {
                            // Save save the new user in the drawing file.
                            instance.SaveDrafted();

                            // Invoke the success Action, allowing the caller to continue.
                            success.Invoke(realUsername + (instance.AddKerman ? " Kerman" : ""));
                        }
                        // Else, if we failed to find one,
                        else if (failedToFindOne)
                        {
                            // Invoke the failure Action, allowing the caller to handle this error.
                            failure.Invoke("Can't draft! No more valid users.");
                        }
                        // Else, if the search was attempted too many times,
                        else if (searchAttempts >= 25)
                        {
                            // Invoke the failure Action, allowing the caller to handle this error.
                            failure.Invoke("Can't draft! Too many attempts.");
                        }
                    }
                }
                // If there is an error,
                else
                {
                    // Invoke failure, stating web error.
                    failure.Invoke("Web error: " + getList.error);
                }
            }
        }

        #endregion

        #region Saving

        /// <summary>
        /// Saves the global settings used in this class.
        /// </summary>
        public void SaveGlobalSettings()
        {
            // Load global settings.
            ConfigNode globalSettings = ConfigNode.Load(settingsLocation + "GlobalSettings.cfg");
            // If the file exists,
            if (globalSettings != null)
            {
                // Get the draft settings node.
                ConfigNode draftSettings = globalSettings.GetNode("DRAFT");

                // If the DRAFT node doesn't exist,
                if (draftSettings == null)
                {
                    // Create a new DRAFT node to write to.
                    draftSettings = globalSettings.AddNode("DRAFT");
                }

                // Write the Channel and AddKerman values to it.
                if (draftSettings.HasValue("channel")) { draftSettings.SetValue("channel", Channel); } else { draftSettings.AddValue("channel", Channel); }
                if (draftSettings.HasValue("addKerman")) { draftSettings.SetValue("addKerman", AddKerman.ToString()); } else { draftSettings.AddValue("addKerman", AddKerman.ToString()); }

                // Get the list of bots to remove.
                ConfigNode botSettings = globalSettings.GetNode("BOTS");

                // If the BOTS node exists,
                if (botSettings != null)
                {
                    // Remove it so it can be replaced.
                    globalSettings.RemoveNode(botSettings);
                }

                // Create a new BOTS node to write to.
                botSettings = globalSettings.AddNode("BOTS");

                // For each user in the BotsToRemove list,
                foreach (string bot in BotsToRemove)
                {
                    // Create a new BOT node and assign the bot name to it.
                    ConfigNode botNode = botSettings.AddNode("BOT");
                    botNode.AddValue("name", bot);
                }

                // Save the file.
                globalSettings.Save(settingsLocation + "GlobalSettings.cfg");
            }
            // If the file doesn't exist,
            else
            {
                // Log a warning that it wasn't found.
                Logger.DebugWarning("(During save) GlobalSettings.cfg wasn't found. Generating to save global settings.");

                // Create a new root node.
                ConfigNode root = new ConfigNode();

                // Create a new DRAFT node to write the general settings to.
                ConfigNode draftSettings = root.AddNode("DRAFT");

                draftSettings.AddValue("channel", Channel);
                draftSettings.AddValue("addKerman", AddKerman.ToString());

                // Create a new BOTS node to write to.
                ConfigNode botSettings = root.AddNode("BOTS");

                // For each user in the BotsToRemove list,
                foreach (string bot in BotsToRemove)
                {
                    // Create a new BOT node and assign the bot name to it.
                    ConfigNode botNode = botSettings.AddNode("BOT");
                    botNode.AddValue("name", bot);
                }

                // Save the file.
                root.Save(settingsLocation + "GlobalSettings.cfg");
            }
        }

        /// <summary>
        /// Saves the list of drawn users.
        /// </summary>
        public void SaveDrawn()
        {
            // Load global settings.
            ConfigNode globalSettings = ConfigNode.Load(settingsLocation + "GlobalSettings.cfg");
            // If the file exists,
            if (globalSettings != null)
            {
                // Get the list of users which have already won a drawing.
                ConfigNode drawnSettings = globalSettings.GetNode("DRAWN");

                // If the DRAWN node exists,
                if (drawnSettings != null)
                {
                    // Remove it so it can be replaced.
                    globalSettings.RemoveNode(drawnSettings);
                }

                // Create a new DRAWN node to write to.
                drawnSettings = globalSettings.AddNode("DRAWN");

                // For each user in the DrawnUsers list,
                foreach (string drawn in DrawnUsers)
                {
                    // Create a new USER node and assign the username to it.
                    ConfigNode drawnNode = drawnSettings.AddNode("USER");
                    drawnNode.AddValue("name", drawn);
                }

                // Save the file.
                globalSettings.Save(settingsLocation + "GlobalSettings.cfg");
            }
            // If the file doesn't exist,
            else
            {
                // Log a warning that it wasn't found.
                Logger.DebugWarning("(During save) GlobalSettings.cfg wasn't found. Generating to save drawn users.");

                // Create a new root node.
                ConfigNode root = new ConfigNode();
                // Create a new DRAWN node to write to.
                ConfigNode drawnSettings = root.AddNode("DRAWN");

                // For each user in the DrawnUsers list,
                foreach (string drawn in DrawnUsers)
                {
                    // Create a new USER node and assign the username to it.
                    ConfigNode drawnNode = drawnSettings.AddNode("USER");
                    drawnNode.AddValue("name", drawn);
                }

                // Save the file.
                root.Save(settingsLocation + "GlobalSettings.cfg");
            }
        }

        /// <summary>
        /// Saves the list of drafted users.
        /// </summary>
        private void SaveDrafted()
        {
            // Load local settings.
            ConfigNode localSettings = ConfigNode.Load(saveLocation + "DTVLocalSettings.cfg");
            // If the file exists,
            if (localSettings != null)
            {
                // Get the DRAFTED node.
                ConfigNode draftedUsers = localSettings.GetNode("DRAFTED");

                // If the DRAFTED node exists,
                if (draftedUsers != null)
                {
                    // Remove it so it can be replaced.
                    localSettings.RemoveNode(draftedUsers);
                }

                // Create a new DRAFTED node to write to.
                draftedUsers = localSettings.AddNode("DRAFTED");

                // For each user in the AlreadyDrafted list,
                foreach (string drafted in AlreadyDrafted)
                {
                    // Create a new USER node and assign the username to it.
                    ConfigNode draftedNode = draftedUsers.AddNode("USER");
                    draftedNode.AddValue("name", drafted);
                }

                // Save the file.
                localSettings.Save(saveLocation + "DTVLocalSettings.cfg");
            }
            // If the file doesn't exist,
            else
            {
                // Log a warning that it wasn't found.
                Logger.DebugWarning("(During save) DTVLocalSettings.cfg wasn't found. Generating to save drafted users.");

                // Create a new root node.
                ConfigNode root = new ConfigNode();
                // Create a new DRAFTED node to write to.
                ConfigNode draftedUsers = root.AddNode("DRAFTED");

                // For each user in the AlreadyDrafted list,
                foreach (string drafted in AlreadyDrafted)
                {
                    // Create a new USER node and assign the username to it.
                    ConfigNode userNode = draftedUsers.AddNode("USER");
                    userNode.AddValue("name", drafted);
                }

                // Save the file.
                root.Save(saveLocation + "DTVLocalSettings.cfg");
            }
        }

        #endregion

        #region Local Settings Handlers

        /// <summary>
        /// Clears local settings.
        /// </summary>
        /// <param name="data">The GameScene being requested.</param>
        private void ClearLocalSettings(GameScenes data)
        {
            // If the GameScene is the Main Menu,
            if (data == GameScenes.MAINMENU)
            {
                // The game is exiting to the main menu, discarding data local to any one save, so DTV should do the same.

                // Log clearing.
                Logger.DebugLog("Clearing Local Settings.");

                // Clear the save location.
                saveLocation = "";

                // Clear Draft List.
                AlreadyDrafted = new List<string>();

                // Set last known scene to Main Menu.
                lastKnownScene = GameScenes.MAINMENU;
            }
        }

        /// <summary>
        /// Loads local settings.
        /// </summary>
        public void LoadLocalSettings()
        {
            // If the last known GameScene is the Main Menu,
            if (lastKnownScene == GameScenes.MAINMENU)
            {
                // The game has loaded into a game save, so it is safe to load this save's local settings.

                // Set the save location.
                saveLocation = "saves/" + HighLogic.CurrentGame.Title.Substring(0, HighLogic.CurrentGame.Title.LastIndexOf(' ')) + "/";

                // Load local settings.
                ConfigNode localSettings = ConfigNode.Load(saveLocation + "DTVLocalSettings.cfg");
                // If the file exists,
                if (localSettings != null)
                {
                    // Used to save if any corrupt nodes were found.
                    bool doSave = false;

                    #region Drafted Users Load
                    // Get the DRAFTED node.
                    ConfigNode draftedUsers = localSettings.GetNode("DRAFTED");

                    // If the DRAFTED node exists,
                    if (draftedUsers != null)
                    {
                        // Get a list of USER nodes.
                        ConfigNode[] userNodes = draftedUsers.GetNodes("USER");

                        // Iterate through,
                        foreach (ConfigNode c in userNodes)
                        {
                            // If the node has a name value,
                            if (c.HasValue("name"))
                            {
                                // Add the string name to the list of users already drafted.
                                AlreadyDrafted.Add(c.GetValue("name"));
                            }
                            // If the node doesn't have a name value,
                            else
                            {
                                // Log a warning that this node is corrupt and is being removed.
                                Logger.DebugWarning("Corrupt BOT node. Removing.");
                                // Remove the corrupt node.
                                draftedUsers.RemoveNode(c);
                                // Set doSave to true so the corrupt nodes remain gone.
                                doSave = true;
                            }
                        }
                    }
                    // If the DRAFTED node doesn't exist,
                    else
                    {
                        // Log a warning that is wasn't found.
                        Logger.DebugWarning("DTVLocalSettings.cfg WAS found, but the DRAFTED node was not. Using empty list.");
                    }
                    #endregion

                    // If corrupt nodes were found and removed,
                    if (doSave)
                    {
                        // Save the settings file so they remain gone.
                        localSettings.Save(saveLocation + "DTVLocalSettings.cfg");
                    }
                }
                // If the file doesn't exist,
                else
                {
                    // Log a warning that it wasn't found.
                    Logger.DebugWarning("DTVLocalSettings.cfg wasn't found. Using defaults.");
                }

                // Set last known scene to Space Center.
                lastKnownScene = GameScenes.SPACECENTER;
            }
        }

        #endregion

        #region Misc Functions

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
                return new string[] { };
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

        /// <summary>
        /// A list of regex strings.
        /// </summary>
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

        /// <summary>
        /// A list of regexes which will be used to test drafted names.
        /// </summary>
        Regex[] regexes;

        /// <summary>
        /// Initializes the array of regexes.
        /// </summary>
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

    #region Space Center Alerter

    /// <summary>
    /// Designed to alert the DraftManager once the Space Center scene is already loaded. Destroys itself when finished. - blast awesomeness: 0.0
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class SpaceCenterAlerter : MonoBehaviour
    {
        void Awake()
        {
            DraftManager.Instance.LoadLocalSettings();
            Destroy(gameObject);
        }
    }

    #endregion
}
