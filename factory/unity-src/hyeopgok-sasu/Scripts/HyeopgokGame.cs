using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame : MonoBehaviour, IMgfGame
    {
        [Serializable] class State : MgfState {
            public int hp=100, coins, attempts, firstTry, moves, hover=-1;
            public string pack_id="",packTitle="",questionId="",prompt="";
            public string[] choices;
            public float[] padScreen;
            public float kingX,kingZ;
            public int redCount,blueCount,padCount,max,earned,spent,investment,pourCount,builtTowers,upgradeLevel;
            public int[] poured;
            public string answerMode="",accept="";
            public bool pending,confirming,tutorialBlocked,assembledFractionVisible;
            public string assembledFractionText="";
            public bool[] visited;
            public float confirm,dwell;
            public float[] kingScreen,exitScreen,collectionScreen;
            public float[] choicePadRects,upgradePadRects,frontLineScreen,shadowSampleScreen,kingRect,battlefieldRect;
            public float[] worldLabelRects;
            public int worldLabelMask;
            public int streak,chestCoins,upgradeCost;
            public int nextUpgradeCost,rewardSerial;
            public int[] towerLevels,towerTypes,upgradeRemaining;
            public string rewardKind="none",rewardTowerType="none",upgradePhase="idle";
            public bool rewardPhase,chestOpen,upgradePouring,upgradeAffordable,victory,lastCorrect,feedbackVisible,wrongFeedback;
        }
        [Serializable] sealed class ProblemSample:MgfProblem {
            public string answer_mode,accept,num_label,den_label,format,explain;
            public int max,difficulty,coin_budget,answerValue;
            public FractionAnswer answerParts;
            public string[] distractor_tags;
        }
        [Serializable] sealed class ProblemSamples { public ProblemSample[] items; }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void HYEOPGOK_PushBank(string json);
        [DllImport("__Internal")] static extern void HYEOPGOK_PushV3Layout(string json);
#else
        static void HYEOPGOK_PushBank(string json) { }
        static void HYEOPGOK_PushV3Layout(string json) { }
#endif
        readonly State st=new State();
        public readonly HyeopgokRules Rules=new HyeopgokRules();
        QuestionPack pack;PackIndex index;int packAt;string bankJson="{\"items\":[]}";
        Camera cam;HyeopgokBattle battle;Transform king;
        bool loaded,loading,playStarted,dragging;
        float feedbackLeft,cameraLanding,uiTick;
        Vector3 cameraFocus=new Vector3(.5f,0,1.5f);
        float cameraReveal;
        int runSerial,answerStreak,rewardSerial;bool rewardIsRatio;Vector3 cameraBase;Quaternion cameraRot;
        HyeopgokChestReward lastReward;
        string feedback="";
        float pointerStarted;Vector2 pointerOrigin;bool movedDuringPress;

        void Awake()
        {
            MgfLook.Quality(65); QualitySettings.pixelLightCount=1;
            BuildWorld(); BuildUi();
            battle=gameObject.AddComponent<HyeopgokBattle>();battle.Init(cam);battle.SetKing(king);battle.SetEconomyRules(Rules);
            StartCoroutine(BootPacks());
        }
        string BaseUrl(){
            string url=Application.absoluteURL;
            if(string.IsNullOrEmpty(url))return "file:///Users/sitpo/math-game-factory/public/g/hyeopgok-sasu/";
            return new Uri(new Uri(url),".").AbsoluteUri;
        }
        string QueryPack(){
            string url=Application.absoluteURL; int q=url.IndexOf('?');if(q<0)return "";
            foreach(var kv in url.Substring(q+1).Split('&')){
                if(kv.StartsWith("pack=")){string s=Uri.UnescapeDataString(kv.Substring(5).Split('#')[0]);
                    foreach(char c in s)if(!char.IsLetterOrDigit(c)&&c!='-'&&c!='_')return "";
                    return s;
                }
            }return "";
        }
        IEnumerator BootPacks()
        {
            loading=true;
            using(var req=UnityWebRequest.Get(BaseUrl()+"packs/index.json")){
                req.timeout=15;yield return req.SendWebRequest();
                if(req.result!=UnityWebRequest.Result.Success){LoadError("팩 목록을 읽지 못했습니다. 다시 눌러 주세요.");yield break;}
                try{index=JsonUtility.FromJson<PackIndex>(req.downloadHandler.text);}catch{index=null;}
            }
            if(index==null||index.packs==null||index.packs.Length==0){LoadError("팩 목록 형식을 확인해 주세요.");yield break;}
            string wanted=QueryPack();if(wanted=="")wanted=index.default_pack;
            packAt=0;for(int i=0;i<index.packs.Length;i++)if(index.packs[i].pack_id==wanted)packAt=i;
            yield return LoadPack(packAt);
        }
        IEnumerator LoadPack(int at)
        {
            loading=true;loaded=false;SetTitleInfo("문제 팩을 불러오는 중…");
            var entry=index.packs[at];
            // Only files inside this game's packs directory are accepted.
            string file=string.IsNullOrEmpty(entry.file)?entry.pack_id+".json":entry.file;
            if(file.Contains("..")||file.Contains(":")||file.Contains("/")||file.Contains("\\")){LoadError("팩 파일 이름을 확인해 주세요.");yield break;}
            QuestionPack candidate=null;
            using(var req=UnityWebRequest.Get(BaseUrl()+"packs/"+file)){
                req.timeout=15;yield return req.SendWebRequest();
                if(req.result==UnityWebRequest.Result.Success){try{candidate=HyeopgokPackJson.Parse(req.downloadHandler.text);}catch{}}
            }
            QuestionPack eligible=FilterEligibleChoices(candidate,out int skipped);
            if(eligible==null||eligible.items==null||eligible.items.Length<10){LoadError("4지선다 문제가 10개 이상인 팩을 골라 주세요.");yield break;}
            if(skipped>0)Debug.LogWarning("[HYEOPGOK_V3] "+candidate.pack_id+": choice가 아닌 문항 또는 잘못된 보기 "+skipped+"개를 건너뜀");
            pack=eligible;packAt=at;st.pack_id=pack.pack_id;st.packTitle=pack.title;
            var bank=new List<ProblemSample>();var chars=new StringBuilder();
            foreach(var p in pack.items){
                bank.Add(new ProblemSample{id=p.id,prompt=p.prompt,choices=p.Mode=="choice"?p.choices:null,answer=p.PublicAnswerToken,answerNumeric=p.format=="text"?double.NaN:p.answerNumeric,unitConcept=p.unitConcept,
                    answer_mode=p.Mode,max=p.max,accept=p.accept,num_label=p.num_label,den_label=p.den_label,answerValue=p.answerValue,answerParts=p.answerParts,format=p.format,explain=p.explain,difficulty=p.difficulty,coin_budget=p.coin_budget,distractor_tags=p.distractor_tags});
                chars.Append(p.prompt).Append(p.explain).Append(p.answer).Append(p.num_label).Append(p.den_label);
            }
            bankJson=JsonUtility.ToJson(new ProblemSamples{items=bank.ToArray()}).Replace(":NaN",":null");
            MgfText.Prewarm(chars.ToString()+"협곡사수출격전선수비성문정답오답왕끌어패드위에서잠깐멈추세요승리재도전문제확률경우의수화면누르면바로시작전투목표압박까지초마다남은지켜라명중시연달성했습니다더필요합니다판단적중모든사건붓는다이대로확정되돌아가면취소먼저코인을이상밖으로나오기선택○");
            string schoolLabel=pack.school=="elementary"?"초":"중";
            loading=false;loaded=true;SetTitleInfo(pack.title+"  ·  "+schoolLabel+pack.grade+" "+pack.semester+"학기");
            // Refresh the same bridge when a user changes packs; QA samples the actual loaded pack.
            MgfBridge.Register(this);
            HYEOPGOK_PushBank(bankJson);
        }
        static long Gcd(long a,long b){a=Math.Abs(a);b=Math.Abs(b);while(b!=0){long t=a%b;a=b;b=t;}return Math.Max(1,a);}
        static bool TryRational(string text,out long numerator,out long denominator){
            numerator=0;denominator=1;if(string.IsNullOrEmpty(text))return false;
            string s=text.Trim();
            if(s.StartsWith("{frac:")&&s.EndsWith("}")){
                string[] parts=s.Substring(6,s.Length-7).Split('/');
                if(parts.Length!=2||!long.TryParse(parts[0],out numerator)||!long.TryParse(parts[1],out denominator)||denominator==0)return false;
            }else if(!long.TryParse(s,out numerator))return false;
            if(denominator<0){numerator=-numerator;denominator=-denominator;}
            long g=Gcd(numerator,denominator);numerator/=g;denominator/=g;return true;
        }
        static bool EligibleChoice(PackItem q,int schema,HashSet<string> ids){
            if(q==null||q.Mode!="choice"||string.IsNullOrEmpty(q.id)||!ids.Add(q.id)||string.IsNullOrEmpty(q.prompt)||string.IsNullOrEmpty(q.explain))return false;
            if(q.answer_type!="choice"||string.IsNullOrEmpty(q.answer)||q.choices==null||q.choices.Length!=4)return false;
            if(schema>=3&&(q.distractor_tags==null||q.distractor_tags.Length!=3))return false;
            int exact=0,equivalent=0;
            for(int i=0;i<4;i++){
                if(string.IsNullOrEmpty(q.choices[i]))return false;
                if(q.choices[i]==q.answer)exact++;
                if(PackItem.EquivalentChoice(q.choices[i],q.answer))equivalent++;
                for(int j=0;j<i;j++)if(PackItem.EquivalentChoice(q.choices[i],q.choices[j]))return false;
            }
            return exact==1&&equivalent==1;
        }
        public static QuestionPack FilterEligibleChoices(QuestionPack source,out int skipped){
            skipped=0;
            if(source==null||source.items==null||string.IsNullOrEmpty(source.title)||source.schema_version<0||source.schema_version>3)return null;
            var ids=new HashSet<string>();var items=new List<PackItem>(source.items.Length);
            foreach(var item in source.items){if(EligibleChoice(item,source.schema_version,ids))items.Add(item);else skipped++;}
            return new QuestionPack{pack_id=source.pack_id,title=source.title,school=source.school,unit_id=source.unit_id,schema_version=source.schema_version,
                grade=source.grade,semester=source.semester,unit_order=source.unit_order,standards=source.standards,economy=source.economy,has_economy=source.has_economy,items=items.ToArray()};
        }
        public static bool ValidPack(QuestionPack p){int skipped;QuestionPack filtered=FilterEligibleChoices(p,out skipped);return filtered!=null&&filtered.items.Length>=10;}
        void LoadError(string msg){loading=false;loaded=false;SetTitleInfo(msg);}
        public void TestStart(){
            if(!loaded)return;
            playStarted=true;dragging=false;st.phase="playing";st.moves=0;feedbackLeft=0;feedback="";answerStreak=0;rewardSerial=0;lastReward=new HyeopgokChestReward{kind=HyeopgokRewardKind.None,towerSlot=-1};
            ResetTutorialProgress();
            int seed=Environment.TickCount^(++runSerial*7919);Rules.Start(pack,seed);battle.Begin(seed^unchecked((int)0x6d2b79f5));cameraLanding=.8f;
            ShowPlaying();Present();MgfSfx.Play("whoosh");
        }
        void Present(){
            battle.SetStage(Rules.Wave);battle.BeginQuestion(Rules.TimeLimit);king.position=Rules.King;
            cameraReveal=.6f;
            SetQuestion(Rules.Current);SetChoices(Rules.Choices);SetPadVisibility();SyncState();RefreshHud();
        }
        void Resolve(){
            // The Kingshot-style reward beat is deliberately brief: seal, coin and
            // building growth land together, then the battle resumes. A tap on the
            // scroll can still pin and expand the complete explanation.
            // Fraction feedback gets a full reduction beat: raw case counts first,
            // then the canonical fraction. Correct feedback never shakes the camera.
            feedbackLeft=1.5f;
            Vector3 spot=Rules.LastPad>=0?HyeopgokRules.Pads[Rules.LastPad]:Rules.King;
            int hits=1,trials=1;long n=1,d=1;
            bool ratio=Rules.Current.format=="frac"&&TryRational(Rules.Current.answer,out n,out d)&&n>0&&d>1&&n<d;
            if(ratio){hits=(int)Math.Min(225,n);trials=(int)Math.Min(225,d);}
            if(Rules.LastCorrect){answerStreak++;rewardSerial++;cameraReveal=.6f;lastReward=battle.Reward(Math.Max(0,Rules.LastPad),spot,hits,trials,answerStreak,1-Rules.Elapsed/Rules.TimeLimit);MgfSfx.Play("correct");}
            else{answerStreak=0;lastReward=new HyeopgokChestReward{kind=HyeopgokRewardKind.None,towerSlot=-1};battle.Punish(spot);MgfSfx.Play("wrong");}
            string reward=lastReward.kind==HyeopgokRewardKind.Tower?"정답 · 보물상자에서 새 "+TowerKorean(lastReward.towerType)+"!":"정답 · 보물상자 코인 +"+lastReward.coinBonus;
            rewardIsRatio=ratio;
            string answer=Rules.Current.PublicAnswerToken;
            string answerLine="정답 "+answer;
            feedback=Rules.LastCorrect?reward+"\n"+answerLine+" · "+Rules.Current.explain:(Rules.LastPad<0?"시간 초과":"오답")+" · 성문 -18\n"+answerLine+" · "+Rules.Current.explain;
            HideTutorial();
            ShowFeedback(Rules.LastCorrect,feedback);SyncState();RefreshHud();
        }
        void Advance(){
            feedbackLeft=0;Rules.Next();
            if(Rules.Ended){Finish();return;}
            HideFeedback();Present();
        }
        public void TestAnswerCorrect(){TestSubmit(true);}
        public void TestAnswerWrong(){TestSubmit(false);}
        static string TowerKorean(HyeopgokTowerKind kind)=>kind==HyeopgokTowerKind.Cannon?"대포탑":kind==HyeopgokTowerKind.Magic?"마법탑":"석궁탑";
        void TapChoicePad(int pad){if(Rules.CommandChoicePad(pad)){HideTutorial();st.moves++;SyncState();}}
        void TestSubmit(bool correct){
            if(!playStarted)TestStart();if(!Rules.Active)return;
            if(Rules.Pending)Advance();if(!Rules.Active)return;
            int pad=Rules.AnswerPad();if(!correct)pad=(pad+1)%4;
            TapChoicePad(pad);
            for(int i=0;i<500&&!Rules.Pending;i++){Rules.Tick(.025f);king.position=Rules.King;}
            king.position=Rules.King;
            if(Rules.Pending)Resolve();else SyncState();
        }
        public string ProblemBankJson()=>bankJson;
        public string StateJson()=>JsonUtility.ToJson(st);
        void SyncState(){
            st.score=Rules.Score;st.lives=Rules.Hp;st.hp=Rules.Hp;st.coins=Rules.Coins;st.level=Math.Max(1,Rules.Wave);
            st.solved=Rules.Correct;st.attempts=Rules.Attempts;st.firstTry=Rules.Correct;
            st.padCount=Rules.PadCount;st.poured=Rules.Poured;st.visited=Rules.Visited;st.hover=Rules.Hover;st.pending=Rules.Pending;st.confirming=Rules.Confirming;st.confirm=Rules.Confirm;st.dwell=Rules.Dwell;st.tutorialBlocked=Rules.TutorialBlocked;
            st.builtTowers=battle.BuiltTowers;st.upgradeLevel=battle.UpgradeLevel;st.earned=Rules.Earned;st.spent=Rules.Spent;st.investment=Rules.Invested;st.pourCount=Rules.PourCount;
            st.streak=answerStreak;st.rewardKind=lastReward.KindName;st.rewardTowerType=lastReward.kind==HyeopgokRewardKind.Tower?lastReward.TowerName:"none";st.chestCoins=lastReward.coinBonus;
            st.rewardPhase=battle.ChestOpen;st.chestOpen=battle.ChestOpen;st.upgradePouring=battle.UpgradePouring;st.upgradePhase=battle.UpgradePouring?"pouring":"idle";st.victory=Rules.Won;
            st.lastCorrect=Rules.LastCorrect;st.feedbackVisible=feedbackLeft>0;st.wrongFeedback=feedbackLeft>0&&!Rules.LastCorrect;st.rewardSerial=rewardSerial;
            battle.CopyTowerState(ref st.towerLevels,ref st.towerTypes,ref st.upgradeRemaining);
            st.upgradeCost=0;for(int i=0;i<st.towerLevels.Length;i++)if(st.towerLevels[i]>=0&&st.towerLevels[i]<2&&(st.upgradeCost==0||st.upgradeRemaining[i]<st.upgradeCost))st.upgradeCost=st.upgradeRemaining[i];st.upgradeAffordable=Rules.Coins>0&&st.upgradeCost>0;
            st.nextUpgradeCost=st.upgradeCost;
            st.assembledFractionVisible=assembledText!=null&&assembledText.gameObject.activeSelf;
            st.assembledFractionText=st.assembledFractionVisible?assembledText.text:"";
            if(Rules.Current!=null){st.answerMode=Rules.Current.Mode;st.max=Rules.Current.Max;st.accept=Rules.Current.accept;}
            st.kingX=Rules.Target.x;st.kingZ=Rules.Target.z;st.redCount=battle.Reds;st.blueCount=battle.Blues;
            if(Rules.Current!=null){st.questionId=Rules.Current.id;st.prompt=Rules.Current.prompt;st.choices=Rules.Current.Mode=="choice"?Rules.Choices:null;}
            UpdatePadScreen();MgfBridge.NotifyChanged();
        }
        void UpdatePadScreen(){
            if(st.padScreen==null){
                st.padScreen=new float[8];st.kingScreen=new float[2];st.exitScreen=new float[2];st.collectionScreen=new float[2];
                st.choicePadRects=new float[16];st.upgradePadRects=new float[HyeopgokBattle.TowerCount*4];st.frontLineScreen=new float[12];
                st.shadowSampleScreen=new float[44];st.kingRect=new float[4];st.battlefieldRect=new float[4];st.worldLabelRects=new float[32];
            }
            Vector3 kingPoint=cam.WorldToScreenPoint(Rules.King),exitPoint=cam.WorldToScreenPoint(HyeopgokRules.Exit),collectionPoint=cam.WorldToScreenPoint(battle.CollectionPoint);
            st.kingScreen[0]=kingPoint.x/Screen.width;st.kingScreen[1]=1-kingPoint.y/Screen.height;st.exitScreen[0]=exitPoint.x/Screen.width;st.exitScreen[1]=1-exitPoint.y/Screen.height;
            st.collectionScreen[0]=collectionPoint.x/Screen.width;st.collectionScreen[1]=1-collectionPoint.y/Screen.height;
            for(int i=0;i<4;i++){
                Vector3 s=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]);st.padScreen[i*2]=s.x/Screen.width;st.padScreen[i*2+1]=1-s.y/Screen.height;
                ProjectGroundRect(HyeopgokRules.Pads[i],.92f,.90f,st.choicePadRects,i*4);
            }
            for(int i=0;i<HyeopgokBattle.TowerCount;i++)ProjectGroundRect(HyeopgokBattle.UpgradePads[i],.72f,.58f,st.upgradePadRects,i*4);
            // The playable plateau plus enemy approach: west gate (-4.1), east
            // watchtower (6.8), southern contact bend (-7.1) and enemy gate (9.8).
            ProjectGroundRect(new Vector3(1.1f,.35f,1.35f),5.7f,8.45f,st.battlefieldRect,0);
            for(int i=0;i<4;i++){
                Vector3 p=battle.ArtFrontPoint(i);Vector3 s=cam.WorldToScreenPoint(p);int at=i*3;
                st.frontLineScreen[at]=s.x/Screen.width;st.frontLineScreen[at+1]=1-s.y/Screen.height;st.frontLineScreen[at+2]=s.z>0&&cam.pixelRect.Contains(s)?1:0;
            }
            ProjectShadowSample(Rules.King,0,1.02f);ProjectShadowSample(battle.ArtShadowWorld,11,1.35f);ProjectShadowSample(new Vector3(1.82f,1.2f,4.62f),22,1.34f);ProjectShadowSample(new Vector3(-2.73f,1.2f,4.15f),33,1.08f);
            ProjectBodyRect(Rules.King,st.kingRect,0);
            CaptureWorldLabelRects(st.worldLabelRects,out st.worldLabelMask);
        }
        void ProjectGroundRect(Vector3 c,float rx,float rz,float[] output,int at){
            float minX=1,minY=1,maxX=0,maxY=0;
            AccumulateProjection(c+new Vector3(-rx,0,-rz),ref minX,ref minY,ref maxX,ref maxY);AccumulateProjection(c+new Vector3(rx,0,-rz),ref minX,ref minY,ref maxX,ref maxY);
            AccumulateProjection(c+new Vector3(rx,0,rz),ref minX,ref minY,ref maxX,ref maxY);AccumulateProjection(c+new Vector3(-rx,0,rz),ref minX,ref minY,ref maxX,ref maxY);
            output[at]=minX;output[at+1]=minY;output[at+2]=maxX-minX;output[at+3]=maxY-minY;
        }
        void AccumulateProjection(Vector3 world,ref float minX,ref float minY,ref float maxX,ref float maxY){Vector3 s=cam.WorldToScreenPoint(world);float x=s.x/Screen.width,y=1-s.y/Screen.height;minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
        void ProjectBodyRect(Vector3 c,float[] output,int at){
            Vector3 a=cam.WorldToScreenPoint(c+new Vector3(-.45f,0,-.3f)),b=cam.WorldToScreenPoint(c+new Vector3(.45f,1.75f,.3f));
            float ax=a.x/Screen.width,ay=1-a.y/Screen.height,bx=b.x/Screen.width,by=1-b.y/Screen.height;
            output[at]=Mathf.Min(ax,bx);output[at+1]=Mathf.Min(ay,by);output[at+2]=Mathf.Abs(bx-ax);output[at+3]=Mathf.Abs(by-ay);
        }
        void ProjectShadowSample(Vector3 c,int at,float ringRadius){
            Vector3 s=cam.WorldToScreenPoint(c);st.shadowSampleScreen[at]=s.z>0&&cam.pixelRect.Contains(s)?1:0;st.shadowSampleScreen[at+1]=s.x/Screen.width;st.shadowSampleScreen[at+2]=1-s.y/Screen.height;
            for(int k=0;k<4;k++){float a=k*Mathf.PI*.5f;Vector3 r=cam.WorldToScreenPoint(c+new Vector3(Mathf.Cos(a)*ringRadius,.01f,Mathf.Sin(a)*ringRadius));st.shadowSampleScreen[at+3+k*2]=r.x/Screen.width;st.shadowSampleScreen[at+4+k*2]=1-r.y/Screen.height;}
        }
        void Finish(){
            if(st.phase=="clear"||st.phase=="gameover"||st.phase=="survived")return;
            st.phase=Rules.Won?"clear":Rules.Hp<=0?"gameover":"survived";
            SyncState();ShowEnd(Rules.Won,Rules.Hp<=0);MgfSfx.Play(Rules.Won?"win":"lose");
        }
        void Update(){
            long before=HyeopgokArtProbe.Begin();
            try{UpdateArtFrame();}finally{HyeopgokArtProbe.End(0,before);}
        }
        void UpdateArtFrame(){
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            UpdateCamera(dt);AnimateUi(dt);
            if(!playStarted){if(MgfPointer.Down)TitleInput();return;}
            if(Rules.Ended){if(feedbackLeft>0){feedbackLeft-=dt;if(feedbackLeft<=0)Finish();}else if(st.phase=="playing")Finish();else if(MgfPointer.Down&&Hit(retryRect))TestStart();return;}
            if(MgfPointer.Down && !HandleBattleUiPointer()){
                pointerStarted=Time.unscaledTime;pointerOrigin=MgfPointer.Position;movedDuringPress=false;
                if(MgfPointer.Position.y>cam.pixelRect.yMax){MgfSfx.Play("tap");}
                else if(MgfPointer.OnPlane(cam,1.24f,out Vector3 p)){
                    // A pointer-down may become a drag. Move immediately but do
                    // not spend a tap coin until release proves it stayed a tap.
                    dragging=true;Rules.Press(p);st.moves++;SyncState();MgfSfx.Play("tap");
                }
            }
            if(dragging&&MgfPointer.Held&&MgfPointer.OnPlane(cam,1.24f,out Vector3 drag)){
                if((MgfPointer.Position-pointerOrigin).sqrMagnitude>100||Time.unscaledTime-pointerStarted>.25f)movedDuringPress=true;
                if(movedDuringPress)Rules.Move(drag);
            }
            if(MgfPointer.Up){
                if(dragging&&MgfPointer.OnPlane(cam,1.24f,out Vector3 tap)){
                    int pad=Rules.PadAt(tap);
                    if(pad>=0)TapChoicePad(pad);else Rules.Move(tap,true);
                }
                dragging=false;SyncState();
            }
            if(feedbackLeft>0){feedbackLeft-=dt;if(feedbackLeft<=0)Advance();}
            else {
                bool upgraded=battle.TickUpgrade(Rules.King,Rules.Target,dt);
                if(Rules.Tick(dt)>=0)Resolve();else if(upgraded){RefreshUpgradePads();SyncState();}
            }
            king.position=Rules.King+Vector3.up*(Mathf.Sin(Time.unscaledTime*18)*.035f*Mathf.Min(1,(Rules.Target-Rules.King).magnitude));
            Vector3 toward=Rules.Target-Rules.King;if(toward.sqrMagnitude>.02f)king.rotation=Quaternion.Slerp(king.rotation,Quaternion.LookRotation(toward),dt*14);
            int damage=battle.DrainGateDamage();if(damage>0){Rules.Damage(damage);SyncState();RefreshHud();}
            if(Rules.Ended && feedbackLeft<=0)Finish();
            uiTick+=dt;if(uiTick>.15f){uiTick=0;UpdateBattleHud();SyncState();}
        }
        void UpdateCamera(float dt){
            if(cameraLanding>0)cameraLanding=Mathf.Max(0,cameraLanding-dt);
            if(cameraReveal>0)cameraReveal=Mathf.Max(0,cameraReveal-dt);
            // Stable strategy-game lens. Portrait is 17% closer than R2 and follows
            // the hero/pad/front focal triangle instead of proving the whole U exists.
            // Landscape is 1.8x closer than R2 and treats the battle front as the hero.
            float aspect=cam.pixelRect.width/Mathf.Max(1,cam.pixelRect.height);
            bool wide=aspect>1.2f;
            float focalSpan=0;
            Vector3 desired=new Vector3(-.15f,0,wide?-.20f:-.35f);
            if(playStarted && king){
                Vector3 k=Rules.King;
                Vector3 f=battle?battle.FrontWorld:new Vector3(2.2f,0,-5.5f);
                focalSpan=Vector2.Distance(new Vector2(k.x,k.z),new Vector2(f.x,f.z));
                float frontWeight=wide?Mathf.Lerp(.20f,.42f,Mathf.InverseLerp(3.8f,8.0f,focalSpan)):.22f;
                Vector3 triangle=Vector3.Lerp(new Vector3(k.x,0,k.z+.95f),new Vector3(f.x,0,f.z),frontWeight);
                float dx=triangle.x-desired.x,dz=triangle.z-desired.z;
                float deadX=wide?.48f:.72f,deadZ=wide?.48f:.82f;
                if(Mathf.Abs(dx)>deadX)desired.x+=Mathf.Sign(dx)*(Mathf.Abs(dx)-deadX)*.52f;
                if(Mathf.Abs(dz)>deadZ)desired.z+=Mathf.Sign(dz)*(Mathf.Abs(dz)-deadZ)*(wide?.72f:.48f);
            }
            // The stronger landscape yaw puts the northern rim tree just behind
            // the parchment unless the world framing leads slightly north. The
            // southern contact line still retains more than the 6% safe margin.
            if(wide)desired.z+=.65f;
            // Lock the lens while a pointer is held so a tap remains the same
            // world target throughout the gesture.
            if(!dragging)cameraFocus=Vector3.Lerp(cameraFocus,desired,1-Mathf.Exp(-dt*2.8f));
            float framing=Mathf.Max(1,.46f/aspect);
            float reveal=Mathf.Sin((1-cameraReveal/.6f)*Mathf.PI)*.95f;
            if(cameraReveal<=0)reveal=0;
            float landscape=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,1.35f,aspect));
            // If the hero walks all the way to a northern pad, temporarily dolly
            // back just enough to retain the front; at rest landscape stays at the
            // 11.0 distance and stronger isometric yaw required by the 70%-wide
            // battlefield framing. This also keeps the battlefield
            // at least 70% wide on the required 1280x800 framing.
            // Keep projection invariant for the entire held gesture. Otherwise a
            // fixed finger would raycast to a moving world point as the hero walks.
            float spanDolly=wide&&!dragging?Mathf.Max(0,focalSpan-3.8f)*.90f:0;
            float distance=Mathf.Lerp(38.6f,11.0f,landscape)*framing+spanDolly+reveal+(playStarted?cameraLanding*.35f:.65f);
            float shake=battle?battle.Shake:0;
            cam.fieldOfView=Mathf.Lerp(32,42,landscape);
            cam.transform.rotation=Quaternion.Euler(55,Mathf.Lerp(-9,-40,landscape),0);
            cam.transform.position=cameraFocus+cam.transform.rotation*(Vector3.back*distance)+new Vector3(Mathf.Sin(Time.unscaledTime*97)*shake,Mathf.Cos(Time.unscaledTime*83)*shake,0);
            // Distance fog starts beyond the playable plateau at either framing.
            RenderSettings.fogStartDistance=distance+13.5f;RenderSettings.fogEndDistance=distance+46;
            QualitySettings.shadowDistance=distance+8;
            AnimateKingRing();
            if(lastWidth!=Screen.width||lastHeight!=Screen.height){lastWidth=Screen.width;lastHeight=Screen.height;LayoutUi();UpdatePadScreen();}
        }
        int lastWidth,lastHeight;
    }
}
