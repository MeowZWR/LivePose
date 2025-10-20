using System;
using LivePose.Resources;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using LivePose.Config;
using Newtonsoft.Json.Converters;

namespace LivePose.Game.Posing;

public class BoneCategories : IDisposable
{
    public IReadOnlyList<BoneCategory> Categories => _categories;

    private readonly List<BoneCategory> _categories = [];

    private ConfigurationService _configurationService;
    
    
    public BoneCategories()
    {
        if(!LivePose.TryGetService<ConfigurationService>(out var configurationService)) throw new Exception("Failed to get ConfigurationService");
        _configurationService = configurationService;
        
        configurationService.OnConfigurationChanged += ReloadBoneCategories;
        ReloadBoneCategories();
    }

    private void ReloadBoneCategories() {
        _categories.Clear();
        var categories = _configurationService.Configuration.BoneCategories;
        
        if(categories == null) {
            categories = [];
            LivePose.Log.Info("Loading BoneCategories from defaults.");
            var boneCategoryFile = ResourceProvider.Instance.GetResourceDocument<BoneCategoryFile>("Data.BoneCategories.json");
            foreach(var (id, entry) in boneCategoryFile.Categories)
            {
                if (id is "weapon" or "ornament" or "other") continue;
                var name = Localize.Get($"bone_categories.{id}", id);
                var category = new BoneCategory(id, name, entry.Type, entry.Bones);
                categories.Add(category);
            }

            _configurationService.Configuration.BoneCategories = _categories;
            _configurationService.Save();
        }
        


        foreach(var category in categories) {
            if (category.Id is "weapon" or "ornament" or "other") continue;
            _categories.Add(category);
        }
        
        _categories.Add(new BoneCategory("weapon", Localize.Get("bone_categories.weapon", "Weapons"), BoneCategoryTypes.Filter, []));
        _categories.Add(new BoneCategory("ornament", Localize.Get("bone_categories.ornament", "Ornaments"), BoneCategoryTypes.Filter, []));
        _categories.Add(new BoneCategory("other", Localize.Get("bone_categories.other", "Other"), BoneCategoryTypes.Filter, []));
    }

    private class BoneCategoryFile
    {
        public Dictionary<string, BoneCategoryFileEntry> Categories { get; set; } = [];

        public record class BoneCategoryFileEntry(BoneCategoryTypes Type, List<string> Bones);
    }
    
    public void Dispose() {
        _configurationService.OnConfigurationChanged -= ReloadBoneCategories;
    }
}

public enum BoneCategoryTypes
{
    [Description("Prefix Match")] Filter,
    [Description("Exact Match")] Exact,
}

public record class BoneCategory(string Id, string Name, BoneCategoryTypes Type, List<string> Bones);
