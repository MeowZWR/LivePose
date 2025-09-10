
using Dalamud.Game.ClientState.Objects.Types;
using LivePose.Capabilities.Core;
using LivePose.Config;
using LivePose.Entities.Actor;
using LivePose.Entities.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;


namespace LivePose.Entities;

public unsafe partial class EntityManager(IServiceProvider serviceProvider, ConfigurationService configurationService) : IDisposable
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Entity? RootEntity => null;

    private readonly List<EntityId> _selectedEntities = [];

    public IReadOnlyList<EntityId> SelectedEntityIds => _selectedEntities.AsReadOnly();

    // Primary selected entity id (first in the multi-selection list)
    public EntityId? SelectedEntityId => _selectedEntities.Count > 0 ? _selectedEntities[0] : (EntityId?)null;

    public Entity? SelectedEntity
    {
        get
        {
            if(SelectedEntityId.HasValue)
            {
                if(TryGetEntity(SelectedEntityId.Value, out var entity))
                {
                    return entity;
                }
            }
            return null;
        }
    }
    
    private readonly Dictionary<EntityId, Entity> _entityMap = [];

    private readonly ConfigurationService _configurationService = configurationService;

    public void AttachEntity(Entity entity, Entity? parent, bool autoDetach = false)
    {
        if(entity.Parent != null)
        {
            if(autoDetach)
            {
                DetachEntity(entity, false);
            }
            else
            {
                throw new InvalidOperationException("Entity is already attached to a parent.");
            }
        }

        parent ??= RootEntity;

        _entityMap[entity.Id] = entity;
        parent?.AddChild(entity);
        entity.OnAttached();
    }

    public void DetachEntity(Entity entity, bool dispose)
    {
        entity.OnDetached();
        _entityMap.Remove(entity.Id);
        entity.Parent?.RemoveChild(entity);
        if(dispose)
        {
            foreach(var child in entity.Children)
                DetachEntity(child, true);

            entity.Dispose();
        }
    }

    public bool TryGetEntity(EntityId id, [MaybeNullWhen(false)] out Entity entity)
    {
        return _entityMap.TryGetValue(id, out entity);
    }

    public bool TryGetEntity<T>(EntityId id, [MaybeNullWhen(false)] out T entity) where T : Entity
    {
        if(TryGetEntity(id, out var e))
        {
            if(e is T t)
            {
                entity = t;
                return true;
            }
        }
        entity = null;
        return false;
    }

    public Entity? GetEntity(EntityId id)
    {
        _entityMap.TryGetValue(id, out var entity);
        return entity;
    }

    public T? GetEntity<T>(EntityId id) where T : Entity
    {
        _entityMap.TryGetValue(id, out var entity);

        if(entity is T t)
        {
            return t;
        }
        return null;
    }

    public bool EntityExists(EntityId id)
    {
        return _entityMap.ContainsKey(id);
    }

    public void SetSelectedEntity(EntityId? id)
    {
        // Clear previous selection and select single id
        foreach(var eId in _selectedEntities.ToArray())
        {
            if(TryGetEntity(eId, out var ent))
                ent.OnDeselected();
        }

        _selectedEntities.Clear();
        if(id.HasValue)
            _selectedEntities.Add(id.Value);

        SelectedEntity?.OnSelected();
    }

    public void SetSelectedEntity(IGameObject go)
    {
        SetSelectedEntity(new EntityId(go));
    }

    public void AddSelectedEntity(EntityId id)
    {
        // If already the only selection, nothing to do
        if(_selectedEntities.Contains(id))
            return;

        // Deselect nothing; only call OnSelected for the entity being added
        if(TryGetEntity(id, out var ent))
        {
            _selectedEntities.Add(id);
            ent.OnSelected();
        }
    }

    public void RemoveSelectedEntity(EntityId id)
    {
        if(_selectedEntities.Contains(id))
        {
            if(TryGetEntity(id, out var ent))
                ent.OnDeselected();

            _selectedEntities.Remove(id);
        }
    }

    public void ClearSelectedEntities()
    {
        foreach(var eId in _selectedEntities.ToArray())
        {
            if(TryGetEntity(eId, out var ent))
                ent.OnDeselected();
        }
        _selectedEntities.Clear();
    }

    public bool SelectedHasCapability<T>(bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        return TryGetCapabilitiesFromSelectedEntities<T>(out _, considerChildren, considerParents);
    }

    public IEnumerable<ActorEntity> TryGetAllActors()
    {
        foreach(var entity in _entityMap.Values)
        {
            if(entity is ActorEntity actor)
            {
                if(actor.IsProp == false)
                    yield return actor;
            }
        }
    }

    public IEnumerable<IGameObject> TryGetAllActorsAsGameObject()
    {
        foreach(var entity in _entityMap.Values)
        {
            if(entity is ActorEntity actor)
            {
                yield return actor.GameObject;
            }
        }
    }

    public bool TryGetCapabilityFromSelectedEntity<T>([MaybeNullWhen(false)] out T capability, bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        if(TryGetCapabilitiesFromSelectedEntity<T>(out var capabilities, considerChildren, considerParents))
        {
            capability = capabilities.First();
            return true;
        }
        capability = null;
        return false;
    }

    public bool TryGetCapabilitiesFromSelectedEntity<T>([MaybeNullWhen(false)] out IEnumerable<T> capabilities, bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        capabilities = null;

        var selected = SelectedEntity;
        if(selected != null)
        {
            if(!selected.IsAttached)
                return false;

            if(selected.TryGetCapabilities<T>(out capabilities, considerChildren, considerParents))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetCapabilitiesFromSelectedEntities<T>([MaybeNullWhen(false)] out IEnumerable<T> capabilities, bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        var results = new List<T>();
        capabilities = results;

        foreach(var id in _selectedEntities)
        {
            if(!TryGetEntity(id, out var entity))
                continue;

            if(!entity.IsAttached)
                continue;

            if(entity.TryGetCapabilities<T>(out var caps, considerChildren, considerParents))
                results.AddRange(caps);
        }

        return results.Count != 0;
    }

    public void Dispose()
    {
        foreach(var entity in _entityMap.Values)
            entity.Dispose();
    }
}
