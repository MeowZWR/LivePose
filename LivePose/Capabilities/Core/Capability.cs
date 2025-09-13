using LivePose.Entities.Core;
using System;

namespace LivePose.Capabilities.Core;

public abstract class Capability(Entity parent) : IDisposable
{
    public Entity Entity { get; } = parent;

    public virtual void OnEntitySelected()
    {
    }

    public virtual void OnEntityDeselected()
    {
    }

    public virtual void Dispose()
    {
        OnEntityDeselected();

        GC.SuppressFinalize(this);
    }
}
