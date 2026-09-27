using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        RectTransform v32CampaignBanner,v32PerkPanel,v32Fade,v32UpgradeBanner,v32ShowcasePanel;
        TextMeshProUGUI v32CampaignText,v32UpgradeTitle,v32UpgradeStats,v32ShowcaseText;
        readonly RectTransform[] v32PerkCards=new RectTransform[3];
        readonly TextMeshProUGUI[] v32PerkTexts=new TextMeshProUGUI[3];
        Image v32FadeImage;
        int v32ShownUpgradeSerial;
        float v32UpgradeBannerLife;

        void BuildV32Ui()
        {
            v32CampaignBanner=RoyalPanel("v3.2 campaign banner",playRoot,new Vector2(.5f,.62f),Vector2.zero,new Vector2(326,86));
            v32CampaignText=DisplayText("",v32CampaignBanner,new Vector2(.5f,.5f),Vector2.zero,new Vector2(306,72),28,MgfLook.Hex("#49301c"));
            v32CampaignBanner.gameObject.SetActive(false);

            v32PerkPanel=RoyalPanel("Permanent enhancement choice",playRoot,new Vector2(.5f,.48f),Vector2.zero,new Vector2(358,392));
            DisplayText("지역 수복 보상",v32PerkPanel,new Vector2(.5f,1),new Vector2(0,-42),new Vector2(320,52),27,MgfLook.Hex("#49301c"));
            string[] sub={"모든 타워 화력이 판이 끝날 때까지 누적됩니다.","떨어진 코인을 더 멀리서 끌어옵니다.","최대 HP가 즉시 늘고 다음 지역 회복량도 커집니다."};
            Color[] faces={MgfLook.Hex("#bd5b3f"),MgfLook.Hex("#267f92"),MgfLook.Hex("#3a8560")};
            for(int i=0;i<3;i++){
                RectTransform card=GameButton("Permanent perk "+i,v32PerkPanel,new Vector2(.5f,1),new Vector2(0,-118-i*91),new Vector2(318,78),faces[i],faces[i]*.74f);v32PerkCards[i]=card;
                var title=DisplayText(V32PerkChoices[i],card,new Vector2(.5f,.67f),Vector2.zero,new Vector2(292,34),21,Color.white);v32PerkTexts[i]=title;
                var detail=Text(sub[i],card,new Vector2(.5f,.25f),Vector2.zero,new Vector2(286,31),11,Color.white);detail.alignment=TextAlignmentOptions.Center;detail.textWrappingMode=TextWrappingModes.Normal;
            }
            v32PerkPanel.gameObject.SetActive(false);

            v32UpgradeBanner=RoyalPanel("Tower level-up banner",playRoot,new Vector2(.5f,.58f),Vector2.zero,new Vector2(338,116));
            v32UpgradeTitle=DisplayText("석궁탑 Lv2!",v32UpgradeBanner,new Vector2(.5f,.72f),Vector2.zero,new Vector2(312,48),28,MgfLook.Hex("#8a5517"));
            v32UpgradeStats=Text("공격력 12 → 20 · 연사 ×2",v32UpgradeBanner,new Vector2(.5f,.28f),Vector2.zero,new Vector2(310,44),16,MgfLook.Hex("#49301c"));v32UpgradeStats.alignment=TextAlignmentOptions.Center;
            v32UpgradeBanner.gameObject.SetActive(false);

            v32ShowcasePanel=RoyalPanel("v3.2 showcase caption",playRoot,new Vector2(.5f,1),new Vector2(0,-50),new Vector2(356,76));
            v32ShowcaseText=DisplayText("",v32ShowcasePanel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(332,64),17,MgfLook.Hex("#49301c"));
            v32ShowcaseText.lineSpacing=-2;v32ShowcasePanel.gameObject.SetActive(false);

            v32Fade=Box("Biome transition fade",playRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,2200),Color.clear);v32FadeImage=v32Fade.GetComponent<Image>();
            v32Fade.SetAsLastSibling();v32Fade.gameObject.SetActive(false);
        }

        void SetV32CampaignUi(string label,bool show,bool perks)
        {
            // Once the parchment is hidden, render/clear the whole framebuffer.
            // Otherwise WebGL preserves stale title/question pixels above the
            // shrunken gameplay camera viewport.
            if(show&&cam)cam.rect=new Rect(0,0,1,1);
            if(v32CampaignBanner){v32CampaignBanner.gameObject.SetActive(show&&!perks);if(show)v32CampaignText.text=label;}
            if(v32PerkPanel)v32PerkPanel.gameObject.SetActive(perks);
            if(questionPanel)questionPanel.gameObject.SetActive(!show);
            if(questionBackdrop)questionBackdrop.gameObject.SetActive(!show);
            if(questionShadow)questionShadow.gameObject.SetActive(!show);
            for(int i=0;i<4;i++){if(padLabelRoots[i])padLabelRoots[i].gameObject.SetActive(!show&&i<Rules.PadCount);if(padPaint[i])padPaint[i].gameObject.SetActive(!show&&i<Rules.PadCount);}
            for(int i=0;i<HyeopgokBattle.TowerCount;i++){
                if(upgradePadRoots[i])upgradePadRoots[i].gameObject.SetActive(false);
                if(upgradeLabelRoots[i])upgradeLabelRoots[i].gameObject.SetActive(false);
            }
            if(!show)RefreshUpgradePads();
        }

        void SetV32TransitionFade(float alpha)
        {
            if(!v32FadeImage)return;alpha=Mathf.Clamp01(alpha);v32Fade.gameObject.SetActive(alpha>.001f);
            v32FadeImage.color=new Color(.035f,.085f,.11f,alpha*.96f);
        }

        void SetV32ShowcaseUi(bool show)
        {
            if(show&&cam)cam.rect=new Rect(0,0,1,1);
            if(v32ShowcasePanel){v32ShowcasePanel.gameObject.SetActive(show);if(show)v32ShowcaseText.text=battle.ShowcaseLabel;}
            if(!show)return;
            if(questionPanel)questionPanel.gameObject.SetActive(false);if(questionBackdrop)questionBackdrop.gameObject.SetActive(false);if(questionShadow)questionShadow.gameObject.SetActive(false);
            if(hpPill)hpPill.gameObject.SetActive(false);if(wavePill)wavePill.gameObject.SetActive(false);if(correctPill)correctPill.gameObject.SetActive(false);if(treasuryPanel)treasuryPanel.gameObject.SetActive(false);
            for(int i=0;i<4;i++){if(padRoots[i])padRoots[i].gameObject.SetActive(false);if(padLabelRoots[i])padLabelRoots[i].gameObject.SetActive(false);}
            for(int i=0;i<HyeopgokBattle.TowerCount;i++)if(upgradePadRoots[i])upgradePadRoots[i].gameObject.SetActive(false);
        }

        bool HandleV32BattleUiPointer(){return v32Phase!="question";}

        void HandleV32PerkPointer()
        {
            for(int i=0;i<3;i++)if(v32PerkCards[i]&&Hit(v32PerkCards[i])){SelectV32Perk(i);MgfSfx.Play("tap");return;}
        }

        void UpdateV32PerkRects(ref float[] output)
        {
            if(output==null||output.Length!=12)output=new float[12];
            for(int i=0;i<3;i++){
                Rect rect=v32PerkCards[i]?WorldLabelScreenRect(v32PerkCards[i]):Rect.zero;int at=i*4;
                output[at]=rect.x/Mathf.Max(1,Screen.width);output[at+1]=1-rect.yMax/Mathf.Max(1,Screen.height);output[at+2]=rect.width/Mathf.Max(1,Screen.width);output[at+3]=rect.height/Mathf.Max(1,Screen.height);
            }
        }

        void AnimateV32Ui(float dt)
        {
            if(battle==null||v32UpgradeBanner==null)return;
            if(battle.UpgradeSequenceSerial!=v32ShownUpgradeSerial){
                v32ShownUpgradeSerial=battle.UpgradeSequenceSerial;v32UpgradeBannerLife=2.15f;
                v32UpgradeTitle.text=battle.UpgradeTargetType+" Lv"+battle.UpgradeSequenceLevel+"!";
                v32UpgradeStats.text=battle.UpgradeStatBefore+" → "+battle.UpgradeStatAfter;
            }
            if(v32UpgradeBannerLife>0){
                v32UpgradeBannerLife=Mathf.Max(0,v32UpgradeBannerLife-dt);bool reveal=battle.UpgradeSequenceProgress>.78f||battle.UpgradeSequencePhase=="range";
                v32UpgradeBanner.gameObject.SetActive(reveal);if(reveal){float pop=1+Mathf.Sin(Mathf.Clamp01((2.15f-v32UpgradeBannerLife)*6)*Mathf.PI)*.09f;v32UpgradeBanner.localScale=Vector3.one*pop;}
            }else v32UpgradeBanner.gameObject.SetActive(false);
        }
    }
}
