using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.HyeopgokSasu
{
    /// <summary>v3.2 stage, biome, boss and tower feedback layered onto the pooled battle.</summary>
    public sealed partial class HyeopgokBattle
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern float HYEOPGOK_CaptureUpgradeProgress();
#else
        static float HYEOPGOK_CaptureUpgradeProgress(){return -1;}
#endif
        static readonly Vector3[] DesertControls={
            // Same authoring coordinates as ArtSource/blender/v32_maps.py.
            new Vector3(5.2f,GroundY,16.5f),new Vector3(5.2f,GroundY,10f),new Vector3(5.2f,GroundY,7.8f),
            new Vector3(3.7f,GroundY,5.4f),new Vector3(5.45f,GroundY,2.6f),new Vector3(3.35f,GroundY,0),
            new Vector3(5f,GroundY,-2.7f),new Vector3(3.55f,GroundY,-5.25f),new Vector3(.9f,GroundY,-7.15f),
            new Vector3(-2.7f,GroundY,-6.35f),new Vector3(-4.2f,GroundY,-3.7f),new Vector3(-4.2f,GroundY,-1f)
        };
        static readonly Vector3[] SnowControls={
            // Centreline of the generated fork; PathPosition fans opposing lanes
            // onto the two authored branches, then merges them at (4.65, 2.4).
            new Vector3(5f,GroundY,16.5f),new Vector3(5f,GroundY,11f),new Vector3(5f,GroundY,9f),
            new Vector3(5.35f,GroundY,6.9f),new Vector3(5.175f,GroundY,4.55f),new Vector3(4.65f,GroundY,2.4f),
            new Vector3(4.45f,GroundY,-1.6f),new Vector3(3f,GroundY,-5.3f),new Vector3(.25f,GroundY,-7.25f),
            new Vector3(-2.8f,GroundY,-6.25f),new Vector3(-4.2f,GroundY,-3.5f),new Vector3(-4.2f,GroundY,-1f)
        };

        // One compact, readable construction beat: freeze -> cyan blueprint ->
        // overshoot/build burst -> punch/fanfare -> stat/range reveal.
        const float UpgradeSequenceDuration=1.12f;
        readonly LineRenderer[] towerRangeRings=new LineRenderer[TowerCount];
        readonly Transform[] maxCrowns=new Transform[TowerCount];
        Material v32GoldLine;
        LineRenderer upgradeBeam;
        AudioSource upgradeCoinSource;
        AudioClip upgradeCoinClip;
        int mapIndex;
        float difficultyMultiplier=1f;
        bool bossActive;
        float bossHp,bossMaxHp,bossClock,bossAttackClock;
        int upgradeSequenceSlot=-1,upgradeSequenceLevel,upgradeSequenceSerial;
        float upgradeSequenceLeft,rangeRingLeft;
        bool upgradeFanfarePlayed,upgradeConstructionBurstPlayed;
        string upgradeTargetType="",upgradeStatBefore="",upgradeStatAfter="";
        bool showcaseMode,showcaseFire;
        bool transitionPaused;
        bool upgradeRequiresExit;
        Transform showcaseRoot;

        public int MapIndex=>mapIndex;
        public float DifficultyMultiplier=>difficultyMultiplier;
        public bool BossActive=>bossActive;
        public float BossProgress=>bossMaxHp<=0?0:Mathf.Clamp01(1-bossHp/bossMaxHp);
        public float UpgradeSequenceProgress=>upgradeSequenceLeft<=0?0:1-upgradeSequenceLeft/UpgradeSequenceDuration;
        public string UpgradeSequencePhase{
            get{
                float p=UpgradeSequenceProgress;
                if(upgradeSequenceLeft<=0)return rangeRingLeft>0?"range":"idle";
                if(p<.09f)return "hitstop";
                if(p<.32f)return "blueprint";
                if(p<.44f)return "overshoot";
                if(p<.58f)return "construction";
                if(p<.78f)return "punch";
                return "stats";
            }
        }
        public int UpgradeSequenceSerial=>upgradeSequenceSerial;
        public int UpgradeSequenceLevel=>upgradeSequenceLevel;
        public string UpgradeTargetType=>upgradeTargetType;
        public string UpgradeStatBefore=>upgradeStatBefore;
        public string UpgradeStatAfter=>upgradeStatAfter;
        public bool RangeRingVisible=>upgradeSequenceSlot>=0&&towerRangeRings[upgradeSequenceSlot]&&towerRangeRings[upgradeSequenceSlot].gameObject.activeSelf;
        public float UpgradePunch{
            get{
                if(upgradeSequenceLeft<=0)return 0;
                float p=UpgradeSequenceProgress;
                return p<.55f||p>.90f?0:Mathf.Sin((p-.55f)/.35f*Mathf.PI)*.62f;
            }
        }
        public bool ShowcaseMode=>showcaseMode;
        public string ShowcaseLabel{get;private set;}="";

        void InitializeV32()
        {
            controls=grassControls;
            v32GoldLine=MgfLook.Unlit(MgfLook.Hex("#ffd05a"));
            for(int i=0;i<TowerCount;i++){
                var go=new GameObject("업그레이드 사거리 링 "+i);go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=v32GoldLine;line.useWorldSpace=true;line.loop=true;
                line.positionCount=40;line.startWidth=line.endWidth=.07f;line.shadowCastingMode=ShadowCastingMode.Off;
                for(int k=0;k<40;k++){float a=k*Mathf.PI*2/40;line.SetPosition(k,towerPositions[i]+new Vector3(Mathf.Cos(a)*3.1f,.055f,Mathf.Sin(a)*3.1f));}
                line.gameObject.SetActive(false);towerRangeRings[i]=line;
                GameObject crown=HyeopgokAiAssets.InstantiateModel("king_crown",towerPositions[i]+Vector3.up*3.1f,Quaternion.identity,.52f,towers[i],towerMaterials[0,2],false);
                if(crown){maxCrowns[i]=crown.transform;crown.SetActive(false);}
            }
            var beamObject=new GameObject("업그레이드 금빛 기둥");beamObject.transform.SetParent(transform,false);
            upgradeBeam=beamObject.AddComponent<LineRenderer>();upgradeBeam.sharedMaterial=v32GoldLine;upgradeBeam.useWorldSpace=true;
            upgradeBeam.positionCount=2;upgradeBeam.startWidth=.42f;upgradeBeam.endWidth=.08f;upgradeBeam.shadowCastingMode=ShadowCastingMode.Off;
            upgradeBeam.gameObject.SetActive(false);
            upgradeCoinSource=gameObject.AddComponent<AudioSource>();upgradeCoinSource.playOnAwake=false;upgradeCoinSource.spatialBlend=0;
            upgradeCoinClip=MakeUpgradeCoinClip();
            ApplyV32ShowcaseFromUrl();
        }

        static AudioClip MakeUpgradeCoinClip()
        {
            const int rate=22050,length=1323;var samples=new float[length];
            for(int i=0;i<length;i++){
                float t=i/(float)rate,env=Mathf.Min(1,i/80f)*Mathf.Exp(-t*31f)*(1-i/(float)length);
                samples[i]=(Mathf.Sin(t*Mathf.PI*2*920)+Mathf.Sin(t*Mathf.PI*2*1840)*.34f)*env*.34f;
            }
            var clip=AudioClip.Create("hyeopgok-upgrade-coin",length,1,rate,false);clip.SetData(samples,0);return clip;
        }

        void PlayUpgradeCoinPitch(int slot)
        {
            if(!upgradeCoinSource||!upgradeCoinClip||slot<0||slot>=TowerCount)return;
            int total=UpgradeCost(towerLevel[slot]);if(total<=0)return;
            int paid=Mathf.Clamp(total-upgradeRemaining[slot],1,total);
            upgradeCoinSource.pitch=Mathf.Lerp(.92f,1.52f,paid/(float)total);
            upgradeCoinSource.PlayOneShot(upgradeCoinClip,.25f);
        }

        public void ConfigureMap(int map,float multiplier)
        {
            mapIndex=Mathf.Clamp(map-1,0,2);difficultyMultiplier=Mathf.Max(1,multiplier);
            controls=mapIndex==1?DesertControls:mapIndex==2?SnowControls:grassControls;
            BuildPath();
        }

        public void BeginStage(int stage,int map,int loop,float multiplier)
        {
            int cumulativeKills=killCount;
            ConfigureMap(map,multiplier);
            ResetBattle(true);
            transitionPaused=false;
            killCount=cumulativeKills;
            wave=1;
            bossActive=false;bossHp=bossMaxHp=bossClock=bossAttackClock=0;
        }

        public void PauseForTransition()
        {
            transitionPaused=true;playing=false;questionActive=false;bossActive=false;pendingDamage=0;
            economy.Clear();
        }

        public void PrepareStageTransition(int stage,int map,int loop,float multiplier)
        {
            // Rebuild the path and pooled armies while the fade is still opaque,
            // then hold that fresh frame until the campaign resumes.
            BeginStage(stage,map,loop,multiplier);
            transitionPaused=true;playing=false;questionActive=false;fireClock=.5f;
        }

        public void ResumeAfterTransition()
        {
            transitionPaused=false;playing=true;
        }

        public void FreezeForGameOver()
        {
            transitionPaused=true;playing=false;questionActive=false;bossActive=false;
            battleTick=0;fireClock=salvoClock=999f;volleys=0;
        }

        float V32SimulationDelta(float dt)=>transitionPaused?0:dt;

        public void BeginBoss(int stage,int loop)
        {
            questionActive=false;bossActive=true;bossClock=0;bossAttackClock=.72f;
            bossMaxHp=(26+stage*4)*Mathf.Pow(1.25f,Mathf.Max(0,loop-1));bossHp=bossMaxHp;
            front=Mathf.Min(front,pathLength-5.2f);
            for(int i=0;i<redCount;i+=47)redKinds[i]=(byte)(i%94==0?4:3);
            if(redCount>4){redKinds[0]=4;redKinds[1]=3;redKinds[2]=3;redKinds[3]=3;}
            enrage=6;shake=Mathf.Max(shake,.13f);Burst(FrontPosition()+Vector3.up*.3f,72,4.2f);MgfSfx.Play("whoosh",.3f);
        }

        public bool TickBoss(float dt)
        {
            if(!bossActive)return false;
            dt=Mathf.Clamp(dt,0,.05f);bossClock+=dt;bossAttackClock-=dt;
            float power=4.5f;
            for(int i=0;i<TowerCount;i++)if(towerLevel[i]>=0)power+=(7+3*towerType[i])*(towerLevel[i]+1);
            if(rules!=null)power*=rules.TowerDamageMultiplier;
            bossHp=Mathf.Max(0,bossHp-power*dt);
            if(bossAttackClock<=0){
                bossAttackClock=.78f/Mathf.Min(1.8f,difficultyMultiplier);
                int damage=Mathf.Max(1,Mathf.RoundToInt(2*difficultyMultiplier-power*.018f));pendingDamage+=damage;
                shake=Mathf.Max(shake,.13f);impacts.Explosion(FrontPosition()+Vector3.up*.22f);
            }
            for(int i=0;i<4&&i<redCount;i++)redKinds[i]=(byte)(i==0?4:3);
            if(bossHp>0&&bossClock<8.5f)return false;
            bossActive=false;bossHp=0;boost=4;front=Mathf.Max(15.3f,front-2.2f);
            Burst(FrontPosition()+Vector3.up*.3f,110,5.2f);impacts.Explosion(FrontPosition()+Vector3.up*.55f);MgfSfx.Play("correct",.42f);
            // Recovered/perk screens pause the battlefield. Pending boss damage
            // stays queued so Game can drain it on the same or following frame.
            transitionPaused=true;
            return true;
        }

        void StartUpgradeSequence(int slot,int oldLevel,int newLevel)
        {
            // Completing one level is one deliberate pour. Keep the next-cost pad
            // visible, but require the king to step out and choose it again instead
            // of silently consuming the whole wallet during the celebration.
            upgradeRequiresExit=true;upgradePad=-1;upgradeDwell=0;upgradeClock[slot]=0;
            upgradeSequenceSlot=slot;upgradeSequenceLevel=newLevel+1;upgradeSequenceLeft=UpgradeSequenceDuration;
            upgradeSequenceSerial++;rangeRingLeft=UpgradeSequenceDuration+3f;
            upgradeFanfarePlayed=false;upgradeConstructionBurstPlayed=false;
            for(int i=0;i<TowerCount;i++)if(towerRangeRings[i])towerRangeRings[i].gameObject.SetActive(false);
            upgradeTargetType=towerType[slot]==1?"대포탑":towerType[slot]==2?"마법탑":"석궁탑";
            int[] cross={12,20,30},cannon={18,30,48},magic={10,18,28};int[] values=towerType[slot]==1?cannon:towerType[slot]==2?magic:cross;
            upgradeStatBefore="공격력 "+values[Mathf.Clamp(oldLevel,0,2)];
            string pattern=towerType[slot]==0?(newLevel==1?" · 연사 ×2":" · 부채꼴 ×3"):towerType[slot]==1?(newLevel==1?" · 폭발 반경 ↑":" · 연쇄 폭발"):newLevel==1?" · 연쇄 2타":" · 연쇄 4타";
            upgradeStatAfter="공격력 "+values[Mathf.Clamp(newLevel,0,2)]+pattern;
            hitStop=Mathf.Max(hitStop,.055f);visualHitStop=Mathf.Max(visualHitStop,.055f);shake=Mathf.Max(shake,.18f);
            // Keep a readable flashed silhouette during the 90 ms hitstop; the
            // following blueprint clip then rebuilds that silhouette bottom-up.
            towerRise[slot]=.55f;
            if(upgradeBeam){upgradeBeam.SetPosition(0,towerPositions[slot]+Vector3.up*.04f);upgradeBeam.SetPosition(1,towerPositions[slot]+Vector3.up*6.2f);upgradeBeam.gameObject.SetActive(true);}
            MgfSfx.Play("pop",.35f);
        }

        void UpdateV32Fx(float dt)
        {
            if(upgradeSequenceLeft>0){
                float captureProgress=HYEOPGOK_CaptureUpgradeProgress();
                if(captureProgress>=0){
                    upgradeSequenceLeft=UpgradeSequenceDuration*(1-Mathf.Clamp01(captureProgress));
                    if(upgradeSequenceSlot>=0)towerRise[upgradeSequenceSlot]=Mathf.Clamp01(.55f+captureProgress*1.25f);
                }else upgradeSequenceLeft=Mathf.Max(0,upgradeSequenceLeft-dt);
                float p=UpgradeSequenceProgress;
                if(!upgradeConstructionBurstPlayed&&p>=.44f){
                    upgradeConstructionBurstPlayed=true;
                    Burst(towerPositions[upgradeSequenceSlot],78,4.2f);
                    impacts.Explosion(towerPositions[upgradeSequenceSlot]+Vector3.up*.35f);
                }
                if(!upgradeFanfarePlayed&&p>=.68f){upgradeFanfarePlayed=true;MgfSfx.Play("correct",.42f);}
                if(p>=.78f&&upgradeSequenceSlot>=0&&towerRangeRings[upgradeSequenceSlot])towerRangeRings[upgradeSequenceSlot].gameObject.SetActive(true);
                if(upgradeBeam)upgradeBeam.gameObject.SetActive(p>.44f&&p<.74f);
            }else if(upgradeBeam&&upgradeBeam.gameObject.activeSelf)upgradeBeam.gameObject.SetActive(false);
            if(rangeRingLeft>0){rangeRingLeft=Mathf.Max(0,rangeRingLeft-dt);if(rangeRingLeft<=0&&upgradeSequenceSlot>=0&&towerRangeRings[upgradeSequenceSlot])towerRangeRings[upgradeSequenceSlot].gameObject.SetActive(false);}
        }

        float V32TowerScale(int slot)
        {
            int level=Mathf.Max(0,towerLevel[slot]);float scale=Mathf.Pow(1.2f,level);
            if(slot!=upgradeSequenceSlot||upgradeSequenceLeft<=0)return scale;
            float p=UpgradeSequenceProgress;
            if(p<.09f)return scale*.82f;
            if(p<.32f)return scale*Mathf.Lerp(.88f,1f,(p-.09f)/.23f);
            if(p<.55f)return scale*Mathf.Lerp(1f,1.25f,Mathf.Sin((p-.32f)/.23f*Mathf.PI*.5f));
            return scale*Mathf.Lerp(1.25f,1f,Mathf.SmoothStep(0,1,(p-.55f)/.45f));
        }

        void FireV32TowerPattern(int tower,bool guaranteedHit=true)
        {
            if(tower<0||tower>=TowerCount||towerLevel[tower]<0)return;
            int level=towerLevel[tower],type=towerType[tower];
            if(type==0){int shots=level+1;for(int i=0;i<shots;i++)FireBolt(tower,false,guaranteedHit,(i-(shots-1)*.5f)*.62f,0,0);}
            else if(type==1){FireBolt(tower,true,guaranteedHit,0,1,(byte)level);}
            else {int shots=level==0?1:level==1?2:4;for(int i=0;i<shots;i++)FireBolt(tower,false,guaranteedHit,(i-(shots-1)*.5f)*.48f,2,0);}
        }

        public void ApplyV32ShowcaseFromUrl()
        {
            string url=Application.absoluteURL??"";int at=url.IndexOf("v32showcase=");if(at<0)return;
            string value=url.Substring(at+12).Split('&')[0];showcaseMode=true;
            if(value=="towers"){
                ShowcaseLabel="석궁탑  |  대포탑  |  마법탑\nLv1 목조 → Lv2 석재+천 → Lv3 요새+룬";
                if(!showcaseRoot)BuildTowerGallery();else{for(int i=0;i<TowerCount;i++)towers[i].gameObject.SetActive(false);redCount=blueCount=displayedReds=displayedBlues=0;playing=false;}}
            else if(value.StartsWith("fire-")){showcaseFire=true;string[] part=value.Split('-');int type=part.Length>1&&part[1]=="cannon"?1:part.Length>1&&part[1]=="magic"?2:0;int level=part.Length>2?Mathf.Clamp(ParseDigit(part[2])-1,0,2):0;
                string[] typeName={"석궁탑","대포탑","마법탑"};string pattern=type==0?(level==0?"단발":level==1?"쌍발":"부채꼴 3연발"):type==1?(level==0?"직격":level==1?"확대 폭발":"연쇄 폭발"):level==0?"마력탄":level==1?"연쇄 번개 2타":"연쇄 번개 4타";
                ShowcaseLabel=typeName[type]+" Lv"+(level+1)+" · "+pattern;
                // A clean live firing range: the real pooled projectile path still
                // runs, but ambient melee flashes cannot hide the level pattern.
                redCount=96;blueCount=0;displayedReds=redCount;displayedBlues=0;battleTick=-8f;fireClock=0;
                for(int i=0;i<TowerCount;i++){towerLevel[i]=i==0?level:-1;towerType[i]=type;upgradeRemaining[i]=level<2?UpgradeCost(level):0;ApplyTowerVisibility(i);} }
        }

        static int ParseDigit(string text){int value;return int.TryParse(text,out value)?value:1;}

        void BuildTowerGallery()
        {
            showcaseRoot=new GameObject("타워 3종 × Lv1~3 쇼케이스").transform;showcaseRoot.SetParent(transform,false);
            string[] names={"crossbow","cannon","magic"};
            bool portrait=Screen.height>Screen.width;
            float displayScale=portrait?1f:.62f;
            float rowStep=portrait?3.45f:2.42f;
            float rowBase=portrait?-4.7f:-4.85f;
            float columnStep=portrait?3.2f:3.75f;
            for(int type=0;type<3;type++)for(int level=0;level<3;level++){
                Vector3 pos=new Vector3(-columnStep+type*columnStep,1.2f,rowBase+level*rowStep);
                GameObject model=HyeopgokAiAssets.InstantiateModel("tower_"+names[type]+"_lv"+(level+1),pos,Quaternion.Euler(0,180,0),displayScale*Mathf.Pow(1.2f,level),showcaseRoot,towerMaterials[type,level],true);
                if(model)model.name=names[type]+" Lv"+(level+1);
            }
            for(int i=0;i<TowerCount;i++)towers[i].gameObject.SetActive(false);
            redCount=blueCount=0;playing=false;
        }
    }
}
