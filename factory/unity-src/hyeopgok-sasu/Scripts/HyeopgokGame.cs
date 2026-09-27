using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame : MonoBehaviour, IMgfGame
    {
        [Serializable] class State : MgfState {
            public int hp=100, coins, attempts, firstTry, moves;
            public string pack_id="",packTitle="",questionId="",prompt="";
            public string[] choices;
            public float[] padScreen;
            public float kingX,kingZ;
            public int redCount,blueCount;
        }
        readonly State st=new State();
        public readonly HyeopgokRules Rules=new HyeopgokRules();
        QuestionPack pack;PackIndex index;int packAt;string bankJson="{\"items\":[]}";
        Camera cam;HyeopgokBattle battle;Transform king;
        bool loaded,loading,playStarted,dragging;
        float feedbackLeft,cameraLanding,uiTick;
        int runSerial;bool rewardIsRatio;Vector3 cameraBase;Quaternion cameraRot;
        string feedback="";

        void Awake()
        {
            MgfLook.Quality(34); QualitySettings.pixelLightCount=1;
            BuildWorld(); BuildUi();
            battle=gameObject.AddComponent<HyeopgokBattle>();battle.Init(cam);battle.SetKing(king);
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
                if(req.result==UnityWebRequest.Result.Success){try{candidate=JsonUtility.FromJson<QuestionPack>(req.downloadHandler.text);}catch{}}
            }
            if(!ValidPack(candidate)){LoadError("문제 팩 형식을 확인해 주세요.");yield break;}
            pack=candidate;packAt=at;st.pack_id=pack.pack_id;st.packTitle=pack.title;
            var bank=new List<MgfProblem>();var chars=new StringBuilder();
            foreach(var p in pack.items){bank.Add(new MgfProblem{id=p.id,prompt=p.prompt,choices=p.choices,answer=p.answer,answerNumeric=p.format=="text"?double.NaN:p.answerNumeric,unitConcept=p.unitConcept});chars.Append(p.prompt).Append(p.explain).Append(p.answer);}
            bankJson=MgfJson.Bank(bank);
            MgfText.Prewarm(chars.ToString()+"협곡사수출격전선수비성문정답오답왕끌어패드위에서잠깐멈추세요승리재도전문제확률경우의수화면누르면바로시작전투목표압박까지초마다남은지켜라명중시연달성했습니다더필요합니다판단적중○");
            string schoolLabel=pack.school=="elementary"?"초":"중";
            loading=false;loaded=true;SetTitleInfo(pack.title+"  ·  "+schoolLabel+pack.grade+" "+pack.semester+"학기");
            // Refresh the same bridge when a user changes packs; QA samples the actual loaded pack.
            MgfBridge.Register(this);
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
        static bool EquivalentChoice(string a,string b){
            if(TryRational(a,out long an,out long ad)&&TryRational(b,out long bn,out long bd))return an==bn&&ad==bd;
            return a==b;
        }
        static bool ValidPack(QuestionPack p){
            if(p==null||p.items==null||p.items.Length<10||string.IsNullOrEmpty(p.title))return false;
            foreach(var q in p.items){
                if(q==null||string.IsNullOrEmpty(q.prompt)||string.IsNullOrEmpty(q.answer)||q.choices==null||q.choices.Length!=4)return false;
                int found=0;for(int i=0;i<4;i++){
                    if(string.IsNullOrEmpty(q.choices[i]))return false;
                    if(EquivalentChoice(q.choices[i],q.answer))found++;
                    for(int j=0;j<i;j++)if(EquivalentChoice(q.choices[i],q.choices[j]))return false;
                }if(found!=1)return false;
            }return true;
        }
        void LoadError(string msg){loading=false;loaded=false;SetTitleInfo(msg);}
        public void TestStart(){
            if(!loaded)return;
            playStarted=true;dragging=false;st.phase="playing";st.moves=0;feedbackLeft=0;feedback="";
            Rules.Start(pack,Environment.TickCount^(++runSerial*7919));battle.Begin();cameraLanding=.8f;
            ShowPlaying();Present();MgfSfx.Play("whoosh");
        }
        void Present(){
            battle.SetStage(Rules.Wave);battle.BeginQuestion();king.position=Rules.King;
            SetQuestion(Rules.Current);SetChoices(Rules.Choices);SyncState();RefreshHud();
        }
        void Resolve(){
            feedbackLeft=Rules.LastCorrect?1.25f:1.65f;
            Vector3 spot=Rules.LastPad>=0?HyeopgokRules.Pads[Rules.LastPad]:Rules.King;
            int hits=1,trials=1;long n=1,d=1;
            bool ratio=Rules.Current.format=="frac"&&TryRational(Rules.Current.answer,out n,out d)&&n>0&&d>1&&n<d;
            if(ratio){hits=(int)Math.Min(225,n);trials=(int)Math.Min(225,d);}
            if(Rules.LastCorrect){battle.Reward(Math.Max(0,Rules.LastPad),spot,hits,trials);MgfSfx.Play("correct");}
            else{battle.Punish(spot);MgfSfx.Play("wrong");}
            string reward=ratio?"정답 · 석궁 "+Rules.Current.answer+" 명중 시연!":Rules.Current.format=="frac"?"정답 · 확률 "+Rules.Current.answer+" 판단 적중!":"정답 · 경우의 수만큼 지원!";
            rewardIsRatio=ratio;
            feedback=(Rules.LastCorrect?reward:Rules.LastPad<0?"시간 초과 · 오답 · 성문 -18":"오답 · 성문 -18")+"\n"+Rules.Current.explain;
            HideTutorial();
            ShowFeedback(Rules.LastCorrect,feedback);SyncState();RefreshHud();
        }
        void Advance(){
            feedbackLeft=0;Rules.Next();
            if(Rules.Ended){Finish();return;}
            HideFeedback();Present();
        }
        public void TestAnswerCorrect(){
            if(!playStarted)TestStart();if(!Rules.Active)return;
            if(Rules.Pending)Advance();if(!Rules.Active)return;
            Rules.Select(Rules.AnswerPad());Resolve();
        }
        public void TestAnswerWrong(){
            if(!playStarted)TestStart();if(!Rules.Active)return;
            if(Rules.Pending)Advance();if(!Rules.Active)return;
            Rules.Select((Rules.AnswerPad()+1)%4);Resolve();
        }
        public string ProblemBankJson()=>bankJson;
        public string StateJson()=>JsonUtility.ToJson(st);
        void SyncState(){
            st.score=Rules.Score;st.lives=Rules.Hp;st.hp=Rules.Hp;st.coins=Rules.Coins;st.level=Math.Max(1,Rules.Wave);
            st.solved=Rules.Correct;st.attempts=Rules.Attempts;st.firstTry=Rules.Correct;
            st.kingX=Rules.Target.x;st.kingZ=Rules.Target.z;st.redCount=battle.Reds;st.blueCount=battle.Blues;
            if(Rules.Current!=null){st.questionId=Rules.Current.id;st.prompt=Rules.Current.prompt;st.choices=Rules.Choices;}
            UpdatePadScreen();MgfBridge.NotifyChanged();
        }
        void UpdatePadScreen(){
            if(st.padScreen==null)st.padScreen=new float[8];
            for(int i=0;i<4;i++){Vector3 s=cam.WorldToScreenPoint(HyeopgokRules.Pads[i]);st.padScreen[i*2]=s.x/Screen.width;st.padScreen[i*2+1]=1-s.y/Screen.height;}
        }
        void Finish(){
            if(st.phase=="clear"||st.phase=="gameover"||st.phase=="survived")return;
            st.phase=Rules.Won?"clear":Rules.Hp<=0?"gameover":"survived";
            SyncState();ShowEnd(Rules.Won,Rules.Hp<=0);MgfSfx.Play(Rules.Won?"win":"lose");
        }
        void Update(){
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            UpdateCamera(dt);AnimateUi(dt);
            if(!playStarted){if(MgfPointer.Down)TitleInput();return;}
            if(Rules.Ended){if(feedbackLeft>0){feedbackLeft-=dt;if(feedbackLeft<=0)Finish();}else if(st.phase=="playing")Finish();else if(MgfPointer.Down&&Hit(retryRect))TestStart();return;}
            if(MgfPointer.Down){
                if(MgfPointer.Position.y>Screen.height*.8f){MgfSfx.Play("tap");}
                else if(MgfPointer.OnPlane(cam,1.24f,out Vector3 p)){
                    dragging=true;Rules.Move(p);st.moves++;HideTutorial();SyncState();MgfSfx.Play("tap");
                }
            }
            if(dragging&&MgfPointer.Held&&MgfPointer.OnPlane(cam,1.24f,out Vector3 drag)){Rules.Move(drag);}
            if(MgfPointer.Up){dragging=false;SyncState();}
            if(feedbackLeft>0){feedbackLeft-=dt;if(feedbackLeft<=0)Advance();}
            else if(Rules.Tick(dt)>=0)Resolve();
            king.position=Rules.King+Vector3.up*(Mathf.Sin(Time.unscaledTime*18)*.035f*Mathf.Min(1,(Rules.Target-Rules.King).magnitude));
            Vector3 toward=Rules.Target-Rules.King;if(toward.sqrMagnitude>.02f)king.rotation=Quaternion.Slerp(king.rotation,Quaternion.LookRotation(toward),dt*14);
            int damage=battle.DrainGateDamage();if(damage>0){Rules.Damage(damage);SyncState();RefreshHud();}
            if(Rules.Ended && feedbackLeft<=0)Finish();
            uiTick+=dt;if(uiTick>.15f){uiTick=0;UpdateBattleHud();}
        }
        void UpdateCamera(float dt){
            if(cameraLanding>0)cameraLanding=Mathf.Max(0,cameraLanding-dt);
            float aspect=(float)Screen.width/Screen.height;
            float fit=Mathf.Max(1.12f,.66f/aspect)+(playStarted?cameraLanding*.03f:.03f);
            Vector3 focus=new Vector3(.5f,0,2.4f);
            float shake=battle?battle.Shake:0;
            cam.transform.position=focus+(cameraBase-focus)*fit+new Vector3(aspect>1.2f?-3.0f:0,0,0)+new Vector3(Mathf.Sin(Time.unscaledTime*97)*shake,Mathf.Cos(Time.unscaledTime*83)*shake,0);
            if(lastWidth!=Screen.width||lastHeight!=Screen.height){lastWidth=Screen.width;lastHeight=Screen.height;LayoutUi();UpdatePadScreen();}
        }
        int lastWidth,lastHeight;
    }
}
