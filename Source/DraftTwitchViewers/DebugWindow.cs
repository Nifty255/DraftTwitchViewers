using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DraftTwitchViewers
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class DebugWindow : MonoBehaviour
    {
        private bool showing = false;

        private Rect windowRect;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            windowRect = new Rect(20f, 20f, 200f, 1f);
        }

        void Update()
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.F6))
            {
                showing = !showing;
            }

            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Screen.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Screen.height - windowRect.height);
        }

        void OnGUI()
        {
            if (showing)
            {
                windowRect = GUILayout.Window(GetInstanceID(), windowRect, DebugWindiw, "DTV Debug", HighLogic.Skin.window);
            }
        }

        void DebugWindiw(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
