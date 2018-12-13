using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // ============= Settings ==============
        static int inventoryMultiplier = 10;
        // =====================================

        static IMyGridTerminalSystem gridSystem;
        static long gridId;
        static IMyRemoteControl rc;
        bool naviagting = false;
        ThrusterControl thrust;
        GyroControl gyros;
        Dictionary<PathKey, Path> paths = new Dictionary<PathKey, Path>();
        Queue<PathKey> currentPaths = new Queue<PathKey>();
        FlightMode mode = FlightMode.OneWay;
        Path recordingPath = null;

        Program ()
        {
            gridSystem = GridTerminalSystem;
            gridId = Me.CubeGrid.EntityId;
            rc = GetBlock<IMyRemoteControl>();
            thrust = new ThrusterControl();
            gyros = new GyroControl();
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Clock.Initialize(UpdateFrequency.Update1);
        }

        void Main (string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update1 || updateSource == UpdateType.Update10 || updateSource == UpdateType.Update100)
            {
                // Main code
                Echo("Running.");
                if (recordingPath != null)
                {
                    if (recordingPath.Status == Path.Mode.None)
                        Stop();
                    double percent = recordingPath.GetEfficency() * 100;
                    Echo(percent.ToString("0.0"));
                    recordingPath.Update();
                    return;
                }
                if (currentPaths.Count > 0)
                {
                    foreach (PathKey key in currentPaths)
                        Echo(key.start + "->" + key.end);
                    Move(paths [currentPaths.Peek()]);
                    CheckPaths();
                }
                else
                {
                    Echo("Not navigating.");
                }

            }
            else
            {
                // Remote command
                Command(argument);
            }
        }

        void Move (Path path)
        {
            Vector3D targetVelocity = path.Velocity;
            Vector3D myPos = rc.GetPosition();

            Vector3D difference = path.Position - myPos;
            double diffLen = difference.Length();

            thrust.Velocity = difference + targetVelocity;
            thrust.Update();

            gyros.FaceVectors(path.Forward, path.Up);
            path.Update();
        }

        void Stop ()
        {
            if (recordingPath != null)
            {
                recordingPath.UpdateStatus(Path.Mode.None);
                recordingPath = null;
            }

            if (currentPaths.Count > 0)
            {
                Path current = paths [currentPaths.Peek()];
                current.UpdateStatus(Path.Mode.None);
                currentPaths.Clear();
            }

            thrust.Reset();
            gyros.Reset();
        }

        bool CompilePath (string [] points, FlightMode mode, out Queue<PathKey> currentPaths)
        {
            currentPaths = new Queue<PathKey>();
            if (points.Length < 3)
                return false;
            for (int i = 2; i < points.Length; i++)
            {
                PathKey p = new PathKey(points [i - 1], points [i]);
                if (ContainsKey(ref p))
                    currentPaths.Enqueue(p);
                else
                    return false;
            }

            if (mode == FlightMode.Circle)
            {
                PathKey p = new PathKey(points [points.Length - 1], points [1]);
                if (ContainsKey(ref p))
                    currentPaths.Enqueue(p);
                else
                    return false;
            }

            if (mode == FlightMode.Patrol)
            {
                for (int i = points.Length - 1; i >= 1; i++)
                {
                    PathKey p = new PathKey(points [i], points [i - 1]);
                    if (ContainsKey(ref p))
                        currentPaths.Enqueue(p);
                    else
                        return false;
                }
            }
            return true;
        }

        void CheckPaths ()
        {
            PathKey key = currentPaths.Peek();
            if (paths [key].Status == Path.Mode.None)
            {
                thrust.Reset();
                gyros.Reset();

                currentPaths.Dequeue();
                if (mode != FlightMode.OneWay)
                    currentPaths.Enqueue(key);
                ActivateNextPath();
            }
        }

        void ActivateNextPath ()
        {
            if (currentPaths.Count == 0)
                return;

            PathKey key = currentPaths.Peek();
            paths [key].UpdateStatus(Path.Mode.Play);
        }

        bool ContainsKey (ref PathKey p)
        {
            if (p.start == p.end)
                return false;
            return paths.ContainsKey(p);
        }

        void Command (string command)
        {
            string [] args = command.Split(';');

            switch (args [0])
            {
                case ("halt"):
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    break;
                case ("record"): // path;start;end
                    if (args.Length == 3 && args [1] != args [2])
                    {
                        Stop();
                        PathKey p = new PathKey(args [1], args [2]);
                        Path path = new Path();
                        paths [p] = path;
                        path.UpdateStatus(Path.Mode.Recording);
                        recordingPath = path;
                    }
                    break;
                case ("stop"):
                    Stop();
                    break;
                case ("oneway"): // oneway;id1;id2;... | Navigates to all of the points in one direction, then stops.
                    if (args.Length > 2)
                    {
                        Queue<PathKey> newCurrentPaths;
                        if (CompilePath(args, FlightMode.OneWay, out newCurrentPaths))
                            currentPaths = newCurrentPaths;
                        else
                            return;
                        ActivateNextPath();
                        mode = FlightMode.OneWay;
                    }
                    break;
                case ("patrol"): // patrol;id1;id2;... | Navigates to all of the points in one direction, then in the reverse direction, and starts over.
                    if (args.Length > 2)
                    {
                        Queue<PathKey> newCurrentPaths;
                        if (CompilePath(args, FlightMode.Patrol, out newCurrentPaths))
                            currentPaths = newCurrentPaths;
                        else
                            return;
                        ActivateNextPath();
                        mode = FlightMode.Patrol;
                    }
                    break;
                case ("circle"): // circle;id1;id2;... | Navigates to all of the points in one direction, then back to the first point, and starts over.
                    if (args.Length > 2)
                    {
                        Queue<PathKey> newCurrentPaths;
                        if (CompilePath(args, FlightMode.Circle, out newCurrentPaths))
                            currentPaths = newCurrentPaths;
                        else
                            return;
                        ActivateNextPath();
                        mode = FlightMode.Circle;
                    }
                    break;
            }
        }

    }
}