using System.Collections.Generic;
using UnityEngine;

namespace DraftTwitchViewers
{
    /// <summary>
    /// The Part Selection Manager. Manages part selection tasks for adding Kerbals to cabins.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    class PartSelectionManager : MonoBehaviour
    {
        #region Variables

        /// <summary>
        /// The upper ScreenMessage template.
        /// </summary>
        ScreenMessage upper;

        /// <summary>
        /// The lower ScreenMessage template.
        /// </summary>
        ScreenMessage lower;

        /// <summary>
        /// The crew member to add.
        /// </summary>
        internal ProtoCrewMember toAdd = null;

        /// <summary>
        /// The list of PartSelectors used to add a Kerbal.
        /// </summary>
        private List<PartSelector> selectors;

        #region Instance

        /// <summary>
        /// The private instance field.
        /// </summary>
        private static PartSelectionManager instance;

        /// <summary>
        /// The public instance property of this class.
        /// </summary>
        public static PartSelectionManager Instance
        {
            get { return instance; }
        }

        #endregion

        #endregion

        #region Unity Functions

        /// <summary>
        /// Called when the MonoBehavior is awakened.
        /// </summary>
        private void Awake()
        {
            // Prevent scene-load destruction.
            DontDestroyOnLoad(gameObject);
            // Assign the instance field to this instance.
            instance = this;

            // Design the ScreenMessage templates.
            upper = new ScreenMessage(string.Empty, 15f, ScreenMessageStyle.UPPER_CENTER);
            lower = new ScreenMessage(string.Empty, 3f, ScreenMessageStyle.LOWER_CENTER);
        }

        /// <summary>
        /// Called when the MonoBehavior is updated.
        /// </summary>
        private void Update()
        {
            //If the placement is in progress (determined by whether or not the toAdd field is null), and player presses escape, cancel the placement.
            if (toAdd != null && Input.GetKeyUp(KeyCode.Escape))
            {
                CleanUp();
            }
        }

        #endregion

        #region KSP Functions

        /// <summary>
        /// Generates a set of PartSelectors and allows the user to click a part to add the crew.
        /// </summary>
        /// <param name="member">The crew member to add.</param>
        public void GenerateSelection(ProtoCrewMember member)
        {
            // Show upper selection message.
            ScreenMessages.PostScreenMessage("<color=" + TwitchPurple.Hex + ">Select a pod for " + member.name + " (" + member.experienceTrait.Title + ").</color>", upper);

            // Lock the the inputs to prevent launch and other shenanigans.
            InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "DTVAddCrew");

            // Create the Selector list.
            selectors = new List<PartSelector>();

            // Gets a list of available seats.
            List<Part> availableSeats = new List<Part>();

            // Cycle through all parts.
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                // If it has crew capacity,
                if (p.CrewCapacity > 0)
                {
                    // And if it has available space,
                    if (p.protoModuleCrew.Count < p.CrewCapacity)
                    {
                        // Add to the list.
                        availableSeats.Add(p);
                    }
                }
            }

            // If no available spots were found,
            if (availableSeats.Count == 0)
            {
                // Post to the screen and leave it.
                ScreenMessages.PostScreenMessage("<color=" + XKCDColors.HexFormat.KSPNotSoGoodOrange + ">No available seating.</color>", 3f, ScreenMessageStyle.LOWER_CENTER);

                // Clean up the manager
                CleanUp();
            }
            // Else, a seat is available.
            else
            {
                // Set the toAdd crew member field.
                toAdd = member;

                // For each available spot,
                foreach(Part p in availableSeats)
                {
                    // Generate a selector.
                    selectors.Add(PartSelector.Create(p, new Callback<Part>(OnPartSelected), TwitchPurple.RGB, new Color(TwitchPurple.RGB.r - 0.2f, TwitchPurple.RGB.g - 0.2f, TwitchPurple.RGB.b - 0.2f)));
                }
            }
        }

        /// <summary>
        /// When a part is selected.
        /// </summary>
        /// <param name="p">The selected part.</param>
        private void OnPartSelected(Part p)
        {
            if (toAdd == null)
                return;

            // Add the crew member.
            p.AddCrewmember(toAdd);
            // Set roster status to Assigned.
            toAdd.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;

            // If the seat isn't null,
            if (toAdd.seat != null)
            {
                // Spawn the crew.
                toAdd.seat.SpawnCrew();
            }

            // Fire a fake crew transfer event to update the crew portraits.
            GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(toAdd, p, p));

            // Post to the screen and leave it.
            ScreenMessages.PostScreenMessage(toAdd.name + " added to " + p.partInfo.title + ".", 3f, ScreenMessageStyle.LOWER_CENTER);

            // Clean up the manager
            CleanUp();
        }

        /// <summary>
        /// Cleans up the manager for next use.
        /// </summary>
        private void CleanUp()
        {
            // For each selector,
            foreach (PartSelector pS in selectors)
            {
                // Dismiss
                pS.Dismiss();
            }

            // Clear the list.
            selectors.Clear();

            // Unassign the toAdd field.
            toAdd = null;

            // Remove the screen message if there.
            ScreenMessages.RemoveMessage(upper);

            // Unlock controls.
            InputLockManager.RemoveControlLock("DTVAddCrew");
        }

        #endregion
    }

    #region Twitch Purple

    /// <summary>
    /// The Twitch Purple color. Provides access to both standard RGB and Hexadecimal formats.
    /// </summary>
    public struct TwitchPurple
    {
        /// <summary>
        /// The standard RGB format of Twitch Purple.
        /// </summary>
        public static Color RGB
        {
            get { return new Color(108f / 255f, 36f / 255f, 152f / 255f); }
        }

        /// <summary>
        /// The Hexadecimal format of Twitch Purple.
        /// </summary>
        public static string Hex
        {
            get { return "#6c2498"; }
        }
    }

    #endregion
}
