using LivePose.Game.Actor.Appearance;
using LivePose.Game.Actor.Interop;

namespace LivePose.Core;


public static class BrioUtilities
{
    public static float DegreesToRadians(float degrees)
    {
        if(degrees == 0)
            return 0;

        return degrees * (float)(System.Math.PI / 180);
    }

    public static float RadiansToDegrees(float radians)
    {
        if(radians == 0)
            return 0;

        return radians * (float)(180 / System.Math.PI);
    }
}
