using LivePose.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using System;

namespace LivePose.Game.Posing;

public unsafe class ModelTransformService : IDisposable
{
    public unsafe Transform GetTransform(IGameObject go)
    {
        var native = go.Native();
        var drawObject = native->DrawObject;
        if(drawObject != null)
        {
            return *(Transform*)(&drawObject->Object.Position);
        }
        else
        {
            return new Transform()
            {
                Position = native->Position
            };
        }
        ;
    }

    public void Dispose() {
        
    }
}
