using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        Sprite roundSprite;TMP_FontAsset roundFont;Material roundFontMaterial,roundBodyMaterial,mathFontMaterial;Sprite holdHandSprite;
        Sprite RoundSprite {
            get {
                if(roundSprite)return roundSprite;
                const int n=40;const float radius=13f;
                var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);texture.name="Shared rounded UI atlas";texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
                var pixels=new Color32[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++){
                    float dx=Mathf.Max(Mathf.Abs(x+.5f-n*.5f)-(n*.5f-radius),0),dy=Mathf.Max(Mathf.Abs(y+.5f-n*.5f)-(n*.5f-radius),0);
                    pixels[y*n+x]=new Color32(255,255,255,(byte)(255*Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f)));
                }
                texture.SetPixels32(pixels);texture.Apply(false,true);
                roundSprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));
                return roundSprite;
            }
        }
        TMP_FontAsset RoundFont {
            get {
                if(roundFont)return roundFont;
                var source=Resources.Load<Font>("HyeopgokSasu/UI/HyeopgokJua");
                if(!source)return MgfText.Font;
                roundFont=TMP_FontAsset.CreateFontAsset(source,72,9,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
                roundFont.name="Hyeopgok Round";roundFont.fallbackFontAssetTable=new List<TMP_FontAsset>{MgfText.Font};
                roundFont.TryAddCharacters("0123456789+-×÷=?.,/()%<>!:;·−≤≥≠□○△ 협곡사수출격단원선택초등학교중학교고등학교학년학기문제정답성문코인멈춰서붓기전투진행승리다시더읽기접기목표확률경우의수");
                return roundFont;
            }
        }
        Material RoundFontMaterial {
            get {
                if(roundFontMaterial)return roundFontMaterial;
                roundFontMaterial=new Material(RoundFont.material);roundFontMaterial.name="Hyeopgok shared dark outline";
                roundFontMaterial.SetColor(ShaderUtilities.ID_OutlineColor,MgfLook.Hex("#152b31"));
                roundFontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.20f);roundFontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate,0f);
                return roundFontMaterial;
            }
        }
        Material RoundBodyMaterial {
            get {
                if(roundBodyMaterial)return roundBodyMaterial;
                roundBodyMaterial=new Material(MgfText.Font.material);roundBodyMaterial.name="Hyeopgok clear body text";
                roundBodyMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,0f);roundBodyMaterial.SetFloat(ShaderUtilities.ID_FaceDilate,0f);
                return roundBodyMaterial;
            }
        }
        Material MathFontMaterial {
            get {
                if(mathFontMaterial)return mathFontMaterial;
                MgfText.Font.TryAddCharacters("∠△°²∥⊥∽≡×÷①②③★→−");
                mathFontMaterial=new Material(MgfText.Font.material);mathFontMaterial.name="Math-safe warm outline";
                mathFontMaterial.SetColor(ShaderUtilities.ID_OutlineColor,MgfLook.Hex("#253b36"));mathFontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.12f);
                return mathFontMaterial;
            }
        }
        TextMeshProUGUI DisplayText(string content,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,float font,Color color){
            var text=Text(content,parent,anchor,offset,size,font,color);text.font=RoundFont;text.fontSharedMaterial=RoundFontMaterial;return text;
        }
        RectTransform Parchment(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size){
            var rim=Box(name,parent,anchor,offset,size,MgfLook.Hex("#5a3a1e"));var face=Box(name+" cream",rim,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(5,5),MgfLook.Hex("#f6e7c8"));
            face.anchorMin=Vector2.zero;face.anchorMax=Vector2.one;face.offsetMin=new Vector2(2.5f,2.5f);face.offsetMax=new Vector2(-2.5f,-2.5f);return rim;
        }
        RectTransform RoyalPanel(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size){
            var rim=Box(name,parent,anchor,offset,size,MgfLook.Hex("#18333f"));
            var blue=Box("Royal blue rail",rim,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(7,7),MgfLook.Hex("#0c73d5"));
            var face=Box("Parchment center",rim,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(19,19),MgfLook.Hex("#f6e7c8"));return rim;
        }
        // Chunky strategy-game button: dark outline, darker bottom lip, bright face, top gleam.
        RectTransform GameButton(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,Color face,Color lip){
            var rim=Box(name,parent,anchor,offset,size,MgfLook.Hex("#18333f"));
            Box(name+" lip",rim,new Vector2(.5f,.5f),new Vector2(0,-2),size-new Vector2(7,9),lip);
            var top=Box(name+" face",rim,new Vector2(.5f,.5f),new Vector2(0,2.5f),size-new Vector2(7,13),face);
            Box(name+" gleam",top,new Vector2(.5f,1),new Vector2(0,-6),new Vector2(size.x-26,4),new Color(1,1,1,.35f));
            return rim;
        }
        void BuildHourglass(Transform parent){
            var h=Group("Hourglass",parent);h.anchorMin=h.anchorMax=new Vector2(.5f,.5f);h.anchoredPosition=new Vector2(-13,0);h.sizeDelta=new Vector2(14,23);
            Box("Hourglass upper rim",h,new Vector2(.5f,.5f),new Vector2(0,10),new Vector2(14,3),MgfLook.Hex("#704b28"));
            Box("Hourglass lower rim",h,new Vector2(.5f,.5f),new Vector2(0,-10),new Vector2(14,3),MgfLook.Hex("#704b28"));
            var a=Box("Hourglass glass A",h,new Vector2(.5f,.5f),Vector2.zero,new Vector2(3,22),MgfLook.Hex("#f8f0d2"));a.localRotation=Quaternion.Euler(0,0,27);
            var b=Box("Hourglass glass B",h,new Vector2(.5f,.5f),Vector2.zero,new Vector2(3,22),MgfLook.Hex("#f8f0d2"));b.localRotation=Quaternion.Euler(0,0,-27);
            Box("Hourglass sand",h,new Vector2(.5f,.5f),new Vector2(0,-6),new Vector2(7,4),MgfLook.Hex("#b37724"));
        }
        Sprite HoldHandSprite {
            get {
                if(holdHandSprite)return holdHandSprite;
                const int w=64,h=80;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color32[w*h];
                var points=new Vector2[]{new Vector2(18,5),new Vector2(43,5),new Vector2(45,17),new Vector2(54,31),new Vector2(54,45),new Vector2(49,51),new Vector2(42,50),new Vector2(36,56),new Vector2(30,54),new Vector2(30,71),new Vector2(26,76),new Vector2(20,76),new Vector2(16,71),new Vector2(16,40),new Vector2(10,45),new Vector2(4,42),new Vector2(3,35),new Vector2(17,17)};
                for(int y=0;y<h;y++)for(int x=0;x<w;x++){
                    bool inside=false;float nearest=999;Vector2 pt=new Vector2(x+.5f,y+.5f);
                    for(int i=0,j=points.Length-1;i<points.Length;j=i++){
                        Vector2 a=points[i],b=points[j],d=b-a;float t=Mathf.Clamp01(Vector2.Dot(pt-a,d)/d.sqrMagnitude);nearest=Mathf.Min(nearest,(pt-(a+d*t)).magnitude);
                        if((a.y>pt.y)!=(b.y>pt.y)&&pt.x<(b.x-a.x)*(pt.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
                    }
                    if(!inside)pixels[y*w+x]=new Color32(0,0,0,0);
                    else if(nearest<3)pixels[y*w+x]=new Color32(18,43,52,252);
                    else if(y<14)pixels[y*w+x]=new Color32(12,91,173,248);
                    else if(y<18)pixels[y*w+x]=new Color32(245,184,35,250);
                    else {
                        // Warm articulated knight glove: blue cuff, gold welt and
                        // two subtle armour seams read at phone scale.
                        bool seam=(y>31&&y<34&&x>15&&x<49)||(y>47&&y<50&&x>26&&x<48);
                        pixels[y*w+x]=seam?new Color32(210,174,107,238):x>42?new Color32(225,211,177,242):new Color32(255,247,218,246);
                    }
                }
                tex.SetPixels32(pixels);tex.Apply(false,true);holdHandSprite=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(.5f,.5f),100);return holdHandSprite;
            }
        }
        void BuildRewardTower(Transform parent,Vector2 offset){
            var root=Group("Reward building pictogram",parent);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.anchoredPosition=offset;root.sizeDelta=new Vector2(58,48);
            Box("Tower stone",root,new Vector2(.5f,0),new Vector2(0,15),new Vector2(35,29),MgfLook.Hex("#d8d6c8"));
            Box("Tower blue door",root,new Vector2(.5f,0),new Vector2(0,9),new Vector2(9,16),MgfLook.Hex("#0c73d5"));
            var roof=Box("Tower roof",root,new Vector2(.5f,1),new Vector2(0,-5),new Vector2(43,15),MgfLook.Hex("#176fc7"));roof.localRotation=Quaternion.Euler(0,0,-2);
            for(int i=0;i<3;i++)Box("Tower crown "+i,root,new Vector2(.5f,1),new Vector2((i-1)*13,-1),new Vector2(9,11),MgfLook.Hex("#f0c34f"));
        }
        void BuildPackGlyph(Transform parent,Vector2 offset){
            var root=Group("Unit battle pictogram",parent);root.anchorMin=root.anchorMax=new Vector2(1,.5f);root.anchoredPosition=offset;root.sizeDelta=new Vector2(49,55);
            var shield=Box("Unit shield",root,new Vector2(.5f,.5f),new Vector2(0,-1),new Vector2(31,38),MgfLook.Hex("#176fc7"));shield.localRotation=Quaternion.Euler(0,0,-3);
            Box("Unit shield gold",shield,new Vector2(.5f,.5f),Vector2.zero,new Vector2(9,26),MgfLook.Hex("#ffd05a"));
            var spear=Box("Unit spear",root,new Vector2(.5f,.5f),new Vector2(13,1),new Vector2(5,49),MgfLook.Hex("#744825"));spear.localRotation=Quaternion.Euler(0,0,-24);
            Box("Unit spear tip",spear,new Vector2(.5f,1),new Vector2(0,3),new Vector2(9,10),MgfLook.Hex("#f5efe1"));
        }
        void BuildKeepVignette(Transform parent,Vector2 anchor,Vector2 offset,float scale,bool hero){
            var root=Group(hero?"Victory hero and keep vignette":"Pack keep vignette",parent);root.anchorMin=root.anchorMax=anchor;root.anchoredPosition=offset;root.sizeDelta=new Vector2(270,112);root.localScale=Vector3.one*scale;
            Color stone=new Color(.39f,.54f,.56f,.30f),deep=new Color(.08f,.24f,.29f,.34f),blue=new Color(.05f,.40f,.76f,.42f),g=new Color(1,.73f,.18f,.55f);
            Box("Keep wall",root,new Vector2(.5f,0),new Vector2(0,28),new Vector2(128,48),stone);
            for(int side=-1;side<=1;side+=2){
                Box("Keep tower",root,new Vector2(.5f,0),new Vector2(side*64,37),new Vector2(38,66),stone);
                var roof=Box("Keep blue roof",root,new Vector2(.5f,0),new Vector2(side*64,75),new Vector2(48,19),blue);roof.localRotation=Quaternion.Euler(0,0,side*3);
                for(int i=0;i<3;i++)Box("Keep merlon",root,new Vector2(.5f,0),new Vector2(side*64+(i-1)*13,72),new Vector2(8,16),deep);
            }
            Box("Keep gate",root,new Vector2(.5f,0),new Vector2(0,21),new Vector2(30,39),deep);
            if(hero){
                Box("Hero cape",root,new Vector2(.5f,0),new Vector2(0,17),new Vector2(35,48),blue);
                Box("Hero helm",root,new Vector2(.5f,0),new Vector2(0,51),new Vector2(27,25),MgfLook.Hex("#f3d06d"));
                for(int i=0;i<3;i++)Box("Hero crown",root,new Vector2(.5f,0),new Vector2((i-1)*9,69+(i==1?4:0)),new Vector2(7,14),g);
            }
        }
        Transform BuildTutorialArrow(){
            var go=new GameObject("Next pad emerald 3D arrow",typeof(MeshFilter),typeof(MeshRenderer));
            Vector2[] profile={new Vector2(-.17f,.57f),new Vector2(.17f,.57f),new Vector2(.17f,0),new Vector2(.40f,0),new Vector2(0,-.5f),new Vector2(-.40f,0),new Vector2(-.17f,0)};
            var vertices=new Vector3[14];var colors=new Color[14];
            for(int i=0;i<7;i++){vertices[i]=new Vector3(profile[i].x,profile[i].y,-.08f);vertices[i+7]=new Vector3(profile[i].x,profile[i].y,.08f);colors[i]=MgfLook.Hex("#7aff29");colors[i+7]=MgfLook.Hex("#199444");}
            var triangles=new List<int>{0,1,2,0,2,6,3,4,5,10,12,11,7,9,8,7,13,9};
            for(int i=0;i<7;i++){int j=(i+1)%7;triangles.Add(i);triangles.Add(j+7);triangles.Add(j);triangles.Add(i);triangles.Add(i+7);triangles.Add(j+7);}
            var mesh=new Mesh{name="Extruded tutorial arrow"};mesh.vertices=vertices;mesh.colors=colors;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=worldMat;go.SetActive(false);return go.transform;
        }
        void AnimateRewardCoins(){
            float age=Time.unscaledTime-resultStarted;
            float spread=(float)Screen.width/Screen.height>1.2f?1.55f:1f;
            // Coins rain down both flanks behind the royal card, never across its text.
            for(int i=0;i<resultCoins.Length;i++){float t=Mathf.Repeat(age*.24f+i*.0833f,1);float side=i%2==0?-1:1;resultCoins[i].anchoredPosition=new Vector2(side*(152+(i/2%3)*24)*spread,430-t*875);resultCoins[i].localRotation=Quaternion.Euler(0,0,age*(35+i*7));}
        }
        RectTransform Pill(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size,Color color){
            var rim=Box(name,parent,anchor,offset,size,MgfLook.Hex("#e8e8d9"));
            var face=Box(name+" inset",rim,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(4,4),color);
            face.anchorMin=Vector2.zero;face.anchorMax=Vector2.one;face.offsetMin=new Vector2(2,2);face.offsetMax=new Vector2(-2,-2);
            return rim;
        }
        void BuildCoinIcon(Transform parent,Vector2 offset,float size){
            var coin=Box("Coin rim",parent,new Vector2(.5f,.5f),offset,new Vector2(size,size),MgfLook.Hex("#744825"));coin.localRotation=Quaternion.Euler(0,0,12);
            var face=Box("Coin gold",coin,new Vector2(.5f,.5f),Vector2.zero,new Vector2(size-4,size-4),MgfLook.Hex("#F2B705"));
            Box("Coin gleam",face,new Vector2(.5f,.5f),new Vector2(-size*.2f,size*.16f),new Vector2(3,size*.42f),MgfLook.Hex("#fff1b4"));
        }
        void BuildCrest(Transform parent,Vector2 anchor,Vector2 offset,float scale){
            var crest=Group("Canyon crown crest",parent);crest.anchorMin=crest.anchorMax=anchor;crest.anchoredPosition=offset;crest.sizeDelta=new Vector2(90,85);crest.localScale=Vector3.one*scale;
            var shield=Box("Shield outline",crest,new Vector2(.5f,.5f),Vector2.zero,new Vector2(70,64),navy);shield.localRotation=Quaternion.Euler(0,0,-3);
            Box("Shield blue",shield,new Vector2(.5f,.5f),Vector2.zero,new Vector2(60,53),MgfLook.Hex("#0C73D5"));
            var crown=Box("Gold crown",shield,new Vector2(.5f,.5f),new Vector2(0,4),new Vector2(39,22),gold);
            for(int i=0;i<3;i++)Box("Crown point",crown,new Vector2(.5f,.5f),new Vector2((i-1)*16,14+(i==1?4:0)),new Vector2(9,15),gold);
            Box("Crown jewel",crown,new Vector2(.5f,.5f),Vector2.zero,new Vector2(7,9),cream);
        }
        Vector2 ClampWorldBubble(Vector2 point,float width){
            float canvasWidth=844f*Screen.width/Mathf.Max(1,Screen.height);
            point.x=Mathf.Clamp(point.x,-canvasWidth*.5f+width*.5f+8,canvasWidth*.5f-width*.5f-8);
            point.y=Mathf.Clamp(point.y,-377,270);return point;
        }
        // Re-measure only when a caption or viewport changes. Positioning below
        // only uses cached RectTransform sizes and value types during animation.
        void LayoutPadName(int i){
            if(padNames[i]==null)return;
            var label=padNames[i];label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Overflow;
            label.fontSize=30;label.lineSpacing=0;label.rectTransform.sizeDelta=new Vector2(165,96);label.rectTransform.anchoredPosition=Vector2.zero;
            label.text="<line-height=85%>"+basePadNames[i]+"</line-height>";
        }

        [System.Serializable] sealed class PackHeader {public string pack_id,title,school,unit_id;public int grade,semester,unit_order;}
        sealed class PackCard {public RectTransform root;public TextMeshProUGUI title,meta,number;public Image face;public int packIndex=-1;}
        RectTransform titleLogo,titlePackBanner,packBrowser,packWindow,packViewport,packClose,packScrollThumb;
        TextMeshProUGUI titleSubline,titleSelectionMeta,packCount,packSchoolText,packEmpty,ctaText;
        readonly RectTransform[] schoolTabs=new RectTransform[3],gradeTabs=new RectTransform[6];
        readonly TextMeshProUGUI[] schoolTabText=new TextMeshProUGUI[3],gradeTabText=new TextMeshProUGUI[6];
        readonly PackCard[] packCards=new PackCard[6];
        readonly List<int> filteredPacks=new List<int>();
        readonly string[] schools={"elementary","middle","high"};
        readonly string[] schoolLabels={"초등","중등","고등"};
        string[] cardMeta,cardSelectedMeta,cardNumbers;
        PackIndex cardCacheIndex;
        PackIndex catalogueIndex;bool catalogueBusy,browserOpen,packDragging;string selectedSchool="middle";int selectedGrade=2,pendingPack=-1;
        float packScroll,packDragStart,packScrollStart;Vector2 packPointerStart;int renderedFirst=-1;float renderedScroll=-1;

        void BuildTitleUi(){
            titleLogo=Group("Title standard",titleRoot);titleLogo.anchorMin=titleLogo.anchorMax=new Vector2(.5f,.86f);titleLogo.sizeDelta=new Vector2(380,130);
            BuildCrest(titleLogo,new Vector2(.5f,.5f),new Vector2(0,63),.7f);
            var titleShadow=DisplayText("협곡 사수",titleLogo,new Vector2(.5f,.5f),new Vector2(2,-5),new Vector2(370,92),63,navy);titleShadow.characterSpacing=-3;
            var title=DisplayText("협곡 사수",titleLogo,new Vector2(.5f,.5f),new Vector2(0,-1),new Vector2(370,92),63,cream);title.characterSpacing=-3;
            titleSubline=Text("코인을 모아 · 답만큼 붓고 · 성문을 지켜라",titleLogo,new Vector2(.5f,.5f),new Vector2(0,-58),new Vector2(372,38),17,cream);
            // Royal parchment plaque with a gold ribbon: the selected expedition.
            titlePackBanner=RoyalPanel("Selected unit plaque",titleRoot,new Vector2(.5f,.265f),Vector2.zero,new Vector2(342,104));
            var packRibbon=Box("Selected unit gold ribbon",titlePackBanner,new Vector2(.5f,1),new Vector2(0,-2),new Vector2(170,30),MgfLook.Hex("#5a3a1e"));
            Box("Ribbon gold face",packRibbon,new Vector2(.5f,.5f),Vector2.zero,new Vector2(164,24),gold);
            Text("이번 원정",packRibbon,new Vector2(.5f,.5f),new Vector2(0,1),new Vector2(160,24),14,MgfLook.Hex("#49301c")).fontSharedMaterial=RoundBodyMaterial;
            titleInfo=Text("문제 팩을 불러오는 중…",titlePackBanner,new Vector2(.5f,.5f),new Vector2(0,4),new Vector2(310,44),26,MgfLook.Hex("#3b2a18"));titleInfo.fontSharedMaterial=RoundBodyMaterial;titleInfo.fontStyle=FontStyles.Bold;
            titleSelectionMeta=Text("나의 학습 원정",titlePackBanner,new Vector2(.5f,.5f),new Vector2(0,-31),new Vector2(290,24),15,MgfLook.Hex("#7a5431"));titleSelectionMeta.fontSharedMaterial=RoundBodyMaterial;
            packRect=GameButton("Open unit collection",titleRoot,new Vector2(.5f,.15f),new Vector2(-108,0),new Vector2(108,64),MgfLook.Hex("#0c73d5"),MgfLook.Hex("#074f9c"));
            DisplayText("단원 선택",packRect,new Vector2(.5f,.5f),new Vector2(0,2),new Vector2(100,52),21,Color.white);
            ctaRect=GameButton("Deploy",titleRoot,new Vector2(.5f,.15f),new Vector2(59,0),new Vector2(207,68),MgfLook.Hex("#ffc93a"),MgfLook.Hex("#d88a12"));
            ctaText=DisplayText("출 격",ctaRect,new Vector2(.5f,.5f),new Vector2(0,3),new Vector2(196,56),36,Color.white);
            Text("10번의 판단 · 7번의 정답 · 하나의 성문",titleRoot,new Vector2(.5f,.065f),Vector2.zero,new Vector2(370,27),15,cream);
            BuildPackBrowser();
        }
        void BuildPackBrowser(){
            packBrowser=Group("Unit collection",titleRoot);
            Box("Collection scrim",packBrowser,new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,1800),new Color(.025f,.08f,.1f,.40f));
            packWindow=RoyalPanel("Collection window",packBrowser,new Vector2(.5f,.5f),Vector2.zero,new Vector2(370,594));
            BuildKeepVignette(packWindow,new Vector2(.5f,1),new Vector2(-132,-34),.29f,false);
            Box("Collection gold ribbon",packWindow,new Vector2(.5f,1),new Vector2(0,-35),new Vector2(379,59),gold);
            DisplayText("원정 단원 선택",packWindow,new Vector2(.5f,1),new Vector2(-10,-39),new Vector2(310,54),30,MgfLook.Hex("#49301c"));
            packClose=Pill("Close collection",packWindow,new Vector2(1,1),new Vector2(-28,-29),new Vector2(40,40),MgfLook.Hex("#8b4d39"));Text("×",packClose,new Vector2(.5f,.5f),Vector2.zero,new Vector2(36,38),26,cream);
            for(int i=0;i<3;i++){
                schoolTabs[i]=Pill("School tab "+i,packWindow,new Vector2(.5f,1),new Vector2((i-1)*112,-95),new Vector2(105,42),MgfLook.Hex("#1972a8"));
                schoolTabText[i]=Text(schoolLabels[i],schoolTabs[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(99,38),18,cream);
            }
            for(int i=0;i<6;i++){
                gradeTabs[i]=Pill("Grade tab "+(i+1),packWindow,new Vector2(.5f,1),new Vector2((i-2.5f)*54,-148),new Vector2(49,42),MgfLook.Hex("#237db0"));
                gradeTabText[i]=Text((i+1)+"학년",gradeTabs[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(47,37),14,cream);
            }
            packCount=Text("단원 목록을 읽는 중",packWindow,new Vector2(.5f,1),new Vector2(0,-185),new Vector2(326,27),14,MgfLook.Hex("#5a3a1e"));
            packViewport=Group("Scrollable unit cards",packWindow);packViewport.anchorMin=packViewport.anchorMax=new Vector2(.5f,.5f);packViewport.anchoredPosition=new Vector2(-3,-81);packViewport.sizeDelta=new Vector2(337,336);packViewport.gameObject.AddComponent<RectMask2D>();
            for(int i=0;i<packCards.Length;i++){
                var card=new PackCard();card.root=Pill("Unit card "+i,packViewport,new Vector2(.5f,1),Vector2.zero,new Vector2(328,84),MgfLook.Hex("#e5d4ad"));
                card.face=card.root.GetChild(0).GetComponent<Image>();
                var seal=Box("Unit seal",card.root,new Vector2(0,.5f),new Vector2(33,0),new Vector2(44,52),MgfLook.Hex("#173d52"));
                card.number=Text("01",seal,new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,43),25,gold);
                BuildPackGlyph(card.root,new Vector2(-22,0));
                card.title=Text("",card.root,new Vector2(.5f,.5f),new Vector2(14,13),new Vector2(202,40),21,MgfLook.Hex("#49301c"));card.title.fontSharedMaterial=RoundBodyMaterial;card.title.alignment=TextAlignmentOptions.MidlineLeft;card.title.overflowMode=TextOverflowModes.Ellipsis;card.title.textWrappingMode=TextWrappingModes.NoWrap;
                card.meta=Text("",card.root,new Vector2(.5f,.5f),new Vector2(14,-22),new Vector2(202,27),14,MgfLook.Hex("#6d5835"));card.meta.fontSharedMaterial=RoundBodyMaterial;card.meta.alignment=TextAlignmentOptions.MidlineLeft;
                packCards[i]=card;
            }
            packEmpty=Text("이 학년의 단원을 준비하고 있어요",packViewport,new Vector2(.5f,.6f),Vector2.zero,new Vector2(300,70),20,MgfLook.Hex("#49301c"));
            packScrollThumb=Box("Collection scroll thumb",packWindow,new Vector2(1,1),new Vector2(-12,-257),new Vector2(4,50),gold);
            Text("단원을 눌러 선택 · 위아래로 밀어 탐색",packWindow,new Vector2(.5f,0),new Vector2(0,23),new Vector2(330,28),14,MgfLook.Hex("#5a3a1e"));
            packBrowser.gameObject.SetActive(false);
        }
        void LayoutTitleUi(float canvasWidth,bool wide){
            if(!titleLogo)return;
            titleLogo.anchorMin=titleLogo.anchorMax=wide?new Vector2(.23f,.78f):new Vector2(.5f,.86f);
            titleLogo.localScale=Vector3.one*(wide?1.1f:1);
            Vector2 baseAnchor=wide?new Vector2(.23f,.31f):new Vector2(.5f,.265f);
            titlePackBanner.anchorMin=titlePackBanner.anchorMax=baseAnchor;
            packRect.anchorMin=packRect.anchorMax=ctaRect.anchorMin=ctaRect.anchorMax=wide?new Vector2(.23f,.17f):new Vector2(.5f,.15f);
            packWindow.sizeDelta=new Vector2(Mathf.Min(370,canvasWidth-16),594);
        }
        void UpdateSelectedPackUi(){
            if(pack==null)return;titleInfo.text=pack.title;
            string school=pack.school=="elementary"?"초등학교":pack.school=="high"?"고등학교":"중학교";
            titleSelectionMeta.text=school+" "+pack.grade+"학년 · "+pack.semester+"학기";
            selectedSchool=string.IsNullOrEmpty(pack.school)?"middle":pack.school;selectedGrade=Mathf.Max(1,pack.grade);
            if(index!=null&&index.packs!=null&&packAt>=0&&packAt<index.packs.Length){
                var entry=index.packs[packAt];FillHeader(entry,new PackHeader{school=pack.school,grade=pack.grade,semester=pack.semester,unit_id=pack.unit_id,unit_order=pack.unit_order});
            }
            if(browserOpen)RefreshCatalogue();
        }
        void EnsureCatalogue(){if(index!=null&&index!=catalogueIndex&&!catalogueBusy)StartCoroutine(ReadCatalogueMetadata());}
        IEnumerator ReadCatalogueMetadata(){
            catalogueBusy=true;catalogueIndex=index;
            foreach(var entry in index.packs){
                if(string.IsNullOrEmpty(entry.school)||entry.grade==0||entry.semester==0||entry.unit_order==0){
                    string file=string.IsNullOrEmpty(entry.file)?entry.pack_id+".json":entry.file;
                    if(file.Contains("..")||file.Contains(":")||file.Contains("/")||file.Contains("\\"))continue;
                    using(var request=UnityWebRequest.Get(BaseUrl()+"packs/"+file)){
                        request.timeout=10;yield return request.SendWebRequest();
                        if(request.result==UnityWebRequest.Result.Success){PackHeader header=null;try{header=JsonUtility.FromJson<PackHeader>(request.downloadHandler.text);}catch{}if(header!=null)FillHeader(entry,header);}
                    }
                }
                if(entry.unit_order==0)entry.unit_order=UnitOrder(entry.pack_id);
            }
            catalogueBusy=false;RefreshCatalogue();
        }
        static int UnitOrder(string id){if(string.IsNullOrEmpty(id))return 0;int at=id.LastIndexOf("-u",System.StringComparison.Ordinal);return at>=0&&int.TryParse(id.Substring(at+2),out int n)?n:0;}
        static void FillHeader(PackEntry entry,PackHeader header){
            if(string.IsNullOrEmpty(entry.school))entry.school=header.school;if(entry.grade==0)entry.grade=header.grade;if(entry.semester==0)entry.semester=header.semester;
            if(entry.unit_order==0)entry.unit_order=header.unit_order>0?header.unit_order:UnitOrder(header.unit_id);
        }
        void OpenCatalogue(){browserOpen=true;packScroll=0;pendingPack=-1;packBrowser.gameObject.SetActive(true);EnsureCatalogue();RefreshCatalogue();MgfSfx.Play("tap");}
        void CachePackCardStrings(){
            int count=index.packs.Length;
            if(cardCacheIndex!=index||cardMeta==null||cardMeta.Length!=count){
                cardCacheIndex=index;cardMeta=new string[count];cardSelectedMeta=new string[count];cardNumbers=new string[count];
            }
            for(int i=0;i<count;i++){
                var entry=index.packs[i];cardMeta[i]=entry.semester+"학기 · "+entry.unit_order+"단원";
                cardSelectedMeta[i]=cardMeta[i]+" · 선택됨";cardNumbers[i]=entry.unit_order.ToString("00");
                // Titles of later rows must not trigger SDF glyph generation
                // while the user scrolls into a newly recycled card.
                if(!string.IsNullOrEmpty(entry.title))MgfText.Font.TryAddCharacters(entry.title);
            }
        }
        bool HitPackControl(RectTransform control){
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(control,MgfPointer.Position,null,out Vector2 local))return false;
            Rect hit=control.rect;Vector2 center=hit.center;
            // At the supported 390x844 and 1280x720/800 viewports this is at
            // least 44 physical pixels. The 52-unit cap stays inside the
            // 54-unit elementary-grade pitch, including expanded hit areas.
            float minimum=Mathf.Min(52,Mathf.Max(44,44/Mathf.Max(.01f,MgfText.Canvas.scaleFactor)));
            float w=Mathf.Max(hit.width,minimum),h=Mathf.Max(hit.height,minimum);
            return new Rect(center.x-w*.5f,center.y-h*.5f,w,h).Contains(local);
        }
        void RefreshCatalogue(){
            if(!packCount||index==null||index.packs==null)return;CachePackCardStrings();filteredPacks.Clear();
            for(int i=0;i<index.packs.Length;i++){var e=index.packs[i];if(e.school==selectedSchool&&e.grade==selectedGrade)filteredPacks.Add(i);}
            filteredPacks.Sort((a,b)=>{var x=index.packs[a];var y=index.packs[b];int s=x.semester.CompareTo(y.semester);return s!=0?s:x.unit_order.CompareTo(y.unit_order);});
            packCount.text=(selectedSchool=="elementary"?"초등":selectedSchool=="high"?"고등":"중등")+" "+selectedGrade+"학년 · "+filteredPacks.Count+"개 단원";
            for(int i=0;i<schoolTabs.Length;i++){schoolTabs[i].GetChild(0).GetComponent<Image>().color=schools[i]==selectedSchool?MgfLook.Hex("#B97926"):MgfLook.Hex("#1972a8");}
            for(int i=0;i<gradeTabs.Length;i++){
                bool show=selectedSchool=="elementary"||i<3;gradeTabs[i].gameObject.SetActive(show);if(show){gradeTabs[i].anchoredPosition=new Vector2((i-(selectedSchool=="elementary"?2.5f:1f))*(selectedSchool=="elementary"?54:108),-148);gradeTabs[i].GetChild(0).GetComponent<Image>().color=i+1==selectedGrade?MgfLook.Hex("#B97926"):MgfLook.Hex("#237db0");}
            }
            packScroll=Mathf.Clamp(packScroll,0,Mathf.Max(0,filteredPacks.Count*94-336));renderedFirst=-1;RenderPackCards();
        }
        void RenderPackCards(){
            int first=Mathf.FloorToInt(packScroll/94);bool changed=first!=renderedFirst;renderedFirst=first;renderedScroll=packScroll;
            for(int i=0;i<packCards.Length;i++){
                int row=first+i;var c=packCards[i];bool visible=row<filteredPacks.Count;c.root.gameObject.SetActive(visible);if(!visible)continue;
                int at=filteredPacks[row];var e=index.packs[at];c.root.anchoredPosition=new Vector2(0,-44-(row*94-packScroll));
                if(changed||c.packIndex!=at){c.packIndex=at;c.title.text=e.title;c.meta.text=at==packAt?cardSelectedMeta[at]:cardMeta[at];c.number.text=cardNumbers[at];c.face.color=at==packAt?MgfLook.Hex("#97c8ca"):MgfLook.Hex("#e5d4ad");}
            }
            packEmpty.gameObject.SetActive(filteredPacks.Count==0);
            float total=Mathf.Max(336,filteredPacks.Count*94);packScrollThumb.gameObject.SetActive(total>336);packScrollThumb.sizeDelta=new Vector2(4,Mathf.Max(28,336*336/total));packScrollThumb.anchoredPosition=new Vector2(-12,-213-packScrollThumb.sizeDelta.y*.5f-(336-packScrollThumb.sizeDelta.y)*(total<=336?0:packScroll/(total-336)));
        }
        void HandleTitlePointer(){
            if(browserOpen){
                if(HitPackControl(packClose)){browserOpen=false;packBrowser.gameObject.SetActive(false);return;}
                for(int i=0;i<schoolTabs.Length;i++)if(HitPackControl(schoolTabs[i])){selectedSchool=schools[i];selectedGrade=Mathf.Min(selectedGrade,selectedSchool=="elementary"?6:3);packScroll=0;RefreshCatalogue();return;}
                for(int i=0;i<gradeTabs.Length;i++)if(gradeTabs[i].gameObject.activeSelf&&HitPackControl(gradeTabs[i])){selectedGrade=i+1;packScroll=0;RefreshCatalogue();return;}
                if(Hit(packViewport)){packDragging=true;packPointerStart=MgfPointer.Position;packDragStart=packPointerStart.y;packScrollStart=packScroll;pendingPack=-1;for(int i=0;i<packCards.Length;i++)if(packCards[i].root.gameObject.activeSelf&&Hit(packCards[i].root))pendingPack=packCards[i].packIndex;}
                return;
            }
            if(Hit(packRect)){OpenCatalogue();return;}
            if(Hit(ctaRect)&&loaded&&!loading){TestStart();return;}
            if(!loaded&&!loading)StartCoroutine(BootPacks());
        }
        void AnimateTitleUi(float dt){
            if(playStarted||!browserOpen)return;
            if(packDragging&&MgfPointer.Held){float delta=(MgfPointer.Position.y-packDragStart)*844f/Mathf.Max(1,Screen.height);packScroll=Mathf.Clamp(packScrollStart+delta,0,Mathf.Max(0,filteredPacks.Count*94-336));if(Mathf.Abs(delta)>9)pendingPack=-1;}
            if(packDragging&&MgfPointer.Up){packDragging=false;if(pendingPack>=0&&!loading){int at=pendingPack;pendingPack=-1;browserOpen=false;packBrowser.gameObject.SetActive(false);MgfSfx.Play("tap");StartCoroutine(LoadPack(at));}}
            float wheel=Input.mouseScrollDelta.y;if(wheel!=0&&Hit(packViewport))packScroll=Mathf.Clamp(packScroll-wheel*36,0,Mathf.Max(0,filteredPacks.Count*94-336));
            if(Mathf.Abs(renderedScroll-packScroll)>.01f)RenderPackCards();
        }
    }
}
