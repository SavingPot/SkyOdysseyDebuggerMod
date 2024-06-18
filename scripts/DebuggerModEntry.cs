using GameCore;
using GameCore.High;
using GameCore.UI;
using UnityEngine;
using SP.Tools.Unity;
using System.Linq;
using System.Collections;
using UnityEngine.InputSystem;
using SP.Tools;

namespace Debugger
{
    public class DebuggerModEntry : ModEntry
    {
        internal static Sprite warningLogSprite;
        internal static Sprite errorLogSprite;
        internal static Sprite exceptionLogSprite;
        internal static Sprite normalLogSprite;

        public override void OnLoaded()
        {
            base.OnLoaded();

            warningLogSprite = ModFactory.CompareTexture("debugger:warning_log_icon").sprite;
            errorLogSprite = ModFactory.CompareTexture("debugger:error_log_icon").sprite;
            exceptionLogSprite = ModFactory.CompareTexture("debugger:exception_log_icon").sprite;
            normalLogSprite = ModFactory.CompareTexture("debugger:normal_log_icon").sprite;

            //初始化调试器 UI
            LogView.Init();
            FastButtonView.Init();

            //将日志代理到 Debugger
            Application.logMessageReceivedThreaded += LogView.OnHandleLog;
            //// GInit.writeLogsToFile = false;
            //// GInit.totalLogTexts.Clear();

            //将加载调试器前的日志显示
            foreach (var item in GInit.totalLogTexts.ToArray())
            {
                LogView.AddLogShower(item.Item1, item.Item2);
            }

            Performance.OutputComputerInfo();


            GScene.AfterChanged += scene =>
            {
                if (scene.name == SceneNames.GameScene)
                {
                    GameSceneDebugger.Init();

                    //强行停止记录
                    if (GameSceneDebugger.isRecordingStructure) GameSceneDebugger.StopRecordingStructure();
                }
                else
                {
                    GameSceneDebugger.Destroy();
                    EntityInfoShower.EntityCanvasPool.stack.Clear();
                }

                Center.SetUIsToFirst();
            };

            MethodAgent.updates += GameSceneDebugger.Update;
            MethodAgent.updates += LogView.CheckSelectedObject;
            MethodAgent.updates += Center.DebuggerCanvasActiveControl;
            EntityCenter.OnAddEntity += EntityInfoShower.ShowEntityInfo;
            EntityCenter.OnRemoveEntity += EntityInfoShower.RecoverEntityCanvasFrom;
            GFiles.settings.uiSpeed = 100;



            //处理按键
            MethodAgent.updates += () =>
            {
                if (Keyboard.current.pauseKey.wasReleasedThisFrame)
                {
                    Time.timeScale = Time.timeScale == 0 ? 1 : 0;
                }
            };
        }

        public override void OnAllModsLoaded()
        {
            base.OnAllModsLoaded();

            //获取快捷按钮
            ModFactory.EachUserMethod((_, _, method) =>
            {
                //获取特性
                if (!AttributeGetter.TryGetAttribute<FastButtonAttribute>(method, out var attribute))
                    return;

                //检查方法是否是静态方法
                if (!method.IsStatic)
                {
                    Debug.LogError($"方法 {method.DeclaringType.FullName}.{method.Name} 不是静态方法，无法添加快捷按钮");
                    return;
                }

                //检查方法是否有参数
                if (method.GetParameters().Length > 0)
                {
                    Debug.LogError($"方法 {method.DeclaringType.FullName}.{method.Name} 有参数，无法添加快捷按钮");
                    return;
                }

                FastButtonView.fastButtons.Add(new FastButton(ReflectionTools.MethodWrapperAction(null, method), attribute.name, attribute.tooltip));
            });

            //刷新快捷按钮
            FastButtonView.RefreshFastButtons();
        }
    }
}