using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Contracts;

namespace DraftTwitchViewers
{
    /// <summary>
    /// The RescueContractModifier. This class hooks into contracts and modifies Kerbal Rescue contracts, drafting viewers instead.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class RescueContractModifier : MonoBehaviour
    {
        #region Variables

        /// <summary>
        /// The instance of this class.
        /// </summary>
        private static RescueContractModifier instance;

        /// <summary>
        /// The public instance of this class.
        /// </summary>
        public static RescueContractModifier Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// The queue used to store contracts waiting to be modified.
        /// </summary>
        private Queue<Contract> contractsToModify;

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

            // Create the queue.
            contractsToModify = new Queue<Contract>();

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
        /// Enqueues any rescue contracts offered.
        /// </summary>
        /// <param name="toEnqueue">The contract to test and enqueue.</param>
        void EnqueueContract(Contract toEnqueue)
        {
            // Create a ConfigNode to save the contract into.
            ConfigNode test = new ConfigNode();
            toEnqueue.Save(test);

            // If the contract is of the RecoverAsset type,
            if (test.GetValue("type") == "RecoverAsset")
            {
                // If the saved ConfigNode contains a kerbalName value,
                if (test.HasValue("kerbalName"))
                {
                    // If the value contained isn't null or empty,
                    if (!string.IsNullOrEmpty(test.GetValue("kerbalName")))
                    {
                        // If the contract wasn't already modified,
                        if (test.GetNodes("PARAM")[0].GetValue("name") != "ModifiedByDTV")
                        {
                            // The contract is a proper rescue contract.
                            Logger.DebugLog("Rescue contract found: " + test.GetValue("kerbalName"));

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
            // Resets failures. The addon should only destroy after 5 consecutive failures.
            failures = 0;

            // Get the next contract in the queue.
            Contract toMod = contractsToModify.Dequeue();

            // Create a ConfigNode to save the contract into.
            ConfigNode replacement = new ConfigNode("CONTRACT");
            toMod.Save(replacement);

            // Get the old Kerbal name for later use.
            string oldName = replacement.GetValue("kerbalName");

            // Replace the old name with the new.
            replacement.SetValue("kerbalName", info["name"]);

            // For each PARAM node in the CONTRACT node,
            foreach (ConfigNode node in replacement.nodes)
            {
                // Get the name of the contract parameter.
                string paramName = node.GetValue("name");

                // Perform certain replacement functions for each parameter.
                switch (paramName)
                {
                    case "AcquireCrew":
                        {
                            node.SetValue("title", "Save " + info["name"]);
                            break;
                        }
                    case "AcquirePart":
                        {
                            string firstName = info["name"].Substring(0, info["name"].IndexOf(' '));
                            node.SetValue("title", "Obtain " + firstName + "'s Scrap");
                            break;
                        }
                    case "RecoverKerbal":
                        {
                            node.SetValue("title", "Recover " + info["name"] + " on Kerbin");
                            break;
                        }
                    case "RecoverPart":
                        {
                            string firstName = info["name"].Substring(0, info["name"].IndexOf(' '));
                            node.SetValue("title", "Recover " + firstName + "'s Scrap on Kerbin");
                            break;
                        }
                }
            }

            // Get a count of parameters currently held by the contract.
            int parameters = toMod.ParameterCount;

            // Iterate using this count, removing the one parameter each time, effectively clearing the list.
            for (int i = 0; i < parameters; i++)
            {
                // Remove the first parameter.
                toMod.RemoveParameter(0);
            }

            // Add the custom parameter indicating DTV has modified this contract.
            toMod.AddParameter(new ModifiedByDTV());
            
            // Reload the contract.
            Contract.Load(toMod, replacement);

            // Get the old Kerbal and rename it.
            ProtoCrewMember toRename = HighLogic.CurrentGame.CrewRoster[oldName];
            toRename.ChangeName(info["name"]);

            // Logging.
            Logger.DebugLog("Draft Success (" + contractsToModify.Count.ToString() + " contracts waiting): " + info["name"]);

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
