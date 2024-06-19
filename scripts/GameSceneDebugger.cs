using System.Collections.Generic;
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
        class BlockRecord
        {
            public Vector2Int offset;
            public bool isBackground;
            public string id;

            public BlockRecord(Vector2Int offset, bool isBackground, string id)
            {
                this.offset = offset;
                this.isBackground = isBackground;
                this.id = id;
            }
        }

        public static InputButtonIdentity randomUpdateIB;
        public static InputButtonIdentity time24IB;
        public static TextIdentity gameStatusText;
        private static readonly StringBuilder gameStatusSB = new();
        public static bool isRecordingStructure;
        static Vector2Int recordAnchorPos;
        static List<BlockRecord> recordedBlocks = new();
        public const int inputButtonsHeight = 30;


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




            //绑定结构体记录事件
            GameCallbacks.OnAddBlock += EnqueueBlockToQueue;
            GameCallbacks.OnBlockDestroyed += DequeueBlockFromQueue;
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




        [FastButton("给予100金币")]
        static void GiveCoins100()
        {
            Player.local.ServerAddCoin(100);
        }

        [FastButton("给予梯子")]
        static void GiveLadders()
        {
            Player.local.ServerAddItem(ModFactory.CompareItem(BlockID.Ladder).DataToItem().SetCount(32));
        }

        [FastButton("生成结构体")]
        static void GenerateStructure()
        {
            var structure = ModFactory.CompareStructure(StructureID.GhostShip);

            Map.instance.GenerateStructure(structure, PosConvert.WorldToMapPos(Player.local.transform.position));
        }

        [FastButton("在记录锚点处生成结构体")]
        static void GenerateStructureAtRecordAnchor()
        {
            if (recordAnchorPos.x == int.MaxValue && recordAnchorPos.y == int.MaxValue)
            {
                Debug.LogWarning("请先设置记录锚点");
                return;
            }

            var structure = ModFactory.CompareStructure(StructureID.GhostShip);

            Map.instance.GenerateStructure(structure, recordAnchorPos);
        }

        [FastButton("开始或结束 记录结构体", "Click to say hello to the console")]
        static void RecordStructure()
        {
            if (isRecordingStructure)
                StopRecordingStructure();
            else
                StartRecordingStructure();
        }

        public static void StartRecordingStructure()
        {
            isRecordingStructure = true;
            recordAnchorPos = new(int.MaxValue, int.MaxValue);
            recordedBlocks.Clear();
            Debug.Log("开始记录结构体");
        }

        public static void EnqueueBlockToQueue(Vector2Int pos, bool isBackground, Block block, Chunk chunk)
        {
            if (!isRecordingStructure)
                return;

            //设置锚点位置
            if (recordAnchorPos.x == int.MaxValue && recordAnchorPos.y == int.MaxValue)
                recordAnchorPos = pos;

            //计算相对锚点偏移
            var offset = pos - recordAnchorPos;

            //检查是否有相同的方块
            for (int i = 0; i < recordedBlocks.Count; i++)
            {
                var blockRecord = recordedBlocks[i];
                if (blockRecord.offset == offset && blockRecord.isBackground == isBackground)
                {
                    recordedBlocks.RemoveAt(i);
                    Debug.LogWarning($"已经记录过相同的方块了, 将被覆盖, 位置: {pos}, 偏移: {offset}, 背景: {isBackground}");
                    break;
                }
            }

            //添加到队列
            recordedBlocks.Add(new(offset, isBackground, block.data.id));
        }

        public static void DequeueBlockFromQueue(Vector2Int pos, bool isBackground, BlockData blockData)
        {
            if (!isRecordingStructure)
                return;

            //计算相对锚点偏移
            var offset = pos - recordAnchorPos;

            //删除队列中的对应位置的方块
            for (int i = 0; i < recordedBlocks.Count; i++)
            {
                var block = recordedBlocks[i];
                if (block.offset == offset && block.isBackground == isBackground)
                {
                    recordedBlocks.RemoveAt(i);
                    return;
                }
            }

            //如果被破坏的方块不在队列中, 则在该位置添加一个空方块
            recordedBlocks.Add(new(offset, isBackground, null));
        }

        public static void StopRecordingStructure()
        {
            isRecordingStructure = false;

            var structure = new StructureData
            {
                fixedBlocks = new AttachedBlockDatum[recordedBlocks.Count],
                jsonFormat = GInit.gameVersion,
            };

            for (int i = 0; i < recordedBlocks.Count; i++)
            {
                var block = recordedBlocks[i];
                structure.fixedBlocks[i] = new(block.id, block.offset, block.isBackground);
            }

            Debug.Log(structure.ToJObject().ToString(Newtonsoft.Json.Formatting.None));
            Debug.Log("结束记录结构体");
        }
    }
}