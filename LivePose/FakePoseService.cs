using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace LivePose;

public class FakePoseService(IClientState clientState) : IDisposable {
    
    private readonly HashSet<ushort> posedObjects = [ 0, 1 ];
    private readonly HashSet<ushort> ownedObjects = [ 0, 1 ];

    public bool IsPosed(IGameObject go)
    {
        if(clientState.IsGPosing) return false;
        return posedObjects.Contains(go.ObjectIndex);
    }

    public void SetPosed(IGameObject go, bool isPosed) {
        switch (isPosed) {
            case false:
                posedObjects.Remove(go.ObjectIndex);
                break;
            case true:
                posedObjects.Add(go.ObjectIndex);
                break;
        }
    }
    
    public void Dispose()
    {
        posedObjects.Clear();
        ownedObjects.Clear();
    }
}
