using System;
using System.Diagnostics;
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
                //构建命令行参数
                string codePath = "D:\\Apps\\Microsoft VS Code\\Code.exe";
                var linkInfo = content.textInfo.linkInfo[linkIndex];
                var commandLineArgs = $"--goto \"{linkInfo.GetLinkText()}\"";

                //启动 VSCode
                Process.Start(new ProcessStartInfo(codePath, commandLineArgs));
            }
        }
    }
}