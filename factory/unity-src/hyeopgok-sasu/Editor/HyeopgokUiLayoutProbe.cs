#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.HyeopgokSasu.Editor
{
    // Editor-side guard for the same dimensions used by the browser v3 gate.
    // It verifies the longest currently eligible choice prompt without folding,
    // ellipsis or a fixed-height crop at 390x844 and 1280x800.
    public static class HyeopgokUiLayoutProbe
    {
        [Serializable]
        sealed class Sample
        {
            public string name,promptId;
            public int screenWidth,screenHeight,promptCharacters,lineCount,totalCharacters,visibleCharacters,measuredGlyphs;
            public float fontSize,lineHeight,panelWidth,bodyWidth,preferredHeight,bodyHeight,panelHeight;
            public float canvasScale,minimumVisibleGlyphHeightPx,requiredGlyphHeightPx;
            public bool fontContract,noOverflow,allCharactersVisible,noEllipsis,bodyFitsPanel,glyphHeightPass,pass;
        }

        [Serializable]
        sealed class Report
        {
            public int schema_version=3,packCount;
            public string longestPack,longestPromptId,longestPrompt;
            public int longestPromptCharacters;
            public string contract="390x844 uses font 24 and >=14px visible body glyphs; 1280x800 uses font 36 and >=20px. Full choice prompt remains unfolded and unclipped.";
            public List<Sample> samples=new List<Sample>();
            public bool pass=true;
        }

        sealed class PromptFixture
        {
            public string pack,id,prompt;
        }

        static string RepoPath()
        {
            string repo=Environment.GetEnvironmentVariable("MGF_HYEOPGOK_REPO");var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-hyeopgokRepo")repo=args[i+1];
            return string.IsNullOrEmpty(repo)?"/Users/sitpo/math-game-factory":repo;
        }

        static PromptFixture LongestChoicePrompt(string repo,out int packCount)
        {
            string directory=Path.Combine(repo,"public/g/hyeopgok-sasu/packs");
            var index=JsonUtility.FromJson<PackIndex>(File.ReadAllText(Path.Combine(directory,"index.json")));
            if(index==null||index.packs==null)throw new Exception("Pack index is unavailable");
            packCount=index.packs.Length;PromptFixture longest=null;
            foreach(var entry in index.packs){
                string file=string.IsNullOrEmpty(entry.file)?entry.pack_id+".json":entry.file;
                var source=HyeopgokPackJson.Parse(File.ReadAllText(Path.Combine(directory,file)));
                var pack=HyeopgokGame.FilterEligibleChoices(source,out _);
                if(pack==null||pack.items==null)continue;
                foreach(var item in pack.items)if(longest==null||(item.prompt??"").Length>longest.prompt.Length)
                    longest=new PromptFixture{pack=pack.pack_id,id=item.id,prompt=item.prompt??""};
            }
            if(longest==null)throw new Exception("No eligible choice prompt exists");
            return longest;
        }

        static bool IsMeasuredBodyGlyph(char value)
        {
            return value>='0'&&value<='9'||value>='A'&&value<='Z'||value>='가'&&value<='힣';
        }

        static void MeasureText(TextMeshProUGUI text,float canvasScale,ref int total,ref int visible,ref int glyphs,ref float minimum)
        {
            text.ForceMeshUpdate(true,true);var info=text.textInfo;
            for(int i=0;i<info.characterCount;i++){
                var character=info.characterInfo[i];
                bool ink=!char.IsWhiteSpace(character.character)&&!char.IsControl(character.character)&&character.color.a>0;
                if(!ink)continue;
                total++;if(!character.isVisible)continue;visible++;
                if(!IsMeasuredBodyGlyph(character.character))continue;
                float height=Mathf.Abs(character.topRight.y-character.bottomLeft.y)*canvasScale;
                if(height>0){glyphs++;minimum=Mathf.Min(minimum,height);}
            }
        }

        static Sample Probe(PromptFixture fixture,int width,int height,float font,float required)
        {
            bool wide=width/(float)height>1.2f;
            float canvasWidth=844f*width/Mathf.Max(1,height);
            float panelWidth=wide?Mathf.Min(canvasWidth*.76f,1060):Mathf.Min(canvasWidth-16,520);
            float bodyWidth=panelWidth-78,lineHeight=wide?45:32;
            float canvasScale=height/844f;

            MgfText.Prewarm(fixture.prompt);
            var text=MgfText.Ui("",new Vector2(.5f,.5f),Vector2.zero,font,Color.black,bodyWidth);
            text.rectTransform.sizeDelta=new Vector2(bodyWidth,2000);
            text.alignment=TextAlignmentOptions.TopLeft;text.lineSpacing=0;
            text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Overflow;text.maxVisibleLines=100;
            var math=new HyeopgokMathText(text);string styled="<line-height="+lineHeight+">"+fixture.prompt+"</line-height>";
            math.Set(styled);text.ForceMeshUpdate(true,true);
            float preferred=text.preferredHeight,natural=Mathf.Max(font*1.35f,preferred+6);
            float bodyHeight=natural+4,panelHeight=Mathf.Max(wide?86:78,natural+24);
            text.rectTransform.sizeDelta=new Vector2(bodyWidth,bodyHeight);
            math.Set(styled);text.ForceMeshUpdate(true,true);

            int total=0,visible=0,glyphs=0;float minimum=float.MaxValue;
            MeasureText(text,canvasScale,ref total,ref visible,ref glyphs,ref minimum);
            var children=text.GetComponentsInChildren<TextMeshProUGUI>(true);
            for(int i=0;i<children.Length;i++)if(children[i]!=text&&children[i].gameObject.activeInHierarchy)
                MeasureText(children[i],canvasScale,ref total,ref visible,ref glyphs,ref minimum);
            if(glyphs==0)minimum=0;

            var sample=new Sample{
                name=width.ToString(),promptId=fixture.id,screenWidth=width,screenHeight=height,promptCharacters=fixture.prompt.Length,
                lineCount=text.textInfo.lineCount,totalCharacters=total,visibleCharacters=visible,measuredGlyphs=glyphs,
                fontSize=font,lineHeight=lineHeight,panelWidth=panelWidth,bodyWidth=bodyWidth,preferredHeight=preferred,
                bodyHeight=bodyHeight,panelHeight=panelHeight,canvasScale=canvasScale,minimumVisibleGlyphHeightPx=minimum,requiredGlyphHeightPx=required,
                fontContract=Mathf.Abs(text.fontSize-font)<.01f,noOverflow=!text.isTextOverflowing,
                allCharactersVisible=total>0&&visible==total,noEllipsis=!text.text.Contains("…"),
                bodyFitsPanel=bodyHeight+20<=panelHeight+.01f,glyphHeightPass=glyphs>=4&&minimum+1e-4f>=required
            };
            sample.pass=sample.fontContract&&sample.noOverflow&&sample.allCharactersVisible&&sample.noEllipsis&&sample.bodyFitsPanel&&sample.glyphHeightPass;
            UnityEngine.Object.DestroyImmediate(text.gameObject);
            return sample;
        }

        public static void Run()
        {
            string repo=RepoPath();var longest=LongestChoicePrompt(repo,out int packCount);
            var report=new Report{packCount=packCount,longestPack=longest.pack,longestPromptId=longest.id,longestPrompt=longest.prompt,longestPromptCharacters=longest.prompt.Length};
            report.samples.Add(Probe(longest,390,844,24,14));
            report.samples.Add(Probe(longest,1280,800,36,20));
            foreach(var sample in report.samples)report.pass&=sample.pass;
            string json=JsonUtility.ToJson(report,true)+"\n";
            string output=Environment.GetEnvironmentVariable("HYEOPGOK_UI_PROBE_OUT");
            if(string.IsNullOrEmpty(output))output="/tmp/hyeopgok-v3-ui-layout-probe.json";
            File.WriteAllText(Path.GetFullPath(output),json);
            Debug.Log("[HYEOPGOK_V3_UI_PROBE] "+JsonUtility.ToJson(report));
            if(!report.pass)throw new Exception("V3 unfolded question layout probe failed; see "+output);
        }
    }
}
#endif
