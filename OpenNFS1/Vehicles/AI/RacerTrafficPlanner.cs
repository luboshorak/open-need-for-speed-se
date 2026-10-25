using System;
using System.Collections.Generic;
namespace OpenNFS1.Vehicles.AI
{
    internal static class RacerTrafficPlanner
    {
        internal readonly record struct Obstacle(float Along, float Side, float Width, float Closing, float Length);
        internal readonly record struct Decision(int Lane, float Brake);
        public static Decision Plan(int lane, int minimum, float[] offsets, IReadOnlyList<Obstacle> obstacles)
        {
            float Risk(int candidate)
            {
                float risk=0;
                foreach(var o in obstacles)
                {
                    if (o.Along < -o.Length || Math.Abs(o.Side-offsets[candidate])>=o.Width) continue;
                    float clearance=Math.Max(0,o.Along-o.Length);
                    float time=o.Closing>1 ? clearance/o.Closing : float.PositiveInfinity;
                    if (clearance<5) risk=Math.Max(risk,1);
                    else if(time<2) risk=Math.Max(risk,(2-time)/2);
                }
                return risk;
            }
            int best=lane; float current=Risk(lane), score=current;
            if(current>.1f)
            foreach(int candidate in new[]{lane-1,lane+1})
            {
                if(candidate<minimum || candidate>4) continue;
                bool beside=false;
                foreach(var o in obstacles)
                    if(Math.Abs(o.Along)<o.Length+12 && Math.Abs(o.Side-offsets[candidate])<o.Width) beside=true;
                if(beside) continue;
                float value=Risk(candidate)+.05f;
                if(value<score) { best=candidate;score=value; }
            }
            // Until the lane change has cleared the obstacle, continue braking.
            return new Decision(best,current>.3f ? Math.Clamp(current,0,1) : 0);
        }
    }
}
