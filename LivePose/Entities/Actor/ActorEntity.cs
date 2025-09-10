using LivePose.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using LivePose.Capabilities.Actor;
using LivePose.Capabilities.Posing;
using LivePose.Config;
using LivePose.Entities.Core;
using LivePose.Game.Actor;
using LivePose.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LivePose.Entities.Actor
{
    public class ActorEntity(IGameObject gameObject, IServiceProvider provider) : Entity(new EntityId(gameObject), provider)
    {
        public readonly IGameObject GameObject = gameObject;

        private readonly ConfigurationService _configService = provider.GetRequiredService<ConfigurationService>();

        public string RawName = "";
        public override string FriendlyName
        {
            get
            {
                if(string.IsNullOrEmpty(RawName))
                {
                    return GameObject.GetFriendlyName();
                }

                return GameObject.GetAsCustomName(RawName);
            }
            set
            {
                RawName = value;
            }
        }
        public override FontAwesomeIcon Icon => IsProp ? FontAwesomeIcon.Cube : GameObject.GetFriendlyIcon();

        public unsafe override bool IsVisible => true;

        public override EntityFlags Flags => EntityFlags.AllowDoubleClick | EntityFlags.DefaultOpen;

        public override int ContextButtonCount => 1;

        public bool IsProp => ActorType == ActorType.Prop;

        public ActorType ActorType => GetActorType();

        private ActorType GetActorType()
        {
            return ActorType.BrioActor;
        }

        public override void OnDoubleClick()
        {
            var aac = GetCapability<ActorAppearanceCapability>();
            RenameActorModal.Open(aac.Actor);
        }
        
        public override void OnAttached()
        {
            AddCapability(ActivatorUtilities.CreateInstance<ActorAppearanceCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<SkeletonPosingCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<PosingCapability>(_serviceProvider, this));
            AddCapability(ActionTimelineCapability.CreateIfEligible(_serviceProvider, this));
        }
    }
}
