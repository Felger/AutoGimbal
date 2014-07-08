using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using InfernalRobotics;
using MuMech;

namespace AutoGimbal
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class AutoGimbalManager : MonoBehaviour
    {
        void Start()
        {
            FlightGlobals.Vessels.ForEach(vessel =>
            {
                vessel.FindPartModulesImplementing<AutoGimbal>().ForEach(gimbal =>
                { gimbal.manager = this; });
            });
        }

        public class AGGroup
        {

        }

        //Make a GUI variable to define window position.
        protected static Rect windowPosn;
        private string rotationTargetString = "0.0";
        public float rotationTarget = 0.0f;
        public bool shipsToggle;
        public bool bodiesToggle;
        public bool guiEnabled = false;
        //TrackingState stores the current state of the gimbal commanding.
        //      0 = not tracking
        //      1 = tracking
        public int TrackingState = 0;
        IButton AutoGimbalButton;
        void Awake()
        {
            guiEnabled = false;
            var scene = HighLogic.LoadedScene;

            if (ToolbarManager.ToolbarAvailable)
            {
                AutoGimbalButton = ToolbarManager.Instance.add("Felger", "AutoGimbalButton");
                AutoGimbalButton.TexturePath = "AutoGimbal/Textures/icon_button";
                AutoGimbalButton.ToolTip = "AutoGimbal Manager";
                AutoGimbalButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                AutoGimbalButton.OnClick += (e) => guiEnabled = !guiEnabled;
                AutoGimbalButton.Visible = true;
            }
            else if (scene == GameScenes.FLIGHT)
            {
                guiEnabled = true;
            }
        }


        //Create the OnDraw function called above into the rendering queue.  This will create the GUI and collect actions input to the GUI.
        void OnGUI()
        {
            //Define the default window position, if no changes have been made:
            if (windowPosn.x == 0 && windowPosn.y == 0)
            {
                windowPosn = new Rect(Screen.width - 300, 50, 10, 10);
            }
            var scene = HighLogic.LoadedScene;
            if (scene == GameScenes.FLIGHT)
            {
                //Creates a new box, specifying the position to follow the defined position above.
                if (guiEnabled)
                {
                    windowPosn = GUILayout.Window(0, windowPosn, WindowMaker, "AutoGimbal");
                }
            }
        }
        protected void WindowMaker(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Target Angle:");
            rotationTargetString = GUILayout.TextField(rotationTargetString);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start"))
            {
                rotationTarget = float.Parse(rotationTargetString);
                TrackingState = 1;
            }
            if (GUILayout.Button("Stop"))
            {
                TrackingState = 0;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            shipsToggle = GUILayout.Toggle(shipsToggle, "Ships", "Button");
            bodiesToggle = GUILayout.Toggle(bodiesToggle, "Bodies", "Button");
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Close"))
            {
                guiEnabled = false;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();   //Make the GUI draggable
        }
        void OnDestroy()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                AutoGimbalButton.Destroy();
            }
        }
    }
}
