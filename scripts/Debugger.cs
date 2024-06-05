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
using TMPro;
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
            lineRenderer.SetPositions(new Vector3[5] { new(chunk.left, chunk.up), new(chunk.left, chunk.down), new(chunk.right, chunk.down), new(chunk.right, chunk.up), new(chunk.left, chunk.up) });
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
        public static ScrollViewIdentity fastButtonScrollView;
        public static bool enabled = true;
        public static int inputButtonsHeight = 30;

        public static readonly List<LogShower> logShowers = new();
        public static ImageIdentity detailedLogBackground;
        public static TextIdentity detailedLogText;
        public static PanelIdentity logToolsPanel;
        public static ButtonIdentity clearLogsButton;
        public static ToggleIdentity mostDownLogsToggle;
        public static ToggleIdentity activateUIToggle;
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
        public static ImageIdentity logPreviewBackground;
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
            if (!activateUIToggle)
                return;

            if (!activateUIToggle.toggle.isOn && GameUI.eventSystem.currentSelectedGameObject == activateUIToggle.gameObject)
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
        public static readonly Vector2 logWindowSize = new(400, 400);
        public static readonly Vector2 detailedLogSize = new(800, 180);
        public static readonly Color entityInfoTextColor = new(1, 1, 1, 0.7f);
        private static readonly StringBuilder gameStatusSB = new();
        public const int logPreviewHeight = 25;

        public static void InitLogPanel()
        {
            /* ---------------------------------- 日志面板 ---------------------------------- */
            logPanel = GameUI.AddPanel("debugger:panel.log_show", GetMainCanvas().transform);
            logPanel.SetAnchorMinMax(0, 1);
            logPanel.sd = logWindowSize;
            logPanel.panelImage.SetColor(0.7f, 0.7f, 0.7f, 0.45f);

            //设置日志面板位置
            SetLogPanelTransform();

            if (!enabled)// || Application.isEditor)
                logPanel.gameObject.SetActive(false);



            /* ---------------------------------- 日志列表 ---------------------------------- */
            logScrollView = GameUI.AddScrollView(UIA.UpperLeft, "debugger:scrollview_log_show", logPanel);
            logScrollView.SetSizeDelta(logPanel.sd.x, logPanel.sd.y - logToolsHeight);
            logScrollView.SetAPos(logScrollView.sd.x / 2, -logScrollView.sd.y / 2 - logToolsHeight);
            logScrollView.gridLayoutGroup.cellSize = new Vector2(logScrollView.gridLayoutGroup.cellSize.x, 35);
            logScrollView.gridLayoutGroup.spacing = new Vector2(0, 2.5f);
            logScrollView.viewportImage.color = new Color32(0, 0, 0, 1);
            logScrollView.scrollViewImage.color = new Color32(0, 0, 0, 0);




            /* --------------------------------- 日志工具面板 --------------------------------- */
            logToolsPanel = GameUI.AddPanel("debugger:panel.log_tools", logPanel);
            logToolsPanel.panelImage.SetColor(0.75f, 0.75f, 0.75f, 0.75f);
            logToolsPanel.SetAPos(logPanel.sd.x / 2, -logToolsHeight / 2);
            logToolsPanel.SetSizeDelta(logWindowSize.x, logToolsHeight);
            logToolsPanel.SetAnchorMinMax(0, 1);




            /* ---------------------------------- 日志预览 ---------------------------------- */
            //初始化日志预览背景
            logPreviewBackground = GameUI.AddImage(UIA.StretchBottom, "debugger:image.log_preview_background", null, GetMainCanvas().transform);
            SetLogPreviewImageTransform();
            logPreviewBackground.image.color = new Color(0.4f, 0.4f, 0.4f, 0.75f);
            logPreviewBackground.SetSizeDelta(0, Core.logPreviewHeight);
            logPreviewBackground.SetAPos(0, logPreviewBackground.sd.y / 2);
            logPreviewBackground.image.raycastTarget = false;
            if (!enabled)// || Application.isEditor)
                logPreviewBackground.gameObject.SetActive(false);

            //初始化日志预览文本
            logPreviewText = GameUI.AddText(UIA.StretchDouble, "debugger:text.log_preview", logPreviewBackground);
            logPreviewText.text.alignment = TMPro.TextAlignmentOptions.Left;
            logPreviewText.text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            logPreviewText.text.SetFontSize(12);
            logPreviewText.SetSizeDelta(0, 0);




            /* ---------------------------------- 详细日志 ---------------------------------- */
            detailedLogBackground = GameUI.AddImage(UIA.UpperRight, "debugger:image.log_detailed_background", "ori:square_button_flat", logPanel);
            detailedLogBackground.image.SetColor(0.7f, 0.7f, 0.7f, 0.45f);
            detailedLogBackground.image.raycastTarget = false;
            detailedLogBackground.sd = detailedLogSize;
            detailedLogBackground.SetAPos(detailedLogSize.x / 2, -detailedLogSize.y / 2);
            detailedLogBackground.gameObject.AddComponent<RectMask2D>(); //防止字体超出背景
            var slider = GameUI.AddSlider(UIA.Left, "debugger:slider.log_detailed", detailedLogBackground);
            slider.slider.value = 0;
            slider.slider.transform.localRotation = Quaternion.Euler(0, 0, -90);
            GameObject.Destroy(slider.text.gameObject);
            slider.SetAPosX(slider.sd.y / 2);
            slider.slider.onValueChanged.AddListener(SetDetailedLogTextPosition);

            detailedLogText = GameUI.AddText(UIA.Up, "debugger:text.log_detailed", detailedLogBackground);
            detailedLogText.text.SetFontSize(11);
            detailedLogText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
            detailedLogText.text.overflowMode = TMPro.TextOverflowModes.Page;
            detailedLogText.text.margin = new Vector4(30, 10, 10, 0);
            detailedLogText.text.raycastTarget = false;
            detailedLogText.autoCompareText = false;
            detailedLogText.SetSizeDelta(detailedLogSize.x, detailedLogSize.y * 10);
            detailedLogText.text.color = new(1, 1, 1, 0.8f);
            SetDetailedLogTextPosition(0);




            /* --------------------------------- 清理日志按钮 --------------------------------- */
            clearLogsButton = GameUI.AddButton(UIA.Left, "debugger:button.clear_logs", logToolsPanel);
            clearLogsButton.SetSizeDelta(70, logToolsHeight);
            clearLogsButton.buttonText.sd = clearLogsButton.sd;
            clearLogsButton.SetAPos(clearLogsButton.sd.x / 2, 0);
            clearLogsButton.buttonText.text.SetFontSize(14);
            clearLogsButton.OnClickBind(() =>
            {
                normalLogCount = 0;
                errorLogCount = 0;
                exceptionLogCount = 0;
                warningLogCount = 0;

                foreach (var item in logShowers)
                {
                    LogShowerPool.Recover(item);
                }

                logShowers.Clear();
            });




            /* --------------------------------- 游戏状态显示 --------------------------------- */
            gameStatusText = GameUI.AddText(UIA.UpperLeft, "debugger:text.game_status", GetFrequentSimpleCanvas().transform);
            gameStatusText.text.SetFontSize(12);
            gameStatusText.SetSizeDelta(600, logPanel.sd.y);
            gameStatusText.SetAPos(logPanel.ap.x + (gameStatusText.sd.x / 2) + (logPanel.sd.x / 2) + 10, -detailedLogSize.y - gameStatusText.sd.y / 2 - 10);
            gameStatusText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
            gameStatusText.AfterRefreshing += t =>
            {
                try
                {
                    gameStatusSB.Clear();

                    gameStatusSB.AppendLine($"FPS: {(int)Tools.smoothFps}");
                    gameStatusSB.AppendLine($"日志显示器数量: {logShowers.Count}/{LogShowerPool.createIndex}");

                    if (GScene.name == SceneNames.GameScene)
                    {
                        gameStatusSB.AppendLine($"时间: {GTime.time24Format} ({GTime.time}/{GTime.timeOneDay}) - 流速:{GTime.timeSpeed}");
                        gameStatusSB.AppendLine($"随机更新几率: {RandomUpdater.randomUpdateProbability}");
                        gameStatusSB.AppendLine($"实体数量: {EntityCenter.all.Count}");

                        if (Player.TryGetLocal(out Player p))
                        {
                            gameStatusSB.AppendLine($"玩家位置: {(Vector2)p.transform.position}");
                            gameStatusSB.AppendLine($"指针世界位置: {p.cursorWorldPos}");
                            gameStatusSB.AppendLine($"玩家速度: {p.rb.velocity}");
                            gameStatusSB.AppendLine($"玩家血量: {p.health}");
                            gameStatusSB.AppendLine($"区域序列: {p.regionIndex}");
                        }
                        if (GFiles.world != null)
                        {
                            gameStatusSB.AppendLine($"世界名: {GFiles.world.basicData.worldName} (种子: {GFiles.world.basicData.seed})");
                        }
                    }
                }
                catch (System.Exception)
                {

                }

                t.text.text = gameStatusSB.ToString();
            };
            gameStatusText.text.raycastTarget = false;




            /* --------------------------------- 初始化快速按钮列表 -------------------------------- */
            fastButtonScrollView = GameUI.AddScrollView(UIA.UpperLeft, "debugger:scrollview_button_show", logPanel);
            fastButtonScrollView.SetSizeDelta(logPanel.sd.x, logPanel.sd.y);
            fastButtonScrollView.SetAPosOnBySizeRight(logScrollView, 0);
            fastButtonScrollView.gridLayoutGroup.cellSize = new Vector2(fastButtonScrollView.gridLayoutGroup.cellSize.x, 45);
            fastButtonScrollView.gridLayoutGroup.spacing = new Vector2(0, 2.5f);
            fastButtonScrollView.viewportImage.color = new Color32(0, 0, 0, 1);
            fastButtonScrollView.scrollViewImage.color = new Color32(0, 0, 0, 0);
            fastButtonScrollView.viewportImage.raycastTarget = false;
            fastButtonScrollView.scrollViewImage.raycastTarget = false;




            /* -------------------------------------------------------------------------- */
            /*                                   日志数量显示                                   */
            /* -------------------------------------------------------------------------- */
            //普通日志
            normalLogCountImage = GameUI.AddImage(UIA.UpperRight, "debugger:image.normal_log_count", "debugger:normal_log_icon", logToolsPanel);
            normalLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            normalLogCountImage.SetAPos(-normalLogCountImage.sd.x / 2, -normalLogCountImage.sd.y / 2);
            normalLogCountImage.image.raycastTarget = false;
            normalLogCountText = GameUI.AddText(UIA.Middle, "debugger:text.normal_log_count", normalLogCountImage);
            normalLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            normalLogCountText.AfterRefreshing += t => t.text.text = normalLogCount.ToString();
            normalLogCountText.text.raycastTarget = false;

            //错误日志
            errorLogCountImage = GameUI.AddImage(UIA.UpperRight, "debugger:image.error_log_count", "debugger:error_log_icon", logToolsPanel);
            errorLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            errorLogCountImage.SetAPosOnBySizeLeft(normalLogCountImage, 1);
            errorLogCountImage.image.raycastTarget = false;
            errorLogCountText = GameUI.AddText(UIA.Middle, "debugger:text.error_log_count", errorLogCountImage);
            errorLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            errorLogCountText.AfterRefreshing += t => t.text.text = errorLogCount.ToString();
            errorLogCountText.text.raycastTarget = false;

            //异常日志
            exceptionLogCountImage = GameUI.AddImage(UIA.UpperRight, "debugger:image.exception_log_count", "debugger:exception_log_icon", logToolsPanel);
            exceptionLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            exceptionLogCountImage.SetAPosOnBySizeLeft(errorLogCountImage, 1);
            exceptionLogCountImage.image.raycastTarget = false;
            exceptionLogCountText = GameUI.AddText(UIA.Middle, "debugger:text.exception_log_count", exceptionLogCountImage);
            exceptionLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            exceptionLogCountText.AfterRefreshing += t => t.text.text = exceptionLogCount.ToString();
            exceptionLogCountText.text.raycastTarget = false;

            //警告日志
            warningLogCountImage = GameUI.AddImage(UIA.UpperRight, "debugger:image.warning_log_count", "debugger:warning_log_icon", logToolsPanel);
            warningLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            warningLogCountImage.SetAPosOnBySizeLeft(exceptionLogCountImage, 1);
            warningLogCountImage.image.raycastTarget = false;
            warningLogCountText = GameUI.AddText(UIA.Middle, "debugger:text.warning_log_count", warningLogCountImage);
            warningLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            warningLogCountText.AfterRefreshing += t => t.text.text = warningLogCount.ToString();
            warningLogCountText.text.raycastTarget = false;







            /* --------------------------------- 锁定最下开关 --------------------------------- */
            mostDownLogsToggle = GameUI.AddToggle(UIA.Left, "debugger:toggle.most_down_logs", logToolsPanel);
            mostDownLogsToggle.SetScale(new Vector2(90, logToolsHeight));
            mostDownLogsToggle.text.text.SetFontSize(14);
            mostDownLogsToggle.SetAPosOnBySizeRight(clearLogsButton, 5);
            mostDownLogsToggle.text.text.alignment = TMPro.TextAlignmentOptions.Center;
            logScrollView.OnUpdate += SetToMostDownPos;
            static void SetToMostDownPos(ScrollViewIdentity ci)
            {
                if (mostDownLogsToggle.toggle.isOn)
                    ci.scrollRect.verticalNormalizedPosition = 0;
            }



            /* --------------------------------- 锁定 UI 开关 ---------- */
            activateUIToggle = GameUI.AddToggle(UIA.Left, "debugger:toggle.activate_ui", logToolsPanel);
            activateUIToggle.SetScale(new Vector2(90, logToolsHeight));
            activateUIToggle.canvasGroup.ignoreParentGroups = true;
            activateUIToggle.SetAPosOnBySizeRight(mostDownLogsToggle, 5);
            activateUIToggle.text.text.SetFontSize(14);
            activateUIToggle.text.text.alignment = TMPro.TextAlignmentOptions.Center;
            activateUIToggle.OnValueChangeBind(v =>
            {
                logPanel.canvasGroup.interactable = v;
            });
        }

        public static void InitPanelForGameScene()
        {
            /* ---------------------------------- 随机更新频率设置框 ---------- */
            randomUpdateIB = GameUI.AddInputButton(UIA.Middle, "debugger:ib.random_update", logPanel);
            randomUpdateIB.SetSize(new Vector2(logPanel.sd.x, inputButtonsHeight));
            randomUpdateIB.SetAPos(0, -logPanel.sd.y / 2 - inputButtonsHeight / 2);
            randomUpdateIB.field.field.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;
            randomUpdateIB.OnClickBind(() =>
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



            /* ---------------------------------- 时间设置框 --------------------------------- */
            time24IB = GameUI.AddInputButton(UIA.Middle, "debugger:ib.time24", logPanel);
            time24IB.SetSize(new Vector2(logPanel.sd.x, inputButtonsHeight));
            time24IB.SetAPos(0, -logPanel.sd.y / 2 - (inputButtonsHeight * 0.5f) * 3);
            time24IB.field.field.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;
            time24IB.OnClickBind(() =>
            {
                if (GFiles.world != null)
                {
                    GTime.time24Format = time24IB.field.field.text.ToInt();
                }
            });
        }

        public static void DestroyPanelForGameScene()
        {
            GameObject.Destroy(randomUpdateIB.gameObject);
            GameObject.Destroy(time24IB.gameObject);
        }

        public static void SetLogPanelTransform()
        {
            logPanel.SetAPos(logPanel.sd.x / 2, -logPanel.sd.y / 2);
            logPanel.rt.localScale = Vector3.one;
        }

        public static void SetDetailedLogTextPosition(float value)
        {
            detailedLogText.SetAPosY(-detailedLogText.sd.y / 2 + detailedLogText.sd.y * value);
        }


        public static void SetLogPreviewImageTransform()
        {
            logPreviewBackground.SetAPos(0, 0);
            logPreviewBackground.rt.localScale = Vector3.one;
        }


        public static List<MethodInfo> fastButtons = new();
        // TODO
        // {
        //     typeof(Player).GetMethod("ServerRunCallTry")
        // };

        public static void RefreshFastButtons()
        {
            foreach (MethodInfo mtd in fastButtons)
            {
                var button = GameUI.AddButton(UIA.Middle, $"debugger:button.fast_button.{mtd.DeclaringType.FullName}.{mtd.Name}");
                button.buttonText.text.text = $"{mtd.DeclaringType.FullName}.{mtd.Name}";
                button.buttonText.autoCompareText = false;

                button.OnClickBind(() => mtd.Invoke(null, null));

                fastButtonScrollView.AddChild(button);
            }
        }

        #endregion

        #region Canvas 操作
        public static class EntityCanvasPool
        {
            public static Stack<Canvas> stack = new();

            public static Canvas Get(Entity entity)
            {
                var canvas = (stack.Count == 0) ? Generation() : stack.Pop();

                canvas.transform.SetParent(entity.transform, false);
                canvas.transform.localPosition = Vector3.zero;
                canvas.transform.localScale = new Vector2(0.075f, 0.075f);
                canvas.enabled = true;

                return canvas;
            }

            public static void Recover(Canvas canvas)
            {
                canvas.enabled = false;
                canvas.transform.SetParent(null);
                canvas.transform.DestroyChildren();
                stack.Push(canvas);
            }

            public static Canvas Generation()
            {
                GameObject go = new("DebuggerEntityCanvas");
                Canvas canvas = go.AddComponent<Canvas>();

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 20;

                return canvas;
            }
        }

        public static Canvas GetEntityCanvasFromEntity(Entity entity)
        {
            Canvas canvas = null;

            for (int i = 0; i < entity.transform.childCount; i++)
            {
                if (entity.transform.GetChild(i).name == "DebuggerEntityCanvas")
                {
                    canvas = entity.transform.GetChild(i).gameObject.GetComponent<Canvas>();
                    break;
                }
            }

            return canvas;
        }

        public static Canvas AddEntityCanvasTo(Entity entity) => EntityCanvasPool.Get(entity);


        public static Canvas GetOrAddEntityCanvasFrom(Entity entity)
        {
            Canvas canvas = GetEntityCanvasFromEntity(entity);

            if (!canvas)
                canvas = AddEntityCanvasTo(entity);

            return canvas;
        }


        public static void RecoverEntityCanvasFrom(Entity entity)
        {
            var canvas = GetEntityCanvasFromEntity(entity);
            if (canvas) EntityCanvasPool.Recover(canvas);
        }
        #endregion

        #region 实体信息展示
        public static void ShowEntityInfo(Entity entity) => MethodAgent.DebugRun(() =>
        {
            Canvas canvas = GetOrAddEntityCanvasFrom(entity);

            TextIdentity text = GameUI.AddText(UIA.Middle, $"debugger:text.entity_info_{entity.netId}", canvas.gameObject);
            text.rt.AddLocalPosY(-45);
            text.text.SetFontSize(7);
            text.text.color = entityInfoTextColor;
            text.text.alignment = TextAlignmentOptions.Top;
            text.text.raycastTarget = false;
            text.autoCompareText = false;
            text.OnUpdate += o => o.rt.localScale = new Vector2(entity.transform.localScale.x.Sign(), 1);
            text.AfterRefreshing += p => p.text.text = $"HP={entity.health}/{entity.data.maxHealth},\nNetId={entity.netId}";
            entity.OnHealthChange += () => text.RefreshUI();
        });
        #endregion

        #region 日志展示
        public static class LogShowerPool
        {
            public static Stack<LogShower> stack = new();
            public static int createIndex;

            public static LogShower Get(string textContent, LogType type)
            {
                LogShower shower;

                //确定 ID 后缀和日志的图标
                string textureId = type switch
                {
                    LogType.Warning => "debugger:warning_log_icon",
                    LogType.Error => "debugger:error_log_icon",
                    LogType.Exception => "debugger:exception_log_icon",
                    _ => "debugger:normal_log_icon",
                };

                if (stack.Count == 0)
                {
                    string idPostfix = $"_{createIndex}";
                    createIndex++;

                    //如果不是的话就直接创建物体
                    ImageIdentity bg = GameUI.AddImage(UIA.UpperLeft, "debugger:image.log_" + idPostfix, "ori:button_flat");
                    TextIdentity text = GameUI.AddText(UIA.Middle, "debugger:text.log_" + idPostfix, bg);
                    ButtonIdentity button = GameUI.AddButton(UIA.Left, "debugger:button.log_" + idPostfix, text, textureId);

                    //设置颜色以增强层级
                    bg.image.SetColorBrightness(0.7f);

                    //日志会大量出现, 要优化性能
                    bg.image.raycastTarget = false;
                    text.text.raycastTarget = false;
                    button.buttonText.text.raycastTarget = false;

                    //设置按钮样式并绑定方法
                    var buttonSize = logScrollView.gridLayoutGroup.cellSize.y * 0.75f;
                    button.SetSizeDelta(buttonSize, buttonSize);
                    button.SetAPos(button.sd.x / 2 + 3, 0);
                    button.OnClickBind(() =>
                    {
                        //将文本获取至剪贴板
                        GUIUtility.systemCopyBuffer = text.text.text;

                        //将详细日志文本的内容刷新
                        if (detailedLogText)
                        {
                            detailedLogText.text.text = text.text.text;
                            detailedLogText.text.pageToDisplay = 1;
                        }

                        //恢复详细日志文本的位置
                        SetDetailedLogTextPosition(0);
                    });

                    //关闭自动刷新
                    text.autoCompareText = false;

                    //更改字体样式   (margin 可以让文本不伸到 button 里面)
                    text.text.SetFontSize(10);
                    text.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
                    text.text.overflowMode = TMPro.TextOverflowModes.Truncate;
                    text.text.margin = new Vector4(27.5f, 2, 0, 2);


                    //设置字体大小, 文本框大小, 颜色, 位置
                    //// 初始化日志重复次数
                    GameObject.Destroy(button.buttonText.gameObject);
                    //// button.buttonText.gameObject.SetActive(false);
                    //// button.buttonText.text.text = "1";
                    //// button.buttonText.autoCompareText = false;
                    //// button.buttonText.text.SetFontSize(30);
                    //// button.buttonText.SetSizeDelta(90, 40);
                    //// button.buttonText.text.color = Color.green;
                    //// button.buttonText.SetAPosY(-button.sd.y / 2 - button.buttonText.sd.y / 2);

                    //设为 ScrollView 子物体后会被打乱, 因此要重新设置
                    logScrollView.AddChild(bg);
                    text.sd = logScrollView.gridLayoutGroup.cellSize;
                    bg.rt.localScale = Vector3.one;

                    //完成
                    shower = new(bg, text, button);
                }
                else
                {
                    shower = stack.Pop();
                    shower.bg.gameObject.SetActive(true);
                    shower.button.image.sprite = ModFactory.CompareTexture(textureId).sprite;
                    shower.bg.transform.SetAsLastSibling();
                }

                //刷新文本
                shower.text.text.text = textContent;

                //刷新日志预览
                logPreviewText.text.text = textContent;

                return shower;
            }

            public static void Recover(LogShower shower)
            {
                shower.bg.gameObject.SetActive(false);
                stack.Push(shower);
            }
        }

        public static void AddLogShower(string textContent, LogType type)
        {
            //// //检测前一个的内容是否和当前一样
            //// if (logShowers.Count > 0 && logShowers[^1].text.text.text == textContent)
            //// {
            ////     //如果是的话就直接添加数字 (节约性能)
            ////     LogShower lastShower = logShowers[^1];
            //// 
            ////     //使文本数字加 1
            ////     lastShower.button.buttonText.text.text = (lastShower.button.buttonText.text.text.ToInt() + 1).ToString();
            //// 
            ////     //启用文本
            ////     lastShower.button.buttonText.gameObject.SetActive(true);
            //// }
            //// else
            //// {
            logShowers.Add(LogShowerPool.Get(textContent, type));
            //// }

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
        public static void OnHandleLog(string logString, string _, LogType type)
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
            logBuilder.Append(Tools.HighlightedStackTrace());

            logBuilder.Append("</color>");


            var result = logBuilder.ToString();
            MethodAgent.QueueOnMainThread(_ => AddLogShower(result, type));
        }
        #endregion
    }
}
