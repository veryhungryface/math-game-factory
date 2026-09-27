using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.HyeopgokSasu
{
    // Fractions are real stacked numerator/bar/denominator UI, never slash text.
    public sealed class HyeopgokMathText
    {
        public readonly TextMeshProUGUI Text;
        struct F { public string n,d; public int raw; public float w; }
        readonly List<RectTransform> boxes=new List<RectTransform>();
        readonly List<F> fractions=new List<F>();
        static readonly Regex token=new Regex(@"\{frac:(-?\d+)/(\d+)\}");
        string lastContent;float lastFont=-1,lastWidth=-1,lastHeight=-1,lastLineSpacing;Color lastColor;
        int lastVisibleLines=-1;TextOverflowModes lastOverflow;TextWrappingModes lastWrapping;
        public HyeopgokMathText(TextMeshProUGUI text){Text=text;Text.richText=true;}
        public void Set(string content)
        {
            content=(content??"").Replace("<sup>","<voffset=0.28em><size=78%>").Replace("</sup>","</size></voffset>")
                .Replace("<sub>","<voffset=-0.18em><size=78%>").Replace("</sub>","</size></voffset>");
            float width=Text.rectTransform.rect.width,height=Text.rectTransform.rect.height;
            if(content==lastContent&&Text.fontSize==lastFont&&width==lastWidth&&height==lastHeight&&Text.color==lastColor&&Text.maxVisibleLines==lastVisibleLines&&Text.overflowMode==lastOverflow&&Text.textWrappingMode==lastWrapping&&Text.lineSpacing==lastLineSpacing)return;
            lastContent=content;lastFont=Text.fontSize;lastWidth=width;lastHeight=height;lastColor=Text.color;
            lastVisibleLines=Text.maxVisibleLines;lastOverflow=Text.overflowMode;lastWrapping=Text.textWrappingMode;lastLineSpacing=Text.lineSpacing;
            foreach(var b in boxes)b.gameObject.SetActive(false);fractions.Clear();
            string result="";int end=0;
            foreach(Match m in token.Matches(content)){
                result+=content.Substring(end,m.Index-end);
                int raw=result.Length;string n=m.Groups[1].Value,d=m.Groups[2].Value;
                string placeholder=new string('0',Mathf.Max(n.Length,d.Length)+1);
                result+="<nobr><color=#00000000>"+placeholder+"</color></nobr>";
                fractions.Add(new F{n=n,d=d,raw=raw+23,w=placeholder.Length});
                end=m.Index+m.Length;
            }
            result+=content.Substring(end);Text.text=result;
            Text.ForceMeshUpdate(true,true);
            for(int k=0;k<fractions.Count;k++){
                F f=fractions[k];int ci=-1;
                for(int j=0;j<Text.textInfo.characterCount;j++)if(Text.textInfo.characterInfo[j].index>=f.raw){ci=j;break;}
                if(ci<0)continue;
                var ch=Text.textInfo.characterInfo[ci];
                // The parent can collapse to two lines. Fractions on hidden lines
                // must disappear with it, including their separate bar/number quads.
                if(ch.lineNumber>=Text.maxVisibleLines||!ch.isVisible)continue;
                var last=Text.textInfo.characterInfo[Mathf.Min(ci+(int)f.w-1,Text.textInfo.characterCount-1)];
                float w=last.xAdvance-ch.origin;
                RectTransform box;
                if(k>=boxes.Count){
                    var go=new GameObject("Stacked fraction",typeof(RectTransform));box=(RectTransform)go.transform;box.SetParent(Text.transform,false);boxes.Add(box);
                    MakeNumber(box,"numerator");MakeNumber(box,"denominator");
                    var line=new GameObject("fraction bar",typeof(RectTransform),typeof(Image));line.transform.SetParent(box,false);line.GetComponent<Image>().color=Text.color;
                }else box=boxes[k];
                box.gameObject.SetActive(true);box.anchorMin=box.anchorMax=new Vector2(.5f,.5f);box.pivot=new Vector2(.5f,.5f);
                box.anchoredPosition=new Vector2(ch.origin+w*.5f,ch.baseLine+Text.fontSize*.3f);box.sizeDelta=new Vector2(w,Text.fontSize*1.3f);
                var nT=box.GetChild(0).GetComponent<TextMeshProUGUI>();var dT=box.GetChild(1).GetComponent<TextMeshProUGUI>();
                nT.font=dT.font=Text.font;nT.fontSharedMaterial=dT.fontSharedMaterial=Text.fontSharedMaterial;
                nT.text=f.n;dT.text=f.d;nT.fontSize=dT.fontSize=Text.fontSize*.82f;nT.color=dT.color=Text.color;
                nT.rectTransform.anchoredPosition=new Vector2(0,Text.fontSize*.35f);dT.rectTransform.anchoredPosition=new Vector2(0,-Text.fontSize*.35f);
                nT.rectTransform.sizeDelta=dT.rectTransform.sizeDelta=new Vector2(w+5,Text.fontSize*.8f);
                var bar=(RectTransform)box.GetChild(2);bar.anchoredPosition=Vector2.zero;bar.sizeDelta=new Vector2(w*.9f,Mathf.Max(1.8f,Text.fontSize*.07f));bar.GetComponent<Image>().color=Text.color;
            }
        }
        void MakeNumber(RectTransform parent,string name){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var t=go.AddComponent<TextMeshProUGUI>();t.font=Text.font;t.fontSharedMaterial=Text.fontSharedMaterial;t.fontStyle=FontStyles.Normal;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;}
    }
}
