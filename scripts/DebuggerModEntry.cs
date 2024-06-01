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
        public static bool setCameraOrthographicSize = true;
        public static float cameraEnableOrthographicSize = 20;
        public static float cameraDisableOrthographicSize = 11;

        public override void OnLoaded()
        {
            base.OnLoaded();

            //初始化日志面板
            Core.InitLogPanel();

            //将加载调试器前的日志显示
            foreach (var item in GInit.totalLogTexts)
            {
                Core.AddLogShower(item.Item1, item.Item2);
            }

            Performance.OutputComputerInfo();

            //将日志代理到 Debugger
            //// GInit.writeLogsToFile = false;
            //// GInit.totalLogTexts.Clear();
            Application.logMessageReceivedThreaded += Core.OnHandleLog;

            GScene.AfterChanged += scene =>
            {
                Core.RefreshFastButtons();

                if (scene.name == SceneNames.GameScene)
                {
                    Core.InitPanelForGameScene();
                }
                else
                {
                    Core.DestroyPanelForGameScene();
                    Core.EntityCanvasPool.stack.Clear();
                }

                Core.SetUIsToFirst();
            };

            MethodAgent.updates += RefreshTexts;
            MethodAgent.updates += Core.CheckSelectedObject;
            MethodAgent.updates += Core.DebuggerCanvasActiveControl;
            EntityCenter.OnAddEntity += Core.ShowEntityNetId;
            EntityCenter.OnRemoveEntity += Core.RecoverEntityCanvasFrom;
            Chunk.SetRenderersEnabled += (chunk, enabled) =>
            {
                Core.chunk.SetLineRendererActivity(chunk, false);
            };
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
            Core.SetUIsToFirst();

            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
            }
        }

        public void RefreshTexts()
        {
            if (Core.logPanel)
            {
                if (Core.gameStatusText && Core.gameStatusText.gameObject.activeInHierarchy)
                    Core.gameStatusText.RefreshUI();

                if (setCameraOrthographicSize && GScene.active.name == SceneNames.GameScene)
                {
                    Tools.instance.mainCamera.orthographicSize = cameraEnableOrthographicSize;
                }
            }
            else
            {
                if (setCameraOrthographicSize && GScene.active.name == SceneNames.GameScene)
                {
                    Tools.instance.mainCamera.orthographicSize = cameraDisableOrthographicSize;
                }
            }
        }
    }
}