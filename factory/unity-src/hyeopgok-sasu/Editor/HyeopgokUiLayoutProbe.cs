using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mgf;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Mgf.HyeopgokSasu.Editor
{
    public static class HyeopgokUiLayoutProbe
    {
        [Serializable] class Sample { public string name,visible;public int fullLines,visibleLines;public float panelHeight;public bool canExpand,pass; }
        [Serializable] class Report { public List<Sample> samples=new List<Sample>();public bool pass=true; }
        public static void Run(){
            var report=new Report();
            string shortPrompt="동전 한 개를 던질 때, 나올 수 있는 모든 경우의 수를 구하시오.";
            string longPrompt="다음 중 y가 x의 함수인 것의 개수를 구하시오. ㄱ. x살인 학생의 5년 후의 나이 y살 ㄴ. 넓이가 20 cm²인 직사각형의 가로의 길이 x cm와 세로의 길이 y cm ㄷ. 자연수 x를 5로 나눈 나머지 y ㄹ. 둘레의 길이가 x cm인 직사각형의 넓이 y cm² ㅁ. 한 변의 길이가 x cm인 정사각형의 둘레의 길이 y cm";
            foreach(string prompt in new[]{shortPrompt,longPrompt}){
                MgfText.Prewarm(prompt);
                var text=MgfText.Ui("",new Vector2(.5f,.5f),Vector2.zero,17.5f,Color.black,298);text.rectTransform.sizeDelta=new Vector2(298,500);text.alignment=TextAlignmentOptions.TopLeft;text.textWrappingMode=TextWrappingModes.Normal;text.lineSpacing=0;
                var math=new HyeopgokMathText(text);string styled="<line-height=22>"+prompt+"</line-height>";
                text.maxVisibleLines=100;text.overflowMode=TextOverflowModes.Overflow;math.Set(styled);text.ForceMeshUpdate(true,true);
                int lines=text.textInfo.lineCount;float span=text.textInfo.lineInfo[0].ascender-text.textInfo.lineInfo[Math.Min(1,lines-1)].descender+7;
                text.maxVisibleLines=2;text.overflowMode=TextOverflowModes.Masking;math.Set(styled);text.ForceMeshUpdate(true,true);
                var visible=new StringBuilder();int visibleLines=0;
                for(int i=0;i<text.textInfo.characterCount;i++){var c=text.textInfo.characterInfo[i];if(!c.isVisible||c.lineNumber>=2)continue;visible.Append(c.character);visibleLines=Math.Max(visibleLines,c.lineNumber+1);}
                bool shortCase=prompt==shortPrompt;var sample=new Sample{name=shortCase?"two-line-question":"long-question",fullLines=lines,visibleLines=visibleLines,panelHeight=Math.Max(66,span+14),canExpand=lines>2,visible=visible.ToString()};
                sample.pass=visibleLines==2&&!sample.visible.Contains("…")&&(shortCase?lines==2&&sample.visible.EndsWith("구하시오."):lines>2);report.pass&=sample.pass;report.samples.Add(sample);UnityEngine.Object.DestroyImmediate(text.gameObject);
            }
            string json=JsonUtility.ToJson(report,true);File.WriteAllText(Environment.GetEnvironmentVariable("HYEOPGOK_UI_PROBE_OUT")??"/tmp/hyeopgok-ui-layout-probe.json",json);Debug.Log("[HYEOPGOK_UI_PROBE] "+json);
            if(!report.pass)throw new Exception("Two-line quest layout probe failed");
        }
    }
}
