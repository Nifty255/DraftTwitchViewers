using UnityEngine;

namespace DraftTwitchViewers
{
    /// <summary>
    /// Designed to alert the DraftManager once the Space Center scene is already loaded. Destroys itself when finished. - blast awesomeness: 0.0
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class SpaceCenterAlerter : MonoBehaviour
    {
        void Awake()
        {
            DraftManager.Instance.LoadLocalSettings();
            StartCoroutine(TourismContractModifier.Instance.ContractCheck());
            StartCoroutine(RescueContractModifier.Instance.ContractCheck());
            Destroy(gameObject);
        }
    }
}
