using GameCore;
using GameCore.UI;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Debugger
{
    public static class Center
    {
        public static bool enabled = true;



        private static Canvas m_mainCanvas;
        public static Canvas GetMainCanvas()
        {
            if (!m_mainCanvas)
            {
                m_mainCanvas = GameObject.Instantiate(GInit.instance.canvasPrefab);
                m_mainCanvas.gameObject.name = "DebuggerCanvas-Main";
                m_mainCanvas.sortingOrder = 10000;
                var scaler = m_mainCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = GameUI.canvasScaler.uiScaleMode;
                scaler.referenceResolution = GameUI.canvasScaler.referenceResolution;

                GameObject.DontDestroyOnLoad(m_mainCanvas.gameObject);
            }

            return m_mainCanvas;
        }

        private static Canvas m_frequentSimpleCanvas;
        public static Canvas GetFrequentSimpleCanvas()
        {
            if (!m_frequentSimpleCanvas)
            {
                m_frequentSimpleCanvas = GameObject.Instantiate(GInit.instance.canvasPrefab);
                m_frequentSimpleCanvas.gameObject.name = "DebuggerCanvas-FrequentSimple";
                m_frequentSimpleCanvas.sortingOrder = 10000;
                var scaler = m_frequentSimpleCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = GameUI.canvasScaler.uiScaleMode;
                scaler.referenceResolution = GameUI.canvasScaler.referenceResolution;

                GameObject.DontDestroyOnLoad(m_frequentSimpleCanvas.gameObject);
            }

            return m_frequentSimpleCanvas;
        }








        public static void DebuggerCanvasActiveControl()
        {
            if (Keyboard.current != null && Keyboard.current.rightShiftKey.isPressed && Keyboard.current.mKey.wasPressedThisFrame)
            {
                //bool newState = !GetDebuggerCanvas().gameObject.activeSelf;
                //GetDebuggerCanvas().gameObject.SetActive(newState);

                bool newState = !GetMainCanvas().enabled;
                GetMainCanvas().enabled = newState;
                GetFrequentSimpleCanvas().enabled = newState;

                //避免在非游戏场景生成 Blockmap
                //if (Blockmap.HasInstance())
                //    chunk.SetAll(newState);
            }
        }

        public static void SetUIsToFirst()
        {
            GetMainCanvas().transform.SetAsFirstSibling();
            GetFrequentSimpleCanvas().transform.SetAsFirstSibling();
        }
    }
}
