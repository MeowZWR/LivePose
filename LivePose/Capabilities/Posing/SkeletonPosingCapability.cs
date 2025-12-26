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
using LivePose.Config;
using LivePose.IPC;

namespace LivePose.Capabilities.Posing
{

    public class SkeletonPosingCapability : ActorCharacterCapability
    {
        private readonly SkeletonService _skeletonService;
        private readonly PosingService _posingService;
        private readonly IFramework _framework;
        private readonly HeelsService _heelsService;
        private readonly ConfigurationService _configurationService;
        private readonly IpcService _ipcService;
        
        public Skeleton? CharacterSkeleton { get; private set; }
        public Skeleton? MainHandSkeleton { get; private set; }
        public Skeleton? OffHandSkeleton { get; private set; }
        
        public Skeleton? OrnamentSkeleton { get; private set; }
        public Skeleton? MinionSkeleton { get; private set; }

        public bool CharacterHasTail { get; private set; }
        public bool CharacterIsIVCS { get; private set; }

        public IReadOnlyList<(Skeleton Skeleton, PoseInfoSlot Slot)> Skeletons => [.. new[] { (CharacterSkeleton, PoseInfoSlot.Character), (MainHandSkeleton, PoseInfoSlot.MainHand), (OffHandSkeleton, PoseInfoSlot.OffHand), (OrnamentSkeleton, PoseInfoSlot.Ornament), (MinionSkeleton, PoseInfoSlot.Minion) }.Where(s => s.Item1 != null).Cast<(Skeleton Skeleton, PoseInfoSlot Slot)>()];

        public PoseInfo PoseInfo { get; set; } = new PoseInfo();

        public Dictionary<(ushort, ushort), PoseInfo> BodyPoses { get; } = [];
        public Dictionary<ushort, PoseInfo> FacePoses { get; } = [];

        public Dictionary<uint, PoseInfo> MinionPoses { get; } = [];

        private readonly List<Action<Bone, BonePoseInfo>> _transitiveActions = [];

        public uint ActiveMinion = 0;

        public SkeletonPosingCapability(ActorEntity parent, SkeletonService skeletonService, PosingService posingService, HeelsService heelsService, IFramework framework, ConfigurationService configurationService, IpcService ipcService) : base(parent)
        {
            _skeletonService = skeletonService;
            _posingService = posingService;
            _framework = framework;
            _heelsService = heelsService;
            _configurationService = configurationService;
            _ipcService = ipcService;
            
            _skeletonService.SkeletonUpdateStart += OnSkeletonUpdateStart;
            _skeletonService.SkeletonUpdateEnd += OnSkeletonUpdateEnd;
            _configurationService.OnConfigurationChanged += OnConfigurationChanged;
            
            LoadCharacterConfiguration();
            
            OnConfigurationChanged();
        }

        private void OnConfigurationChanged() {
            if(Character.ObjectIndex == 0) {
                CursedMode = _configurationService.Configuration.Posing.CursedMode;
                if(_heelsService.IsAvailable) {
                    _heelsService.SetPlayerPoseTag();
                }
            }
        }

        public void ResetPose()
        {
            PoseInfo = new PoseInfo();
            
            if(GameObject.ObjectIndex == 0) {
                if(LivePose.TryGetService<HeelsService>(out var heelsService) && heelsService.IsAvailable) {
                    heelsService.SetPlayerPoseTag();
                }
            }
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
            RegisterTransitiveAction((bone, bonePoseInfo) => importer.ApplyBone(bone, bonePoseInfo));
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
            
            var ornamentSkeleton = OrnamentSkeleton;
            if(ornamentSkeleton != null)
            {
                foreach(var bone in ornamentSkeleton!.Bones)
                {
                    if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                        continue;

                    poseFile.Ornament[bone.Name] = bone.LastRawTransform;
                }
            }
            
            var minionSkeleton = MinionSkeleton;
            if(minionSkeleton != null) {
                foreach(var bone in minionSkeleton.Bones) {
                    if (bone.IsPartialRoot && !bone.IsSkeletonRoot) continue;
                    poseFile.Companion[bone.Name] = bone.LastRawTransform;
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

            if(OrnamentSkeleton != null && OrnamentSkeleton == bone.Skeleton) {
                return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Ornament);
            }

            if(MinionSkeleton != null && MinionSkeleton == bone.Skeleton) {
                return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Minion);
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
                PoseInfoSlot.Ornament => OrnamentSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
                PoseInfoSlot.Minion => MinionSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
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
                PoseInfoSlot.Ornament => OrnamentSkeleton?.GetFirstVisibleBone(name),
                PoseInfoSlot.Minion => MinionSkeleton?.GetFirstVisibleBone(name),
                _ => null,
            };
        }

        public (ushort, ushort) ActiveBodyTimelines { get; private set; }
        public ushort ActiveFaceTimeline { get; private set; }
        
        public bool CursedMode { get; set; }
        
        public unsafe void ApplyTimelinePose() {
            if(!IsReady) return;
            if(CursedMode) return;
            var chr = (Character*)Character.Address;
            var currentBodyPose = chr->Timeline.TimelineSequencer.GetSlotTimeline(0);
            var currentUpperBodyPose = chr->Timeline.TimelineSequencer.GetSlotTimeline(1);
            var currentFacePose =  chr->Timeline.TimelineSequencer.GetSlotTimeline(2);
            ApplyTimelinePose(currentBodyPose, currentUpperBodyPose, currentFacePose);
        }

        private void ApplyTimelinePose(ushort main, ushort upperBody, ushort face) {
            if(CursedMode) return;
            if(!IsReady) return;
            ActiveBodyTimelines = (main, upperBody);
            ActiveFaceTimeline = face;

            if(BodyPoses.TryGetValue(ActiveBodyTimelines, out var bodyPose)) {
                PoseInfo = bodyPose;
            } else {
                if(Character.ObjectIndex == 0) {
                    PoseInfo = BodyPoses[ActiveBodyTimelines] = new PoseInfo();
                } else {
                    PoseInfo = new PoseInfo();
                }
            }

            PoseInfo.Clear(FilterFaceBones);
                
            if(FacePoses.TryGetValue(ActiveFaceTimeline, out var facePose)) {
                PoseInfo.Overlay(facePose, FilterFaceBones);
            }
        }

        public unsafe void ApplyMinionPose() {
            var chr = (Character*)Character.Address;
            var minionObj = chr->CompanionObject;
            ApplyMinionPose(minionObj == null ? 0 : minionObj->BaseId);
        }
        
        private void ApplyMinionPose(uint id) {
            PoseInfo.Clear(b => b.Slot == PoseInfoSlot.Minion);
            ActiveMinion = id;
            if(MinionPoses.TryGetValue(id, out var minionPose)) {
                PoseInfo.Overlay(minionPose, b => b.Slot == PoseInfoSlot.Minion);
            }
        }
        
        public void UpdatePoseCache(bool announceToHeels = false) {
            if(CursedMode) return;

            _framework.RunOnFrameworkThread(() => {
                if(!IsReady) return;

                var facePose = PoseInfo.Clone(FilterFaceBones);
                if(facePose.IsOverridden()) {
                    FacePoses[ActiveFaceTimeline] = PoseInfo.Clone(FilterFaceBones);
                } else {
                    FacePoses.Remove(ActiveFaceTimeline);
                }
                
                if(ActiveMinion != 0) {
                    var minionPose = PoseInfo.Clone(b => b.Slot == PoseInfoSlot.Minion);
                    if(minionPose.IsOverridden()) {
                        MinionPoses[ActiveMinion] = PoseInfo.Clone(b => b.Slot == PoseInfoSlot.Minion);
                    } else {
                        MinionPoses.Remove(ActiveMinion);
                    }
                }
                    
                if(ActiveBodyTimelines != (0, 0)) {
                    var bodyPose = PoseInfo.Clone(FilterNonFaceBones);
                    if(bodyPose.IsOverridden()) {
                        BodyPoses[ActiveBodyTimelines] = bodyPose;
                    } else {
                        BodyPoses.Remove(ActiveBodyTimelines);
                    }
                }
                
                if(announceToHeels) {
                    _framework.RunOnTick(() => {
                        if(_heelsService.IsAvailable) {
                            _heelsService.SetPlayerPoseTag();

                        }
                    }, delayTicks: 1);
                }
            }).Wait();
        }
        
        private unsafe void UpdateCache() {
            if(!CursedMode && IsReady) {
                var chr = (Character*)Character.Address;
                var currentBodyPose = chr->Timeline.TimelineSequencer.GetSlotTimeline(0);
                var currentUpperBodyPose = chr->Timeline.TimelineSequencer.GetSlotTimeline(1);
                var currentFacePose =  chr->Timeline.TimelineSequencer.GetSlotTimeline(2);
                
                var minionObj = chr->CompanionObject;
                var minion = 0U;
                if(minionObj != null) {
                    minion = minionObj->BaseId;
                }
                
                if(ActiveBodyTimelines != (currentBodyPose, currentUpperBodyPose) || currentFacePose != ActiveFaceTimeline || ActiveMinion != minion) {
                    if (chr->ObjectIndex == 0) {
                        UpdatePoseCache();
                    }

                    ApplyTimelinePose(currentBodyPose, currentUpperBodyPose, currentFacePose);
                    ApplyMinionPose(minion);
                }
            }

            CharacterSkeleton = _skeletonService.GetSkeleton(Character.GetCharacterBase());
            MainHandSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.MainHand));
            OffHandSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.OffHand));
            OrnamentSkeleton = _skeletonService.GetSkeleton(Character.GetOrnamentBase());
            MinionSkeleton = _skeletonService.GetSkeleton(Character.GetMinionBase());

            _skeletonService.RegisterForFrameUpdate(CharacterSkeleton, this);
            _skeletonService.RegisterForFrameUpdate(MainHandSkeleton, this);
            _skeletonService.RegisterForFrameUpdate(OffHandSkeleton, this);
            _skeletonService.RegisterForFrameUpdate(OrnamentSkeleton, this);
            _skeletonService.RegisterForFrameUpdate(MinionSkeleton, this);

            CharacterHasTail = CharacterSkeleton?.GetFirstVisibleBone("n_sippo_a") != null;
            CharacterIsIVCS = CharacterSkeleton?.GetFirstVisibleBone("iv_ko_c_l") != null;
        }

        
        private readonly Dictionary<BonePoseInfoId, bool> isFaceBoneMap = new();
        private bool IsFaceBone(BonePoseInfoId obj) {
            if(obj.Slot != PoseInfoSlot.Character) return false;
            if(isFaceBoneMap.TryGetValue(obj, out var val)) return val;
            var skeleton = obj.Slot switch {
                PoseInfoSlot.Character => CharacterSkeleton,
                _ => null
            };

            var bone = skeleton?.GetFirstVisibleBone(obj.BoneName);
            isFaceBoneMap.TryAdd(obj, bone?.IsFaceBone ?? false);
            return bone?.IsFaceBone ?? false;
        }
        
        public bool FilterFaceBones(BonePoseInfoId obj) => IsFaceBone(obj);

        public bool FilterNonFaceBones(BonePoseInfoId obj) => !IsFaceBone(obj);

        private void OnSkeletonUpdateStart()
        {
            UpdateCache();
        }

        private void OnSkeletonUpdateEnd()
        {
            _transitiveActions.Clear();
        }

        public void LoadCharacterConfiguration() {
            if(_configurationService.Configuration.Posing.CursedMode) return;
            if(CharacterConfiguration is not NoCharacterConfiguration) {
                BodyPoses.Clear();
                FacePoses.Clear();
                MinionPoses.Clear();
                foreach(var pose in CharacterConfiguration.BodyPoses) {
                    BodyPoses[(pose.TimelineId, pose.SecondaryTimelineId)] = _ipcService.DeserializePose(pose.Pose);
                }
            
                foreach(var pose in CharacterConfiguration.FacePoses) {
                    FacePoses[pose.TimelineId] = _ipcService.DeserializePose(pose.Pose);
                }

                foreach(var pose in CharacterConfiguration.MinionPoses) {
                    MinionPoses[pose.Minion] = _ipcService.DeserializePose(pose.Pose);
                }
                
                ApplyTimelinePose();
                ApplyMinionPose();
            }
        }
        
        public void SaveCharacterConfiguration() {
            if(_configurationService.Configuration.Posing.CursedMode) return;
            UpdatePoseCache();
            
            var bodyPoses = new List<LivePoseCacheEntry>();
            var facePoses = new List<LivePoseCacheEntry>();
            var minionPoses = new List<LivePoseMinionEntry>();
            
            foreach(var (key, p) in BodyPoses) {
                var pose = _ipcService.SerializePose(this, p);
                if(pose.Count > 0) {
                    bodyPoses.Add(new LivePoseCacheEntry(key.Item1, key.Item2, pose));
                }
            }
        
            foreach(var (key, p) in FacePoses) {
                var pose = _ipcService.SerializePose(this, p);
                if(pose.Count > 0)
                    facePoses.Add(new LivePoseCacheEntry(key, pose));
            }

            foreach(var (id, p) in MinionPoses) {
                var pose = _ipcService.SerializePose(this, p);
                if(pose.Count > 0) {
                    minionPoses.Add(new LivePoseMinionEntry(id, pose));
                }
            }

            CharacterConfiguration.BodyPoses = bodyPoses;
            CharacterConfiguration.FacePoses = facePoses;
            CharacterConfiguration.MinionPoses = minionPoses;
            CharacterConfiguration.Save();
        }
        

        public override void Dispose()
        {
            _skeletonService.SkeletonUpdateStart -= OnSkeletonUpdateStart;
            _skeletonService.SkeletonUpdateEnd -= OnSkeletonUpdateEnd;
            _configurationService.OnConfigurationChanged -= OnConfigurationChanged;

            _transitiveActions.Clear();

            PoseInfo.Clear();
            base.Dispose();
        }
        
        public string? IpcDataJson { get; set; }
    }
}
