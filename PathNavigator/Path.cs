using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        class Path
        {
            Dictionary<int, Point> values = new Dictionary<int, Point>();
            int movementTick = 0;
            int rotationTick = 0;
            int lastTick = 0;
            Clock timer;
            public bool Valid
            {
                get; private set;
            }
            public enum Mode
            {
                None, Recording, Play
            }
            public Mode Status
            {
                get; private set;
            }

            public Vector3D Position
            {
                get
                {
                    float seconds = (float)timer.GetSeconds(rotationTick);
                    Point lastKnown = values [movementTick];
                    return lastKnown.position.Value + Velocity * seconds;
                }
            }
            public Vector3D Velocity
            {
                get
                {
                    if (Status == Mode.Play)
                        return values [movementTick].velocity.Value;
                    throw new Exception("Cannot access velocity while not in play mode.");
                }
            }
            public Vector3D Forward
            {
                get
                {
                    if (Status == Mode.Play)
                        return values [rotationTick].forward.Value;
                    throw new Exception("Cannot access forward direction while not in play mode.");
                }
            }
            public Vector3D Up
            {
                get
                {
                    if (Status == Mode.Play)
                        return values [rotationTick].up.Value;
                    throw new Exception("Cannot access upward direction while not in play mode.");
                }
            }

            public Path ()
            {
                timer = new Clock();
                Status = Mode.None;
            }

            public void Update ()
            {
                int tick = timer.Runtime;
                if (Status == Mode.Recording)
                {
                    bool vel = false;
                    Point lastMovement = values [movementTick];
                    Vector3 velocity = rc.GetShipVelocities().LinearVelocity;
                    if (!velocity.Equals(lastMovement.velocity.Value, 0.00001f))
                    {
                        vel = true;
                        movementTick = tick;
                    }

                    bool rot = false;
                    Point lastRotation = values [rotationTick];
                    if (!rc.WorldMatrix.Forward.Equals(lastRotation.forward.Value, 0.00001f)
                        || !rc.WorldMatrix.Up.Equals(lastRotation.up.Value, 0.00001f))
                    {
                        rot = true;
                        rotationTick = tick;
                    }

                    if (vel || rot)
                    {
                        Point p = new Point(rc, vel, rot);
                        values.Add(tick, p);
                    }
                }
                else if (Status == Mode.Play)
                {
                    if (tick < lastTick && values.ContainsKey(tick))
                    {
                        Point p = values [tick];
                        if (p.position.HasValue)
                            movementTick = tick;
                        if (p.forward.HasValue)
                            rotationTick = tick;
                    }
                    else if (tick == lastTick)
                    {
                        Status = Mode.None;
                    }
                }
                timer.Update();
            }

            public void UpdateStatus (Mode newStatus)
            {
                if (newStatus == Status)
                    return;

                if (newStatus == Mode.Recording)
                {
                    values.Clear();
                    values.Add(0, new Point(rc, true, true));
                    Valid = true;
                }

                if (newStatus == Mode.None && Status == Mode.Recording)
                {
                    lastTick = timer.Runtime;
                    values.Add(lastTick, new Point(rc, true, true));
                }

                movementTick = 0;
                rotationTick = 0;
                timer.Start();
                Status = newStatus;
            }

            public float GetEfficency ()
            {
                float max = timer.Runtime;
                return (max - values.Count) / max;
            }
        }
        struct PathKey
        {
            public string start;
            public string end;
            public PathKey (string start, string end)
            {
                this.start = start;
                this.end = end;
            }

            public override bool Equals (object obj)
            {
                if (!(obj is PathKey))
                {
                    return false;
                }

                var key = (PathKey)obj;
                return start == key.start &&
                       end == key.end;
            }

            public override int GetHashCode ()
            {
                var hashCode = 1075529825;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(start);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(end);
                return hashCode;
            }
        }
        struct Point
        {
            public Vector3? position;
            public Vector3? velocity;
            public Vector3? forward;
            public Vector3? up;
            public Point (IMyShipController rc, bool vel, bool rot)
            {
                if (vel)
                {
                    position = rc.GetPosition();
                    velocity = rc.GetShipVelocities().LinearVelocity;
                }
                else
                {
                    position = null;
                    velocity = null;
                }

                if (rot)
                {
                    forward = rc.WorldMatrix.Forward;
                    up = rc.WorldMatrix.Up;
                }
                else
                {
                    forward = null;
                    up = null;
                }
            }

        }

    }
}
