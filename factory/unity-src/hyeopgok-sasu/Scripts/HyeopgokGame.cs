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
            public float[] kingScreen,exitScreen;
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
#else
        static void HYEOPGOK_PushBank(string json) { }
#endif
        readonly State st=new State();
        public readonly HyeopgokRules Rules=new HyeopgokRules();
        QuestionPack pack;PackIndex index;int packAt;string bankJson="{\"items\":[]}";
        Camera cam;HyeopgokBattle battle;Transform king;
        bool loaded,loading,playStarted,dragging;
        float feedbackLeft,cameraLanding,uiTick;
        Vector3 cameraFocus=new Vector3(.5f,0,1.5f);
        float cameraReveal;
        int runSerial;bool rewardIsRatio;Vector3 cameraBase;Quaternion cameraRot;
        string feedback="";
        float pointerStarted;Vector2 pointerOrigin;bool movedDuringPress;

        void Awake()
        {
            MgfLook.Quality(65); QualitySettings.pixelLightCount=1;
            BuildWorld(); BuildUi();
            battle=gameObject.AddComponent<HyeopgokBattle>();battle.Init(cam);battle.SetKing(king);battle.SetEconomyRules(Rules);
            Rules.OnPour=(pad,amount)=>{battle.PourCoin(pad);RefreshPadAmounts();SyncState();MgfSfx.Play("tap",.18f);};
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
            if(!ValidPack(candidate)){LoadError("문제 팩 형식을 확인해 주세요.");yield break;}
            pack=candidate;packAt=at;st.pack_id=pack.pack_id;st.packTitle=pack.title;
            var bank=new List<ProblemSample>();var chars=new StringBuilder();
            foreach(var p in pack.items){
                bank.Add(new ProblemSample{id=p.id,prompt=p.prompt,choices=p.Mode=="choice"?p.choices:null,answer=p.AnswerToken,answerNumeric=p.format=="text"?double.NaN:p.answerNumeric,unitConcept=p.unitConcept,
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
        public static bool ValidPack(QuestionPack p){
            if(p==null||p.items==null||p.items.Length<10||string.IsNullOrEmpty(p.title))return false;
            if(p.schema_version!=0&&p.schema_version!=1&&p.schema_version!=2)return false;
            if(p.schema_version==2&&p.economy==null)return false;
            if(p.economy!=null&&(p.economy.carry_capacity!=HyeopgokRules.CarryCapacity||p.economy.coin_per_kill!=HyeopgokRules.CoinPerKill||p.economy.min_spawn_coins!=HyeopgokRules.MinimumSpawnCoins))return false;
            var ids=new HashSet<string>();
            foreach(var q in p.items){
                if(q==null||string.IsNullOrEmpty(q.id)||!ids.Add(q.id)||string.IsNullOrEmpty(q.prompt)||string.IsNullOrEmpty(q.explain))return false;
                if(q.Mode=="amount"){
                    if(q.answer_type!="amount"||q.max<1||q.max>60||q.answerValue<0||q.answerValue>q.max)return false;
                }else if(q.Mode=="fraction_parts"){
                    if(q.answer_type!="fraction_parts"||q.answerParts==null||q.max<1||q.max>60||q.answerParts.num<0||q.answerParts.num>q.max||q.answerParts.den<1||q.answerParts.den>q.max)return false;
                    if(q.accept!="exact_parts"&&q.accept!="equivalent"&&q.accept!="reduced")return false;
                    if(string.IsNullOrEmpty(q.num_label)||string.IsNullOrEmpty(q.den_label))return false;
                    if(q.accept=="reduced"&&PackItem.Gcd(q.answerParts.num,q.answerParts.den)!=1)return false;
                }else if(q.Mode=="choice"){
                    if(q.answer_type!="choice"||string.IsNullOrEmpty(q.answer)||q.choices==null||q.choices.Length!=4)return false;
                    int exact=0,equivalent=0;for(int i=0;i<4;i++){
                        if(string.IsNullOrEmpty(q.choices[i]))return false;
                        if(q.choices[i]==q.answer)exact++;
                        if(PackItem.EquivalentChoice(q.choices[i],q.answer))equivalent++;
                        for(int j=0;j<i;j++)if(PackItem.EquivalentChoice(q.choices[i],q.choices[j]))return false;
                    }
                    // Authored packs must include the authored token, not merely
                    // another spelling of the same rational. Scoring still uses
                    // rational equivalence as a defensive backstop.
                    if(exact!=1||equivalent!=1)return false;
                }else return false;
                if(q.Mode!="choice"){
                    // Budget is determined by input mode, never the hidden answer.
                    // Every legal input in the public 0..max domain must be payable.
                    int expected=q.Mode=="amount"?HyeopgokRules.AmountBudget:HyeopgokRules.FractionBudget;
                    int budget=q.has_coin_budget||q.coin_budget!=0?q.coin_budget:expected;
                    int domainCost=q.max*(q.Mode=="fraction_parts"?2:1);
                    int answerCost=q.Mode=="amount"?q.answerValue:q.answerParts.num+q.answerParts.den;
                    if(budget!=expected||domainCost>budget||answerCost>budget||budget>HyeopgokRules.CarryCapacity||budget>HyeopgokRules.MinimumSpawnCoins)return false;
                }
            }return true;
        }
        void LoadError(string msg){loading=false;loaded=false;SetTitleInfo(msg);}
        public void TestStart(){
            if(!loaded)return;
            playStarted=true;dragging=false;st.phase="playing";st.moves=0;feedbackLeft=0;feedback="";
            ResetTutorialProgress();
            Rules.Start(pack,Environment.TickCount^(++runSerial*7919));battle.Begin();cameraLanding=.8f;
            ShowPlaying();Present();MgfSfx.Play("whoosh");
        }
        void Present(){
            battle.SetStage(Rules.Wave);battle.BeginQuestion(Rules.TimeLimit);king.position=Rules.King;
            cameraReveal=.6f;
            SetQuestion(Rules.Current);SetChoices(Rules.Choices);SetPadVisibility();SyncState();RefreshHud();
        }
        void Resolve(){
            feedbackLeft=Rules.LastCorrect?1.55f:3.4f;
            Vector3 spot=Rules.LastPad>=0?HyeopgokRules.Pads[Rules.LastPad]:Rules.King;
            int hits=1,trials=1;long n=1,d=1;
            bool ratio=Rules.Current.format=="frac"&&TryRational(Rules.Current.answer,out n,out d)&&n>0&&d>1&&n<d;
            if(ratio){hits=(int)Math.Min(225,n);trials=(int)Math.Min(225,d);}
            if(Rules.LastCorrect){cameraReveal=.6f;battle.Reward(Math.Max(0,Rules.LastPad),spot,hits,trials,Rules.TotalPoured);MgfSfx.Play("correct");}
            else{battle.Punish(spot);MgfSfx.Play("wrong");}
            string reward=Rules.Current.Mode=="choice"?"정답 · 지원군 출격!":"정답 · "+Rules.TotalPoured+"닢 투자 · 건물 성장!";
            rewardIsRatio=ratio;
            string answer=Rules.Current.Mode=="fraction_parts"?Rules.Current.PartsToken:Rules.Current.AnswerToken;
            feedback=Rules.LastCorrect?reward+"\n"+Rules.Current.explain:(Rules.LastPad<0?"시간 초과":"오답 · "+Rules.TotalPoured+"닢 소실")+" · 성문 -18\n정답 "+answer+" · "+Rules.Current.explain;
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
        void TestSubmit(bool correct){
            if(!playStarted)TestStart();if(!Rules.Active)return;
            if(Rules.Pending)Advance();if(!Rules.Active)return;
            if(Rules.Current.Mode=="choice"){
                int pad=Rules.AnswerPad();if(!correct)pad=(pad+1)%4;
                Rules.Move(HyeopgokRules.Pads[pad]);
                for(int i=0;i<500&&!Rules.Pending;i++)Rules.Tick(.025f);
            }else{
                int a=Rules.Current.Mode=="amount"?Rules.Current.answerValue:Rules.Current.answerParts.den;
                int n=Rules.Current.Mode=="fraction_parts"?Rules.Current.answerParts.num:0;
                if(!correct){if(Rules.Current.Mode=="fraction_parts")n=n<Rules.Current.Max?n+1:n-1;else a=a<Rules.Current.Max?a+1:a-1;}
                // Already poured amounts cannot be undone, including in QA commands.
                // A fresh question is exercised by QA; partially played questions may
                // therefore truthfully fail a correct command after an overpour.
                battle.SimulateEarnedCoins(Mathf.Min(Rules.WalletCapacity,Mathf.Max(0,a-Rules.Poured[0])+Mathf.Max(0,n-Rules.Poured[1])));
                TestPourPad(0,a);if(Rules.PadCount==2)TestPourPad(1,n);
                Rules.Move(HyeopgokRules.Exit);
                for(int i=0;i<300&&!Rules.Pending;i++)Rules.Tick(.025f);
            }
            king.position=Rules.King;
            if(Rules.Pending)Resolve();else SyncState();
        }
        void TestPourPad(int pad,int wanted){
            if(Rules.Pending)return;
            if(wanted==0){
                // Zero is deliberate: stop long enough to select the empty pad,
                // then leave before the first automatic coin. A fly-through is
                // intentionally ignored by the production state machine.
                Rules.Move(HyeopgokRules.Pads[pad]);
                for(int i=0;i<200&&!Rules.Pending&&!Rules.Visited[pad];i++)Rules.Tick(.025f);
                return;
            }
            Rules.Move(HyeopgokRules.Pads[pad],true);
            for(int i=0;i<250&&!Rules.Pending&&(Rules.King-Rules.Target).sqrMagnitude>.012f;i++)Rules.Tick(.025f);
            for(int i=0;i<150&&!Rules.Pending&&Rules.Poured[pad]<wanted;i++){Rules.Move(HyeopgokRules.Pads[pad],true);Rules.Tick(.025f);}
        }
        public string ProblemBankJson()=>bankJson;
        public string StateJson()=>JsonUtility.ToJson(st);
        void SyncState(){
            st.score=Rules.Score;st.lives=Rules.Hp;st.hp=Rules.Hp;st.coins=Rules.Coins;st.level=Math.Max(1,Rules.Wave);
            st.solved=Rules.Correct;st.attempts=Rules.Attempts;st.firstTry=Rules.Correct;
            st.padCount=Rules.PadCount;st.poured=Rules.Poured;st.visited=Rules.Visited;st.hover=Rules.Hover;st.pending=Rules.Pending;st.confirming=Rules.Confirming;st.confirm=Rules.Confirm;st.dwell=Rules.Dwell;st.tutorialBlocked=Rules.TutorialBlocked;
            st.builtTowers=battle.BuiltTowers;st.upgradeLevel=battle.UpgradeLevel;st.earned=Rules.Earned;st.spent=Rules.Spent;st.investment=Rules.Invested;st.pourCount=Rules.PourCount;
            st.assembledFractionVisible=assembledText!=null&&assembledText.gameObject.activeSelf;
            st.assembledFractionText=st.assembledFractionVisible?assembledText.text:"";
            if(Rules.Current!=null){st.answerMode=Rules.Current.Mode;st.max=Rules.Current.Max;st.accept=Rules.Current.accept;}
            st.kingX=Rules.Target.x;st.kingZ=Rules.Target.z;st.redCount=battle.Reds;st.blueCount=battle.Blues;
            if(Rules.Current!=null){st.questionId=Rules.Current.id;st.prompt=Rules.Current.prompt;st.choices=Rules.Current.Mode=="choice"?Rules.Choices:null;}
            UpdatePadScreen();MgfBridge.NotifyChanged();
        }
        void UpdatePadScreen(){
            if(st.padScreen==null){st.padScreen=new float[8];st.kingScreen=new float[2];st.exitScreen=new float[2];}
            Vector3 kingPoint=cam.WorldToScreenPoint(Rules.King),exitPoint=cam.WorldToScreenPoint(HyeopgokRules.Exit);
            st.kingScreen[0]=kingPoint.x/Screen.width;st.kingScreen[1]=1-kingPoint.y/Screen.height;st.exitScreen[0]=exitPoint.x/Screen.width;st.exitScreen[1]=1-exitPoint.y/Screen.height;
            for(int i=0;i<4;i++){Vector3 s=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]);st.padScreen[i*2]=s.x/Screen.width;st.padScreen[i*2+1]=1-s.y/Screen.height;}
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
                if(MgfPointer.Position.y>Screen.height*.85f){MgfSfx.Play("tap");}
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
                if(dragging&&!movedDuringPress&&MgfPointer.OnPlane(cam,1.24f,out Vector3 tap))Rules.Move(tap,true);
                dragging=false;SyncState();
            }
            if(feedbackLeft>0){feedbackLeft-=dt;if(feedbackLeft<=0)Advance();}
            else if(Rules.Tick(dt)>=0)Resolve();
            king.position=Rules.King+Vector3.up*(Mathf.Sin(Time.unscaledTime*18)*.035f*Mathf.Min(1,(Rules.Target-Rules.King).magnitude));
            Vector3 toward=Rules.Target-Rules.King;if(toward.sqrMagnitude>.02f)king.rotation=Quaternion.Slerp(king.rotation,Quaternion.LookRotation(toward),dt*14);
            int damage=battle.DrainGateDamage();if(damage>0){Rules.Damage(damage);SyncState();RefreshHud();}
            if(Rules.Ended && feedbackLeft<=0)Finish();
            uiTick+=dt;if(uiTick>.15f){uiTick=0;UpdateBattleHud();SyncState();}
        }
        void UpdateCamera(float dt){
            if(cameraLanding>0)cameraLanding=Mathf.Max(0,cameraLanding-dt);
            if(cameraReveal>0)cameraReveal=Mathf.Max(0,cameraReveal-dt);
            // Stable 55-degree/32-degree lens. The dead zone preserves pad targeting,
            // while a modest follow makes the settlement feel continuous offscreen.
            float aspect=(float)Screen.width/Screen.height;
            Vector3 desired=new Vector3(.35f,0,1.25f);
            if(playStarted && king){
                Vector3 k=Rules.King;
                float dx=k.x-desired.x,dz=k.z+1.5f;
                if(Mathf.Abs(dx)>1.15f)desired.x+=Mathf.Sign(dx)*(Mathf.Abs(dx)-1.15f)*.22f;
                if(Mathf.Abs(dz)>1.2f)desired.z+=Mathf.Sign(dz)*(Mathf.Abs(dz)-1.2f)*.24f;
            }
            // Lock the lens while a pointer is held so a tap remains the same
            // world target throughout the gesture.
            if(!dragging)cameraFocus=Vector3.Lerp(cameraFocus,desired,1-Mathf.Exp(-dt*2.8f));
            float framing=Mathf.Max(1,.46f/aspect);
            float reveal=Mathf.Sin((1-cameraReveal/.6f)*Mathf.PI)*.95f;
            if(cameraReveal<=0)reveal=0;
            float distance=46.5f*framing+reveal+(playStarted?cameraLanding*.45f:.85f);
            float shake=battle?battle.Shake:0;
            cam.fieldOfView=32;
            cam.transform.rotation=Quaternion.Euler(55,-8,0);
            cam.transform.position=cameraFocus+cam.transform.rotation*(Vector3.back*distance)+new Vector3(Mathf.Sin(Time.unscaledTime*97)*shake,Mathf.Cos(Time.unscaledTime*83)*shake,0);
            if(lastWidth!=Screen.width||lastHeight!=Screen.height){lastWidth=Screen.width;lastHeight=Screen.height;LayoutUi();UpdatePadScreen();}
        }
        int lastWidth,lastHeight;
    }
}
