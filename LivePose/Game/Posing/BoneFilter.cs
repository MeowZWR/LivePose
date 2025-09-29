using LivePose.Game.Posing.Skeletons;
using System.Collections.Generic;
using System.Linq;

namespace LivePose.Game.Posing;

public class BoneFilter {
    private readonly PosingService _posingService;

    private readonly HashSet<string> _allowedCategories = [];

    private readonly HashSet<string> _excludedPrefixes = [];

    public IReadOnlyList<BoneCategories.BoneCategory> AllCategories => _posingService.BoneCategories.Categories;

    public BoneFilter(PosingService posingService) {
        _posingService = posingService;

        foreach(var category in _posingService.BoneCategories.Categories)
            _allowedCategories.Add(category.Id);
    }

    private Dictionary<string, bool> boneValidCache = new();
    public unsafe bool IsBoneValid(Bone bone, PoseInfoSlot slot, bool considerHidden = false) {
        bool foundBone = false;

        if(bone.IsHidden && !considerHidden)
            return false;

        // Look for excludes
        foreach(var excluded in _excludedPrefixes) {
            if(bone.Name.StartsWith(excluded))
                return false;
        }

        // Weapon bone names don't matter
        if(slot == PoseInfoSlot.MainHand || slot == PoseInfoSlot.OffHand)
            if(WeaponsAllowed)
                return true;
            else
                return false;

        if(boneValidCache.TryGetValue(bone.Name, out var valid)) return valid;
        boneValidCache[bone.Name] = true;

        // Check if the bone is in any of the categories and that category is visible
        foreach(var category in AllCategories) {
            if(category.Type != BoneCategories.BoneCategoryTypes.Filter)
                continue;

            foreach(var boneName in category.Bones) {
                if(bone.Name.StartsWith(boneName)) {
                    foundBone = true;

                    if(_allowedCategories.Any(x => category.Id == x))
                        return true;
                }
            }
        }

        // If we didn't find a bone, and the "other" category is visible, we should display it
        if(!foundBone && OtherAllowed)
            return true;

        boneValidCache[bone.Name] = false;
        return false;
    }

    public bool WeaponsAllowed => _allowedCategories.Any((x) => x == "weapon");

    public bool OtherAllowed => _allowedCategories.Any((x) => x == "other");

    public bool IsCategoryEnabled(string id) => _allowedCategories.Any((x) => x == id);
    public bool IsCategoryEnabled(BoneCategories.BoneCategory category) => _allowedCategories.Any(x => category.Id == x);

    public void DisableCategory(string id) {
        boneValidCache.Clear();
        _allowedCategories.Remove(id);
    }

    public void DisableCategory(BoneCategories.BoneCategory category) {
        boneValidCache.Clear();
        _allowedCategories.Remove(category.Id);
    }

    public void EnableCategory(string id) {
        boneValidCache.Clear();
        _allowedCategories.Add(id);
    }

    public void EnableCategory(BoneCategories.BoneCategory category) {
        boneValidCache.Clear();
        _allowedCategories.Add(category.Id);
    }

    public void AddExcludedPrefix(string bonePrefix) {
        boneValidCache.Clear();
        _excludedPrefixes.Add(bonePrefix);
    }

    public void EnableAll() {
        boneValidCache.Clear();
        _allowedCategories.Clear();
        foreach(var category in AllCategories)
            _allowedCategories.Add(category.Id);
    }

    public void DisableAll() {
        boneValidCache.Clear();
        _allowedCategories.Clear();
    }

    public void EnableOnly(string id) {
        boneValidCache.Clear();
        _allowedCategories.Clear();
        _allowedCategories.Add(id);
    }

    public void EnableOnly(BoneCategories.BoneCategory category) {
        boneValidCache.Clear();
        _allowedCategories.Clear();
        _allowedCategories.Add(category.Id);
    }
}
