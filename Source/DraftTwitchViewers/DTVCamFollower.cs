using UnityEngine;

namespace DraftTwitchViewers
{
    /// <summary>
    /// A simple MonoBehaviour which follows the main camera.
    /// </summary>
    class DTVCamFollower : MonoBehaviour
    {
        /// <summary>
        /// Called when the MonoBehaviour is awakened.
        /// </summary>
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Called after Unity finishes all Update() calls.
        /// </summary>
        void LateUpdate()
        {
            try
            {
                transform.position = Camera.main.transform.position;
            }
            catch { }
        }
    }
}
