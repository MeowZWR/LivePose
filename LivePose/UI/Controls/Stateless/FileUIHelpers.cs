using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using LivePose.Capabilities.Posing;
using LivePose.Config;
using LivePose.Core;
using LivePose.Files;
using LivePose.Game.Posing;
using LivePose.Library;
using LivePose.Library.Filters;
using LivePose.UI.Controls.Core;
using LivePose.UI.Controls.Editors;
using OneOf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using LivePose.IPC;

namespace LivePose.UI.Controls.Stateless;

public class FileUIHelpers
{
    static bool freezeOnLoad = false;
    static bool smartDefaults = false;

    static bool doExpression = false;
    static bool doBody = false;
    static bool doTransform = false;
    static TransformComponents? transformComponents = null;
    public static void DrawImportPoseMenuPopup(PosingCapability capability, bool showImportOptions = true)
    {
        using var popup = ImRaii.Popup("DrawImportPoseMenuPopup");

        if(popup.Success)
        {
            var imIO = ImGui.GetIO();
            var _lastGlobalScale = imIO.FontGlobalScale;
            imIO.FontGlobalScale = 1f;

            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {
                var size = new Vector2(245, 400); //= ImGui.GetContentRegionAvail(); //ImGui.CalcTextSize("XXXX Freeze Actor on Import");

                size.Y = 44;

                var buttonSize = size / 8;

                ImGui.Checkbox("Freeze Actor on Import", ref freezeOnLoad);

                ImGui.Separator();

                using(ImRaii.Disabled(true))
                    ImGui.Checkbox("Smart Import", ref smartDefaults);

                transformComponents ??= capability.PosingService.DefaultImporterOptions.TransformComponents;

                using(ImRaii.Disabled(smartDefaults))
                {
                    using(ImRaii.Disabled(doExpression))
                    {
                        if(ImBrio.ToggelFontIconButton("ImportPosition", FontAwesomeIcon.ArrowsUpDownLeftRight, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Position), hoverText: "Import Position"))
                        {
                            if(transformComponents.Value.HasFlag(TransformComponents.Position))
                                transformComponents &= ~TransformComponents.Position;
                            else
                                transformComponents |= TransformComponents.Position;
                        }
                        ImGui.SameLine();
                        if(ImBrio.ToggelFontIconButton("ImportRotation", FontAwesomeIcon.ArrowsSpin, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Rotation), hoverText: "Import Rotation"))
                        {
                            if(transformComponents.Value.HasFlag(TransformComponents.Rotation))
                                transformComponents &= ~TransformComponents.Rotation;
                            else
                                transformComponents |= TransformComponents.Rotation;
                        }
                        ImGui.SameLine();
                        if(ImBrio.ToggelFontIconButton("ImportScale", FontAwesomeIcon.ExpandAlt, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Scale), hoverText: "Import Scale"))
                        {
                            if(transformComponents.Value.HasFlag(TransformComponents.Scale))
                                transformComponents &= ~TransformComponents.Scale;
                            else
                                transformComponents |= TransformComponents.Scale;
                        }
                    }

                    ImGui.SameLine();
                    if(ImBrio.ToggelFontIconButton("ImportTransform", FontAwesomeIcon.ArrowsToCircle, buttonSize, doTransform, hoverText: "Import Model Transform"))
                    {
                        doTransform = !doTransform;
                    }

                    if(smartDefaults == true)
                    {
                        transformComponents = null;
                    }
                }

                ImGui.Separator();

                if(ImBrio.ToggelButton("Import Body", new(size.X, 35), doBody))
                {
                    doBody = !doBody;
                }

                if(ImBrio.ToggelButton("Import Expression", new(size.X, 35), doExpression))
                {
                    doExpression = !doExpression;
                }

                using(ImRaii.Disabled(doExpression || doBody))
                {
                    if(ImBrio.Button("Import Options", FontAwesomeIcon.Cog, new(size.X, 25), centerTest: true, hoverText: "Import Options"))
                        ImGui.OpenPopup("import_optionsImportPoseMenuPopup");
                }

                ImGui.Separator();

                if(ImGui.Button("Import", new(size.X, 25)))
                {
                    ShowImportPoseModal(capability, freezeOnLoad: freezeOnLoad, transformComponents: transformComponents, applyModelTransformOverride: doTransform);
                }

                ImGui.Separator();

                if(ImGui.Button("Import A-Pose", new(size.X, 25)))
                {
                    capability.LoadResourcesPose("Data.BrioAPose.pose", freezeOnLoad: freezeOnLoad, asBody: true);
                    ImGui.CloseCurrentPopup();
                }

                if(ImGui.Button("Import T-Pose", new(size.X, 25)))
                {
                    capability.LoadResourcesPose("Data.BrioTPose.pose", freezeOnLoad: freezeOnLoad, asBody: true);
                    ImGui.CloseCurrentPopup();
                }
            }

            using(var popup2 = ImRaii.Popup("import_optionsImportPoseMenuPopup"))
            {
                if(popup2.Success && showImportOptions && LivePosePlugin.TryGetService<PosingService>(out var service))
                {
                    PosingEditorCommon.DrawImportOptionEditor(service.DefaultImporterOptions, true);
                }
            }

            ImGui.GetIO().FontGlobalScale = _lastGlobalScale;
        }
    }

    public static void ShowImportPoseModal(PosingCapability capability, PoseImporterOptions? options = null, bool asExpression = false,
        bool asBody = false, bool freezeOnLoad = false, TransformComponents? transformComponents = null, bool? applyModelTransformOverride = false)
    {
        TypeFilter filter = new("Poses", typeof(CMToolPoseFile), typeof(PoseFile));
        
        LibraryManager.GetWithFilePicker(filter, (r) =>
        {
            if(r is CMToolPoseFile cmPose)
            {
                ImportPose(capability, cmPose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
            }
            else if(r is PoseFile pose)
            {
                ImportPose(capability, pose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
            }
        });
        
    }

    private static void ImportPose(PosingCapability capability, OneOf<PoseFile, CMToolPoseFile> rawPoseFile, PoseImporterOptions? options = null,
        TransformComponents? transformComponents = null, bool? applyModelTransformOverride = false)
    {
        if(doBody && doExpression)
        {
            capability.ImportPose(rawPoseFile, options: capability.PosingService.DefaultIPCImporterOptions, asExpression: false, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: null, applyModelTransformOverride: applyModelTransformOverride);

            if(capability.GameObject.ObjectIndex == 0) {
                if(LivePosePlugin.TryGetService<HeelsService>(out var service) && service.IsAvailable) {
                    service.SetPlayerPoseTag();
                }
            }
            
            return;
        }

        if(doBody)
        {
            capability.ImportPose(rawPoseFile, options: null, asExpression: false, asBody: true, freezeOnLoad: freezeOnLoad,
                transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
        }
        else if(doExpression)
        {
            capability.ImportPose(rawPoseFile, options: null, asExpression: true, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: null, applyModelTransformOverride: null);
        }
        else
        {
            capability.ImportPose(rawPoseFile, options: options, asExpression: false, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
        }
        
        if(capability.GameObject.ObjectIndex == 0) {
            if(LivePosePlugin.TryGetService<HeelsService>(out var service) && service.IsAvailable) {
                service.SetPlayerPoseTag();
            }
        }
        
        
    }

    public static void ShowExportPoseModal(PosingCapability capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export Pose###export_pose", "Pose File (*.pose){.pose}", "brio", ".pose",
                (success, path) =>
                {
                    if(success)
                    {
                        if(!path.EndsWith(".pose"))
                            path += ".pose";

                        var directory = Path.GetDirectoryName(path);
                        if(directory is not null)
                        {
                            ConfigurationService.Instance.Configuration.LastExportPath = directory;
                            ConfigurationService.Instance.Save();
                        }

                        capability.ExportSavePose(path);
                    }
                }, ConfigurationService.Instance.Configuration.LastExportPath, true);
    }
}


