using System.Collections.Generic;
using System.Reflection;
using GameCore.UI;
using SP.Tools.Unity;
using UnityEngine;

namespace Debugger
{
    public static class FastButtonView
    {
        public static ScrollViewIdentity fastButtonScrollView;
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

        public static void Init()
        {
            /* --------------------------------- 初始化快速按钮列表 -------------------------------- */
            fastButtonScrollView = GameUI.AddScrollView(UIA.UpperLeft, "debugger:scrollview_button_show", LogView.logPanel);
            fastButtonScrollView.SetSizeDelta(LogView.logPanel.sd.x, LogView.logPanel.sd.y);
            fastButtonScrollView.SetAPosOnBySizeRight(LogView.logScrollView, 0);
            fastButtonScrollView.gridLayoutGroup.cellSize = new Vector2(fastButtonScrollView.gridLayoutGroup.cellSize.x, 45);
            fastButtonScrollView.gridLayoutGroup.spacing = new Vector2(0, 2.5f);
            fastButtonScrollView.viewportImage.color = new Color32(0, 0, 0, 1);
            fastButtonScrollView.scrollViewImage.color = new Color32(0, 0, 0, 0);
            fastButtonScrollView.viewportImage.raycastTarget = false;
            fastButtonScrollView.scrollViewImage.raycastTarget = false;
        }
    }
}