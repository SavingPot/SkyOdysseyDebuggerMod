using GameCore;
using GameCore.High;
using GameCore.UI;
using UnityEngine;
using SP.Tools.Unity;
using System.Linq;
using System.Collections;
using UnityEngine.InputSystem;

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
                FastButtonView.RefreshFastButtons();

                if (scene.name == SceneNames.GameScene)
                {
                    GameSceneDebugger.Init();
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
            CoroutineStarter.Do(IESetUIsToFirst());



            //处理按键
            MethodAgent.updates += () =>
            {
                if (Keyboard.current.pauseKey.wasReleasedThisFrame)
                {
                    Time.timeScale = Time.timeScale == 0 ? 1 : 0;
                }
            };
        }

        IEnumerator IESetUIsToFirst()
        {
            Center.SetUIsToFirst();

            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
            }
        }
    }
}