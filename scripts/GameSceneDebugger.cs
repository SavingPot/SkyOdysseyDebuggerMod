using System.Text;
using GameCore;
using GameCore.UI;
using SP.Tools;
using SP.Tools.Unity;
using UnityEngine;

namespace Debugger
{
    public static class GameSceneDebugger
    {
        public static InputButtonIdentity randomUpdateIB;
        public static InputButtonIdentity time24IB;
        public static TextIdentity gameStatusText;
        private static readonly StringBuilder gameStatusSB = new();
        public static int inputButtonsHeight = 30;


        public static void Init()
        {
            var logPanel = LogView.logPanel;

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



            /* --------------------------------- 游戏状态显示 --------------------------------- */
            gameStatusText = GameUI.AddText(UIA.UpperLeft, "debugger:text.game_status", Center.GetFrequentSimpleCanvas().transform);
            gameStatusText.text.SetFontSize(12);
            gameStatusText.SetSizeDelta(600, logPanel.sd.y);
            gameStatusText.SetAPos(logPanel.ap.x + (gameStatusText.sd.x / 2) + (logPanel.sd.x / 2) + 10, -LogView.detailedLogSize.y - gameStatusText.sd.y / 2 - 10);
            gameStatusText.text.alignment = TMPro.TextAlignmentOptions.TopLeft;
            gameStatusText.text.raycastTarget = false;
            gameStatusText.AfterRefreshing += t =>
            {
                try
                {
                    gameStatusSB.Clear();

                    gameStatusSB.AppendLine($"FPS: {(int)Tools.smoothFps}");
                    gameStatusSB.AppendLine($"日志显示器数量: {LogView.logShowers.Count}/{LogView.LogShowerPool.createIndex}");

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
        }

        public static void Update()
        {
            if (gameStatusText && gameStatusText.gameObject.activeInHierarchy)
                gameStatusText.RefreshUI();
        }

        public static void Destroy()
        {
            GameObject.Destroy(randomUpdateIB.gameObject);
            GameObject.Destroy(time24IB.gameObject);
            GameObject.Destroy(gameStatusText.gameObject);
        }
    }
}