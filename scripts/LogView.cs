using GameCore.UI;
using GameCore;
using SP.Tools.Unity;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace Debugger
{
    public static class LogView
    {
        internal static readonly List<LogShower> logShowers = new();
        internal static PanelIdentity logPanel;
        internal static ScrollViewIdentity logScrollView;
        internal static ImageIdentity detailedLogBackground;
        private static LinkText detailedLogText;
        private static InputFieldIdentity detailedLogTextContent;
        private static PanelIdentity logToolsPanel;
        private static ButtonIdentity clearLogsButton;
        private static ToggleIdentity mostDownLogsToggle;
        private static ToggleIdentity activateUIToggle;
        private static ImageIdentity logPreviewBackground;
        private static TextIdentity logPreviewText;
        private static ImageIdentity normalLogCountImage;
        private static ImageIdentity errorLogCountImage;
        private static ImageIdentity exceptionLogCountImage;
        private static ImageIdentity warningLogCountImage;
        private static TextIdentity normalLogCountText;
        private static TextIdentity errorLogCountText;
        private static TextIdentity exceptionLogCountText;
        private static TextIdentity warningLogCountText;
        private static readonly StringBuilder logBuilder = new();
        private static int _normalLogCount;
        private static int _errorLogCount;
        private static int _exceptionLogCount;
        private static int _warningLogCount;
        public static int NormalLogCount
        {
            get => _normalLogCount;
            set
            {
                _normalLogCount = value;
                normalLogCountText.RefreshUI();
            }
        }
        public static int ErrorLogCount
        {
            get => _errorLogCount;
            set
            {
                _errorLogCount = value;
                errorLogCountText.RefreshUI();
            }
        }
        public static int ExceptionLogCount
        {
            get => _exceptionLogCount;
            set
            {
                _exceptionLogCount = value;
                exceptionLogCountText.RefreshUI();
            }
        }
        public static int WarningLogCount
        {
            get => _warningLogCount;
            set
            {
                _warningLogCount = value;
                warningLogCountText.RefreshUI();
            }
        }
        public const int logToolsHeight = 30;
        public const int logPreviewHeight = 25;
        public static readonly Vector2 logWindowSize = new(400, 400);
        public static readonly Vector2 detailedLogSize = new(400, 200);












        internal static void CheckSelectedObject()
        {
            if (!activateUIToggle)
                return;

            if (!activateUIToggle.toggle.isOn && GameUI.eventSystem.currentSelectedGameObject == activateUIToggle.gameObject)
            {
                GameUI.eventSystem.SetSelectedGameObject(null);
            }
        }


        internal static void AddLogShower(string textContent, LogType type)
        {
            logShowers.Add(LogShowerPool.Get(textContent, type));

            //添加日志数量
            switch (type)
            {
                case LogType.Warning:
                    WarningLogCount++;
                    break;

                case LogType.Error:
                    ErrorLogCount++;
                    break;

                case LogType.Exception:
                    ExceptionLogCount++;
                    break;

                default:
                    NormalLogCount++;
                    break;
            };
        }

        internal static void OnHandleLog(string logString, string stackTrace, LogType type)
        {
            logBuilder.Clear();

            switch (type)
            {
                case LogType.Warning:
                    logBuilder.Append("<color=yellow>")
                              .Append(logString)
                              .Append("</color>");
                    break;

                case LogType.Error:
                    logBuilder.Append("<color=orange>")
                              .Append(logString)
                              .Append("</color>");
                    break;

                case LogType.Exception:
                    logBuilder.Append("<color=red>")
                              .Append(logString)
                              .Append("</color>");
                    break;

                default:
                    logBuilder.Append(logString);
                    break;
            }

            logBuilder.Append("\n\n")
                      .Append(Tools.HighlightedStackTraceWithoutAt(stackTrace))
                      .Append("</color>");


            var result = logBuilder.ToString();
            MethodAgent.QueueOnMainThread(_ => AddLogShower(result, type));
        }

        internal static void Init()
        {
            /* ---------------------------------- 日志面板 ---------------------------------- */
            logPanel = GameUI.AddPanel("debugger:panel.log_view", Center.GetMainCanvas().transform);
            logPanel.SetAnchorMinMax(0, 0.3f, 0.23f, 1);
            logPanel.panelImage.SetColor(0.7f, 0.7f, 0.7f, 0.45f);

            //设置日志面板位置
            SetLogPanelTransform();

            if (!Center.enabled)// || Application.isEditor)
                logPanel.gameObject.SetActive(false);



            /* ---------------------------------- 日志列表 ---------------------------------- */
            logScrollView = GameUI.AddScrollView(UIA.UpperLeft, "debugger:scrollview_log_show", logPanel);
            logScrollView.SetAnchorMinMax(0, 0, 1, 0.94f);
            logScrollView.sd = Vector2.zero;
            logScrollView.ap = Vector2.zero;
            logScrollView.gridLayoutGroup.cellSize = new Vector2(logPanel.rt.anchorMax.x * GameUI.canvasScaler.referenceResolution.x, 35);
            logScrollView.gridLayoutGroup.spacing = new Vector2(0, 2.5f);
            logScrollView.viewportImage.color = new Color32(0, 0, 0, 1);
            logScrollView.scrollViewImage.color = new Color32(0, 0, 0, 0);




            /* --------------------------------- 日志工具面板 --------------------------------- */
            logToolsPanel = GameUI.AddPanel("debugger:panel.log_tools", logPanel);
            logToolsPanel.panelImage.SetColor(0.75f, 0.75f, 0.75f, 0.75f);
            logToolsPanel.SetAnchorMinMax(0, 0.94f, 1, 1);




            /* ---------------------------------- 日志预览 ---------------------------------- */
            //初始化日志预览背景
            logPreviewBackground = GameUI.AddImage(UIA.StretchBottom, "debugger:image.log_preview_background", null, Center.GetMainCanvas().transform);
            SetLogPreviewImageTransform();
            logPreviewBackground.image.color = new Color(0.4f, 0.4f, 0.4f, 0.75f);
            logPreviewBackground.SetSizeDelta(0, logPreviewHeight);
            logPreviewBackground.SetAPos(0, logPreviewBackground.sd.y / 2);
            logPreviewBackground.image.raycastTarget = false;
            if (!Center.enabled)// || Application.isEditor)
                logPreviewBackground.gameObject.SetActive(false);

            //初始化日志预览文本
            logPreviewText = GameUI.AddText(UIA.StretchDouble, "debugger:text.log_preview", logPreviewBackground);
            logPreviewText.text.alignment = TextAlignmentOptions.Left;
            logPreviewText.text.overflowMode = TextOverflowModes.Ellipsis;
            logPreviewText.text.SetFontSize(12);
            logPreviewText.SetSizeDelta(0, 0);




            /* ---------------------------------- 详细日志 ---------------------------------- */
            detailedLogBackground = GameUI.AddImage(UIA.UpperLeft, "debugger:image.log_detailed_background", "ori:square_button_flat", Center.GetMainCanvas().transform);
            detailedLogBackground.SetAnchorMinMax(0, 0f, 0.23f, 0.3f);
            detailedLogBackground.image.SetColor(0.7f, 0.7f, 0.7f, 0.45f);
            detailedLogBackground.image.raycastTarget = false;
            detailedLogBackground.gameObject.AddComponent<RectMask2D>(); //防止字体超出背景
            detailedLogBackground.sd = Vector2.zero;
            detailedLogBackground.ap = Vector2.zero;

            //文本滚动条
            var slider = GameUI.AddSlider(UIA.Left, "debugger:slider.log_detailed", detailedLogBackground);
            slider.slider.value = 0;
            slider.slider.transform.localRotation = Quaternion.Euler(0, 0, -90);
            GameObject.Destroy(slider.text.gameObject);
            slider.SetAPosX(slider.sd.y / 2);
            slider.slider.onValueChanged.AddListener(SetDetailedLogTextPosition);

            detailedLogTextContent = GameUI.AddInputField(UIA.Up, "debugger:text.log_detailed", detailedLogBackground);
            detailedLogTextContent.field.textComponent.SetFontSize(11);
            detailedLogTextContent.field.textComponent.alignment = TextAlignmentOptions.TopLeft;
            detailedLogTextContent.field.textComponent.overflowMode = TextOverflowModes.Page;
            detailedLogTextContent.field.textComponent.margin = new Vector4(10, 10, 10, 0);
            detailedLogTextContent.field.textComponent.color = new(1, 1, 1, 0.8f);
            detailedLogTextContent.field.lineType = TMP_InputField.LineType.MultiLineSubmit;
            detailedLogTextContent.field.readOnly = true;
            detailedLogTextContent.autoCompareText = false;
            detailedLogTextContent.SetAPosX(slider.fillImage.rectTransform.sizeDelta.x);
            detailedLogTextContent.SetSizeDelta(detailedLogSize.x - slider.fillImage.rectTransform.sizeDelta.x, detailedLogSize.y * 10);
            Component.Destroy(detailedLogTextContent.image); //删除背景图
            detailedLogText = detailedLogTextContent.gameObject.AddComponent<LinkText>();
            detailedLogText.content = detailedLogTextContent.field.textComponent;
            SetDetailedLogTextPosition(0);
            //TODO: code --goto "D:\MakeGames\GameProject\ori\scripts\Entities\FishingFloat.cs":10




            /* --------------------------------- 清理日志按钮 --------------------------------- */
            clearLogsButton = GameUI.AddButton(UIA.Left, "debugger:button.clear_logs", logToolsPanel);
            clearLogsButton.SetSizeDelta(70, logToolsHeight);
            clearLogsButton.buttonText.sd = clearLogsButton.sd;
            clearLogsButton.SetAPos(clearLogsButton.sd.x / 2, 0);
            clearLogsButton.buttonText.text.SetFontSize(14);
            clearLogsButton.OnClickBind(() =>
            {
                NormalLogCount = 0;
                ErrorLogCount = 0;
                ExceptionLogCount = 0;
                WarningLogCount = 0;

                foreach (var item in logShowers)
                {
                    LogShowerPool.Recover(item);
                }

                logShowers.Clear();

                //清除其他文本
                detailedLogTextContent.field.text = string.Empty;
                logPreviewText.text.text = string.Empty;
            });





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
            normalLogCountText.AfterRefreshing += t => t.text.text = NormalLogCount.ToString();
            normalLogCountText.text.raycastTarget = false;

            //错误日志
            errorLogCountImage = GameUI.AddImage(UIA.UpperRight, "debugger:image.error_log_count", "debugger:error_log_icon", logToolsPanel);
            errorLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            errorLogCountImage.SetAPosOnBySizeLeft(normalLogCountImage, 1);
            errorLogCountImage.image.raycastTarget = false;
            errorLogCountText = GameUI.AddText(UIA.Middle, "debugger:text.error_log_count", errorLogCountImage);
            errorLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            errorLogCountText.AfterRefreshing += t => t.text.text = ErrorLogCount.ToString();
            errorLogCountText.text.raycastTarget = false;

            //异常日志
            exceptionLogCountImage = GameUI.AddImage(UIA.UpperRight, "debugger:image.exception_log_count", "debugger:exception_log_icon", logToolsPanel);
            exceptionLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            exceptionLogCountImage.SetAPosOnBySizeLeft(errorLogCountImage, 1);
            exceptionLogCountImage.image.raycastTarget = false;
            exceptionLogCountText = GameUI.AddText(UIA.Middle, "debugger:text.exception_log_count", exceptionLogCountImage);
            exceptionLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            exceptionLogCountText.AfterRefreshing += t => t.text.text = ExceptionLogCount.ToString();
            exceptionLogCountText.text.raycastTarget = false;

            //警告日志
            warningLogCountImage = GameUI.AddImage(UIA.UpperRight, "debugger:image.warning_log_count", "debugger:warning_log_icon", logToolsPanel);
            warningLogCountImage.SetSizeDelta(logToolsHeight, logToolsHeight);
            warningLogCountImage.SetAPosOnBySizeLeft(exceptionLogCountImage, 1);
            warningLogCountImage.image.raycastTarget = false;
            warningLogCountText = GameUI.AddText(UIA.Middle, "debugger:text.warning_log_count", warningLogCountImage);
            warningLogCountText.text.SetFontSize(13);
            normalLogCountText.autoCompareText = false;
            warningLogCountText.AfterRefreshing += t => t.text.text = WarningLogCount.ToString();
            warningLogCountText.text.raycastTarget = false;







            /* --------------------------------- 锁定最下开关 --------------------------------- */
            mostDownLogsToggle = GameUI.AddToggle(UIA.Left, "debugger:toggle.most_down_logs", logToolsPanel);
            mostDownLogsToggle.SetToggleSize(new Vector2(90, logToolsHeight));
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
            activateUIToggle.SetToggleSize(new Vector2(90, logToolsHeight));
            activateUIToggle.canvasGroup.ignoreParentGroups = true;
            activateUIToggle.SetAPosOnBySizeRight(mostDownLogsToggle, 5);
            activateUIToggle.text.text.SetFontSize(14);
            activateUIToggle.text.text.alignment = TextAlignmentOptions.Center;
            activateUIToggle.OnValueChangeBind(v =>
            {
                logPanel.canvasGroup.interactable = v;
            });
        }

        static void SetLogPanelTransform()
        {
            logPanel.rt.localScale = Vector3.one;
        }

        static void SetDetailedLogTextPosition(float value)
        {
            detailedLogTextContent.SetAPosY(-detailedLogTextContent.sd.y / 2 + detailedLogTextContent.sd.y * value);
        }


        static void SetLogPreviewImageTransform()
        {
            logPreviewBackground.SetAPos(0, 0);
            logPreviewBackground.rt.localScale = Vector3.one;
        }




        internal class LogShower
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





        internal static class LogShowerPool
        {
            public static Stack<LogShower> stack = new();
            public static int createIndex;

            public static LogShower Get(string textContent, LogType type)
            {
                LogShower shower;

                //确定 ID 后缀和日志的图标
                var sprite = type switch
                {
                    LogType.Warning => DebuggerModEntry.warningLogSprite,
                    LogType.Error => DebuggerModEntry.errorLogSprite,
                    LogType.Exception => DebuggerModEntry.exceptionLogSprite,
                    _ => DebuggerModEntry.normalLogSprite,
                };

                if (stack.Count == 0)
                {
                    string idPostfix = $"_{createIndex}";
                    createIndex++;

                    //如果不是的话就直接创建物体
                    ImageIdentity bg = GameUI.AddImage(UIA.UpperLeft, "debugger:image.log_" + idPostfix, "ori:button_flat");
                    TextIdentity text = GameUI.AddText(UIA.Middle, "debugger:text.log_" + idPostfix, bg);
                    ButtonIdentity button = GameUI.AddButton(UIA.Left, "debugger:button.log_" + idPostfix, text, null);

                    //设置颜色以增强层级
                    bg.image.SetColorBrightness(0.7f);

                    //日志会大量出现, 要优化性能
                    bg.image.raycastTarget = false;
                    text.text.raycastTarget = false;
                    ////button.buttonText.text.raycastTarget = false;

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
                            detailedLogTextContent.field.text = text.text.text;
                            detailedLogTextContent.field.textComponent.pageToDisplay = 1;
                        }

                        //恢复详细日志文本的位置
                        SetDetailedLogTextPosition(0);
                    });

                    //关闭自动刷新
                    text.autoCompareText = false;

                    //更改字体样式   (margin 可以让文本不伸到 button 里面)
                    text.text.SetFontSize(10);
                    text.text.alignment = TextAlignmentOptions.TopLeft;
                    text.text.overflowMode = TextOverflowModes.Truncate;
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
                    shower.bg.transform.SetAsLastSibling();
                }

                //刷新展示器
                shower.button.image.sprite = sprite;
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
    }
}