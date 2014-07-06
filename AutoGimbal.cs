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
        [KSPField(isPersistant = true, guiActive = true)]
        public int TrackingState = 0;
        [KSPField(isPersistant = true, guiActive = true)]
        public int MoveState = 0;

        private float min_Delta;

        public AutoGimbalManager manager;

        Vessel currentVess;
        List<CelestialBody> body = FlightGlobals.Bodies;
        List<Vessel> ship = FlightGlobals.Vessels;

        public override void OnStart(StartState start)
        {
            print("Celestial Body count:" + body.Count);
            print("Vessel count:" + ship.Count);
            currentVess = FlightGlobals.fetch.activeVessel;
        }

        //This override to the OnUpdate function runs on every physics frame, and updates all values and calculations we're interested in.
        public override void OnUpdate()
        {
            var Robotics = part.FindModulesImplementing<MuMechToggle>().First();
            rotation = Robotics.rotation;
            min_Delta = float.Parse(Robotics.stepIncrement) * 2;

            Vector3d position = currentVess.GetWorldPos3D();
            Vector3d t_pos = body[2].position;
            Quaternion q = currentVess.ReferenceTransform.rotation;
            Vector3d delta = (position - t_pos);
            
            Target = body[2].GetName();

            TrackingState = manager.TrackingState;
            float Rotation_Target = manager.rotationTarget;
            MoveState = IR_Commander(TrackingState, MoveState, Rotation_Target);

            pos_X = delta.x;
            pos_Y = delta.y;
            pos_Z = delta.z;
        }

        //IR_Commander interfaces with Infernal Robotics to command motion to a specific point as specified on input.
        //      TrackingState is input from the GUI, commanded by user.
        //      MoveState keeps track of whether the part is in motion, and what direction.
        //      Rotation_Target is specified by math (or user input)
        public int IR_Commander(int TrackingState, int MoveState, float Rotation_Target)
        {
            var Robotics = part.FindModulesImplementing<MuMechToggle>().First();
            float Delta = Math.Abs(Robotics.rotation - Rotation_Target);

            if (TrackingState == 1) //User has enabled motion, to track to rotation target.
            {
                //Logic to catch either case with the servo inverted or non-inverted.
                if (((Robotics.rotation > Rotation_Target && !Robotics.invertAxis) ||
                     (Robotics.rotation < Rotation_Target && Robotics.invertAxis))
                    && Delta > min_Delta && MoveState == 0) //Movestate = 0 defines stopped
                {
                    Robotics.moveFlags |= 0x100; //Start negative motion
                    MoveState = 1;  //MoveState = 1 defines negative motion
                }
                else if (((Robotics.rotation < Rotation_Target && !Robotics.invertAxis) ||
                     (Robotics.rotation > Rotation_Target && Robotics.invertAxis))
                    && Delta > min_Delta && MoveState == 0) //Movestate = 0 defines stopped
                {
                    Robotics.moveFlags |= 0x200; //Start positive motion
                    MoveState = 2;  //MoveState = 2 defines positive motion
                }
                else if (((Robotics.rotation <= Rotation_Target && !Robotics.invertAxis) ||
                    (Robotics.rotation >= Rotation_Target && Robotics.invertAxis))
                    && MoveState == 1)
                {
                    Robotics.moveFlags &= ~0x100; //Stop negative motion
                    MoveState = 0; 
                }
                else if (((Robotics.rotation >= Rotation_Target && !Robotics.invertAxis) ||
                    (Robotics.rotation <= Rotation_Target && Robotics.invertAxis))
                    && MoveState == 2)
                {
                    Robotics.moveFlags &= ~0x200; //Stop positive motion
                    MoveState = 0; //MoveState = 0 defines negative motion
                }
            }
            else if (TrackingState == 0)
            {
                if (MoveState == 2)
                {
                    Robotics.moveFlags &= ~0x200; //Stop positive motion
                    MoveState = 0; //MoveState = 0 defines negative motion
                }
                else if (MoveState == 1)
                {
                    Robotics.moveFlags &= ~0x100; //Stop negative motion
                    MoveState = 0; 
                }
            }
            return MoveState;
        }//end IR_Commander

    }

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
            Debug.Log("[AutoGimbalManager] started");
        }

        //Make a GUI variable to define window position.
        protected static Rect windowPosn;
        private string rotationTargetString = "0.0";
        public float rotationTarget;
        private bool shipsToggle;
        private bool bodiesToggle;
        private bool guiEnabled = false;
        //TrackingState stores the current state of the gimbal commanding.
        //      0 = not tracking
        //      1 = tracking
        public int TrackingState;
        IButton AutoGimbalButton;

        void Awake()
        {
            Debug.Log("[AutoGimbalManager] awake");
            guiEnabled = false;
            var scene = HighLogic.LoadedScene;
            if (scene == GameScenes.FLIGHT)
            {//Need to check at some point if we need to reload groups from vessel
            }
            if (ToolbarManager.ToolbarAvailable)
            {
                AutoGimbalButton = ToolbarManager.Instance.add("Felger", "AutoGimbalButton");
                AutoGimbalButton.TexturePath = "AutoGimbal/Textures/icon_button";
                AutoGimbalButton.ToolTip = "AutoGimbal Manager";
                AutoGimbalButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH, GameScenes.FLIGHT);
                AutoGimbalButton.OnClick += (e) => guiEnabled = !guiEnabled;
                AutoGimbalButton.Visible = true;
            }
            else
            {
                guiEnabled = true;
            }
        }


        //Create the OnDraw function called above into the rendering queue.  This will create the GUI and collect actions input to the GUI.
        void OnGUI()
        {
            //Define the default window position, if no changes have been made:
            if(windowPosn.x == 0 && windowPosn.y == 0)
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
            if(GUILayout.Button("Stop"))
            {
                TrackingState = 0;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            shipsToggle = GUILayout.Toggle(shipsToggle,"Ships","Button");
            bodiesToggle = GUILayout.Toggle(bodiesToggle, "Bodies","Button");
            GUILayout.EndHorizontal();
            if (ToolbarManager.ToolbarAvailable)
            {
                if (GUILayout.Button("Close"))
                {
                    guiEnabled = false;
                }
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