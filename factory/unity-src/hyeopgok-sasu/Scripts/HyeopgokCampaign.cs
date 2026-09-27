using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        static readonly string[] V32PerkChoices={"타워 공격력 +15%","코인 흡수 반경 +30%","성문 최대 HP +20"};
        string v32Phase="question";
        float v32PhaseClock;
        bool v32MapApplied;
        int v32MapSerial=1,v32TransitionSerial,v32BestQuestions;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int HYEOPGOK_LoadBest();
        [DllImport("__Internal")] static extern void HYEOPGOK_SaveBest(int value);
#else
        static int HYEOPGOK_LoadBest(){try{return PlayerPrefs.GetInt("hyeopgok-v32-best",0);}catch{return 0;}}
        static void HYEOPGOK_SaveBest(int value){try{PlayerPrefs.SetInt("hyeopgok-v32-best",value);PlayerPrefs.Save();}catch{}}
#endif

        void ApplyV32Fixture()
        {
            int map=QueryV32Int("v32map",1);
            if(map>=2&&map<=3)Rules.DebugSetStage(map);
        }

        int QueryV32Int(string key,int fallback)
        {
            string url=Application.absoluteURL??"";int q=url.IndexOf('?');if(q<0)return fallback;
            string[] entries=url.Substring(q+1).Split('&');
            for(int i=0;i<entries.Length;i++)if(entries[i].StartsWith(key+"=")){int value;if(int.TryParse(entries[i].Substring(key.Length+1).Split('#')[0],out value))return value;}
            return fallback;
        }

        bool HasV32Query(string key)
        {
            string url=Application.absoluteURL??"";return url.Contains(key+"=");
        }

        void StartV32Run()
        {
            v32Phase="question";v32PhaseClock=0;v32MapSerial=1;v32TransitionSerial=0;v32MapApplied=true;
            v32BestQuestions=HYEOPGOK_LoadBest();
            ApplyV32Map(Rules.Map);battle.BeginStage(Rules.Stage,Rules.Map,Rules.Loop,Rules.DifficultyMultiplier);
            battle.ApplyV32ShowcaseFromUrl();
            if(battle.ShowcaseMode){v32Phase="showcase";SetV32ShowcaseUi(true);}
            else SetV32ShowcaseUi(false);
        }

        void SetV32QuestionPhase()
        {
            if(v32Phase=="showcase")return;
            v32Phase="question";v32PhaseClock=0;v32MapApplied=true;SetV32CampaignUi("",false,false);SetV32TransitionFade(0);
        }

        void BeginV32Boss()
        {
            Rules.BeginBoss();v32Phase="boss";v32PhaseClock=0;
            battle.BeginBoss(Rules.Stage,Rules.Loop);SetV32CampaignUi("보스 웨이브",true,false);SyncState();
        }

        void BeginV32Recovered()
        {
            v32Phase="recovered";v32PhaseClock=1.15f;SetV32CampaignUi("지역 수복!",true,false);SyncState();
        }

        void BeginV32PerkChoice()
        {
            v32Phase="perk";v32PhaseClock=0;SetV32CampaignUi("영구 강화 1개 선택",true,true);SyncState();
        }

        public void SelectV32Perk(int index)
        {
            if(v32Phase!="perk")return;
            index=Mathf.Clamp(index,0,2);Rules.AdvanceStage(index);
            // Freeze and clear the old battlefield while the camera crosses maps.
            // Starting the new battle here would let transition-time kills leak a
            // coin into the next stage even though the carry rule resets coins.
            battle.PauseForTransition();
            v32Phase="transition";v32PhaseClock=1.5f;v32MapApplied=false;v32MapSerial++;v32TransitionSerial++;
            SetV32CampaignUi(V32MapName(Rules.Map)+"로 이동",true,false);SetV32TransitionFade(0);MgfSfx.Play("whoosh",.28f);SyncState();
        }

        bool TickV32Campaign(float dt)
        {
            if(v32Phase=="question")return false;
            if(v32Phase=="showcase")return true;
            if(v32Phase=="perk"){
                if(MgfPointer.Down)HandleV32PerkPointer();
                return true;
            }
            if(v32Phase=="boss"){
                if(battle.TickBoss(dt))BeginV32Recovered();
                return true;
            }
            if(v32Phase=="recovered"){
                v32PhaseClock=Mathf.Max(0,v32PhaseClock-dt);if(v32PhaseClock<=0)BeginV32PerkChoice();
                return true;
            }
            if(v32Phase=="transition"){
                v32PhaseClock=Mathf.Max(0,v32PhaseClock-dt);float elapsed=1.5f-v32PhaseClock;
                float alpha=elapsed<.75f?Mathf.SmoothStep(0,1,elapsed/.75f):Mathf.SmoothStep(1,0,(elapsed-.75f)/.75f);SetV32TransitionFade(alpha);
                if(!v32MapApplied&&elapsed>=.72f){
                    v32MapApplied=true;
                    battle.PrepareStageTransition(Rules.Stage,Rules.Map,Rules.Loop,Rules.DifficultyMultiplier);
                    ApplyV32Map(Rules.Map);cameraFocus=new Vector3(.5f,0,1.5f);cameraLanding=1;
                }
                if(v32PhaseClock<=0){battle.ResumeAfterTransition();SetV32TransitionFade(0);SetV32CampaignUi("",false,false);Present();}
                return true;
            }
            return false;
        }

        bool HandleV32TestSubmit()
        {
            if(!playStarted)return false;
            if(v32Phase=="perk"){SelectV32Perk((Rules.Stage-1)%3);return true;}
            return v32Phase!="question";
        }

        void SyncV32State()
        {
            st.stage=Rules.Stage;st.map=Rules.Map;st.loop=Rules.Loop;st.stageQuestion=Rules.StageQuestion;st.totalQuestions=Rules.Attempts;st.maxHp=Rules.MaxHp;
            st.mapName=V32MapName(Rules.Map);st.stagePhase=v32Phase;st.mapSerial=v32MapSerial;st.transitionSerial=v32TransitionSerial;st.difficultyMultiplier=Rules.DifficultyMultiplier;
            st.endReason=Rules.EndReason;st.kills=battle==null?0:battle.Kills;st.firstAttemptRate=Rules.Attempts<=0?0:Rules.Correct/(float)Rules.Attempts;
            st.bestQuestions=v32BestQuestions;st.questionDifficulty=Rules.QuestionDifficulty;st.questionHistory=Rules.QuestionHistory;
            st.perkDamage=Rules.PerkDamage;st.perkMagnet=Rules.PerkMagnet;st.perkHealth=Rules.PerkHealth;st.perkChoices=V32PerkChoices;
            st.bossWave=v32Phase=="boss";st.regionRecovered=v32Phase=="recovered";st.perkSelection=v32Phase=="perk";st.bossProgress=battle==null?0:battle.BossProgress;
            if(battle!=null){st.upgradeSeqPhase=battle.UpgradeSequencePhase;st.upgradeSeqProgress=battle.UpgradeSequenceProgress;st.upgradeSeqSerial=battle.UpgradeSequenceSerial;st.upgradeTargetLevel=battle.UpgradeSequenceLevel;st.upgradeTargetType=battle.UpgradeTargetType;st.upgradeStatBefore=battle.UpgradeStatBefore;st.upgradeStatAfter=battle.UpgradeStatAfter;st.rangeRing=battle.RangeRingVisible;}
            UpdateV32PerkRects(ref st.perkRects);
        }

        void FinishV32Record()
        {
            if(Rules.Attempts>v32BestQuestions){v32BestQuestions=Rules.Attempts;HYEOPGOK_SaveBest(v32BestQuestions);}
        }
    }
}
