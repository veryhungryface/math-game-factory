// 폭신한 꼭대기 — 꿰맨 천 라벨 HUD. 세로는 위쪽 두 장, 가로는 왼쪽 한 기둥으로 합친다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.PoksinhanKkokdaegi
{
    public sealed class CushionHud
    {
        public const string AllStaticText="폭신한꼭대기늘려라네모방석고리를당겨시작중학교학년산포도최고첫제출연습봉합실시간정지초필요남은실수본판목표허용회기록평균분산표준편차자료개수편차의제곱합칸변길이같게정하시오모서리끌어놓으시오격자위에서손떼기초록아래로표시다시만드시오완성탑이여기멈췄습니다큰누르면쌓기짝비교최솟값최댓값한번더까지놓습니다솔기풀렸가연습은줄지않넓이나누항상쓰범위절반모든절댓값아닌만든보다작큽게임의입니다더고릅니다풀이끝났가닥모두썼닿을수없평균이같아도됩";
        static readonly Color Ivory=MgfLook.Hex("#F5E7CC"), Ink=MgfLook.Hex("#352A32"), Deep=MgfLook.Hex("#8C2F49"), Lime=MgfLook.Hex("#B9CC45"), Alarm=MgfLook.Hex("#B62245");

        CanvasGroup titleUi,gameUi,revealUi,endUi,toastUi;
        Image hudBg,recordBg,revealBg,endBg,ringImg,ghostImg,targetImg,rippleImg,sideBg;
        TextMeshProUGUI titleMeta,progressTxt,livesTxt,timeTxt,masteryTxt,promptTxt,recordTxt,formulaTxt,guideTxt,sideTxt,targetTxt;
        TextMeshProUGUI revealTitle,revealMath,endTitle,endStats,endTap,toastTxt;
        readonly Image[] stamps=new Image[6], threads=new Image[3];
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        Sprite dot,ring;
        RectTransform canvasRect;
        bool land,layoutDone;
        Vector2 lastCanvas;
        float toastClock;

        // ── 그림 재료
        Sprite Stitched(int w,int h,Color fill,Color stitch)
        {
            string key=w+"x"+h+fill;if(sprites.TryGetValue(key,out var c))return c;
            int W=w*2,H=h*2;var tex=new Texture2D(W,H,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};var px=new Color32[W*H];
            const float rad=22,inset=9;
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)
            {
                float qx=Mathf.Max(Mathf.Abs(x+.5f-W*.5f)-(W*.5f-rad),0),qy=Mathf.Max(Mathf.Abs(y+.5f-H*.5f)-(H*.5f-rad),0);
                float dist=Mathf.Sqrt(qx*qx+qy*qy);float a=Mathf.Clamp01(rad-dist+.8f);
                Color col=fill;
                float grain=((x*7+y*13)%11)/11f*.022f;col=new Color(col.r-grain,col.g-grain,col.b-grain,1);
                float inner=rad-dist-inset;
                if(Mathf.Abs(inner)<2.0f)
                {
                    bool horiz=Mathf.Abs(y+.5f-H*.5f)>Mathf.Abs(x+.5f-W*.5f)-(W-H)*.5f;
                    float along=horiz?x:y;
                    if(Mathf.Repeat(along,20f)<12f)col=stitch;
                }
                col.a=a;px[y*W+x]=col;
            }
            tex.SetPixels32(px);tex.Apply(false,true);
            var s=Sprite.Create(tex,new Rect(0,0,W,H),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprites[key]=s;return s;
        }

        static Sprite Round(int size,bool hollow)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};var px=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=x+.5f-size*.5f,dy=y+.5f-size*.5f,d=Mathf.Sqrt(dx*dx+dy*dy);
                float a=hollow?Mathf.Clamp01(1-Mathf.Abs(d-size*.40f)/(size*.07f)):Mathf.Clamp01(size*.48f-d+.8f);
                px[y*size+x]=new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px);tex.Apply(false,true);
            return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        }

        RectTransform Rect(string n,Transform p,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            var g=new GameObject(n,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;
            r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        Image Img(Transform p,string n,Sprite sp,Color c,Vector2 size)
        {
            var im=Rect(n,p,new Vector2(.5f,.5f),Vector2.zero,size).gameObject.AddComponent<Image>();im.sprite=sp;im.color=c;im.raycastTarget=false;return im;
        }
        TextMeshProUGUI Txt(Transform p,float size,Color c,TextAlignmentOptions al=TextAlignmentOptions.Center)
        {
            var x=Rect("T",p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(340,size*3)).gameObject.AddComponent<TextMeshProUGUI>();
            x.font=MgfText.Font;x.fontSize=size;x.color=c;x.alignment=al;x.raycastTarget=false;x.textWrappingMode=TextWrappingModes.Normal;return x;
        }
        CanvasGroup Group(string n)
        {
            var r=Rect(n,MgfText.Canvas.transform,Vector2.zero,Vector2.zero,Vector2.zero);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            return r.gameObject.AddComponent<CanvasGroup>();
        }
        static void Set(CanvasGroup g,bool on){g.gameObject.SetActive(on);g.alpha=on?1:0;}
        static void At(Component c,float x,float y,float w=-1,float h=-1)
        {
            var r=(RectTransform)c.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);
            if(w>0)r.sizeDelta=new Vector2(w,h);
        }

        public void Build()
        {
            canvasRect=(RectTransform)MgfText.Canvas.transform;
            dot=Round(64,false);ring=Round(96,true);
            titleUi=Group("TitleUI");gameUi=Group("GameUI");revealUi=Group("RevealUI");endUi=Group("EndUI");toastUi=Group("ToastUI");

            titleMeta=Txt(titleUi.transform,14,Ivory);titleMeta.outlineWidth=.2f;titleMeta.outlineColor=Ink;

            hudBg=Img(gameUi.transform,"HudCloth",null,Color.white,new Vector2(370,92));
            recordBg=Img(gameUi.transform,"RecordCloth",null,Color.white,new Vector2(370,126));
            progressTxt=Txt(gameUi.transform,16,Ink,TextAlignmentOptions.Left);
            livesTxt=Txt(gameUi.transform,14,Deep,TextAlignmentOptions.Left);
            timeTxt=Txt(gameUi.transform,16,Ink,TextAlignmentOptions.Right);
            masteryTxt=Txt(gameUi.transform,13,Deep,TextAlignmentOptions.Left);
            for(int i=0;i<6;i++)stamps[i]=Img(gameUi.transform,"Stamp",dot,Color.white,new Vector2(19,19));
            for(int i=0;i<3;i++)threads[i]=Img(gameUi.transform,"Thread",dot,Deep,new Vector2(8,20));
            promptTxt=Txt(gameUi.transform,15,Deep);
            recordTxt=Txt(gameUi.transform,16,Ink);
            formulaTxt=Txt(gameUi.transform,11.5f,Deep);
            guideTxt=Txt(gameUi.transform,15,Ivory);guideTxt.outlineWidth=.24f;guideTxt.outlineColor=Ink;

            ringImg=Img(gameUi.transform,"HandleRing",ring,Lime,new Vector2(70,70));
            targetImg=Img(gameUi.transform,"Target",ring,Ivory,new Vector2(56,56));
            targetTxt=Txt(gameUi.transform,13,Ink);
            ghostImg=Img(gameUi.transform,"GhostFinger",dot,new Color(Ivory.r,Ivory.g,Ivory.b,.92f),new Vector2(34,34));
            sideBg=Img(gameUi.transform,"SideTag",Stitched(86,46,Lime,Ink),Color.white,new Vector2(86,46));
            sideTxt=Txt(gameUi.transform,23,Ink);
            rippleImg=Img(MgfText.Canvas.transform,"Ripple",ring,new Color(1,1,1,0),new Vector2(64,64));

            revealBg=Img(revealUi.transform,"RevealCloth",null,Color.white,new Vector2(362,108));
            revealTitle=Txt(revealUi.transform,18,Deep);
            revealMath=Txt(revealUi.transform,14,Ink);

            endBg=Img(endUi.transform,"EndCloth",null,Color.white,new Vector2(362,270));
            endTitle=Txt(endUi.transform,26,Deep);
            endStats=Txt(endUi.transform,14,Ink);
            endTap=Txt(endUi.transform,15,Deep);

            var tb=Img(toastUi.transform,"ToastCloth",Stitched(330,44,Ink,Ivory),Color.white,new Vector2(330,44));At(tb,0,-330);
            toastTxt=Txt(toastUi.transform,14,Ivory);At(toastTxt,0,-330,310,40);

            HideAll();
        }

        public void HideAll(){Set(titleUi,false);Set(gameUi,false);Set(revealUi,false);Set(endUi,false);Set(toastUi,false);}

        public void ShowTitle(int best)
        {
            HideAll();Set(titleUi,true);
            titleMeta.text="중학교 3학년 · 산포도"+(best>0?"     최고 첫 제출 "+best+"/6":"");
        }
        public void ShowGame(){Set(titleUi,false);Set(endUi,false);Set(revealUi,false);Set(gameUi,true);layoutDone=false;}

        public void LoadProblem(CushionProblem p,bool practice,int index)
        {
            promptTxt.text=PromptLine(p,practice);
            recordTxt.text=RecordLines(p);
            formulaTxt.text=FormulaFor(MgfBridge.IsLandLayout);
            Guide(practice?"고리를 움직이는 점처럼 끌어 3칸에서 손을 떼시오":index==0?"격자 위를 끌어 한 변을 만들고 손을 떼면 제출입니다":"끌어서 한 변을 만들고 손을 떼시오");
            bool show=practice;
            targetImg.gameObject.SetActive(show);targetTxt.gameObject.SetActive(show);ghostImg.gameObject.SetActive(show);
            targetTxt.text="3칸";
        }

        static string FormulaFor(bool isLand)=>"분산 = (편차 제곱의 합) ÷ (자료 개수)"+(isLand?"\n":"   ·   ")+"표준편차 = √분산";

        static string PromptLine(CushionProblem p,bool practice)
        {
            if(practice)return "한 변의 길이를 표준편차와 같게 정하시오";
            if(!string.IsNullOrEmpty(p.pairId))return "비교 짝 "+p.pairRole+" · 한 변의 길이를 표준편차와 같게";
            if(p.band==1)return "분산에서 표준편차로 · 한 변의 길이를 정하시오";
            if(p.band==2)return "제곱의 평균이 분산 · 한 변의 길이를 정하시오";
            return "편차부터 구해 · 한 변의 길이를 정하시오";
        }

        static string RecordLines(CushionProblem p)
        {
            string raw="기록  "+PoksinRules.ArrayText(p.raw);
            string info="평균 "+p.mean+"  ·  자료 "+p.raw.Length+"개";
            if(p.band==0)return raw+"\n"+info+"  ·  분산 9칸²";
            if(p.band==1)return raw+"\n"+info+"  ·  분산 "+p.variance+"칸²";
            if(p.band==2)
            {
                var sq=new int[p.deviations.Length];for(int i=0;i<sq.Length;i++)sq[i]=p.deviations[i]*p.deviations[i];
                return raw+"\n"+info+"\n편차²  "+PoksinRules.ArrayText(sq);
            }
            if(!string.IsNullOrEmpty(p.pairId))return raw+"\n"+info+"\n최솟값 "+p.raw[0]+"  ·  최댓값 "+p.raw[p.raw.Length-1];
            return raw+"\n"+info;
        }

        public void Guide(string s){guideTxt.text=s;}

        public void Toast(string s){toastTxt.text=s;Set(toastUi,true);toastClock=1.6f;}

        public void Ripple(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out var local);
            rippleImg.rectTransform.anchorMin=rippleImg.rectTransform.anchorMax=new Vector2(.5f,.5f);
            rippleImg.rectTransform.anchoredPosition=local;rippleImg.rectTransform.localScale=Vector3.one*.4f;rippleImg.color=Ivory;
            rippleImg.gameObject.SetActive(true);
        }

        public void ShowReveal(string title,string math){Set(revealUi,true);revealTitle.text=title;revealMath.text=math;}
        public void HideReveal(){Set(revealUi,false);}

        public void ShowEnd(bool clear,int solved,int first,string detail)
        {
            Set(gameUi,false);Set(revealUi,false);Set(toastUi,false);Set(endUi,true);
            endTitle.text=clear?"폭신한 꼭대기 완성":"탑이 여기서 멈췄습니다";
            endStats.text="완성 "+solved+"/6층   ·   첫 제출 "+first+"/6\n"+detail;
            endTap.text="눌러서 다시 쌓기";layoutDone=false;
        }

        public void RefreshHud(bool onboarding,int solved,int lives,float time,int remaining,int firstAttempts,int mask)
        {
            progressTxt.text=onboarding?"연습":"방석 "+solved+"/6";
            livesTxt.text="봉합실";
            timeTxt.text=onboarding?"정지":Mathf.CeilToInt(time)+"초";
            timeTxt.color=!onboarding&&time<=20?Alarm:Ink;
            masteryTxt.text=onboarding?"본판 첫 제출 5/6 필요":"첫 제출 5/6 필요 · 남은 첫 실수 "+remaining+"회";
            masteryTxt.color=remaining>0||onboarding?Deep:Alarm;
            for(int i=0;i<6;i++)stamps[i].color=i<firstAttempts?(((mask&(1<<i))!=0)?Lime:Alarm):new Color(Deep.r,Deep.g,Deep.b,.20f);
            for(int i=0;i<3;i++)threads[i].color=i<lives?Deep:new Color(Deep.r,Deep.g,Deep.b,.16f);
        }

        void Layout(bool isLand,Rect safe)
        {
            land=isLand;layoutDone=true;formulaTxt.text=FormulaFor(isLand);
            float W=canvasRect.rect.width,H=canvasRect.rect.height,L=-W*.5f,T=H*.5f;
            var cloth=Ivory;var st=MgfLook.Hex("#C44B68");
            At(titleMeta,0,-H*.5f+26,Mathf.Min(W-20,360),30);
            if(land)
            {
                float cw=Mathf.Min(258,W*.36f),cx=L+cw*.5f+12,top=T-10,ch=H-20;
                hudBg.sprite=Stitched((int)cw,(int)ch,cloth,st);hudBg.gameObject.SetActive(true);At(hudBg,cx,0,cw,ch);
                recordBg.gameObject.SetActive(false);
                float tl=cx-cw*.5f+18,inner=cw-36;
                At(progressTxt,tl+inner*.3f,top-26,inner*.6f,24);At(timeTxt,tl+inner*.75f,top-26,inner*.5f,24);
                At(livesTxt,tl+inner*.25f,top-52,inner*.5f,22);
                for(int i=0;i<3;i++)At(threads[i],tl+58+i*14,top-52);
                for(int i=0;i<6;i++)At(stamps[i],tl+10+i*25,top-78);
                At(masteryTxt,cx,top-104,inner,36);masteryTxt.alignment=TextAlignmentOptions.Left;
                At(promptTxt,cx,top-150,inner,48);promptTxt.fontSize=13;
                At(recordTxt,cx,top-226,inner,104);recordTxt.fontSize=15;
                At(formulaTxt,cx,top-ch+46,inner,62);formulaTxt.fontSize=11.5f;
                float gx=cx+cw*.5f+(W-cw-12)*.5f;
                At(guideTxt,gx,-H*.5f+26,W-cw-30,46);guideTxt.fontSize=14;
                revealBg.sprite=Stitched(420,104,cloth,st);At(revealBg,gx,-H*.5f+66,420,104);
                At(revealTitle,gx,-H*.5f+92,396,30);revealTitle.fontSize=17;
                At(revealMath,gx,-H*.5f+52,396,56);revealMath.fontSize=13.5f;
                endBg.sprite=Stitched((int)cw,(int)ch,cloth,st);At(endBg,cx,0,cw,ch);
                At(endTitle,cx,H*.5f-60,inner,70);endTitle.fontSize=19;
                At(endStats,cx,-8,inner,210);endStats.fontSize=13;
                At(endTap,cx,-H*.5f+44,inner,34);
            }
            else
            {
                float cw=Mathf.Min(W-16,560),cx=0;
                hudBg.sprite=Stitched((int)cw,86,cloth,st);hudBg.gameObject.SetActive(true);At(hudBg,cx,T-49,cw,86);
                recordBg.gameObject.SetActive(true);recordBg.sprite=Stitched((int)cw,124,cloth,st);At(recordBg,cx,T-158,cw,124);
                float tl=-cw*.5f+18,right=W*.5f-62;
                At(progressTxt,tl+50,T-30,100,24);
                At(livesTxt,tl+150,T-30,70,22);
                for(int i=0;i<3;i++)At(threads[i],tl+148+i*13-8+24,T-30);
                At(timeTxt,right-50,T-30,74,24);timeTxt.fontSize=15;
                At(masteryTxt,tl+106,T-66,212,22);masteryTxt.alignment=TextAlignmentOptions.Left;
                float sx=Mathf.Min(cw*.5f-22,W*.5f-22)-5*20;
                for(int i=0;i<6;i++)At(stamps[i],sx+i*20,T-66);
                At(promptTxt,cx,T-112,cw-30,24);promptTxt.fontSize=14.5f;
                At(recordTxt,cx,T-154,cw-30,62);recordTxt.fontSize=15;
                At(formulaTxt,cx,T-204,cw-30,20);formulaTxt.fontSize=11;
                At(guideTxt,0,-H*.5f+48,Mathf.Min(W-24,540),60);guideTxt.fontSize=15;
                revealBg.sprite=Stitched((int)cw,108,cloth,st);At(revealBg,0,-H*.5f+70,cw,108);
                At(revealTitle,0,-H*.5f+99,cw-26,30);revealTitle.fontSize=17;
                At(revealMath,0,-H*.5f+54,cw-26,58);revealMath.fontSize=13.5f;
                float eh=Mathf.Min(300,H*.42f);
                endBg.sprite=Stitched((int)cw,(int)eh,cloth,st);At(endBg,0,-H*.5f+eh*.5f+12,cw,eh);
                float ey=-H*.5f+12;
                At(endTitle,0,ey+eh-42,cw-30,44);endTitle.fontSize=25;
                At(endStats,0,ey+eh*.47f,cw-30,eh-110);endStats.fontSize=14;
                At(endTap,0,ey+30,cw-30,30);
            }
        }

        public void Tick(float dt,CushionWorld world,bool practice,bool showTag,int side,bool showRing)
        {
            bool isLand=MgfBridge.IsLandLayout;
            var size=canvasRect.rect.size;
            if(!layoutDone||isLand!=land||size!=lastCanvas){lastCanvas=size;Layout(isLand,default);}
            if(toastUi.gameObject.activeSelf){toastClock-=dt;if(toastClock<=0)Set(toastUi,false);}
            if(rippleImg.gameObject.activeSelf)
            {
                rippleImg.rectTransform.localScale=Vector3.MoveTowards(rippleImg.rectTransform.localScale,Vector3.one*1.6f,dt*4f);
                var c=rippleImg.color;c.a=Mathf.MoveTowards(c.a,0,dt*2.6f);rippleImg.color=c;if(c.a<=0)rippleImg.gameObject.SetActive(false);
            }
            if(!gameUi.gameObject.activeSelf)return;
            float t=Time.unscaledTime;
            ringImg.gameObject.SetActive(showRing);
            if(showRing)
            {
                At(ringImg,ToCanvas(world,world.CornerWorld(0)).x,ToCanvas(world,world.CornerWorld(0)).y);
                ringImg.rectTransform.localScale=Vector3.one*(1f+Mathf.Sin(t*4f)*.10f);
            }
            if(targetImg.gameObject.activeSelf)
            {
                Vector2 a=ToCanvas(world,world.CornerWorld(0)),b=ToCanvas(world,world.CornerWorld(3));
                At(targetImg,b.x,b.y);At(targetTxt,b.x,b.y,50,24);
                float u=Mathf.Repeat(t*.55f,1.25f);u=Mathf.Clamp01(u);u=u*u*(3f-2f*u);
                var p=Vector2.Lerp(a,b,u);At(ghostImg,p.x,p.y);
                ghostImg.gameObject.SetActive(practice);
            }
            sideBg.gameObject.SetActive(showTag);sideTxt.gameObject.SetActive(showTag);
            if(showTag)
            {
                var p=ToCanvas(world,world.CornerWorld(side))+new Vector2(52,8);
                float hw=canvasRect.rect.width*.5f-46;p.x=Mathf.Clamp(p.x,-hw,hw);
                At(sideBg,p.x,p.y);At(sideTxt,p.x,p.y,80,40);
                sideTxt.text=side+"칸";
            }
        }

        Vector2 ToCanvas(CushionWorld world,Vector3 w)
        {
            var sp=world.Cam.WorldToScreenPoint(w);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,sp,null,out var local);
            return local;
        }
    }
}
