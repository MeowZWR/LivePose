using LivePose.Game.Actor.Extensions;
using LivePose.Capabilities.Actor;
using LivePose.Entities.Actor;
using LivePose.Files;
using LivePose.Game.Actor.Appearance;
using LivePose.Game.Posing;
using LivePose.Game.Posing.Skeletons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using LivePose.IPC;

namespace LivePose.Capabilities.Posing
{

    public class SkeletonPosingCapability : ActorCharacterCapability
    {
        private readonly SkeletonService _skeletonService;
        private readonly PosingService _posingService;
        private readonly IFramework _framework;
        private readonly HeelsService _heelsService;


        public Skeleton? CharacterSkeleton { get; private set; }
        public Skeleton? MainHandSkeleton { get; private set; }
        public Skeleton? OffHandSkeleton { get; private set; }

        public bool CharacterHasTail { get; private set; }
        public bool CharacterIsIVCS { get; private set; }

        public IReadOnlyList<(Skeleton Skeleton, PoseInfoSlot Slot)> Skeletons => [.. new[] { (CharacterSkeleton, PoseInfoSlot.Character), (MainHandSkeleton, PoseInfoSlot.MainHand), (OffHandSkeleton, PoseInfoSlot.OffHand) }.Where(s => s.Item1 != null).Cast<(Skeleton Skeleton, PoseInfoSlot Slot)>()];

        public PoseInfo PoseInfo { get; set; } = new PoseInfo();

        public Dictionary<(ushort, ushort), PoseInfo> BodyPoses { get; } = [];
        public Dictionary<ushort, PoseInfo> FacePoses { get; } = [];

        private readonly List<Action<Bone, BonePoseInfo>> _transitiveActions = [];


        public SkeletonPosingCapability(ActorEntity parent, SkeletonService skeletonService, PosingService posingService, HeelsService heelsService, IFramework framework) : base(parent)
        {
            _skeletonService = skeletonService;
            _posingService = posingService;
            _framework = framework;
            _heelsService = heelsService;

            _skeletonService.SkeletonUpdateStart += OnSkeletonUpdateStart;
            _skeletonService.SkeletonUpdateEnd += OnSkeletonUpdateEnd;

        }

        public void ResetPose()
        {
            if(GameObject.ObjectIndex == 0) {
                if(LivePose.TryGetService<HeelsService>(out var heelsService) && heelsService.IsAvailable) {
                    heelsService.SetPlayerPoseTag();
                }
            }
            PoseInfo.Clear();
        }

        public void RegisterTransitiveAction(Action<Bone, BonePoseInfo> action)
        {
            _transitiveActions.Add(action);
        }

        public void ExecuteTransitiveActions(Bone bone, BonePoseInfo poseInfo)
        {
            _transitiveActions.ForEach(a => a(bone, poseInfo));
        }

        public void ImportSkeletonPose(PoseFile poseFile, PoseImporterOptions options, bool expressionPhase = false)
        {
            var importer = new PoseImporter(poseFile, options, expressionPhase);
            RegisterTransitiveAction(importer.ApplyBone);
        }

        public void ExportSkeletonPose(PoseFile poseFile)
        {
            var skeleton = CharacterSkeleton;
            if(skeleton != null)
            {
                foreach(var bone in CharacterSkeleton!.Bones)
                {
                    if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                        continue;

                    poseFile.Bones[bone.Name] = bone.LastRawTransform;
                }
            }

            var mainHandSkeleton = MainHandSkeleton;
            if(mainHandSkeleton != null)
            {
                foreach(var bone in mainHandSkeleton!.Bones)
                {
                    if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                        continue;

                    poseFile.MainHand[bone.Name] = bone.LastRawTransform;
                }
            }

            var offHandSkeleton = OffHandSkeleton;
            if(offHandSkeleton != null)
            {
                foreach(var bone in offHandSkeleton!.Bones)
                {
                    if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                        continue;

                    poseFile.OffHand[bone.Name] = bone.LastRawTransform;
                }
            }
        }

        public unsafe BonePoseInfo GetBonePose(BonePoseInfoId bone)
        {
            return PoseInfo.GetPoseInfo(bone);
        }

        public unsafe BonePoseInfo GetBonePose(Bone bone)
        {
            if(CharacterSkeleton != null && CharacterSkeleton == bone.Skeleton)
            {
                return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Character);
            }

            if(MainHandSkeleton != null && MainHandSkeleton == bone.Skeleton)
            {
                return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.MainHand);
            }

            if(OffHandSkeleton != null && OffHandSkeleton == bone.Skeleton)
            {
                return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.OffHand);
            }

            return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Unknown);
        }

        public Bone? GetBone(BonePoseInfoId? id)
        {
            if(id == null)
                return null;

            return id.Value.Slot switch
            {
                PoseInfoSlot.Character => CharacterSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
                PoseInfoSlot.MainHand => MainHandSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
                PoseInfoSlot.OffHand => OffHandSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
                _ => null,
            };
        }

        public Bone? GetBone(string name, PoseInfoSlot slot)
        {
            return slot switch
            {
                PoseInfoSlot.Character => CharacterSkeleton?.GetFirstVisibleBone(name),
                PoseInfoSlot.MainHand => MainHandSkeleton?.GetFirstVisibleBone(name),
                PoseInfoSlot.OffHand => OffHandSkeleton?.GetFirstVisibleBone(name),
                _ => null,
            };
        }

        private (ushort, ushort) activeBodyPose;
        private ushort activeFacePose;
        
        public unsafe void ApplyTimelinePose() {
            var chr = (Character*)Character.Address;
            var currentBodyPose = chr->Timeline.TimelineSequencer.GetSlotTimeline(0);
            var currentUpperBodyPose = chr->Timeline.TimelineSequencer.GetSlotTimeline(1);
            var currentFacePose =  chr->Timeline.TimelineSequencer.GetSlotTimeline(2);
            ApplyTimelinePose(currentBodyPose, currentUpperBodyPose, currentFacePose);
        }

        private void ApplyTimelinePose(ushort main, ushort upperBody, ushort face) {
            activeBodyPose = (main, upperBody);
            activeFacePose = face;

            if(BodyPoses.TryGetValue(activeBodyPose, out var bodyPose)) {
                PoseInfo = bodyPose;
            } else {
                PoseInfo = BodyPoses[activeBodyPose] = new PoseInfo();
            }

            PoseInfo.Clear(FilterFaceBones);
                
            if(!FacePoses.TryGetValue(activeFacePose, out var facePose)) {
                facePose = FacePoses[activeFacePose] = new PoseInfo();
            }

            PoseInfo.Overlay(facePose, FilterFaceBones);
        }
        
        private unsafe void UpdateCache() {
            var chr = (Character*)Character.Address;
            var currentBodyPose = chr->Timeline.TimelineSequencer.GetSlotTimeline(0);
            var currentUpperBodyPose = chr->Timeline.TimelineSequencer.GetSlotTimeline(1);
            var currentFacePose =  chr->Timeline.TimelineSequencer.GetSlotTimeline(2);

            if(activeBodyPose != (currentBodyPose, currentUpperBodyPose) || currentFacePose != activeFacePose) {
                if (chr->ObjectIndex == 0) {
                    if (activeFacePose != 0)
                        FacePoses[activeFacePose] = PoseInfo.Clone(FilterFaceBones);
                    if (activeBodyPose != (0, 0))
                        BodyPoses[activeBodyPose] = PoseInfo.Clone();


                    _framework.RunOnTick(() => {
                        if(_heelsService.IsAvailable) {
                            _heelsService.SetPlayerPoseTag();
                            
                        }
                    }, delayTicks: 1);


                }

                ApplyTimelinePose(currentBodyPose, currentUpperBodyPose, currentFacePose);
            }
            
            CharacterSkeleton = _skeletonService.GetSkeleton(Character.GetCharacterBase());
            MainHandSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.MainHand));
            OffHandSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.OffHand));

            _skeletonService.RegisterForFrameUpdate(CharacterSkeleton, this);
            _skeletonService.RegisterForFrameUpdate(MainHandSkeleton, this);
            _skeletonService.RegisterForFrameUpdate(OffHandSkeleton, this);

            CharacterHasTail = CharacterSkeleton?.GetFirstVisibleBone("n_sippo_a") != null;
            CharacterIsIVCS = CharacterSkeleton?.GetFirstVisibleBone("iv_ko_c_l") != null;
        }

        private bool FilterFaceBones(BonePoseInfoId obj) {
            var skeleton = obj.Slot switch {
                PoseInfoSlot.Character => CharacterSkeleton,
                PoseInfoSlot.MainHand => MainHandSkeleton,
                PoseInfoSlot.OffHand => OffHandSkeleton,
                _ => null
            };

            var bone = skeleton?.GetFirstVisibleBone(obj.BoneName);
            if(bone == null) return false;

            return bone.IsFaceBone;
        }

        private void OnSkeletonUpdateStart()
        {
            UpdateCache();
        }

        private void OnSkeletonUpdateEnd()
        {
            _transitiveActions.Clear();
        }

        public override void Dispose()
        {
            _skeletonService.SkeletonUpdateStart -= OnSkeletonUpdateStart;
            _skeletonService.SkeletonUpdateEnd -= OnSkeletonUpdateEnd;

            _transitiveActions.Clear();

            PoseInfo.Clear();
            base.Dispose();
        }
    }
}
