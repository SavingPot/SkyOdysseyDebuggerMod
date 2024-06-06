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
        public static readonly Color entityInfoTextColor = new(1, 1, 1, 0.7f);





        public static void ShowEntityInfo(Entity entity) => MethodAgent.DebugRun(() =>
        {
            Canvas canvas = GetOrAddEntityCanvasFrom(entity);

            TextIdentity text = GameUI.AddText(UIA.Middle, $"debugger:text.entity_info_{entity.netId}", canvas.gameObject);
            text.rt.AddLocalPosY(-45);
            text.text.SetFontSize(7);
            text.text.color = entityInfoTextColor;
            text.text.alignment = TextAlignmentOptions.Top;
            text.text.raycastTarget = false;
            text.autoCompareText = false;
            text.OnUpdate += o => o.rt.localScale = new Vector2(entity.transform.localScale.x.Sign(), 1);
            text.AfterRefreshing += p => p.text.text = $"HP={entity.health}/{entity.data.maxHealth},\nNetId={entity.netId}";
            entity.OnHealthChange += () => text.RefreshUI();
        });

        public static Canvas GetEntityCanvasFromEntity(Entity entity)
        {
            Canvas canvas = null;

            for (int i = 0; i < entity.transform.childCount; i++)
            {
                if (entity.transform.GetChild(i).name == "DebuggerEntityCanvas")
                {
                    canvas = entity.transform.GetChild(i).gameObject.GetComponent<Canvas>();
                    break;
                }
            }

            return canvas;
        }

        public static Canvas AddEntityCanvasTo(Entity entity) => EntityCanvasPool.Get(entity);


        public static Canvas GetOrAddEntityCanvasFrom(Entity entity)
        {
            Canvas canvas = GetEntityCanvasFromEntity(entity);

            if (!canvas)
                canvas = AddEntityCanvasTo(entity);

            return canvas;
        }


        public static void RecoverEntityCanvasFrom(Entity entity)
        {
            var canvas = GetEntityCanvasFromEntity(entity);
            if (canvas) EntityCanvasPool.Recover(canvas);
        }

        public static class EntityCanvasPool
        {
            public static Stack<Canvas> stack = new();

            public static Canvas Get(Entity entity)
            {
                var canvas = (stack.Count == 0) ? Generation() : stack.Pop();

                canvas.transform.SetParent(entity.transform, false);
                canvas.transform.localPosition = Vector3.zero;
                canvas.transform.localScale = new Vector2(0.075f, 0.075f);
                canvas.enabled = true;

                return canvas;
            }

            public static void Recover(Canvas canvas)
            {
                canvas.enabled = false;
                canvas.transform.SetParent(null);
                canvas.transform.DestroyChildren();
                stack.Push(canvas);
            }

            public static Canvas Generation()
            {
                GameObject go = new("DebuggerEntityCanvas");
                Canvas canvas = go.AddComponent<Canvas>();

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 20;

                return canvas;
            }
        }
    }
}