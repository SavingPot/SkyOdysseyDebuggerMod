using GameCore;
using GameCore.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine;
using GameCore.High;
using SP.Tools;
using SP.Tools.Unity;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Debugger
{
    [CreateAfterSceneLoad(SceneNames.MainMenu)]
    public class ModMaker : MonoBehaviour
    {
        static readonly string[] exceptedDlls = new string[]
        {
            "where-allocation",
            "Unity.2D.Tilemap.Extra",
            "UnityEngine.AIModule",
            "UnityEngine.AnimationModule",
            "UnityEngine.ARModule",
            "UnityEngine.ClothModule",
            "UnityEngine.NVIDIAModule",
            "UnityEngine.ProfilerModule",
            "UnityEngine.SpriteShapeModule",
            "UnityEngine.SubsystemsModule",
            "UnityEngine.TerrainModule",
            "UnityEngine.TerrainPhysicsModule",
            "UnityEngine.TilemapModule",
            "UnityEngine.UmbraModule",
            "UnityEngine.VRModule",
            "UnityEngine.WindModule",
            "UnityEngine.XRModule",
            "UnityEngine.AssetBundleModule",
            // "Unity.Services.Core.Analytics",
            // "Unity.Services.Core.Configuration",
            // "Unity.Services.Core.Device",
            // "Unity.Services.Core",
            // "Unity.Services.Core.Environments",
            // "Unity.Services.Core.Environments.Internal",
            // "Unity.Services.Core.Internal",
            // "Unity.Services.Core.Networking",
            // "Unity.Services.Core.Registration",
            // "Unity.Services.Core.Scheduler",
            // "Unity.Services.Core.Telemetry",
            // "Unity.Services.Core.Threading",
            // "Unity.Burst.Cecil",
            // "Unity.Burst.Cecil.Mdb",
            // "Unity.Burst.Cecil.Pdb",
            // "Unity.Burst.Cecil.Rocks",
            // "Unity.Burst",
            // "Unity.Burst.Unsafe",
        };
        static readonly string[] exceptedHeads = new string[]
        {
            "System",
            "Sirenix"
        };

        private InternalUIAdder _iua;
        public InternalUIAdder iua
        {
            get
            {
                if (!_iua)
                    _iua = FindObjectOfType<InternalUIAdder>();

                return _iua;
            }
        }
        public static readonly Vector2 addContentButtonSize = new Vector2(30, 30);

        public PanelIdentity editContentPanel;
        public PanelIdentity createModPanel;
        public string editingModInfoPath;
        public string editingModPath { get => IOTools.GetParentPath(editingModInfoPath); }
        public KeyValuePair<string, GameLang> editingLang;
        public GameLang_Text editingText;

        public void AddChangeContentButton()
        {
            for (int i = 0; i < iua.modScrollView.content.childCount; i++)
            {
                if (UObjectTools.GetComponent<ImageTextButtonIdentity>(iua.modScrollView.content.GetChild(i), out var itb))
                {
                    int index = i;

                    //设置模组介绍文本的大小以给按钮移出空间
                    itb.buttonTextDown.SetSizeDelta(itb.buttonTextDown.sd.x - addContentButtonSize.x, itb.buttonTextDown.sd.y);
                    itb.buttonTextDown.SetAPos(itb.buttonTextDown.ap.x - addContentButtonSize.x / 2, itb.buttonTextDown.ap.y);

                    var dir = iua.modDirs[index];

                    var editButton = GameUI.AddButton(UPC.lowerRight, "debugger:button.edit_content_" + dir.info.id, itb, "ori:square_button");
                    editButton.rt.SetParent(itb.rt);
                    string str = GameUI.CompareText("debugger:button.mod_edit_content.text").text;
                    editButton.buttonText.AfterRefreshing += tc => tc.text.text = str;
                    editButton.sd = addContentButtonSize;
                    editButton.SetAPos(-editButton.sd.x / 2, editButton.sd.y / 2);
                    editButton.buttonText.sd = editButton.sd;
                    editButton.buttonText.text.SetFontSize(6);
                    editButton.AddMethod(() =>
                    {
                        editingModInfoPath = dir.path;
                        editContentPanel.CustomMethod("debugger:refresh", null);
                        GameUI.ChangeMain(IdentityCenter.ComparePanelIdentity("ori:panel.mod_view").gameObject, editContentPanel.gameObject);
                    });
                    editButton.RefreshUI();
                }
            }
        }

        public void Start()
        {
            Scene scene = GScene.active;

            if (scene.name == SceneNames.MainMenu)
            {
                if (!iua)
                    return;

                //获取内置添加器的 UI
                var modViewPanel = IdentityCenter.ComparePanelIdentity("ori:panel.mod_view");
                var modViewPort = IdentityCenter.CompareScrollViewIdentity("ori:scrollview.mod_view");
                var modOpenSourceButton = IdentityCenter.CompareButtonIdentity("ori:button.mod_open_source");




                #region 模组项目
                ButtonIdentity generateCSProjectContentButton = GameUI.AddButton(UPC.middle, "debugger:button.generate_cs_project_content", modViewPanel).AddMethod(() =>
                {
                    string[] preselectedList = GInit.coreDlls;

                    // Console.WriteLine("输入想导入的其他 DLL 的目录 (没有请直接点击回车~~)");

                    // //获取自定义导入 DLL
                    // string input = null;

                    // while (input == null)
                    // {
                    //     input = Console.ReadLine();

                    //     if (input == null || input == "")
                    //     {
                    //         break;
                    //     }

                    //     if (!File.Exists(input))
                    //     {
                    //         Console.WriteLine("目录不正确, 未检测到文件");
                    //         input = null;
                    //     }
                    //     else if (IOTools.GetExtension(input) != ".dll")
                    //     {
                    //         Console.WriteLine("文件后缀名必须为 .dll");
                    //         input = null;
                    //     }
                    //     else
                    //     {
                    //         dllPaths.Add(input);
                    //         input = null;
                    //     }
                    // }

                    // Console.Write("\n\n\n\n\n");

                    var temp = new List<string>();

                    for (int i = 0; i < preselectedList.Length; i++)
                    {
                        string path = preselectedList[i];
                        string fileName = IOTools.GetFileName(path, false);

                        if (!exceptedDlls.Any(str => str == fileName))
                        {
                            if (!fileName.Contains('.'))
                                temp.Add(path);
                            else
                            {
                                string head = fileName.Split('.')[0];

                                if (!exceptedHeads.Any(str => str == head))
                                    temp.Add(path);
                            }
                        }
                    }

                    StringBuilder content = new();
                    content.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
                    content.AppendLine("  <PropertyGroup>");
                    content.AppendLine("    <LangVersion>9.0</LangVersion>");
                    content.AppendLine("    <TargetFramework>net4.7.1</TargetFramework>");
                    content.AppendLine("  </PropertyGroup>");
                    content.AppendLine("  <ItemGroup>");

                    for (int i = 0; i < temp.Count; i++)
                    {
                        string path = temp[i];
                        content.AppendLine($"    <Reference Include=\"{IOTools.GetFileName(path, false)}\">");
                        content.AppendLine($"      <HintPath>{path}</HintPath>");
                        content.AppendLine($"    </Reference>");
                    }

                    content.AppendLine($"  </ItemGroup>");
                    content.AppendLine($"</Project>");

                    GUIUtility.systemCopyBuffer = content.ToString();
                    Debug.Log("内容已被写入剪贴板, 请自行写入到 *.csproj 文件中");
                });
                generateCSProjectContentButton.SetAPosOnBySizeDown(modOpenSourceButton, 10); ;


                #endregion




                #region 编辑模组内容
                #region 第一界面
                //添加 UI
                //生成编辑模组内容界面
                editContentPanel = GameUI.AddPanel("debugger:panel.edit_content", GameUI.canvasRT, true);
                var openInfoPanel = GameUI.AddPanel("debugger:panel.edit_content.info", GameUI.canvasRT, true);
                var openAssetsPanel = GameUI.AddPanel("debugger:panel.edit_content.assets", GameUI.canvasRT, true);

                var openAssetsButton = GameUI.AddButton(UPC.middle, "debugger:button.edit_content.assets", editContentPanel);
                var openDataButton = GameUI.AddButton(UPC.middle, "debugger:button.edit_content.data", editContentPanel);
                var openInfoButton = GameUI.AddButton(UPC.middle, "debugger:button.edit_content.info", editContentPanel);
                var applyAllContentChanges = GameUI.AddButton(UPC.down, "debugger:button.edit_content.apply", editContentPanel);

                //设置 UI 位置
                openAssetsButton.SetAPos(0, openDataButton.sd.y / 2 + 30);
                openDataButton.SetAPos(0, openAssetsButton.ap.y - openDataButton.sd.y / 2 - 30);
                applyAllContentChanges.SetAPos(0, applyAllContentChanges.sd.y / 2 + 30);

                //添加方法
                openAssetsButton.AddMethod(() =>
                {
                    GameUI.ChangeMain(editContentPanel, openAssetsPanel);
                });
                openInfoButton.AddMethod(() =>
                {
                    GameUI.ChangeMain(editContentPanel, openInfoPanel);
                });
                applyAllContentChanges.AddMethod(() =>
                {
                    iua.RefreshMods();
                    iua.RefreshModView();

                    GameUI.ChangeMain(editContentPanel, modViewPanel);
                });
                #endregion



                #region 信息
                //添加 UI
                var openInfo_id = GameUI.AddInputField(UPC.middle, "debugger:button.edit_content.info.id", openInfoPanel);
                var openInfo_version = GameUI.AddInputField(UPC.middle, "debugger:button.edit_content.info.version", openInfoPanel);
                var openInfo_description = GameUI.AddInputField(UPC.middle, "debugger:button.edit_content.info.description", openInfoPanel);
                var openInfo_name = GameUI.AddInputField(UPC.middle, "debugger:button.edit_content.info.name", openInfoPanel);
                var apply_info = GameUI.AddButton(UPC.down, "debugger:button.edit_content.info.apply", openInfoPanel);

                //设置 UI 位置
                openInfoButton.SetAPosOnBySizeDown(openDataButton, 15);
                openInfo_description.ap = Vector2.zero;
                openInfo_name.SetAPosOnBySizeDown(openInfo_description, 15);
                openInfo_version.SetAPosOnBySizeUp(openInfo_description, 15);
                openInfo_id.SetAPosOnBySizeUp(openInfo_version, 15);
                apply_info.SetAPosOnBySizeUp(apply_info, 15);

                //添加方法
                //应用修改并返回
                apply_info.AddMethod(() =>
                {
                    //根据文本框内容初始化模组信息
                    Mod_Info modInfo = new Mod_Info()
                    {
                        id = openInfo_id.field.text,
                        version = openInfo_version.field.text,
                        name = openInfo_name.field.text,
                        description = openInfo_description.field.text,
                    };
                    SaveInfoToFile(ModFactory.GetInfoPath(editingModPath), modInfo);

                    GameUI.ChangeMain(openInfoPanel, editContentPanel);
                });
                #endregion



                #region 资源
                #region 资源-文本
                #region 资源-文本-第一界面
                //添加 UI
                //用于显示模组的所有文本
                var openAssetsTextsPanel = GameUI.AddPanel("debugger:panel.edit_content.assets.langs", GameUI.canvasRT, true);
                var openAssets_textsButton = GameUI.AddButton(UPC.middle, "debugger:button.edit_content.assets.langs", openAssetsPanel);
                var apply_assets = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.apply", openAssetsPanel);

                //用于显示模组的一个文本中所有文本
                var openAssetsTextsTextsShowPanel = GameUI.AddPanel("debugger:panel.edit_content.assets.langs.texts", GameUI.canvasRT, true);



                //应用模组的文本修改
                var apply_texts = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.langs.apply", openAssetsTextsPanel);
                var modTextsViewPort = GameUI.AddScrollView(UPC.middle, "debugger:scrollview.edit_content.assets.langs", openAssetsTextsPanel);



                //设置 UI 位置
                apply_texts.SetAPos(0, apply_texts.sd.y / 2 + 30);
                openAssets_textsButton.ap = Vector2.zero;
                apply_assets.SetAPos(0, apply_assets.sd.y / 2 + 30);



                //添加方法
                apply_texts.AddMethod(() =>
                {
                    SaveEditingTextToLangThenFile();
                    modTextsViewPort.CustomMethod("debugger:refresh", null);
                    GameUI.ChangeMain(openAssetsTextsPanel, openAssetsPanel);
                });
                //应用修改并返回
                openAssets_textsButton.AddMethod(() =>
                {
                    GameUI.ChangeMain(openAssetsPanel, openAssetsTextsPanel);
                });
                apply_assets.AddMethod(() =>
                {
                    GameUI.ChangeMain(openAssetsPanel, editContentPanel);
                });
                #endregion

                #region 创建新文本
                //添加 UI
                var createNewTextPanel = GameUI.AddPanel("debugger:panel.edit_content.assets.langs.create", GameUI.canvasRT, true);

                var createNewTextButton = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.langs.create", openAssetsTextsPanel);

                var applyCreateNewTextButton = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.langs.create.apply", createNewTextPanel);
                var cancelCreateNewTextButton = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.langs.create.cancel", createNewTextPanel);

                var ifCreateTextId = GameUI.AddInputField(UPC.middle, "debugger:if.edit_content.assets.langs.create.id", createNewTextPanel);
                var ifCreateTextFile = GameUI.AddInputField(UPC.middle, "debugger:if.edit_content.assets.langs.create.file", createNewTextPanel);

                //设置 UI 位置
                createNewTextButton.SetAPosOnBySizeUp(createNewTextButton, 65);
                applyCreateNewTextButton.SetAPosOnBySizeUp(applyCreateNewTextButton, 15);
                cancelCreateNewTextButton.SetAPosOnBySizeUp(applyCreateNewTextButton, 15);
                ifCreateTextId.SetAPosY(ifCreateTextId.sd.y / 2 + 30);
                ifCreateTextFile.SetAPosOnBySizeUp(ifCreateTextId, 15);

                //添加方法
                createNewTextButton.AddMethod(() => GameUI.ChangeMain(openAssetsTextsPanel, createNewTextPanel));
                applyCreateNewTextButton.AddMethod(() =>
                {
                    if (ifCreateTextId.field.text.IsNullOrWhiteSpace() || ifCreateTextFile.field.text.IsNullOrWhiteSpace())
                        return;

                    GameLang newText = new GameLang()
                    {
                        id = ifCreateTextId.field.text,
                    };

                    SaveLangToFile(Path.Combine(ModFactory.GetLangsPath(editingModPath), ifCreateTextFile.field.text + ".json"), newText);
                    modTextsViewPort.CustomMethod("debugger:refresh", null);

                    GameUI.ChangeMain(createNewTextPanel, openAssetsTextsPanel);
                });
                cancelCreateNewTextButton.AddMethod(() => GameUI.ChangeMain(createNewTextPanel, openAssetsTextsPanel));
                #endregion
                #endregion
                #endregion
                #endregion



                #region 创建模组
                #region 编辑
                //添加 UI
                var openAssetsLangsTextsEditPanel = GameUI.AddPanel("debugger:panel.edit_content.assets.langs.texts.edit", GameUI.canvasRT, true);

                var createNewLangTextPanel = GameUI.AddPanel("debugger:panel.edit_content.assets.langs.texts.create", GameUI.canvasRT, true);

                var ifEditTextId = GameUI.AddInputField(UPC.middle, "debugger:if.edit_content.assets.langs.texts.edit.id", openAssetsLangsTextsEditPanel);
                var ifEditTextText = GameUI.AddInputField(UPC.middle, "debugger:if.edit_content.assets.langs.texts.edit.text", openAssetsLangsTextsEditPanel);
                var applyEditText = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.langs.texts.edit.apply", openAssetsLangsTextsEditPanel);

                var applyTextChildEdit = GameUI.AddButton(UPC.down, $"debugger:button.edit_content.assets.langs.texts.child.edit.apply", openAssetsTextsTextsShowPanel);
                var modTextsEditViewPort = GameUI.AddScrollView(UPC.middle, $"debugger:scrollview.edit_content.assets.langs.texts.child.edit", openAssetsTextsTextsShowPanel);

                #region 创建子文本
                var createNewTextTextButton = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.langs.texts.create", openAssetsTextsTextsShowPanel);
                var ifCreateTextTextId = GameUI.AddInputField(UPC.middle, "debugger:if.edit_content.assets.langs.texts.create.id", createNewLangTextPanel);
                var ifCreateTextTextText = GameUI.AddInputField(UPC.middle, "debugger:if.edit_content.assets.langs.texts.create.text", createNewLangTextPanel);
                var applyCreateNewTextTextButton = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.langs.texts.create.apply", createNewLangTextPanel);
                var cancelCreateNewTextTextButton = GameUI.AddButton(UPC.down, "debugger:button.edit_content.assets.langs.texts.create.cancel", createNewLangTextPanel);

                createNewTextTextButton.SetAPos(0, applyEditText.sd.y / 2 + 80);
                ifCreateTextTextId.SetAPos(0, ifCreateTextTextId.sd.y / 2 + 30);
                ifCreateTextTextText.SetAPos(0, ifCreateTextTextId.ap.y - ifCreateTextTextText.sd.y / 2 - 30);
                applyCreateNewTextTextButton.SetAPos(0, applyCreateNewTextTextButton.sd.y / 2 + 30);
                cancelCreateNewTextTextButton.SetAPos(0, applyCreateNewTextTextButton.ap.y + cancelCreateNewTextTextButton.sd.y / 2 + 30);

                applyCreateNewTextTextButton.AddMethod(() =>
                {
                    if (ifCreateTextTextId.field.text.IsNullOrWhiteSpace() || ifCreateTextTextText.field.text.IsNullOrWhiteSpace())
                        return;

                    editingText = new GameLang_Text()
                    {
                        id = ifCreateTextTextId.field.text,
                        text = ifCreateTextTextText.field.text
                    };
                    SaveEditingTextToLangThenFile();
                    modTextsViewPort.CustomMethod("debugger:refresh", null);

                    GameUI.ChangeMain(createNewLangTextPanel, openAssetsTextsTextsShowPanel);
                });
                createNewTextTextButton.AddMethod(() =>
                {
                    GameUI.ChangeMain(openAssetsTextsTextsShowPanel, createNewLangTextPanel);
                });
                cancelCreateNewTextTextButton.AddMethod(() =>
                {
                    GameUI.ChangeMain(createNewLangTextPanel, openAssetsTextsTextsShowPanel);
                });
                #endregion

                //设置 UI 位置
                applyEditText.SetAPos(0, applyEditText.sd.y / 2 + 30);
                ifEditTextId.SetAPos(0, ifEditTextId.sd.y / 2 + 30);
                ifEditTextText.SetAPosOnBySizeDown(ifEditTextId, 15);
                applyTextChildEdit.SetAPos(0, applyTextChildEdit.sd.y / 2 + 30);
                applyTextChildEdit.AddMethod(() =>
                {
                    GameUI.ChangeMain(openAssetsTextsTextsShowPanel, openAssetsTextsPanel);
                });

                //添加方法
                //应用文本的文本修改
                applyEditText.AddMethod(() =>
                {
                    editingText.id = ifEditTextId.field.text;
                    editingText.text = ifEditTextText.field.text;

                    SaveEditingTextToLangThenFile();
                    modTextsViewPort.CustomMethod("debugger:refresh", null);

                    GameUI.ChangeMain(openAssetsLangsTextsEditPanel, openAssetsTextsTextsShowPanel);
                });
                modTextsViewPort.CustomMethod += (id, _) =>
                {
                    if (id == "debugger:refresh")
                    {
                        modTextsViewPort.Clear();

                        foreach (var cf in IOTools.GetFilesInFolder(ModFactory.GetLangsPath(editingModPath), true, "json"))
                        {
                            //如果不是 Json 就跳过避免抛出问题
                            if (!JsonTools.IsJsonByPath(cf))
                                continue;

                            //加载文本
                            JObject jo = JsonTools.LoadJObjectByPath(cf);
                            var tempText = ModLoading.LoadText(jo);

                            var show = GameUI.AddButton(UPC.middle, $"debugger:itb.edit_content.assets.langs_texts_{tempText.id}", openAssetsTextsPanel);
                            show.buttonText.AfterRefreshing += t => t.text.text = tempText.id;
                            modTextsViewPort.AddChild(show);




                            show.AddMethod(() =>
                            {
                                editingLang = new KeyValuePair<string, GameLang>(cf, tempText);
                                modTextsEditViewPort.Clear();
                                for (int i = 0; i < tempText.texts.Count; i++)
                                {
                                    int index = i;

                                    var tempB = GameUI.AddButton(UPC.middle, $"debugger:itb.edit_content.assets.langs.texts_{tempText.id}_{tempText.texts[index].id}");
                                    tempB.buttonText.AfterRefreshing += t => t.text.text = $"{tempText.texts[index].text} ({tempText.texts[index].id})";

                                    modTextsEditViewPort.AddChild(tempB);

                                    tempB.AddMethod(() =>
                                    {
                                        editingText = tempText.texts[index];
                                        ifEditTextId.field.text = tempText.texts[index].id;
                                        ifEditTextText.field.text = tempText.texts[index].text;
                                        GameUI.ChangeMain(openAssetsTextsTextsShowPanel, openAssetsLangsTextsEditPanel);
                                    });
                                }

                                GameUI.ChangeMain(openAssetsTextsPanel, openAssetsTextsTextsShowPanel);
                            });
                        }
                    }
                };
                #endregion
                #endregion






                //添加刷新编辑界面回调
                editContentPanel.CustomMethod += (id, _) =>
                {
                    if (id == "debugger:refresh")
                    {
                        #region 获取目录
                        string modInfoPath = ModFactory.GetInfoPath(editingModPath);
                        string modIconPath = ModFactory.GetIconPath(editingModPath);

                        string modAudioPath = ModFactory.GetAudioPath(editingModPath);
                        string modLangsPath = ModFactory.GetLangsPath(editingModPath);
                        string modTexturesPath = ModFactory.GetTexturesPath(editingModPath);
                        string modAudioSettingsPath = ModFactory.GetAudioSettingsPath(editingModPath);
                        string modTextureSettingsPath = ModFactory.GetTextureSettingsPath(editingModPath);

                        string modCraftingRecipesPath = ModFactory.GetCraftingRecipesPath(editingModPath);
                        string modScriptsPath = ModFactory.GetScriptsPath(editingModPath);
                        string modBlocksPath = ModFactory.GetBlocksPath(editingModPath);
                        string modBiomesPath = ModFactory.GetBiomePath(editingModPath);
                        string modEntitiesPath = ModFactory.GetEntitiesPath(editingModPath);
                        string modItemsPath = ModFactory.GetItemsPath(editingModPath);
                        string modStructurePath = ModFactory.GetStructurePath(editingModPath);
                        #endregion

                        #region 排查目录
                        if (!Directory.Exists(editingModPath))
                        {
                            Debug.LogError("模组目录不存在");
                            return;
                        }
                        if (!File.Exists(modInfoPath))
                        {
                            Debug.LogError("模组没有信息文件");
                            return;
                        }
                        #endregion

                        #region 加载目录
                        Mod_Info info = ModLoading.LoadInfo(JsonTools.LoadJObjectByPath(modInfoPath), modIconPath);
                        #endregion

                        #region 将默认值写入    
                        openInfo_id.field.text = info.id;
                        openInfo_name.field.text = info.name;
                        openInfo_description.field.text = info.description;
                        openInfo_version.field.text = info.version;
                        #endregion

                        #region 显示文本
                        modTextsViewPort.CustomMethod("debugger:refresh", null);
                        #endregion
                    }
                };
            }
        }

        public void SaveInfoToFile(string path, Mod_Info info)
        {
            IOTools.CreateText(path, JsonTools.ConvertJsonString(ModCreate.ToJContainer(info).ToString(Formatting.None))).Dispose();
        }

        public void SaveLangToFile(string path, GameLang text)
        {
            IOTools.CreateText(path, JsonTools.ConvertJsonString(ModCreate.ToJContainer(text).ToString(Formatting.None))).Dispose();
        }

        public void SaveEditingLangToFile()
        {
            if (editingLang.Value?.id == null)
            {
                Debug.LogWarning($"{nameof(editingLang)} 为空");
                return;
            }

            SaveLangToFile(editingLang.Key, editingLang.Value);
        }

        public void BackEditingTextToLang()
        {
            if (editingText?.id == null || editingLang.Value?.id == null)
            {
                Debug.LogWarning($"{nameof(editingText)} 或 {nameof(editingLang)} 为空");
                return;
            }

            for (int i = 0; i < editingLang.Value.texts.Count; i++)
            {
                if (editingLang.Value.texts[i].id == editingText.id)
                {
                    editingLang.Value.texts[i] = editingText;
                    return;
                }
            }

            editingLang.Value.texts.Add(editingText);
        }

        public void SaveEditingTextToLangThenFile()
        {
            BackEditingTextToLang();
            SaveEditingLangToFile();
        }
    }
}
