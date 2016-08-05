using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Contracts;

namespace DraftTwitchViewers
{
    /// <summary>
    /// The TourismContractModifier. This class hooks into contracts and modifies Kerbal Tourism contracts, drafting viewers instead.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class TourismContractModifier : MonoBehaviour
    {
        #region Variables

        /// <summary>
        /// The instance of this class.
        /// </summary>
        private static TourismContractModifier instance;

        /// <summary>
        /// The public instance of this class.
        /// </summary>
        public static TourismContractModifier Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// The queue used to store contracts waiting to be modified.
        /// </summary>
        private Queue<Contract> contractsToModify;

        /// <summary>
        /// The queue used to store drafted names. Acts as a buffer to hold names since a single tourism contract can have multiple tourists.
        /// </summary>
        private Queue<string> draftNames;

        /// <summary>
        /// True if the contract modifier is currently waiting on a draft.
        /// </summary>
        private bool working = false;

        /// <summary>
        /// Indicates the number of consecutive draft failures. The addon destroys itself at 5 failures.
        /// </summary>
        private int failures = 0;

        #endregion

        #region Unity Functions

        /// <summary>
        /// Called when the MonoBehaviour is first created.
        /// </summary>
        void Awake()
        {
            // Assign the instance.
            instance = this;

            // Prevent destroy.
            DontDestroyOnLoad(gameObject);

            // Create the queues.
            contractsToModify = new Queue<Contract>();
            draftNames = new Queue<string>();

            // Hook into contract offers.
            GameEvents.Contract.onOffered.Add(EnqueueContract);
        }

        /// <summary>
        /// Called when the MonoBehaviour is destroted.
        /// </summary>
        void OnDestroy()
        {
            // Remove the contract hook.
            GameEvents.Contract.onOffered.Remove(EnqueueContract);
        }

        #endregion

        #region Contract Functions

        /// <summary>
        /// Called when the space center is loaded. Runs all contracts through the enqueueing process, which weeds out invalid and already modified contracts.
        /// </summary>
        public IEnumerator ContractCheck()
        {
            yield return new WaitForSeconds(1f);

            // Iterate through each contract,
            foreach (Contract c in ContractSystem.Instance.Contracts)
            {
                // And enqueue it.
                EnqueueContract(c);
            }
        }

        /// <summary>
        /// Enqueues any tourism contracts offered.
        /// </summary>
        /// <param name="toEnqueue">The contract to test and enqueue.</param>
        void EnqueueContract(Contract toEnqueue)
        {
            // Create a ConfigNode to save the contract into.
            ConfigNode test = new ConfigNode();
            toEnqueue.Save(test);

            // If the contract is of the TourismContract type,
            if (test.GetValue("type") == "TourismContract")
            {
                // If the saved ConfigNode contains a tourists value,
                if (test.HasValue("tourists"))
                {
                    // If the value contained isn't null or empty,
                    if (!string.IsNullOrEmpty(test.GetValue("tourists")))
                    {
                        // If the contract wasn't already modified,
                        if (test.GetNodes("PARAM")[0].GetValue("name") != "ModifiedByDTV")
                        {
                            // The contract is a proper tourism contract.
                            Logger.DebugLog("Tourism contract found: " + test.GetValue("tourists"));

                            // Enqueue it.
                            contractsToModify.Enqueue(toEnqueue);

                            // If a draft is not currently underway,
                            if (!working)
                            {
                                // Begin a draft and indicate waiting status.
                                StartCoroutine(ScenarioDraftManager.DraftKerbal(DraftSuccess, DraftFailure, false, false, "Any"));
                                working = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when a draft succeeds.
        /// </summary>
        /// <param name="kerbalName">The name of the drafted viewer.</param>
        void DraftSuccess(Dictionary<string, string> info)
        {
            // Enqueue the name first thing, since it needs to be in the queue whether it has enough with it or not.
            draftNames.Enqueue(info["name"]);

            // Resets failures. The addon should only destroy after 5 consecutive failures.
            failures = 0;

            // Peek at the next contract in the queue instead of dequeueing because there might not yet be enough names to cover the contract.
            Contract toMod = contractsToModify.Peek();

            // Create a ConfigNode to save the contract into.
            ConfigNode replacement = new ConfigNode("CONTRACT");
            toMod.Save(replacement);

            // Obtain a list of the old tourists in the contract.
            string[] oldTourists = replacement.GetValue("tourists").Split('|');

            // If the count of names in the queue plus this name equals the number of tourists,
            if (draftNames.Count == oldTourists.Length)
            {
                // Dequeue the contract we peeked because there are enough names for it.
                contractsToModify.Dequeue();

                // Create an array from the queue and clear it.
                string[] newTourists = draftNames.ToArray();
                draftNames.Clear();

                // Replace the contract "tourists" string.
                replacement.SetValue("tourists", string.Join("|", newTourists));

                // Get a list of PARAM nodes in the contract.
                ConfigNode[] paramNodes = replacement.GetNodes("PARAM");

                // Iterate through them,
                for (int i = 0; i < paramNodes.Length; i++)
                {
                    // And replace their kerbalName values.
                    paramNodes[i].SetValue("kerbalName", newTourists[i]);

                    // Iterate through any sub-PARAMS,
                    foreach (ConfigNode subParam in paramNodes[i].GetNodes("PARAM"))
                    {
                        // And replace their kerbalName values as well.
                        subParam.SetValue("kerbalName", newTourists[i]);
                    }

                    // Remove the parameter from the actual contract to prevent duplicates.
                    toMod.RemoveParameter(0);

                    // Get an old Kerbal and rename it.
                    ProtoCrewMember toRename = HighLogic.CurrentGame.CrewRoster[oldTourists[i]];
                    toRename.name = newTourists[i];
                }

                // Add the custom parameter indicating DTV has modified this contract.
                toMod.AddParameter((ContractParameter)new ModifiedByDTV());

                // Reload the contract.
                Contract.Load(toMod, replacement);

                // Logging.
                Logger.DebugLog("Draft Success (" + contractsToModify.Count.ToString() + " contracts waiting): " + string.Join("|", newTourists));

                // Refresh the contract list by firing the onContractListChanged event.
                GameEvents.Contract.onContractsListChanged.Fire();

                // If the queue is not empty,
                if (contractsToModify.Count > 0)
                {
                    // Begin another draft.
                    StartCoroutine(ScenarioDraftManager.DraftKerbal(DraftSuccess, DraftFailure, false, false, "Any"));
                }
                // Else, the queue is empty.
                else
                {
                    // Indicate a stop in waiting status.
                    working = false;
                }
            }
            // Else, run another draft.
            else
            {
                StartCoroutine(ScenarioDraftManager.DraftKerbal(DraftSuccess, DraftFailure, false, false, "Any"));
            }
        }

        /// <summary>
        /// Called when a draft fails.
        /// </summary>
        /// <param name="reason">The reason for the failure.</param>
        void DraftFailure(string reason)
        {
            // If the reason is because a channel isn't specified,
            if (reason == "Please specify a channel!")
            {
                // Simply clear the contract queue and then indicate a stop in waiting status.
                contractsToModify.Clear();
                working = false;

                // Notify the player that the contract draft failed because of a lack of channel.
                ScreenMessages.PostScreenMessage("<color=" + XKCDColors.HexFormat.KSPNotSoGoodOrange + ">Contract draft FAILED. (Please input a channel to draft from.)</color>", 5f, ScreenMessageStyle.UPPER_CENTER);

            }
            // Else, follow normal failure procedure.
            else
            {
                // Increment failures.
                failures++;

                // Log a warning with the reason.
                Logger.DebugWarning("Contract Draft failed (" + failures.ToString() + " failures): " + reason);

                // If the queue is not empty, and the failure count isn't 5,
                if (contractsToModify.Count > 0 && failures < 5)
                {
                    // Retry the draft.
                    StartCoroutine(ScenarioDraftManager.DraftKerbal(DraftSuccess, DraftFailure, false, false, "Any"));
                }
                // Else, the queue is empty, or 5 failures have occurred.
                else
                {
                    // Indicate a stop in waiting status.
                    working = false;
                }

                // If 5 failures have occurred,
                if (failures == 5)
                {
                    // Notify the player that the contract draft failed consecutively.
                    ScreenMessages.PostScreenMessage("<color=" + XKCDColors.HexFormat.KSPNotSoGoodOrange + ">Contract draft FAILED. (5 failed attempts. Deactivating.)</color>", 5f, ScreenMessageStyle.UPPER_CENTER);

                    // Log an error and destroy the addon.
                    Logger.DebugError("5 failed Contract Drafts. Disabling.");
                    Destroy(gameObject);
                }
            }
        }

        #endregion
    }
}
