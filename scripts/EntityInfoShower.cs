using System.Collections.Generic;
using GameCore;
using GameCore.UI;
using SP.Tools;
using SP.Tools.Unity;
using TMPro;
using UnityEngine;

namespace Debugger
{
    public static class EntityInfoShower
    {
        public static readonly Color entityInfoTextColor = new(1, 1, 1, 0.6f);





        public static void ShowEntityInfo(Entity entity)
        {
            Canvas canvas = entity.GetOrAddEntityCanvas();

            TextIdentity text = GameUI.AddText(UIA.Middle, $"debugger:text.entity_info_{entity.netId}", canvas.gameObject);
            text.rt.AddLocalPosY(-45);
            text.text.SetFontSize(7);
            text.text.color = entityInfoTextColor;
            text.text.alignment = TextAlignmentOptions.Top;
            text.text.raycastTarget = false;
            text.autoCompareText = false;
            text.OnUpdate += o => o.rt.localScale = new Vector2(entity.transform.localScale.x.Sign(), 1);
            text.AfterRefreshing += p => p.text.text = $"HP {entity.health}/{entity.maxHealth},\nNetId {entity.netId}";
            entity.OnHealthChange += _ => text.RefreshUI();
        }
    }
}