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
            Debug.Log("[AutoGimbal] - Partmodule loaded");
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
            //Rotate vector into ship frame.  Unity does quaternion rotations implicitly.
            Vector3d PV_ship = q * delta;
            //Grab attachment rotation of the part, then rotate pointing vector into part frame.
            Quaternion q_part = part.attRotation;
            Vector3d PV_part = q_part * PV_ship;

            //With the pointing vector in the part frame, project the pointing vector onto the rotation plane of the part itself.  The rotation axis is available in the Robotics object Robotics.rotateAxis, just need to project onto the plane.
            // believe the rotation value is relative


            


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
}