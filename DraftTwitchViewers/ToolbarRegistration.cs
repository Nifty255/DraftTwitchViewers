
using UnityEngine;
using ToolbarControl_NS;

namespace DraftTwitchViewers
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(DraftManagerApp.MODID, DraftManagerApp.MODNAME);
        }
    }
}