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
        public static readonly Vector2 viewAnchorMin = new(0.85f, 0.5f);
        public static readonly Vector2 viewAnchorMax = Vector2.one;
        public static readonly List<FastButton> fastButtons = new();
        public static ScrollViewIdentity fastButtonScrollView;



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
            fastButtonScrollView.SetAnchorMinMax(viewAnchorMin, viewAnchorMax);
            fastButtonScrollView.ap = Vector2.zero;
            fastButtonScrollView.sd = Vector2.zero;
            fastButtonScrollView.gridLayoutGroup.cellSize = new Vector2(GameUI.canvasScaler.referenceResolution.x * (fastButtonScrollView.rt.anchorMax.x - fastButtonScrollView.rt.anchorMin.x), 35);
            fastButtonScrollView.gridLayoutGroup.spacing = new Vector2(0, 2.5f);
            fastButtonScrollView.viewportImage.color = new Color32(0, 0, 0, 1);
            fastButtonScrollView.scrollViewImage.color = new Color32(0, 0, 0, 0);
            fastButtonScrollView.viewportImage.raycastTarget = false;
            fastButtonScrollView.scrollViewImage.raycastTarget = false;
        }
    }
}