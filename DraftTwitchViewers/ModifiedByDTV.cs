using Contracts;

namespace DraftTwitchViewers
{
    /// <summary>
    /// A custom contract paramater which does nothing but indicate that Draft Twitch Viewers has modified the parent contract.
    /// </summary>
    class ModifiedByDTV : ContractParameter
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public ModifiedByDTV()
        {
            // Instantly set this parameter as complete.
            state = ParameterState.Complete;
        }

        /// <summary>
        /// Returns the title of this parameter.
        /// </summary>
        /// <returns>The title of this parameter.</returns>
        protected override string GetTitle()
        {
            // Return text indicating the parent contract has been modified by Draft Twitch Viewers.
            return "Modified by DTV";
        }
    }
}
