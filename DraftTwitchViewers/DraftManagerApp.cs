using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;

using ClickThroughFix;
using ToolbarControl_NS;

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
        //private ApplicationLauncherButton draftManagerButton;
        ToolbarControl toolbarControl;

        /// <summary>
        /// Is the game UI hidden?
        /// </summary>
        private bool isUIHidden = false;
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
        /// Use the hotkey to draft?
        /// </summary>
        private bool useHotkey = true;
        /// <summary>
        /// Skin selection
        /// </summary>
        private bool useKSPSkin = true;

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
        /// <summary>
        /// Has something changed that we need to save?
        /// </summary>
        private bool needSave = false;
        /// <summary>
        /// The current delay time for saving.
        /// </summary>
        private float currentSaveDelay = 1f;
        /// <summary>
        /// The max delay time for saving.
        /// </summary>
        private const float maxSaveDelay = 1f;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// UseHotkey property. Triggers autosave when changed.
        /// </summary>
        private bool UseHotkey
        {
            get { return useHotkey; }
            set { if (useHotkey != value) { useHotkey = value; SaveSettings(); } }
        }

        /// <summary>
        /// UseHotkey property. Triggers autosave when changed.
        /// </summary>
        private bool UseKSPSkin
        {
            get { return useKSPSkin; }
            set { if (useKSPSkin != value) { useKSPSkin = value; SaveSettings(); } }
        }

        /// <summary>
        /// AddToCraft property. Triggers autosave when changed.
        /// </summary>
        private bool AddToCraft
        {
            get { return addToCraft; }
            set { if (addToCraft != value) { addToCraft = value; SaveSettings(); } }
        }

        /// <summary>
        /// DraftMessage property. Triggers autosave (after delay) when changed.
        /// </summary>
        private string DraftMessage
        {
            get { return draftMessage; }
            set { if (draftMessage != value) { draftMessage = value; needSave = true; currentSaveDelay = 0f; } }
        }

        /// <summary>
        /// DrawMessage property. Triggers autosave (after delay) when changed.
        /// </summary>
        private string DrawMessage
        {
            get { return drawMessage; }
            set { if (drawMessage != value) { drawMessage = value; needSave = true; currentSaveDelay = 0f; } }
        }

        #endregion

        #region Unity Functions

        internal const string MODID = "DraftTwitchViewers_NS";
        internal const string MODNAME = "Draft Twitch Viewers";

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
#if false
            // Create the App Launcher button and add it.
            draftManagerButton = ApplicationLauncher.Instance.AddModApplication(
                       DisplayApp,
                       HideApp,
                       HoverApp,
                       UnhoverApp,
                       DummyVoid,
                       Disable,
                       ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT,
                       GameDatabase.Instance.GetTexture("DraftTwitchViewers/Textures/Toolbar", false));

            // This app should be mutually exclusive. (It should disappear when the player clicks on another app.
            ApplicationLauncher.Instance.EnableMutuallyExclusive(draftManagerButton);
#endif
            

            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(DisplayApp,
                       HideApp,
                       HoverApp,
                       UnhoverApp, DummyVoid,
                       Disable,
                ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT,
                MODID,
                "draftTwitchViewersButton",
                "DraftTwitchViewers/Textures/Toolbar-38",
                "DraftTwitchViewers/Textures/Toolbar-24",
                MODNAME
            );
            toolbarControl.AddLeftRightClickCallbacks(null, DoRightClick);


            // This app should be mutually exclusive. (It should disappear when the player clicks on another app.
            toolbarControl.EnableMutuallyExclusive();

            // Set up the window bounds.
            windowRect = new Rect(Screen.width - windowWidth, 40f, windowWidth, windowHeight);
            // Set up the alert bounds.
            alertRect = new Rect(Screen.width / 2 - windowWidth / 2, Screen.height / 2 - windowHeight / 4, windowWidth, 1f);

            // Set up app destroyer.
            GameEvents.onGameSceneLoadRequested.Add(DestroyApp);
            Logger.DebugLog("DTV App Created.");
        }

        /// <summary>
        /// Called when the MonoBehaviour is started.
        /// </summary>
        private void Start()
        {
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onHideUI.Add(OnHideUI);
        }

        /// <summary>
        /// Called when Unity updates.
        /// </summary>
        void Update()
        {
            if (UseHotkey && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyUp(KeyCode.Insert))
            {
                // Lowercase the channel.
                ScenarioDraftManager.Instance.channel = ScenarioDraftManager.Instance.channel.ToLower();

                // Perform the draft.
                DoDraft(false);
            }

            // Update the save delay if needed.
            if (currentSaveDelay < maxSaveDelay)
            {
                currentSaveDelay += Time.deltaTime;
            }
            // Save if the delay has been reached.
            else if (needSave)
            {
                SaveSettings();
                needSave = false;
            }
        }

        private void OnDestroy()
        {
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);
        }

#endregion

#region App Functions

        void DoRightClick()
        {
            // Lowercase the channel.
            ScenarioDraftManager.Instance.channel = ScenarioDraftManager.Instance.channel.ToLower();

            // Perform the draft.
            DoDraft(false);
        }

      
        /// <summary>
        /// Displays the app when the player clicks.
        /// </summary>
        private void DisplayApp()
        {
            isShowing = true;
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
            //// Gets the button's anchor in 3D space.
            //float anchor = draftManagerButton.GetAnchor().x;

            //// Adjusts the window bounds.
            //windowRect = new Rect(Mathf.Min(anchor + 1210.5f - (windowWidth * (isCustomizing ? 2 : 1)), Screen.width - (windowWidth * (isCustomizing ? 2 : 1))), 40f, (windowWidth * (isCustomizing ? 2 : 1)), windowHeight);

            // If the current scene is flight,
            if (HighLogic.LoadedSceneIsFlight)
            {
                // Set the window to the top right, offsetting for the size and launcher area.
                windowRect = new Rect(Screen.width - (windowWidth /* * (isCustomizing ? 2 : 1) */ ) - 42, 0f, (windowWidth /* * (isCustomizing ? 2 : 1) */ ), windowHeight);
            }
            // Else, if the current scene is the Space Center,
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                // Set the window to the bottom right, offsetting for the size and launcher area.
                windowRect = new Rect(Screen.width - (windowWidth /* * (isCustomizing ? 2 : 1) */ ), 42f, (windowWidth /* * (isCustomizing ? 2 : 1) */ ), windowHeight);
            }
        }

        /// <summary>
        /// Destroys the app.
        /// </summary>
        private void DestroyApp(GameScenes data)
        {
            if (data == GameScenes.MAINMENU)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);

#if false
                GameEvents.onGameSceneLoadRequested.Remove(DestroyApp);
                ApplicationLauncher.Instance.RemoveModApplication(draftManagerButton);
#endif
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

        GUISkin ActiveSkin;

        /// <summary>
        /// Called when Unity reaches the GUI phase.
        /// </summary>
        private void OnGUI()
        {
            if (UseKSPSkin)
                ActiveSkin = HighLogic.Skin;
            else
                ActiveSkin = GUI.skin;

            // If the app is showing ir hovered over,
            if ((isShowing || isHovering) && !isUIHidden)
            {
                // Display the window.
                ClickThruBlocker.GUILayoutWindow(GetInstanceID(), windowRect, AppWindow, "Draft Twitch Viewers", ActiveSkin.window);
            }

            // If the alert is showing,
            if (alertShowing && !isUIHidden)
            {
                // Display the window.
                ClickThruBlocker.GUILayoutWindow(GetInstanceID() + 1, alertRect, AlertWindow, "DTV Alert: " + (failedToDraft ? "Failed!" : (draftBusy ? "Working..." : "Success!")), ActiveSkin.window);
            }

            Reposition();
        }

        /// <summary>
        /// Draws the app window.
        /// </summary>
        /// <param name="windowID">The windiw ID.</param>
        private void AppWindow(int windowID)
        {
            if (ScenarioDraftManager.Instance == null)
                return;

          
            if (GUI.Button(new Rect(windowRect.width - 20, 2, 18, 18), "x"))

            {
                toolbarControl.SetFalse(true);
                return;
            }

            GUILayout.BeginVertical(ActiveSkin.box);

            // Show draft shortcut (Alt+D)
            GUILayout.Label("Quick Draft: Alt+Insert (Toggle " + (UseHotkey ? "off" : "on") + " in Customize)", ActiveSkin.label);
            GUILayout.Label("", ActiveSkin.label);
        

            if (ScenarioDraftManager.Instance == null)
                Log.Info("ScenarioDraftManager.Instance is null");
            if (ScenarioDraftManager.Instance.channel == null)
                Log.Info("ScenarioDraftManager.Instance.channel is null");
            // Channel
            GUILayout.Label("Channel (Lowercase):", ActiveSkin.label);
            ScenarioDraftManager.Instance.channel = GUILayout.TextField(ScenarioDraftManager.Instance.channel, ActiveSkin.textField);

            //Spacer Label
            GUILayout.Label("", ActiveSkin.label);
    

            // If career, display the cost of next draft.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                GUILayout.Label("Next Draft: -" + (GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount())).ToString("N0") + " Funds", ActiveSkin.label);
            }

            // Draft a Viewer from Twitch, skipping viewers who aren't Pilots.
            if (GUILayout.Button("Draft a Pilot", ActiveSkin.button))
            {
                // Lowercase the channel.
                ScenarioDraftManager.Instance.channel = ScenarioDraftManager.Instance.channel.ToLower();

                // Perform the draft.
                DoDraft(false, "Pilot");
            }

            // Draft a Viewer from Twitch, skipping viewers who aren't Engineers.
            if (GUILayout.Button("Draft an Engineer", ActiveSkin.button))
            {
                // Lowercase the channel.
                ScenarioDraftManager.Instance.channel = ScenarioDraftManager.Instance.channel.ToLower();

                // Perform the draft.
                DoDraft(false, "Engineer");
            }

            // Draft a Viewer from Twitch, skipping viewers who aren't Scientists.
            if (GUILayout.Button("Draft a Scientist", ActiveSkin.button))
            {
                // Lowercase the channel.
                ScenarioDraftManager.Instance.channel = ScenarioDraftManager.Instance.channel.ToLower();

                // Perform the draft.
                DoDraft(false, "Scientist");
            }

            // Draft a Viewer from Twitch, with any job.
            if (GUILayout.Button("Draft Any Viewer", ActiveSkin.button))
            {
                // Lowercase the channel.
                ScenarioDraftManager.Instance.channel = ScenarioDraftManager.Instance.channel.ToLower();

                // Perform the draft.
                DoDraft(false);
            }

            //Spacer Label
            GUILayout.Label("", ActiveSkin.label);

            // Pull a name for a drawing
            if (GUILayout.Button("Do a Viewer Drawing", ActiveSkin.button))
            {
                // Lowercase the channel.
                ScenarioDraftManager.Instance.channel = ScenarioDraftManager.Instance.channel.ToLower();

                // Perform the draft.
                DoDraft(true);
            }
  
            GUI.enabled = (ScenarioDraftManager.Instance.DrawnUsers.Count > 0);
   
            // Reset drawing list
            if (GUILayout.Button((ScenarioDraftManager.Instance.DrawnUsers.Count == 0 ? "Drawn User List Empty!" : "Empty Drawn User List"), ActiveSkin.button))
            {
                // Empty the list.
                ScenarioDraftManager.Instance.DrawnUsers = new List<string>();
                // Save the list.
                ScenarioDraftManager.Instance.SaveDrawn();
            }
#if false
            GUI.enabled = (ScenarioDraftManager.Instance.AlreadyDrafted.Count > 0);
            // Reset drafting list
            if (GUILayout.Button((ScenarioDraftManager.Instance.AlreadyDrafted.Count == 0 ? "Drawn User List Empty!" : "Empty Drafted User List"), ActiveSkin.button))
            {
                // Empty the list.
                ScenarioDraftManager.Instance.AlreadyDrafted = new List<string>();
                // Save the list.
                //ScenarioDraftManager.Instance.SaveDrafted();
            }
#endif
            GUI.enabled = true;

            //Spacer Label
            GUILayout.Label("", ActiveSkin.label);

            // Customize
            if (GUILayout.Button("Customize", ActiveSkin.button))
            {
                isCustomizing = !isCustomizing;
            }
            if (isCustomizing)
            {
                // Use hotkey toggle.
                UseHotkey = GUILayout.Toggle(UseHotkey, "Quick Draft Hotkey", ActiveSkin.toggle);

                // Use UseKSPSkin toggle.
                UseKSPSkin = GUILayout.Toggle(UseKSPSkin, "Use KSP Skin", ActiveSkin.toggle);

                // Add drafted to craft toggle.
                AddToCraft = GUILayout.Toggle(AddToCraft, "Add drafted Kerbals to craft (Preflight Only)", ActiveSkin.toggle);

                // Add "Kerman" toggle.
                ScenarioDraftManager.Instance.AddKerman = GUILayout.Toggle(ScenarioDraftManager.Instance.AddKerman, "Add \"Kerman\" to names", ActiveSkin.toggle);

                // On successful draft.
                GUILayout.Label("Successful Draft:", ActiveSkin.label);
                DraftMessage = GUILayout.TextField(DraftMessage, ActiveSkin.textField);

                // On successful draw.
                GUILayout.Label("Successful Drawing:", ActiveSkin.label);
                DrawMessage = GUILayout.TextField(DrawMessage, ActiveSkin.textField);

                // $user Explanation
                GUILayout.Label("", ActiveSkin.label);
                GUILayout.Label("\"&user\" = The user drafted.", ActiveSkin.label);
                GUILayout.Label("\"&skill\" = The user's skill.", ActiveSkin.label);

                // Bots to remove
                GUILayout.Label("", ActiveSkin.label);
                GUILayout.Label("Bots to Remove (One per line, no commas):", ActiveSkin.label);
                string botsString = string.Join("\n", ScenarioDraftManager.Instance.BotsToRemove.ToArray());
                botsString = GUILayout.TextArea(botsString, ActiveSkin.textArea);
                ScenarioDraftManager.Instance.BotsToRemove = new List<string>();
                if (botsString != "") { ScenarioDraftManager.Instance.BotsToRemove.AddRange(botsString.Split('\n')); }

                // Save
                if (GUILayout.Button("Save", ActiveSkin.button))
                {
                    SaveSettings();
                    ScenarioDraftManager.Instance.SaveGlobalSettings();
                }
            }
 
            //Version Label
            GUILayout.Label("Version " + (typeof(DraftManagerApp).Assembly.GetName().Version.ToString()), ActiveSkin.label);
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
            GUILayout.Label(alertingMsg, ActiveSkin.label);

            // The close button.
            GUILayout.Label("", ActiveSkin.label);
            if (GUILayout.Button("Close", ActiveSkin.button))
            {
                alertingMsg = "";
                alertShowing = false;
                failedToDraft = false;
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Called when the game UI is shown.
        /// </summary>
        private void OnShowUI()
        {
            isUIHidden = false;
        }

        /// <summary>
        /// Called when the game UI is hidden.
        /// </summary>
        private void OnHideUI()
        {
            isUIHidden = true;
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
            ScenarioDraftManager.Instance.SaveGlobalSettings();

            // Shows the alert as working.
            alertShowing = true;
            draftBusy = true;
            startClip.Play();

            if (forDrawing)
            {
                StartCoroutine(ScenarioDraftManager.DraftKerbal(DrawingSuccess, DraftFailure, forDrawing, false, job));
            }
            else
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && !Funding.CanAfford(GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount())))
                {
                    DraftFailure("You can't afford this draft!");
                }
                else
                {
                    StartCoroutine(ScenarioDraftManager.DraftKerbal(DraftSuccess, DraftFailure, forDrawing, false, job));
                }
            }
        }

        /// <summary>
        /// Creates a new Kerbal based on the provided name.
        /// </summary>
        /// <param name="kerbalName">The name of the new Kerbal.</param>
        private void DraftSuccess(Dictionary<string, string> info)
        {
            ProtoCrewMember newKerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
            newKerbal.ChangeName(info["name"]);
            KerbalRoster.SetExperienceTrait(newKerbal, info["job"]);

            // If the game is career, subtract the cost of hiring.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Funding.Instance.AddFunds(-GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount() - 1), TransactionReasons.CrewRecruited);
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
                alertingMsg = draftMessage.Replace("&user", info["name"]).Replace("&skill", newKerbal.experienceTrait.Title);
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
        private void DrawingSuccess(Dictionary<string, string> info)
        {
            // Alert in-game.
            alertingMsg = drawMessage.Replace("&user", info["winner"]);
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

                // Write the useHotkey setting to it.
                draftSettings.SetValue("useHotkey", useHotkey.ToString(), true);

                // Write the UseKSPSkin setting to it.
                draftSettings.SetValue("UseKSPSkin", UseKSPSkin, true);

                

                // Write the addToCraft setting to it.
                draftSettings.SetValue("addToCraft", addToCraft.ToString(), true); 

                // Get the message settings node.
                ConfigNode messageSettings = globalSettings.GetNode("MESSAGES");

                // If the MESSAGES node doesn't exist,
                if (messageSettings == null)
                {
                    // Create a new MESSAGES node to write to.
                    messageSettings = globalSettings.AddNode("MESSAGES");
                }

                // Write the messages to it.
                messageSettings.SetValue("draftMessage", draftMessage, true);
                messageSettings.SetValue("drawMessage", drawMessage, true); 

                // Save the file.
                globalSettings.Save(settingsLocation + "GlobalSettings.cfg");
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
                    return (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH);
                    //return FlightGlobals.ActiveVessel.landedAt == "KSC_LaunchPad_Platform" || FlightGlobals.ActiveVessel.landedAt == "Runway";
                }

                return false;
            }
        }

#endregion
    }
}