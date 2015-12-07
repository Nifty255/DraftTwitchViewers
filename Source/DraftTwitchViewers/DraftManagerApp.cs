using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        #region UI Management

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
        private float windowWidth = 350;
        /// <summary>
        /// The height of the window.
        /// </summary>
        private float windowHeight = 250f;
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

        #endregion

        #region Audio

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

        #endregion

        #region Settings

        /// <summary>
        /// Add the kerbal to the current craft when drafted?
        /// </summary>
        private bool addToCraft = false;
        /// <summary>
        /// The message used when a draft succeeds.
        /// </summary>
        private string draftMessage = "&user has been drafted as a &skill!";
        /// <summary>
        /// The message used when a user is pulled in a drawing.
        /// </summary>
        private string drawMessage = "&user has won the drawing!";

        #endregion

        #region Misc Variables

        /// <summary>
        /// The settings save location.
        /// </summary>
        private string settingsLocation = "GameData/DraftTwitchViewers/";

        #endregion

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

            SoundManager.LoadSound("DraftTwitchViewers/Sounds/Start", "Start");
            SoundManager.LoadSound("DraftTwitchViewers/Sounds/Success", "Success");
            SoundManager.LoadSound("DraftTwitchViewers/Sounds/Failure", "Failure");
            startClip = SoundManager.CreateSound("Start", false);
            successClip = SoundManager.CreateSound("Success", false);
            failureClip = SoundManager.CreateSound("Failure", false);

            // Load global settings.
            ConfigNode globalSettings = ConfigNode.Load(settingsLocation + "GlobalSettings.cfg");
            // If the file exists,
            if (globalSettings != null)
            {
                #region Draft Settings Load
                // Get the DRAFT node.
                ConfigNode draftSettings = globalSettings.GetNode("DRAFT");

                // If the DRAFT node exists,
                if (draftSettings != null)
                {
                    // Get the global settings.
                    if (draftSettings.HasValue("addToCraft")) { try { addToCraft = bool.Parse(draftSettings.GetValue("addToCraft")); } catch { } }
                }
                // If the DRAFT node doesn't exist,
                else
                {
                    // Log a warning that is wasn't found.
                    Logger.DebugWarning("GlobalSettings.cfg WAS found, but the DRAFT node was not. Using defaults.");
                }
                #endregion

                #region Message Settings Load
                // Get the MESSAGES node.
                ConfigNode messageSettings = globalSettings.GetNode("MESSAGES");

                // If the MESSAGES node exists,
                if (messageSettings != null)
                {
                    // Get the global settings.
                    if (messageSettings.HasValue("draftMessage")) { draftMessage = messageSettings.GetValue("draftMessage"); }
                    if (messageSettings.HasValue("drawMessage")) { drawMessage = messageSettings.GetValue("drawMessage"); }
                }
                // If the DRAFT node doesn't exist,
                else
                {
                    // Log a warning that is wasn't found.
                    Logger.DebugWarning("GlobalSettings.cfg WAS found, but the MESSAGES node was not. Using defaults.");
                }
                #endregion
            }
            // If the file doesn't exist,
            else
            {
                // Log a warning that it wasn't found.
                Logger.DebugWarning("GlobalSettings.cfg wasn't found. Using defaults.");
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

            // Set up app destroyer.
            GameEvents.onGameSceneLoadRequested.Add(DestroyApp);
            Logger.DebugLog("DTV App Created.");
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
                DraftManager.Instance.Channel = DraftManager.Instance.Channel.ToLower();

                // Perform the draft.
                DoDraft(false);

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
        /// Destroys the app.
        /// </summary>
        private void DestroyApp(GameScenes data)
        {
            if (data == GameScenes.MAINMENU)
            {
                GameEvents.onGameSceneLoadRequested.Remove(DestroyApp);
                ApplicationLauncher.Instance.RemoveModApplication(draftManagerButton);
                instance = null;
                Destroy(gameObject);
                Logger.DebugLog("DTV App Destroyed.");
            }
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
                GUILayout.Window(GetInstanceID() + 1, alertRect, AlertWindow, "DTV Alert: " + (failedToDraft ? "Failed!" : (draftBusy ? "Working..." : "Success!")), HighLogic.Skin.window);
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
            DraftManager.Instance.Channel = GUILayout.TextField(DraftManager.Instance.Channel, HighLogic.Skin.textField);

            //Spacer Label
            GUILayout.Label("", HighLogic.Skin.label);

            // If career, display the cost of next draft.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                GUILayout.Label("Next Draft: -" + (GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount() + 1)).ToString("N0") + " Funds", HighLogic.Skin.label);
            }

            // Draft a Viewer from Twitch, skipping viewers who aren't Pilots.
            if (GUILayout.Button("Draft a Pilot", HighLogic.Skin.button))
            {
                // Lowercase the channel.
                DraftManager.Instance.Channel = DraftManager.Instance.Channel.ToLower();

                // Perform the draft.
                DoDraft(false, "Pilot");
            }

            // Draft a Viewer from Twitch, skipping viewers who aren't Engineers.
            if (GUILayout.Button("Draft an Engineer", HighLogic.Skin.button))
            {
                // Lowercase the channel.
                DraftManager.Instance.Channel = DraftManager.Instance.Channel.ToLower();

                // Perform the draft.
                DoDraft(false, "Engineer");
            }

            // Draft a Viewer from Twitch, skipping viewers who aren't Scientists.
            if (GUILayout.Button("Draft a Scientist", HighLogic.Skin.button))
            {
                // Lowercase the channel.
                DraftManager.Instance.Channel = DraftManager.Instance.Channel.ToLower();

                // Perform the draft.
                DoDraft(false, "Scientist");
            }

            // Draft a Viewer from Twitch, with any job.
            if (GUILayout.Button("Draft Any Viewer", HighLogic.Skin.button))
            {
                // Lowercase the channel.
                DraftManager.Instance.Channel = DraftManager.Instance.Channel.ToLower();

                // Perform the draft.
                DoDraft(false);
            }

            //Spacer Label
            GUILayout.Label("", HighLogic.Skin.label);

            // Pull a name for a drawing
            if (GUILayout.Button("Do a Viewer Drawing", HighLogic.Skin.button))
            {
                // Lowercase the channel.
                DraftManager.Instance.Channel = DraftManager.Instance.Channel.ToLower();

                // Perform the draft.
                DoDraft(true);
            }

            GUI.enabled = (DraftManager.Instance.DrawnUsers.Count > 0);

            // Reset drawing list
            if (GUILayout.Button((DraftManager.Instance.DrawnUsers.Count == 0 ? "Drawn User List Empty!" : "Empty Drawn User List"), HighLogic.Skin.button))
            {
                // Empty the list.
                DraftManager.Instance.DrawnUsers = new List<string>();
                // Save the list.
                DraftManager.Instance.SaveDrawn();
            }

            GUI.enabled = true;

            //Spacer Label
            GUILayout.Label("", HighLogic.Skin.label);

            // Customize
            if (GUILayout.Button("Customize", HighLogic.Skin.button))
            {
                isCustomizing = !isCustomizing;
            }
            if (isCustomizing)
            {
                // Add "Kerman" toggle.
                DraftManager.Instance.AddKerman = GUILayout.Toggle(DraftManager.Instance.AddKerman, "Add \"Kerman\" to names", HighLogic.Skin.toggle);

                // Add drafted to craft toggle.
                addToCraft = GUILayout.Toggle(addToCraft, "Add drafted Kerbals to craft (Preflight Only)", HighLogic.Skin.toggle);

                // On successful draft.
                GUILayout.Label("Successful Draft:", HighLogic.Skin.label);
                draftMessage = GUILayout.TextField(draftMessage, HighLogic.Skin.textField);

                // On successful draw.
                GUILayout.Label("Successful Drawing:", HighLogic.Skin.label);
                drawMessage = GUILayout.TextField(drawMessage, HighLogic.Skin.textField);

                // $user Explanation
                GUILayout.Label("", HighLogic.Skin.label);
                GUILayout.Label("\"&user\" = The user drafted.", HighLogic.Skin.label);
                GUILayout.Label("\"&skill\" = The user's skill.", HighLogic.Skin.label);

                // Bots to remove
                GUILayout.Label("", HighLogic.Skin.label);
                GUILayout.Label("Bots to Remove (One per line, no commas):", HighLogic.Skin.label);
                string botsString = string.Join("\n", DraftManager.Instance.BotsToRemove.ToArray());
                botsString = GUILayout.TextArea(botsString, HighLogic.Skin.textArea);
                DraftManager.Instance.BotsToRemove = new List<string>();
                if (botsString != "") { DraftManager.Instance.BotsToRemove.AddRange(botsString.Split('\n')); }

                // Save
                if (GUILayout.Button("Save", HighLogic.Skin.button))
                {
                    SaveSettings();
                    DraftManager.Instance.SaveGlobalSettings();
                }
            }

            //Version Label
            GUILayout.Label("Version " + (typeof(DraftManagerApp).Assembly.GetName().Version.ToString()), HighLogic.Skin.label);

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
        /// Sets up for a draft.
        /// </summary>
        /// <param name="forDrawing">Whether this draft is for a plain drawing or an actual draft.</param>
        /// <param name="job">The job for the Kerbal. Optional and defaults to "Any" and is not needed if forDrawing is true.</param>
        private void DoDraft(bool forDrawing, string job = "Any")
        {
            SaveSettings();
            DraftManager.Instance.SaveGlobalSettings();

            // Shows the alert as working.
            alertShowing = true;
            draftBusy = true;
            startClip.Play();

            if (forDrawing)
            {
                StartCoroutine(DraftManager.DraftKerbal(DrawingSuccess, DraftFailure, forDrawing, false, job));
            }
            else
            {
                StartCoroutine(DraftManager.DraftKerbal(DraftSuccess, DraftFailure, forDrawing, false, job));
            }
        }

        /// <summary>
        /// Creates a new Kerbal based on the provided name.
        /// </summary>
        /// <param name="kerbalName">The name of the new Kerbal.</param>
        private void DraftSuccess(string kerbalName)
        {
            ProtoCrewMember newKerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
            newKerbal.name = kerbalName;
            KerbalRoster.SetExperienceTrait(newKerbal);

            // If the game is career, subtract the cost of hiring.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Funding.Instance.AddFunds(-(double)GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount()), TransactionReasons.CrewRecruited);
            }
            // If the game mode is not Career, set the skill level to maximum possible.
            else
            {
                newKerbal.experienceLevel = 5;
                newKerbal.experience = 9999;
            }

            // If the game is Preflight and the user wants to automatically add to the craft,
            if (IsPreflight && addToCraft)
            {
                // Generate a selection of parts available for adding.
                PartSelectionManager.Instance.GenerateSelection(newKerbal);

                // Remove the in-game alert but still play the success tone.
                alertingMsg = "";
                draftBusy = false;
                alertShowing = false;
                failedToDraft = false;
                if (startClip.isPlaying) { startClip.Stop(); }
                successClip.Play();
            }
            // Otherwise,
            else
            {
                // Alert in-game.
                alertingMsg = draftMessage.Replace("&user", kerbalName).Replace("&skill", newKerbal.experienceTrait.Title);
                draftBusy = false;
                failedToDraft = false;
                alertShowing = true;
                if (startClip.isPlaying) { startClip.Stop(); }
                successClip.Play();
            }
        }

        /// <summary>
        /// Displays the winner of the drawing.
        /// </summary>
        /// <param name="winner">The winner of the drawing.</param>
        private void DrawingSuccess(string winner)
        {
            // Alert in-game.
            alertingMsg = drawMessage.Replace("&user", winner);
            draftBusy = false;
            failedToDraft = false;
            alertShowing = true;
            if (startClip.isPlaying) { startClip.Stop(); }
            successClip.Play();
        }

        /// <summary>
        /// Indicates draft failure.
        /// </summary>
        /// <param name="reason">The reason for failure.</param>
        private void DraftFailure(string reason)
        {
            // Alert in-game.
            alertingMsg = reason;
            draftBusy = false;
            failedToDraft = true;
            alertShowing = true;
            if (startClip.isPlaying) { startClip.Stop(); }
            failureClip.Play();
        }

        /// <summary>
        /// Saves the settings.
        /// </summary>
        private void SaveSettings()
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

                // Write the addToCraft setting to it.
                draftSettings.AddValue("addToCraft", addToCraft.ToString());

                // Get the message settings node.
                ConfigNode messageSettings = globalSettings.GetNode("MESSAGES");

                // If the MESSAGES node doesn't exist,
                if (messageSettings == null)
                {
                    // Create a new MESSAGES node to write to.
                    messageSettings = globalSettings.AddNode("MESSAGES");
                }

                // Write the messages to it.
                if (messageSettings.HasValue("draftMessage")) { messageSettings.SetValue("draftMessage", draftMessage); } else { messageSettings.AddValue("draftMessage", draftMessage); }
                if (messageSettings.HasValue("drawMessage")) { messageSettings.SetValue("drawMessage", drawMessage); } else { messageSettings.AddValue("drawMessage", drawMessage); }
            }
            // If the file doesn't exist,
            else
            {
                // Log a warning that it wasn't found.
                Logger.DebugWarning("(During save) GlobalSettings.cfg wasn't found. Generating to save settings.");

                // Create a new root node.
                ConfigNode root = new ConfigNode();

                // Create a new DRAFT node to write the general settings to.
                ConfigNode draftSettings = root.AddNode("DRAFT");

                // Write the addToCraft setting to it.
                draftSettings.AddValue("addToCraft", addToCraft.ToString());

                // Create a new MESSAGES node.
                ConfigNode messageSettings = root.AddNode("MESSAGES");

                // Write the messages to it.
                messageSettings.AddValue("draftMessage", draftMessage);
                messageSettings.AddValue("drawMessage", drawMessage);

                // Save the file.
                root.Save(settingsLocation + "GlobalSettings.cfg");
            }
        }

        #endregion

        #region Misc Methods

        /// <summary>
        /// Determines if the game is currently in Preflight status (The loaded scene is Flight and the active vessel is on the LaunchPad or Runway).
        /// </summary>
        /// <returns>True if the game is currently in Preflight status.</returns>
        bool IsPreflight
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    return FlightGlobals.ActiveVessel.landedAt == "KSC_LaunchPad_Platform" || FlightGlobals.ActiveVessel.landedAt == "Runway";
                }

                return false;
            }
        }

        #endregion
    }
}