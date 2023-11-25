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
    public class CoreEntry : ModEntry
    {
        public static bool setCameraOrthographicSize = true;
        public static float cameraEnableOrthographicSize = 20;
        public static float cameraDisableOrthographicSize = 10;

        public override void OnLoaded()
        {
            base.OnLoaded();

            Performance.OutputComputerInfo();

            InternalUIAdder.AfterRefreshModView += () =>
            {
                MethodAgent.CallUntil(() => GameObject.FindObjectOfType<ModMaker>(), () => MethodAgent.CallNextFrame(() => GameObject.FindObjectOfType<ModMaker>().AddChangeContentButton()));
            };
            Core.InitAllUIs();

            //将加载调试器前的日志显示
            for (int i = 0; i < Tools.totalLogTexts.Count; i++)
            {
                var ele = Tools.totalLogTexts.ElementAt(i);
                Core.AddLogShower(ele.Key, ele.Value);
            }
            GScene.AfterChanged += scene =>
            {
                Core.InitAllUIs();

                if (scene.name == SceneNames.GameScene)
                {
                    Core.InitGameStatusText();
                    Core.InitRandomUpdateIB();
                    Core.InitTime24IB();
                }
                else
                {
                    GameObject.Destroy(Core.gameStatusText.gameObject);
                    GameObject.Destroy(Core.randomUpdateIB.gameObject);
                    GameObject.Destroy(Core.time24IB.gameObject);
                }

                Core.SetDetailedLogPos();
                Core.SetUIsToFirst();
            };

            //将日志代理到 Debugger
            Tools.writeLogsToFile = false;
            Tools.totalLogTexts.Clear();
            Application.logMessageReceivedThreaded += Core.OnHandleLog;

            MethodAgent.AddUpdate(RefreshTexts);
            MethodAgent.AddUpdate(Core.CheckSelectedObject);
            MethodAgent.AddUpdate(Core.DebuggerCanvasActiveControl);
            Entity.OnEntityGetNetworkId += Core.ShowEntityNetId;
            Chunk.SetRenderersEnabled += (chunk, enabled) =>
            {
                Core.chunk.SetLineRendererActivity(chunk, false);
            };
            GFiles.settings.uiSpeed = 100;
            CoroutineStarter.Do(IESetUIsToFirst());



            //处理按键
            MethodAgent.AddUpdate(() =>
            {
                if (Keyboard.current.pauseKey.wasReleasedThisFrame)
                {
                    Time.timeScale = Time.timeScale == 0 ? 1 : 0;
                }
            });
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