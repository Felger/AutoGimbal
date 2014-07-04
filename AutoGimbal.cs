using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;
using InfernalRobotics;
using MuMech;

namespace AutoGimbal
{
    public class AutoGimbal : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true)]
        public double pos_X = 0;
        [KSPField(isPersistant = true, guiActive = true)]
        public double pos_Y = 0;
        [KSPField(isPersistant = true, guiActive = true)]
        public double pos_Z = 0;
        [KSPField(isPersistant = true, guiActive = true)]
        public string Target = "none";
        [KSPField(isPersistant = true, guiActive = true)]
        public float rotation = 0;



        List<CelestialBody> body = FlightGlobals.Bodies;
        List<Vessel> ship = FlightGlobals.Vessels;



        public override void OnStart(StartState start)
        {
            print("Celestial Body count:" + body.Count);
            print("Vessel count:" + ship.Count);
        }
        //This override to the OnUpdate function runs on every physics frame, and updates all values and calculations we're interested in.  
        public override void OnUpdate()
        {
            Vessel currentVess = FlightGlobals.fetch.activeVessel;

            var Robotics = part.FindModulesImplementing<MuMechToggle>().First();


       
            //rotation = paMuMech.MuMechToggle.rotation;

            Vector3d position = currentVess.GetWorldPos3D();
            Vector3d t_pos = body[2].position;
            Quaternion q = currentVess.ReferenceTransform.rotation;
            Vector3d delta = (position - t_pos);

            Target = body[2].GetName();

            pos_X = delta.x;
            pos_Y = delta.y;
            pos_Z = delta.z;
        }

    }

    public class AutoGimbal2 : PartModule
    {
        bool target_window_exist = false;

        // This event will pop up the target selection window, once a target is selected tracking will beging for this gimbal.  
        [KSPEvent(guiActive = true, guiName = "Select Target")]
        public void ActivateEvent()
        {
            ScreenMessages.PostScreenMessage("Clicked Activate", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            target_window_exist = true;

            RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));//start the GUI


            // This will hide the Activate event, and show the Deactivate event.
            Events["ActivateEvent"].active = false;
            Events["DeactivateEvent"].active = true;
        }

        // By deselecting a target, you effectively stop tracking.
        [KSPEvent(guiActive = true, guiName = "Deselect Target", active = false)]
        public void DeactivateEvent()
        {
            ScreenMessages.PostScreenMessage("Clicked Deactivate", 5.0f, ScreenMessageStyle.UPPER_CENTER);

            // This will hide the Deactivate event, and show the Activate event.
            if (target_window_exist == true)
            {
                RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI
            }

            Events["ActivateEvent"].active = true;
            Events["DeactivateEvent"].active = false;
        }


        protected Rect windowPos;

        private void WindowGUI(int windowID)
        {
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            mySty.normal.textColor = mySty.focused.textColor = Color.white;
            mySty.hover.textColor = mySty.active.textColor = Color.yellow;
            mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
            mySty.padding = new RectOffset(8, 8, 8, 8);

            GUILayout.BeginVertical();
            if (GUILayout.Button("DESTROY", mySty, GUILayout.ExpandWidth(true)))//GUILayout.Button is "true" when clicked
            {
                ScreenMessages.PostScreenMessage("DESTROYED", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            GUILayout.EndVertical();

            //DragWindow makes the window draggable. The Rect specifies which part of the window it can by dragged by, and is 
            //clipped to the actual boundary of the window. You can also pass no argument at all and then the window can by
            //dragged by any part of it. Make sure the DragWindow command is AFTER all your other GUI input stuff, or else
            //it may "cover up" your controls and make them stop responding to the mouse.
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

        }
        private void drawGUI()
        {
            GUI.skin = HighLogic.Skin;
            windowPos = GUILayout.Window(1, windowPos, WindowGUI, "Self Destruct", GUILayout.MinWidth(100));
            if ((windowPos.x == 0) && (windowPos.y == 0))//windowPos is used to position the GUI window, lets set it in the center of the screen
            {
                windowPos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
            }
        }
    }
}