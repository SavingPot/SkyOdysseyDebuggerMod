using System;
using System.Collections.Generic;
using System.Reflection;
using GameCore;
using GameCore.UI;
using SP.Tools.Unity;
using UnityEngine;

namespace Debugger
{
    public class FastButton
    {
        public Action action;
        public string name;
        public string tooltip;

        public FastButton(Action action, string name, string tooltip)
        {
            this.action = action;
            this.name = name;
            this.tooltip = tooltip;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class FastButtonAttribute : Attribute
    {
        public string name;
        public string tooltip;

        public FastButtonAttribute(string name, string tooltip = null)
        {
            this.name = name;
            this.tooltip = tooltip;
        }
    }

    public static class FastButtonView
    {
        public static readonly List<FastButton> fastButtons = new();
        public static ScrollViewIdentity fastButtonScrollView;
        public static Vector2 viewSize = new(210, LogView.logPanel.sd.y);



        public static void RefreshFastButtons()
        {
            fastButtonScrollView.Clear();

            foreach (var fast in fastButtons)
            {
                var button = GameUI.AddButton(UIA.Middle, $"debugger:button.fast_button.{Tools.randomGUID}");
                button.buttonText.autoCompareText = false;
                button.buttonText.text.text = fast.name;

                button.OnClickBind(new(fast.action));

                fastButtonScrollView.AddChild(button);
            }
        }

        public static void Init()
        {
            /* --------------------------------- 初始化快速按钮列表 -------------------------------- */
            fastButtonScrollView = GameUI.AddScrollView(UIA.UpperRight, "debugger:scrollview_button_show", Center.GetMainCanvas().transform);
            fastButtonScrollView.SetSizeDelta(viewSize);
            fastButtonScrollView.SetAPos(-fastButtonScrollView.sd.x / 2, -LogView.detailedLogBackground.sd.y);
            fastButtonScrollView.gridLayoutGroup.cellSize = new Vector2(200, 35);
            fastButtonScrollView.gridLayoutGroup.spacing = new Vector2(0, 2.5f);
            fastButtonScrollView.viewportImage.color = new Color32(0, 0, 0, 1);
            fastButtonScrollView.scrollViewImage.color = new Color32(0, 0, 0, 0);
            fastButtonScrollView.viewportImage.raycastTarget = false;
            fastButtonScrollView.scrollViewImage.raycastTarget = false;
        }
    }
}