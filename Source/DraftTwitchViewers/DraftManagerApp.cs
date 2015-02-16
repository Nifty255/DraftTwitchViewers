using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DraftTwitchViewers
{
    /// <summary>
    /// The Draft Manager App. This app is used to connect to twitch and draft users into the game as Kerbals.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
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
        private float windowWidth = 400f;
        /// <summary>
        /// The height of the window.
        /// </summary>
        private float windowHeight = 400f;
        /// <summary>
        /// Is the draft failure alert showing?
        /// </summary>
        private bool alertShowing = false;
        /// <summary>
        /// The message the alert is showing for.
        /// </summary>
        private string alertingMsg = "";

        /// <summary>
        /// The twitch username.
        /// </summary>
        private string username = "";
        /// <summary>
        /// The twitch password (an API key).
        /// </summary>
        private string password = "";
        /// <summary>
        /// The twitch channel.
        /// </summary>
        private string channel = "";
        /// <summary>
        /// The message used when a draft succeeds.
        /// </summary>
        private string draftMessage = "&user has been drafted!";
        /// <summary>
        /// The massage used when a user is already drafted.
        /// </summary>
        private string thereMessage = "&user has already been drafted.";
        /// <summary>
        /// The message used when the crew limit is reached.
        /// </summary>
        private string cantMessage = "&user can't be drafted. Crew limit reached!";
        /// <summary>
        /// Remember the username, password, and channel?
        /// </summary>
        private bool remember = false;

        /// <summary>
        /// The settings save location.
        /// </summary>
        private string saveLocation = "GameData/DraftTwitchViewers/";

        /// <summary>
        /// The TCP client used to connect to twitch.
        /// </summary>
        private TcpClient twitchClient;
        /// <summary>
        /// The network stream.
        /// </summary>
        private NetworkStream twitchStream;
        /// <summary>
        /// The stream reader.
        /// </summary>
        private StreamReader twitchReader;
        /// <summary>
        /// The stream writer
        /// </summary>
        private StreamWriter twitchWriter;
        /// <summary>
        /// Is the client connected?
        /// </summary>
        private bool connected = false;
        /// <summary>
        /// Is the client connecting?
        /// </summary>
        private bool connecting = false;
        /// <summary>
        /// The network listener thread.
        /// </summary>
        private Thread listen;

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
            GameEvents.onGameSceneLoadRequested.Add((e) => { if (e == GameScenes.MAINMENU) { if (connected || connecting) { Disconnect(); } instance = null; Destroy(gameObject); } });

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
                    if (userSettings.HasValue("username")) { username = userSettings.GetValue("username"); }
                    if (userSettings.HasValue("password")) { password = userSettings.GetValue("password"); }
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
            alertRect = new Rect(Screen.width / 2 - windowWidth / 4, Screen.height / 2 - windowHeight / 4, windowWidth / 2, windowHeight / 2);

            // Add a level-loaded event which will reposition the app.
            GameEvents.onLevelWasLoaded.Add((e) => { Reposition(); });
        }

        #endregion

        #region App Functions

        /// <summary>
        /// Displays the app when the player clicks.
        /// </summary>
        private void DisplayApp()
        {
            Reposition();
            isShowing = true;
        }

        /// <summary>
        /// Displays the app while the player hovers.
        /// </summary>
        private void HoverApp()
        {
            Reposition();
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
            windowRect = new Rect(Mathf.Min(anchor + 1210.5f - windowWidth, 1920f - windowWidth), 40f, windowWidth, windowHeight);
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
                GUILayout.Window(GetInstanceID() + 1, alertRect, AlertWindow, "Draft Failed", HighLogic.Skin.window);
            }
        }

        /// <summary>
        /// Draws the app window.
        /// </summary>
        /// <param name="windowID">The windiw ID.</param>
        private void AppWindow(int windowID)
        {
            GUILayout.BeginVertical(HighLogic.Skin.box);

            // Connected
            if (connected)
            {
                // Draft a twitch viewer as a crewmember
                if (GUILayout.Button("Draft a Viewer into Crew", HighLogic.Skin.button))
                {
                    StartCoroutine(DraftIntoKrew());
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

                    // Bots to remove
                    GUILayout.Label("", HighLogic.Skin.label);
                    GUILayout.Label("To edit the bots to remove from the draft list, you must first disconnect.", HighLogic.Skin.label);


                    // Save
                    if (GUILayout.Button("Save", HighLogic.Skin.button))
                    {
                        SaveMessages();
                    }
                }

                // Disconnect
                GUILayout.Label("", HighLogic.Skin.label);
                if (GUILayout.Button("Disconnect", HighLogic.Skin.button))
                {
                    Disconnect();
                }
            }

            // Connecting
            else if (connecting)
            {
                GUILayout.Label("Connecting...", HighLogic.Skin.label);
            }

            // Disconnected
            else
            {
                // Heading
                GUILayout.Label("Be sure you verify these values. The mod isn't going to check for validity and might even crash!", HighLogic.Skin.label);
                GUILayout.Label("", HighLogic.Skin.label);

                // Username
                GUILayout.Label("Username (From twitch):", HighLogic.Skin.label);
                username = GUILayout.TextField(username, HighLogic.Skin.textField);

                // Password
                GUILayout.Label("Password (API Key):", HighLogic.Skin.label);
                password = GUILayout.PasswordField(password, '•', HighLogic.Skin.textField);

                // Channel
                GUILayout.Label("Channel (Lowercase):", HighLogic.Skin.label);
                channel = GUILayout.TextField(channel, HighLogic.Skin.textField);

                // Remember
                remember = GUILayout.Toggle(remember, "Remember Me", HighLogic.Skin.toggle);

                // Connect
                if (GUILayout.Button("Connect", HighLogic.Skin.button))
                {
                    SaveUser();

                    Connect();
                }

                // Get API Key
                GUILayout.Label("", HighLogic.Skin.label);
                GUILayout.Label("If you don't have an API Key yet, log into your bot account on twitch, and then click the button below:", HighLogic.Skin.label);
                if (GUILayout.Button("Get API Key", HighLogic.Skin.button))
                {
                    Application.OpenURL("http://www.twitchapps.com/tmi");
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
            }

            GUILayout.EndVertical();
        }

        #endregion

        #region IRC Functions

        /// <summary>
        /// Connects to the twitch server and joins the specified channel.
        /// </summary>
        private void Connect()
        {
            // Indicate it is connecting.
            connecting = true;

            // (Re)instantiate user list.
            usersInChat = new List<string>();

            //Opens connection to the twitch IRC
            twitchClient = new TcpClient("irc.twitch.tv", 6667);
            twitchStream = twitchClient.GetStream();
            twitchReader = new StreamReader(twitchStream, Encoding.GetEncoding("iso_8859_1"));
            twitchWriter = new StreamWriter(twitchStream, Encoding.GetEncoding("iso_8859_1"));

            //Starts a thread that reads all data from IRC
            listen = new Thread(Listen);
            listen.IsBackground = true;
            listen.Start();

            // Logs into the twitch IRC.
            SendTwitchMessage("PASS " + password);
            SendTwitchMessage("NICK " + username);
            
            // Joins the specified channel.
            SendTwitchMessage("JOIN #" + channel);

            // Broadcasts online status to the chat.
            SendTwitchMessage("PRIVMSG #" + channel + " :" + username + " online.");
        }

        /// <summary>
        /// Disconnects to the twitch server after leaving the channel.
        /// </summary>
        private void Disconnect()
        {
            // Broacasts offline status to the chat.
            SendTwitchMessage("PRIVMSG #" + channel + " :" + username + " offline.");

            // Leaves the channel.
            SendTwitchMessage("PART #" + channel);

            // Close the connection to twitch.
            twitchClient.Close();

            // Indicate it is not connected.
            connected = false;

            // Stop the listening thread.
            listen.Abort();
        }

        /// <summary>
        /// Listens to the twitch server and processes the messages.
        /// </summary>
        private void Listen()
        {
            // The messages received.
            string received = "";

            // Detect exceptions.
            try
            {
                // While there are messages.
                while ((received = twitchReader.ReadLine()) != null)
                {
                    // The strings used to sort through different types of messages.

                    // The user listing prefix
                    string userListPrefix = ":" + username.ToLower() + ".tmi.twitch.tv 353 " + username.ToLower() + " = #" + channel + " :";
                    // The end user list string.
                    string userListEnd = ":" + username.ToLower() + ".tmi.twitch.tv 366 " + username.ToLower() + " #" + channel + " :End of /NAMES list";
                    // The user joined postfix.
                    string joinPostfix = ".tmi.twitch.tv JOIN #" + channel;
                    // The user left postfix.
                    string partPostfix = ".tmi.twitch.tv PART #" + channel;
                    // The private message midfix.
                    string pvMsgMidfix = ".tmi.twitch.tv PRIVMSG #" + channel + " :";


                    //Respond PINGs with PONGs and the same message
                    if (received.IndexOf("PING") == 0)
                    {
                        string pingMsg = received.Substring("PING ".Length);
                        SendMessage("PONG " + pingMsg);
                    }

                    // Listen for user list.
                    else if (received.Contains(userListPrefix))
                    {
                        // Get user list from message.
                        string[] usersShown = received.Substring(userListPrefix.Length).Split(' ');

                        // Add users to list.
                        usersInChat.AddRange(usersShown);

                        // Remove known bots
                        usersInChat.Remove(username.ToLower());
                        foreach(string bot in botsToRemove)
                        {
                            usersInChat.Remove(bot);
                        }
                    }

                    // Listen for end of user list.
                    else if (received.Contains(userListEnd))
                    {
                        // The user list is finished. The client is completely connected.
                        connected = true;
                        connecting = false;
                    }

                    // Listen for joins.
                    else if (received.Contains(joinPostfix))
                    {
                        // Get the user from the message.
                        string userToAdd = received.Substring(1, received.IndexOf('!') - 1);

                        // If the bot list doesn't contain the user,
                        if (!botsToRemove.Contains(userToAdd))
                        {
                            // Add the user to the chat list.
                            usersInChat.Add(userToAdd);
                        }
                    }

                    // Listen for parts.
                    else if (received.Contains(partPostfix))
                    {
                        // Get the user from the message and remove.
                        string userToRemove = received.Substring(1, received.IndexOf('!') - 1);
                        usersInChat.Remove(userToRemove);
                    }

                    // Listen for private messages.
                    else if (received.Contains(pvMsgMidfix))
                    {
                        // Parse the message to get the user who sent it, and the message.
                        string userWithMsg = received.Substring(1, received.IndexOf('!') - 1);
                        string takeout = ":" + userWithMsg + "!" + userWithMsg + "@" + userWithMsg + pvMsgMidfix;
                        string message = received.Substring(takeout.Length);

                        // Currently no use for this.
                    }
                }
            }
            catch (Exception e)
            {
                // If connected, disconnect.
                // If not connected, chances are ThreadAbortException was thrown and an additional Abort isn't needed.
                if (connected)
                {
                    // Indicate disconnection, abort the listener, and close the connection..
                    connected = false;
                    listen.Abort();
                    twitchClient.Close();
                }
            }
        }

        /// <summary>
        /// Sends a twitch message. This does not wrap the message in twitch syntax and must be added prior to calling.
        /// </summary>
        /// <param name="message">Message to send.</param>
        private void SendTwitchMessage(string message)
        {
            // Write to the stream writer.
            twitchWriter.WriteLine(message);
            twitchWriter.Flush();
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
            // Gets a random user from the list.
            string userDrafted = usersInChat[UnityEngine.Random.Range(0, usersInChat.Count)];

            // Creates a new Unity web request (WWW) using the user chosen.
            WWW www = new WWW("https://api.twitch.tv/kraken/users/" + userDrafted);

            // Waits for the web request to finish.
            yield return www;

            // Parses the real username of the chosen user.
            string realUsername = www.text.Substring(www.text.IndexOf("\"display_name\""));
            realUsername = realUsername.Substring(realUsername.IndexOf(":") + 2);
            realUsername = realUsername.Substring(0, realUsername.IndexOf(",") - 1);

            // Gets the roster system.
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            // Checks for the prior presence of the chosen user.
            foreach (ProtoCrewMember p in roster.Crew)
            {
                // If the user is present in the roster,
                if (p.name == realUsername + " Kerman")
                {
                    // The user is present. No need to add again.

                    // Send alert to twitch.
                    SendTwitchMessage("PRIVMSG #" + channel + " :" + thereMessage.Replace("&user", realUsername));

                    // Alert in-game.
                    alertingMsg = thereMessage.Replace("&user", realUsername);
                    alertShowing = true;

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
                    // The rister is full. Can't add another.

                    // Send alert to twitch.
                    SendTwitchMessage("PRIVMSG #" + channel + " :" + cantMessage.Replace("&user", realUsername));

                    // Alert in-game.
                    alertingMsg = cantMessage.Replace("&user", realUsername);
                    alertShowing = true;

                    // Return (for coroutines).
                    yield break;
                }
            }

            // All checks have passed.

            // Create a new Kerbal and rename.
            ProtoCrewMember newKerbal = roster.GetNewKerbal();
            newKerbal.name = realUsername + " Kerman";

            // If the game mode is not Career, set the skill level to maximum possible.
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
            {
                newKerbal.experienceLevel = 5;
                newKerbal.experience = 9999;
            }

            // Send draft message to twitch.
            SendTwitchMessage("PRIVMSG #" + channel + " :" + draftMessage.Replace("&user", realUsername));

            // Alert in-game.
            alertingMsg = draftMessage.Replace("&user", realUsername);
            alertShowing = true;
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
                settings.AddValue("username", username);
                settings.AddValue("password", password);
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
    }
}