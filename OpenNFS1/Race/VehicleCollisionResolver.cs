using System;
using System.Collections.Generic;
using System.Numerics;
using OpenNFS1.Vehicles;
using OpenNFS1.Vehicles.AI;
using OpenNFS1.Physics;

namespace OpenNFS1
{
    /// <summary>
    /// Solid vehicle-versus-vehicle contact. A simple Arcade OBB solver,
    /// intentionally separate from scenery/fence collisions. Uses the CFM
    /// dimensions and world X/Z heading, not the oversized render frustum sphere.
    /// </summary>
    internal static class VehicleCollisionResolver
    {
        public static void Resolve(List<IDriver> drivers)
        {
            // Traffic cars and racers use the same DrivableVehicle base type.
            // Pairwise cost is modest for the 11-car race grid.
            for (int i = 0; i < drivers.Count; i++)
            {
                Vehicle first = drivers[i].Vehicle;
                if (first == null || first.CurrentNode == null)
                    continue;

                for (int j = i + 1; j < drivers.Count; j++)
                {
                    Vehicle second = drivers[j].Vehicle;
                    if (second == null || second.CurrentNode == null)
                        continue;

                    // Cars on separate overpass levels must not hit each other.
                    if (Math.Abs(first.Position.Y - second.Position.Y) >
                        (first.CollisionHeight + second.CollisionHeight) * 0.5f)
                        continue;

                    var a = new CollisionFootprint(
                        new Vector2(first.Position.X, first.Position.Z),
                        new Vector2(first.Direction.X, first.Direction.Z),
                        first.CollisionWidth, first.CollisionLength);
                    var b = new CollisionFootprint(
                        new Vector2(second.Position.X, second.Position.Z),
                        new Vector2(second.Direction.X, second.Direction.Z),
                        second.CollisionWidth, second.CollisionLength);

                    if (!a.Overlaps(b, out Vector2 contact, out float depth))
                        continue;

                    // Resolve penetrations while allowing a tiny contact margin.
                    float separation = depth + 0.02f;
                    var offset = contact * (separation * 0.5f);
                    first.Position -= new Microsoft.Xna.Framework.Vector3(offset.X, 0, offset.Y);
                    second.Position += new Microsoft.Xna.Framework.Vector3(offset.X, 0, offset.Y);

                    // Simple impulse along the collision normal. Do not reverse
                    // an idle car merely because its bumper is touching another.
                    var firstHeading = new Vector2(first.Direction.X, first.Direction.Z);
                    var secondHeading = new Vector2(second.Direction.X, second.Direction.Z);
                    float relativeClosing = Vector2.Dot(
                        firstHeading * first.Speed - secondHeading * second.Speed, contact);
                    if (relativeClosing <= 0)
                        continue;

                    float massA = first is DrivableVehicle carA
                        ? Math.Max(1, carA.Descriptor.Mass) : 1500f;
                    float massB = second is DrivableVehicle carB
                        ? Math.Max(1, carB.Descriptor.Mass) : 1500f;
                    const float restitution = 0.08f;
                    float impulse = (1 + restitution) * relativeClosing /
                                    (1f / massA + 1f / massB);
                    float newSpeedA = first.Speed - impulse / massA *
                        Vector2.Dot(firstHeading, contact);
                    float newSpeedB = second.Speed + impulse / massB *
                        Vector2.Dot(secondHeading, contact);
                    first.Speed = Math.Max(0, newSpeedA);
                    second.Speed = Math.Max(0, newSpeedB);

                    // Subtle lateral response, never a full spin just from touching.
                    float sideA = firstHeading.X * contact.Y - firstHeading.Y * contact.X;
                    float sideB = secondHeading.X * contact.Y - secondHeading.Y * contact.X;
                    if (Math.Abs(sideA) > 0.2f && first is DrivableVehicle driveA)
                        driveA.RotateCarAfterCollision = -Math.Sign(sideA) * 0.035f;
                    if (Math.Abs(sideB) > 0.2f && second is DrivableVehicle driveB)
                        driveB.RotateCarAfterCollision = Math.Sign(sideB) * 0.035f;
                }
            }
        }
    }
}
