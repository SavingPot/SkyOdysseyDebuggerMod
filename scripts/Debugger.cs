using GameCore;
using GameCore.High;
using GameCore.UI;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Debugger
{
    public class Core_Chunk
    {
        public LineRenderer GetLineRenderer(Chunk chunk)
        {
            LineRenderer lineRenderer = chunk.GetComponent<LineRenderer>();

            if (lineRenderer)
                return lineRenderer;

            float width = 0.07f;
            //Color color = new(contentLayer.layer / 20, contentLayer.layer / 20, contentLayer.layer / 20);
            Color color = Color.white;

            lineRenderer = chunk.gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 5;
            lineRenderer.sortingOrder = 10;
            lineRenderer.SetPositions(new Vector3[5] { chunk.leftUpPoint, chunk.leftDownPoint, chunk.rightDownPoint, chunk.rightUpPoint, chunk.leftUpPoint });
            lineRenderer.SetMaterialToSpriteDefault();
            lineRenderer.SetWidth(width);
            lineRenderer.SetColor(color);

            return lineRenderer;
        }

        public void SetLineRendererActivity(Chunk chunk, bool enabled)
        {
            GetLineRenderer(chunk).enabled = enabled;
        }

        public void SetAll(bool active)
        {
            for (int l = 0; l < Map.instance.chunks.Count; l++)
            {
                SetLineRendererActivity(Map.instance.chunks[l], active);
            }
        }
    }

    public class LogShower
    {
        public ImageIdentity bg;
        public TextIdentity text;
        public ButtonIdentity button;

        public LogShower(ImageIdentity bg, TextIdentity text, ButtonIdentity button)
        {
            this.bg = bg;
            this.text = text;
            this.button = button;
        }
    }

    public static class Core
    {
        private static Canvas m_mainCanvas;
        public static Canvas GetMainCanvas()
        {
            if (!m_mainCanvas)
            {
                m_mainCanvas = GameObject.Instantiate(GInit.instance.canvasPrefab);
                m_mainCanvas.gameObject.name = "DebuggerCanvas-Main";
                var scaler = m_mainCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = GameUI.canvasScaler.uiScaleMode;
                scaler.referenceResolution = GameUI.canvasScaler.referenceResolution;

                GameObject.DontDestroyOnLoad(m_mainCanvas.gameObject);
            }

            return m_mainCanvas;
        }

        private static Canvas m_frequentSimpleCanvas;
        public static Canvas GetFrequentSimpleCanvas()
        {
            if (!m_frequentSimpleCanvas)
            {
                m_frequentSimpleCanvas = GameObject.Instantiate(GInit.instance.canvasPrefab);
                m_frequentSimpleCanvas.gameObject.name = "DebuggerCanvas-FrequentSimple";
                var scaler = m_frequentSimpleCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = GameUI.canvasScaler.uiScaleMode;
                scaler.referenceResolution = GameUI.canvasScaler.referenceResolution;

                GameObject.DontDestroyOnLoad(m_frequentSimpleCanvas.gameObject);
            }

            return m_frequentSimpleCanvas;
        }

        public static Core_Chunk chunk = new();
        public static PanelIdentity logPanel;
        public static ScrollViewIdentity logScrollView;
        public static ScrollViewIdentity buttonScrollView;
        public static bool enabled = true;
        public static int inputButtonsHeight = 30;

        public static List<LogShower> logShowers = new();
        public static ImageIdentity detailedLogImage;
        public static TextIdentity detailedLogText;
        public static PanelIdentity logToolsPanel;
        public static ButtonIdentity clearLogsButton;
        public static ToggleIdentity mostDownLogsToggle;
        public static ToggleIdentity doActiveToggle;
        public static InputButtonIdentity randomUpdateIB;
        public static InputButtonIdentity time24IB;
        public static ImageIdentity normalLogCountImage;
        public static ImageIdentity errorLogCountImage;
        public static ImageIdentity exceptionLogCountImage;
        public static ImageIdentity warningLogCountImage;
        public static TextIdentity normalLogCountText;
        public static TextIdentity errorLogCountText;
        public static TextIdentity exceptionLogCountText;
        public static TextIdentity warningLogCountText;
        public static TextIdentity gameStatusText;
        public static ImageIdentity logPreviewImage;
        public static TextIdentity logPreviewText;

        private static int _normalLogCount;
        private static int _errorLogCount;
        private static int _exceptionLogCount;
        private static int _warningLogCount;
        public static int normalLogCount
        {
            get => _normalLogCount;
            set
            {
                _normalLogCount = value;
                Core.normalLogCountText.RefreshUI();
            }
        }
        public static int errorLogCount
        {
            get => _errorLogCount;
            set
            {
                _errorLogCount = value;
                Core.errorLogCountText.RefreshUI();
            }
        }
        public static int exceptionLogCount
        {
            get => _exceptionLogCount;
            set
            {
                _exceptionLogCount = value;
                Core.exceptionLogCountText.RefreshUI();
            }
        }
        public static int warningLogCount
        {
            get => _warningLogCount;
            set
            {
                _warningLogCount = value;
                Core.warningLogCountText.RefreshUI();
            }
        }
        public const int logToolsHeight = 30;

        public static Tools tools => Tools.instance;

        public static void DebuggerCanvasActiveControl()
        {
            if (Keyboard.current != null && Keyboard.current.rightShiftKey.isPressed && Keyboard.current.mKey.wasPressedThisFrame)
            {
                //bool newState = !GetDebuggerCanvas().gameObject.activeSelf;
                //GetDebuggerCanvas().gameObject.SetActive(newState);

                bool newState = !GetMainCanvas().enabled;
                GetMainCanvas().enabled = newState;
                GetFrequentSimpleCanvas().enabled = newState;

                //避免在非游戏场景生成 Blockmap
                //if (Blockmap.HasInstance())
                //    chunk.SetAll(newState);
            }
        }

        public static void CheckSelectedObject()
        {
            if (!doActiveToggle)
                return;

            if (!doActiveToggle.toggle.isOn && GameUI.eventSystem.currentSelectedGameObject == doActiveToggle.gameObject)
            {
                GameUI.eventSystem.SetSelectedGameObject(null);
            }
        }

        public static void SetUIsToFirst()
        {
            GetMainCanvas().enabled = false;
            GetMainCanvas().enabled = true;

            GetFrequentSimpleCanvas().enabled = false;
            GetFrequentSimpleCanvas().enabled = true;
        }

        #region 初始化UI
        public const int logWindowSize = 400;

        public static void InitLogPanel()
        {
            if (logPanel)
                return;

            logPanel = GameUI.AddPanel("debugger:panel.log_show", GetMainCanvas().transform);
            logPanel.SetAnchorMinMax(0, 1);
            logPanel.SetSizeDelta(logWindowSize, logWindowSize);
            logPanel.panelImage.SetAlpha(0.5f);

            SetLogPanelTransform();

            if (!enabled)// || Application.isEditor)
                logPanel.gameObject.SetActive(false);
        }

        public static void SetLogPanelTransform()
        {
            logPanel.SetAPos(logPanel.sd.x / 2, -logPanel.sd.y / 2);
            logPanel.rt.localScale = Vector3.one;
        }

        public static void InitLogScrollView()
        {
            if (logScrollView)
                return;

            InitLogPanel();

            logScrollView = GameUI.AddScrollView(UPC.upperLeft, "debugger:scrollview_log_show", logPanel);
            logScrollView.SetSizeDelta(logPanel.sd.x, logPanel.sd.y - logToolsHeight);
            logScrollView.SetAPos(logScrollView.sd.x / 2, -logScrollView.sd.y / 2 - logToolsHeight);
            logScrollView.gridLayoutGroup.cellSize = new Vector2(logScrollView.gridLayoutGroup.cellSize.x, 45);
            logScrollView.gridLayoutGroup.spacing = new Vector2(0, 2.5f);
            logScrollView.viewportImage.color = new Color32(0, 0, 0, 1);
            logScrollView.scrollViewImage.color = new Color32(0, 0, 0, 0);
        }

        public static void InitButtonScrollView()
        {
            if (buttonScrollView)
                return;

            InitLogPanel();

            buttonScrollView = GameUI.AddScrollView(UPC.upperLeft, "debugger:scrollview_button_show", logPanel);
            buttonScrollView.SetSizeDelta(logPanel.sd.x, logPanel.sd.y);
            buttonScrollView.SetAPosOnBySizeRight(logScrollView, 0);
            buttonScrollView.gridLayoutGroup.cellSize = new Vector2(buttonScrollView.gridLayoutGroup.cellSize.x, 45);
            buttonScrollView.gridLayoutGroup.spacing = new Vector2(0, 2.5f);
            buttonScrollView.viewportImage.color = new Color32(0, 0, 0, 1);
            buttonScrollView.scrollViewImage.color = new Color32(0, 0, 0, 0);
        }

        public static void InitRandomUpdateIB()
        {
            if (randomUpdateIB)
                return;

            InitLogPanel();

            randomUpdateIB = GameUI.AddInputButton(UPC.middle, "debugger:ib.random_update", logPanel);
            randomUpdateIB.SetSize(new Vector2(logPanel.sd.x, inputButtonsHeight));
            randomUpdateIB.SetAPos(0, -logPanel.sd.y / 2 - inputButtonsHeight / 2);
            randomUpdateIB.field.field.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;
            randomUpdateIB.AddMethod(() =>
            {
                if (GFiles.world != null)
                {
                    if (!byte.TryParse(randomUpdateIB.field.field.text, out byte result))
                    {
                        Debug.LogWarning($"输入的数字有问题哦, 记得在 {byte.MinValue} 和 {byte.MaxValue} 之间, 不要有字母");
                        return;
                    }

                    RandomUpdater.randomUpdateProbability = result;
                }
            });
        }

        public static void InitTime24IB()
        {
            if (time24IB)
                return;

            InitLogPanel();

            time24IB = GameUI.AddInputButton(UPC.middle, "debugger:ib.time24", logPanel);
            time24IB.SetSize(new Vector2(logPanel.sd.x, inputButtonsHeight));
            time24IB.SetAPos(0, -logPanel.sd.y / 2 - (inputButtonsHeight / 2) * 3);
            time24IB.field.field.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;
            time24IB.AddMethod(() =>
            {
                if (GFiles.world != null)
                {
                    GTime.time24Format = time24IB.field.field.text.ToInt();
                }
            });
        }

        public static void InitLogToolsPanel()
        {
            if (logToolsPanel)
                return;

            InitLogPanel();

            logToolsPanel = GameUI.AddPanel("debugger:panel.log_tools", logPanel);
            logToolsPanel.panelImage.SetColor(0.75f, 0.75f, 0.75f, 0.75f);
            logToolsPanel.SetAPos(logPanel.sd.x / 2, -logToolsHeight / 2);
            logToolsPanel.SetSizeDelta(logWindowSize, logToolsHeight);
            logToolsPanel.SetAnchorMinMax(0, 1);
        }

        public static void IntiDetailedLog()
        {
            if (detailedLogImage && detailedLogText)
                return;

            InitLogToolsPanel();
            InitLogPreviewText();

            detailedLogImage = GameUI.AddImage(UPC.down, "debugger:image.log_detailed", "ori:square_button_flat", logPanel);
            detailedLogImage.image.SetColor(0.6f, 0.6f, 0.6f, 1);
            detailedLogImage.image.raycastTarget = false;

            detailedLogText = GameUI.AddText(UPC.middle, "debugger:text.log_detailed", detailedLogImage);
            detailedLogText.text.SetFontSize(11);
            detailedLogText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
            detailedLogText.text.overflowMode = TMPro.TextOverflowModes.Page;
            detailedLogText.text.margin = new Vector4(30, 30, 30, 30);
            detailedLogText.text.raycastTarget = false;
            detailedLogText.autoCompareText = false;
            detailedLogText.sd = new(detailedLogImage.sd.x, 800);

            SetDetailedLogPos();


            detailedLogImage.gameObject.AddComponent<RectMask2D>();
            var slider = GameUI.AddSlider(UPC.left, "debugger:slider.log_detailed", detailedLogImage);
            slider.slider.value = 0;
            slider.slider.transform.localRotation = Quaternion.Euler(0, 0, -90);
            GameObject.Destroy(slider.text.gameObject);
            slider.SetAPosX(slider.sd.y / 2);
            slider.slider.onValueChanged.AddListener(value =>
            {
                detailedLogText.SetAPosY(0 + detailedLogText.sd.y * value - (detailedLogText.sd.y - detailedLogImage.sd.y) / 2);
            });
        }

        public static void SetDetailedLogPos()
        {
            detailedLogImage.SetSizeDelta(logWindowSize, logWindowSize);
            detailedLogImage.SetAPos(0, (-logPanel.sd.y / 2));
            if (GScene.name == SceneNames.GameScene)
                detailedLogImage.AddAPosY(-inputButtonsHeight * 2);
            detailedLogText.sd = detailedLogImage.sd;
        }

        public static void InitClearLogsButton()
        {
            if (clearLogsButton)
                return;

            InitLogScrollView();
            InitLogToolsPanel();

            clearLogsButton = GameUI.AddButton(UPC.left, "debugger:button.clear_logs", logToolsPanel);
            clearLogsButton.SetSizeDelta(70, logToolsHeight);
            clearLogsButton.buttonText.sd = clearLogsButton.sd;
            clearLogsButton.SetAPos(clearLogsButton.sd.x / 2, 0);
            clearLogsButton.buttonText.text.SetFontSize(14);
            clearLogsButton.AddMethod(() =>
            {
                normalLogCount = 0;
                errorLogCount = 0;
                exceptionLogCount = 0;
                warningLogCount = 0;
                logScrollView.Clear();
                logShowers.Clear();
            });
        }

        public static void InitMostDownLogsButton()
        {
            if (mostDownLogsToggle)
                return;

            InitLogScrollView();
            InitLogToolsPanel();

            mostDownLogsToggle = GameUI.AddToggle(UPC.left, "debugger:toggle.most_down_logs", logToolsPanel);
            mostDownLogsToggle.SetScale(new Vector2(90, logToolsHeight));
            mostDownLogsToggle.text.text.SetFontSize(14);
            mostDownLogsToggle.SetAPosOnBySizeRight(clearLogsButton, 5);
            mostDownLogsToggle.text.text.alignment = TMPro.TextAlignmentOptions.Center;
            logScrollView.OnUpdate += SetToMostDownPos;

            void SetToMostDownPos(ScrollViewIdentity ci)
            {
                if (mostDownLogsToggle.toggle.isOn)
                    ci.scrollRect.verticalNormalizedPosition = 0;
            }
        }

        public static void InitDoActiveToggle()
        {
            if (doActiveToggle)
                return;

            InitMostDownLogsButton();

            doActiveToggle = GameUI.AddToggle(UPC.left, "debugger:toggle.do_active", logToolsPanel);
            doActiveToggle.SetScale(new Vector2(90, logToolsHeight));
            doActiveToggle.canvasGroup.ignoreParentGroups = true;
            doActiveToggle.SetAPosOnBySizeRight(mostDownLogsToggle, 5);
            doActiveToggle.text.text.SetFontSize(14);
            doActiveToggle.text.text.alignment = TMPro.TextAlignmentOptions.Center;

            doActiveToggle.AddMethod(v =>
            {
                logPanel.canvasGroup.interactable = v;
            });
        }

        public const int logPreviewHeight = 25;

        public static void InitLogPreviewImage()
        {
            if (logPreviewImage)
                return;

            InitLogToolsPanel();

            logPreviewImage = GameUI.AddImage(UPC.stretchBottom, "debugger:image.log_preview", "ori:button_flat", GetMainCanvas().transform);
            logPreviewImage.image.sprite = null;
            SetLogPreviewImageTransform();
            logPreviewImage.image.color = new Color(0.4f, 0.4f, 0.4f, 0.75f);
            logPreviewImage.SetSizeDelta(0, Core.logPreviewHeight);
            logPreviewImage.SetAPos(0, logPreviewImage.sd.y / 2);
            logPreviewImage.image.raycastTarget = false;

            if (!enabled)// || Application.isEditor)
                logPreviewImage.gameObject.SetActive(false);
        }

        public static void InitLogPreviewText()
        {
            if (logPreviewText)
                return;

            InitLogPreviewImage();

            logPreviewText = GameUI.AddText(UPC.stretchDouble, "debugger:text.log_preview", logPreviewImage);
            logPreviewText.text.alignment = TMPro.TextAlignmentOptions.Left;
            logPreviewText.text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            logPreviewText.text.SetFontSize(12);
            logPreviewText.SetSizeDelta(0, 0);
        }

        public static void SetLogPreviewImageTransform()
        {
            logPreviewImage.SetAPos(0, 0);
            logPreviewImage.rt.localScale = Vector3.one;
        }

        public static void InitNormalLogCountImage()
        {
            if (normalLogCountImage)
                return;

            InitLogToolsPanel();

            normalLogCountImage = GameUI.AddImage(UPC.upperRight, "debugger:image.normal_log_count", "debugger:normal_log_icon", logToolsPanel);
            normalLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            normalLogCountImage.SetAPos(-normalLogCountImage.sd.x / 2, -normalLogCountImage.sd.y / 2);
            normalLogCountImage.image.raycastTarget = false;
        }

        public static void InitErrorLogCountImage()
        {
            if (errorLogCountImage)
                return;

            InitLogToolsPanel();

            errorLogCountImage = GameUI.AddImage(UPC.upperRight, "debugger:image.error_log_count", "debugger:error_log_icon", logToolsPanel);
            errorLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            errorLogCountImage.SetAPosOnBySizeLeft(normalLogCountImage, 1);
            errorLogCountImage.image.raycastTarget = false;
        }

        public static void InitExceptionLogCountImage()
        {
            if (exceptionLogCountImage)
                return;

            InitLogToolsPanel();

            exceptionLogCountImage = GameUI.AddImage(UPC.upperRight, "debugger:image.exception_log_count", "debugger:exception_log_icon", logToolsPanel);
            exceptionLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            exceptionLogCountImage.SetAPosOnBySizeLeft(errorLogCountImage, 1);
            exceptionLogCountImage.image.raycastTarget = false;
        }

        public static void InitWarningLogCountImage()
        {
            if (warningLogCountImage)
                return;

            InitLogToolsPanel();

            warningLogCountImage = GameUI.AddImage(UPC.upperRight, "debugger:image.warning_log_count", "debugger:warning_log_icon", logToolsPanel);
            warningLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            warningLogCountImage.SetAPosOnBySizeLeft(exceptionLogCountImage, 1);
            warningLogCountImage.image.raycastTarget = false;
        }



        public static void InitNormalLogCountText()
        {
            if (normalLogCountText)
                return;

            InitNormalLogCountImage();

            normalLogCountText = GameUI.AddText(UPC.middle, "debugger:text.normal_log_count", normalLogCountImage);
            normalLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            normalLogCountText.AfterRefreshing += t =>
            {
                t.text.text = normalLogCount.ToString();
            };
            normalLogCountText.text.raycastTarget = false;
        }

        public static void InitErrorLogCountText()
        {
            if (errorLogCountText)
                return;

            InitErrorLogCountImage();

            errorLogCountText = GameUI.AddText(UPC.middle, "debugger:text.error_log_count", errorLogCountImage);
            errorLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            errorLogCountText.AfterRefreshing += t =>
            {
                t.text.text = errorLogCount.ToString();
            };
            errorLogCountText.text.raycastTarget = false;
        }

        public static void InitExceptionLogCountText()
        {
            if (exceptionLogCountText)
                return;

            InitExceptionLogCountImage();

            exceptionLogCountText = GameUI.AddText(UPC.middle, "debugger:text.exception_log_count", exceptionLogCountImage);
            exceptionLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            exceptionLogCountText.AfterRefreshing += t =>
            {
                t.text.text = exceptionLogCount.ToString();
            };
            exceptionLogCountText.text.raycastTarget = false;
        }

        public static void InitWarningLogCountText()
        {
            if (warningLogCountText)
                return;

            InitWarningLogCountImage();

            warningLogCountText = GameUI.AddText(UPC.middle, "debugger:text.warning_log_count", warningLogCountImage);
            warningLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            warningLogCountText.AfterRefreshing += t =>
            {
                t.text.text = warningLogCount.ToString();
            };
            warningLogCountText.text.raycastTarget = false;
        }

        private static StringBuilder gameStatusSB = new StringBuilder();

        public static void InitGameStatusText()
        {
            if (gameStatusText)
                return;

            InitLogPanel();

            gameStatusText = GameUI.AddText(UPC.upperLeft, "debugger:text.game_status", GetFrequentSimpleCanvas().transform);
            gameStatusText.text.SetFontSize(12);
            gameStatusText.SetSizeDelta(600, logPanel.sd.y);
            gameStatusText.SetAPos(logPanel.ap.x + (gameStatusText.sd.x / 2) + (logPanel.sd.x / 2) + 10, (-gameStatusText.sd.y / 2 - 10));
            gameStatusText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
            gameStatusText.AfterRefreshing += t =>
            {
                try
                {
                    gameStatusSB.Clear();

                    gameStatusSB.AppendLine($"时间: {GTime.time24Format} ({GTime.time}/{GTime.timeOneDay}) - 流速:{GTime.timeSpeed}");
                    gameStatusSB.AppendLine($"随机更新几率: {RandomUpdater.randomUpdateProbability}");

                    if (Player.GetLocal(out Player p))
                    {
                        gameStatusSB.AppendLine($"玩家位置: {(Vector2)p.transform.position}");
                        gameStatusSB.AppendLine($"玩家速度: {p.rb.velocity}");

                        if (p.correctedSyncVars)
                        {
                            gameStatusSB.AppendLine($"玩家血量: {p.health}");

                            gameStatusSB.AppendLine($"沙盒: {p.sandboxIndex}");

                            if (GFiles.world != null)
                            {
                                if (p.TryGetSandbox(out Sandbox sandbox))
                                {
                                    gameStatusSB.AppendLine($"沙盒群系: {sandbox.biome}");
                                    gameStatusSB.AppendLine($"沙盒大小: {sandbox.size}");
                                }
                            }
                        }
                        else
                        {
                            gameStatusSB.AppendLine("正在修正同步变量");
                        }
                    }
                    if (GFiles.world != null)
                    {
                        gameStatusSB.AppendLine($"世界名: {GFiles.world.basicData.worldName} 种子: {GFiles.world.basicData.seed}");
                    }
                }
                catch (System.Exception)
                {

                }

                t.text.text = gameStatusSB.ToString();
            };
            gameStatusText.text.raycastTarget = false;
        }

        public static void InitAllUIs()
        {
            InitLogPanel();
            InitLogScrollView();
            InitLogToolsPanel();
            InitButtonScrollView();
            InitClearLogsButton();

            InitNormalLogCountText();
            InitErrorLogCountText();
            InitExceptionLogCountText();
            InitWarningLogCountText();

            InitMostDownLogsButton();
            InitDoActiveToggle();

            InitLogPreviewText();
            IntiDetailedLog();

            fastButtons = new()
            {
                //typeof(Player).GetMethod("ServerRunCallTry")
            };
            RefreshFastButtons();
        }

        public static List<MethodInfo> fastButtons = new();

        public static void RefreshFastButtons()
        {
            foreach (MethodInfo mtd in fastButtons)
            {
                var button = GameUI.AddButton(UPC.middle, $"debugger:button.fast_button.{mtd.DeclaringType.FullName}.{mtd.Name}");
                button.buttonText.text.text = $"{mtd.DeclaringType.FullName}.{mtd.Name}";
                button.buttonText.autoCompareText = false;

                button.AddMethod(() => mtd.Invoke(null, null));

                buttonScrollView.AddChild(button);
            }
        }
        //TODO: 显示实体是否在等待变量注册
        #endregion

        #region Canvas 操作
        public static Canvas GetCanvasFromEntity(Entity entity)
        {
            Canvas canvas = null;

            for (int i = 0; i < entity.transform.childCount; i++)
            {
                if (entity.transform.GetChild(i).name == "Canvas")
                {
                    canvas = entity.transform.GetChild(i).gameObject.GetComponent<Canvas>();
                    break;
                }
            }

            return canvas;
        }

        public static Canvas AddCanvasToEntity(Entity entity)
        {
            GameObject go = new GameObject("Canvas");
            go.transform.SetParent(entity.transform);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.transform.localPosition = Vector3.zero;
            canvas.transform.localScale = new Vector2(0.075f, 0.075f);
            canvas.renderMode = RenderMode.WorldSpace;

            return canvas;
        }

        public static Canvas GetOrAddCanvasFromEntity(Entity entity)
        {
            Canvas canvas = GetCanvasFromEntity(entity);

            if (!canvas)
                canvas = AddCanvasToEntity(entity);

            return canvas;
        }
        #endregion

        #region 实体信息展示
        public static void ShowEntityNetId(Entity entity) => MethodAgent.TryRun(() =>
        {
            Canvas canvas = GetOrAddCanvasFromEntity(entity);

            TextIdentity text = GameUI.AddText(UPC.middle, $"debugger:text.entity_net_id_{entity.netId}", canvas.gameObject);
            text.rt.AddLocalPosY(-45);
            text.text.SetFontSize(12);
            text.text.alignment = TMPro.TextAlignmentOptions.Top;
            text.OnUpdate += o => o.rt.localScale = new Vector2(entity.transform.localScale.x.Sign(), 1);
            text.AfterRefreshing += p => p.text.text = $"NetId={entity.netId}";
        }, true);

        public static void ShowEntitySandboxIndex(Entity entity, Vector2Int _) => MethodAgent.TryRun(() =>
        {
            string textIdShouldBe = $"debugger:text.entity_sandbox_index_{entity.netId}";

            TextIdentity text = IdentityCenter.CompareTextIdentity(textIdShouldBe);

            if (!text)
            {
                Canvas canvas = GetOrAddCanvasFromEntity(entity);

                text = GameUI.AddText(UPC.middle, textIdShouldBe, canvas.gameObject);
                text.rt.AddLocalPosY(-60);
                text.text.SetFontSize(12);
                text.text.alignment = TMPro.TextAlignmentOptions.Top;
                text.OnUpdate += o => o.rt.localScale = new Vector2(entity.transform.localScale.x.Sign(), 1);
                text.AfterRefreshing += p => p.text.text = $"Sbi={entity.sandboxIndex}";
            }

            text.RefreshUI();
        }, true);
        #endregion

        #region 日志展示
        public static void AddLogShower(string textContent, LogType type)
        {
            InitAllUIs();

            //确定 ID 后缀和日志的图标
            string textureId = null;
            string textAndImageIdFix = null;

            switch (type)
            {
                case LogType.Warning:
                    textureId = "debugger:warning_log_icon";
                    textAndImageIdFix = "warning" + warningLogCount;
                    break;

                case LogType.Error:
                    textureId = "debugger:error_log_icon";
                    textAndImageIdFix = "error" + errorLogCount;
                    break;

                case LogType.Exception:
                    textureId = "debugger:exception_log_icon";
                    textAndImageIdFix = "exception" + exceptionLogCount;
                    break;

                default:
                    textureId = "debugger:normal_log_icon";
                    textAndImageIdFix = "normal" + normalLogCount;
                    break;
            }


            //检测前一个的内容是否和当前一样
            if (logShowers.Count > 0 && logShowers[logShowers.Count - 1].text.text.text == textContent)
            {
                //如果是的话就直接添加数字 (节约性能)
                LogShower lastShower = logShowers[logShowers.Count - 1];

                //使文本数字加 1
                lastShower.button.buttonText.AfterRefreshing += t => t.text.text = (lastShower.button.buttonText.text.text.ToInt() + 1).ToString();
                lastShower.button.buttonText.RefreshUI();

                //启用文本
                lastShower.button.buttonText.gameObject.SetActive(true);
            }
            else
            {
                //如果不是的话就直接创建物体
                ImageIdentity bg = GameUI.AddImage(UPC.upperLeft, "debugger:image.log_" + textAndImageIdFix, "ori:button_flat");
                TextIdentity text = GameUI.AddText(UPC.middle, "debugger:text.log_" + textAndImageIdFix, bg);
                ButtonIdentity button = GameUI.AddButton(UPC.upperLeft, "debugger:image.log_" + textAndImageIdFix, text, textureId);

                //设置颜色以增强层级
                bg.image.SetColorBrightness(0.7f);

                //日志会大量出现, 要优化性能
                bg.image.raycastTarget = false;
                text.text.raycastTarget = false;
                button.buttonText.text.raycastTarget = false;

                //设置按钮样式并绑定方法
                button.SetSizeDelta(90, 90);
                button.SetAPos(15, -15);
                button.rt.localScale = new Vector2(0.3f, 0.3f);
                button.AddMethod(() =>
                {
                    //将文本获取至剪贴板
                    GUIUtility.systemCopyBuffer = text.text.text;

                    //将详细日志文本的内容刷新
                    if (detailedLogText)
                    {
                        detailedLogText.text.text = text.text.text;
                        detailedLogText.text.pageToDisplay = 1;
                    }
                });

                //使文本变为 textContent
                text.autoCompareText = false;
                text.text.text = textContent;

                //更改字体样式   (margin 可以让文本不伸到 button 里面)
                text.text.SetFontSize(10);
                text.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
                text.text.overflowMode = TMPro.TextOverflowModes.Page;
                text.text.margin = new Vector2(27.5f, 0);

                //刷新日志预览
                if (logPreviewText)
                {
                    logPreviewText.text.text = textContent;
                }


                //默认为第一个文本, 如果没重复过, 默认禁用文本
                button.buttonText.AfterRefreshing += t => t.text.text = 1.ToString();
                button.buttonText.gameObject.SetActive(false);

                //设置字体大小, 文本框大小, 颜色, 位置
                button.buttonText.text.SetFontSize(30);
                button.buttonText.SetSizeDelta(90, 40);
                button.buttonText.text.color = Color.green;
                button.buttonText.SetAPosY(-button.sd.y / 2 - button.buttonText.sd.y / 2);

                //完成添加
                logShowers.Add(new LogShower(bg, text, button));
                logScrollView.AddChild(bg);

                //设为 ScrollView 子物体后会被打乱, 因此要重新设置
                text.sd = logScrollView.gridLayoutGroup.cellSize;
                bg.rt.localScale = Vector3.one;
            }

            //添加日志数量
            switch (type)
            {
                case LogType.Warning:
                    warningLogCount++;
                    break;
                case LogType.Error:
                    errorLogCount++;
                    break;
                case LogType.Exception:
                    exceptionLogCount++;
                    break;
                default:
                    normalLogCount++;
                    break;
            };
        }

        internal static StringBuilder logBuilder = new();
        public static void OnHandleLog(string logString, string stack, LogType type)
        {
            logBuilder.Clear();

            switch (type)
            {
                case LogType.Warning:
                    logBuilder.Append("<color=yellow>");
                    logBuilder.Append(logString);
                    logBuilder.Append("</color>");
                    break;

                case LogType.Error:
                    logBuilder.Append("<color=orange>");
                    logBuilder.Append(logString);
                    logBuilder.Append("</color>");
                    break;

                case LogType.Exception:
                    logBuilder.Append("<color=red>");
                    logBuilder.Append(logString);
                    logBuilder.Append("</color>");
                    break;

                default:
                    logBuilder.Append(logString);
                    break;
            }

            logBuilder.Append("\n\n");

            try
            {
                string origin = Environment.StackTrace;

                logBuilder.Append(Tools.HighlightedStackTrace());
            }
            catch
            {

            }

            logBuilder.Append("</color>");

            MethodAgent.RunOnMainThread(_ => AddLogShower(logBuilder.ToString(), type));
        }
        #endregion
    }
}
