using System;
using System.Diagnostics;
using System.IO;
using GameCore.UI;
using SP.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

namespace Debugger
{
    public class LinkText : MonoBehaviour, IPointerClickHandler
    {
        public TMP_Text content;

        public void OnPointerClick(PointerEventData eventData)
        {
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(content, eventData.position, null);
            if (linkIndex != -1)
            {
                //检查 VSCode 是否安装
                string codePath = "D:\\Apps\\Microsoft VS Code\\Code.exe"; //TODO
                if (!File.Exists(codePath))
                {
                    Debug.LogError("VS Code not found!");
                    return;
                }

                //启动 VSCode
                var linkInfo = content.textInfo.linkInfo[linkIndex];
                var commandLineArgs = $"--goto \"{linkInfo.GetLinkText()}\"";
                Process.Start(new ProcessStartInfo(codePath, commandLineArgs));
            }
        }
    }
}