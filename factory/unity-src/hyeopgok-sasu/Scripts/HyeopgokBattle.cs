using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.HyeopgokSasu
{
    /// <summary>
    /// Pooled, allocation-free canyon battle. The board owns the king, pads, terrain,
    /// question clock and HP. This component owns the two edge towers, all soldiers,
    /// corpses, bolts and impact chips. A lane queue is a cheap one-dimensional
    /// separation solver over an arc-length-sampled Catmull-Rom flow field.
    /// </summary>
    public sealed class HyeopgokBattle : MonoBehaviour
    {
        public const float PressureGrace = 18f;
        public const float PressureInterval = 4f;
        public const int PressureDamage = 3;
        const int RedCap = 600, BlueCap = 150, RedLanes = 8, BlueLanes = 4;
        const int ParticleCap = 384, CorpseCap = 54, BoltCap = 56, PathSamples = 320;
        const float RedSpacing = .335f, BlueSpacing = .325f, GroundY = .27f;
        readonly float[] redS = new float[RedCap];
        readonly float[] blueS = new float[BlueCap];
        readonly float[] redHitTimes=new float[RedCap],blueHitTimes=new float[BlueCap];
        readonly float[] redDrawHits=new float[RedCap+CorpseCap],blueDrawHits=new float[BlueCap+CorpseCap];
        readonly float[] archerDrawHits=new float[RedCap],giantDrawHits=new float[RedCap],bossDrawHits=new float[4];
        readonly MaterialPropertyBlock soldierProperties=new MaterialPropertyBlock();
        static readonly int HitTimeId=Shader.PropertyToID("_HitTime");
        readonly byte[] redKinds=new byte[RedCap];
        readonly Matrix4x4[] redMatrices = new Matrix4x4[RedCap + CorpseCap];
        readonly Matrix4x4[] blueMatrices = new Matrix4x4[BlueCap + CorpseCap];
        readonly Matrix4x4[] shadowMatrices = new Matrix4x4[RedCap + BlueCap];
        readonly Matrix4x4[] whiteMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] goldMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] dustMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] boltMatrices = new Matrix4x4[BoltCap];
        readonly Matrix4x4[] trailMatrices = new Matrix4x4[BoltCap];
        readonly Matrix4x4[] archerMatrices=new Matrix4x4[RedCap];
        readonly Matrix4x4[] giantMatrices=new Matrix4x4[RedCap];
        readonly Matrix4x4[] bossMatrices=new Matrix4x4[4];
        readonly Matrix4x4[] whiteRedMatrices=new Matrix4x4[CorpseCap];
        readonly Matrix4x4[] whiteBlueMatrices=new Matrix4x4[BlueCap+CorpseCap];
        readonly Matrix4x4[] whiteGiantMatrices=new Matrix4x4[CorpseCap];
        readonly Matrix4x4[] whiteBossMatrices=new Matrix4x4[CorpseCap];
        readonly Matrix4x4[] allyArcherMatrices=new Matrix4x4[2];
        readonly Matrix4x4[] hpBackgroundMatrices=new Matrix4x4[24];
        readonly Matrix4x4[] hpMatrices=new Matrix4x4[24];
        readonly Matrix4x4[] swordMatrices=new Matrix4x4[24];
        readonly Vector3[] path = new Vector3[PathSamples];
        readonly float[] distances = new float[PathSamples];
        readonly Chip[] chips = new Chip[ParticleCap];
        readonly Corpse[] corpses = new Corpse[CorpseCap];
        readonly Bolt[] bolts = new Bolt[BoltCap];
        readonly Transform[] towers = new Transform[2];
        readonly float[] towerRise = new float[2];
        readonly float[] towerRecoil = new float[2];
        readonly GameObject[,] towerStages=new GameObject[2,3];
        readonly Renderer[,] towerRenderers=new Renderer[2,3];
        readonly Transform[] towerBows=new Transform[2];
        readonly MaterialPropertyBlock towerProperties=new MaterialPropertyBlock();
        readonly int[] towerLevel=new int[2];
        readonly Vector3[] towerPositions = {
            new Vector3(1.75f, 1.2f, -3.5f), new Vector3(1.75f, 1.2f, 4.7f)
        };
        readonly Vector3[] controls = {
            // Exact terrain control points from ArtSource/blender/build_models.py.
            new Vector3(4.3f,GroundY,9), new Vector3(4.3f,GroundY,5),
            new Vector3(4.15f,GroundY,1), new Vector3(3.7f,GroundY,-3.5f),
            new Vector3(2.7f,GroundY,-5.9f), new Vector3(.2f,GroundY,-7.1f),
            new Vector3(-2.6f,GroundY,-6.6f), new Vector3(-4,GroundY,-4.8f),
            new Vector3(-4.1f,GroundY,-1)
        };

        struct Chip
        {
            public Vector3 position, velocity;
            public float age, duration, size, rotation;
            public byte kind;
        }
        struct Corpse
        {
            public Vector3 position, velocity;
            public float age, duration, yaw, spin;
            public bool blue;
            public byte kind;
        }
        struct Bolt
        {
            public Vector3 start, target, previous;
            public float age, duration;
            public bool heavy, hit;
        }

        Camera renderCamera;
        Mesh soldierMesh, blueSoldierMesh, cubeMesh, shadowMesh, boltMesh, trailMesh;
        Mesh giantMesh,archerMesh,allyArcherMesh,bossMesh,hpMesh;
        Material redMaterial, blueMaterial, whiteMaterial, goldMaterial, dustMaterial, shadowMaterial;
        Material silhouetteMaterial,swordMaterial,hpMaterial,hpBackgroundMaterial,boltTrailMaterial;
        HyeopgokImpact impacts;
        HyeopgokEconomyFx economy;
        int redCount = 400, blueCount = 100, chipCursor, corpseCursor, boltCursor, killCount, pendingDamage;
        int wave = 1, volleys, volleyHits, volleyTrials, volleyFired, laneCursor, displayedReds, displayedBlues;
        float pathLength, front, clock, sinceAnswer, pressureClock, battleTick, fireClock, salvoClock;
        float salvoSpacing = .035f;
        float enrage, hitStop, flash, shake, boost;
        float visualHitStop, impactCooldown;
        int cachedRedDraws, cachedBlueDraws, cachedShadowDraws;
        int recycledKind,upgradeCount;
        float questionGrace=PressureGrace;
        public float QuestionGrace=>questionGrace;
        bool initialized, playing, questionActive;
        uint randomState = 0x5e3a07c1u;

        public int Kills { get { return killCount; } }
        public int BuiltTowers=>(towerLevel[0]>=0?1:0)+(towerLevel[1]>=0?1:0);
        public int UpgradeLevel=>upgradeCount;
        public int Reds { get { return displayedReds; } }
        public int Blues { get { return displayedBlues; } }
        public float Shake { get { return shake; } }
        public int Coins { get { return economy==null?0:economy.Coins; } }
        public int DepositRemaining { get { return economy==null?0:economy.Remaining; } }
        public bool Depositing { get { return economy!=null&&economy.Depositing; } }
        public void SetKing(Transform king){if(economy!=null)economy.SetKing(king);}
        public void SetEconomyRules(HyeopgokRules rules){economy.SetRules(rules);}
        public void PourCoin(int pad){economy.Pour(HyeopgokRules.Pads[pad]);}
        // Synchronous QA command uses actual kills, grounded coins and their pickup
        // trajectory. It never assigns a wallet or calls answer adjudication.
        public void SimulateEarnedCoins(int needed){
            for(int tick=0;tick<400&&Coins<needed;tick++){
                MoveSoldiers(.12f);
                if(tick%2==0)KillRed((tick/2)%RedLanes,.8f);
                economy.Simulate(.12f);
            }
        }
        public void EmitFire(Vector3 position,float intensity){if(impacts!=null)impacts.Fire(position,intensity);}

        public void Init(Camera cam)
        {
            if (initialized) return;
            renderCamera = cam;
            Shader shader = Resources.Load<Shader>("HyeopgokSasu/Shaders/Horde");
            redMaterial = MakeMaterial(shader, MgfLook.Hex("#D0060C").linear, false);
            blueMaterial = MakeMaterial(shader, MgfLook.Hex("#0C73D5").linear, false);
            whiteMaterial = MakeMaterial(shader, new Color(1,.995f,.95f), true);
            goldMaterial = MakeMaterial(shader, new Color(1,.59f,.028f), true);
            dustMaterial = MakeMaterial(shader, new Color(.89f,.81f,.66f), true);
            shadowMaterial = new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/Contact"));Color shadowTint=MgfLook.Hex("#2C6A5A").linear;shadowTint.a=.35f;shadowMaterial.SetColor("_Color",shadowTint);shadowMaterial.enableInstancing=true;
            silhouetteMaterial=MakeMaterial(shader,Color.white,true);silhouetteMaterial.SetFloat("_Flash",1);
            swordMaterial=MakeMaterial(shader,new Color(1,.08f,.18f),true);
            hpMaterial=MakeMaterial(shader,new Color(.91f,.08f,.09f),true);
            hpBackgroundMaterial=MakeMaterial(shader,new Color(.14f,.16f,.19f),true);
            boltTrailMaterial=MakeMaterial(shader,new Color(.33f,.77f,1),true);
            soldierMesh = LoadSoldier("soldier");
            blueSoldierMesh = LoadSoldier("soldier_blue");
            giantMesh=LoadSoldier("giant");archerMesh=LoadSoldier("archer_red");
            allyArcherMesh=LoadSoldier("archer_blue");bossMesh=LoadSoldier("boss");
            hpMesh=MakeBarMesh();
            cubeMesh = MakeChipMesh();
            shadowMesh = MakeShadowMesh();
            boltMesh = MakeBoltMesh();
            trailMesh = MakeTrailMesh();
            impacts = new HyeopgokImpact(cam,shader);
            economy=new HyeopgokEconomyFx(cam,shader);
            BuildPath();
            BuildTowers();
            initialized = true;
            ResetBattle(false);
        }

        public void Begin()
        {
            if (!initialized) return;
            ResetBattle(true);
        }

        public void SetStage(int nextWave)
        {
            bool finalBoss=wave<10&&nextWave>=10;
            wave = Mathf.Clamp(nextWave, 1, 10);
            int nextCount = Mathf.Min(RedCap, 400 + (wave - 1) * 22);
            for (int i = redCount; i < nextCount; i++) {redS[i] = -.15f * (i - redCount + 1);redKinds[i]=KindFor(i);}
            redCount = nextCount;
            // The final-wave leader enters several ranks back, so it stays
            // readable through the answer reveal instead of dying in one tick.
            if(finalBoss)redKinds[120]=3;
        }

        public void BeginQuestion(float seconds=24)
        {
            sinceAnswer = pressureClock = 0;
            questionGrace=Mathf.Max(PressureGrace,seconds-14);
            questionActive = playing;
        }

        public void Reward(int padIndex, Vector3 pos, int hits, int trials,int invested=0)
        {
            if (!initialized) return;
            questionActive = false;
            enrage = 0;
            boost = 4.3f;
            hitStop = .05f;
            flash = 1;
            int tower = towerLevel[0]<0?0:Mathf.Abs(padIndex)%2;
            towerRise[tower] = .001f;
            towerRecoil[tower] = .25f;
            upgradeCount+=1+Mathf.Min(2,invested/20);
            towerLevel[tower]=Mathf.Min(2,(upgradeCount+1)/3);

            volleyTrials = Mathf.Clamp(trials, 1, 225);
            volleyHits = Mathf.Clamp(hits, 0, volleyTrials);
            volleyFired = 0;
            volleys = volleyTrials;
            salvoSpacing = Mathf.Clamp(2.8f / volleyTrials, .018f, .12f);
            salvoClock = 0;
            int added = Mathf.Min(8 + Mathf.Min(28,invested) + Mathf.RoundToInt(16f * volleyHits / volleyTrials), BlueCap - blueCount);
            for (int i = blueCount; i < blueCount + added; i++)
                blueS[i] = pathLength + (i - blueCount) * .10f;
            blueCount += added;
            // The front retreats visibly; the next frames advance the relieved blue army.
            front = Mathf.Max(15.3f, front - 1.25f);
            Burst(pos + Vector3.up * .2f, 36, 3.5f);
            Burst(FrontPosition(), 70, 4.0f);
            if (playing) MgfSfx.Play("whoosh", .20f);
        }

        public void Punish(Vector3 pos)
        {
            if (!initialized) return;
            questionActive = false;
            enrage = 4;
            boost = 0;
            shake = Mathf.Max(shake,.16f);
            front = Mathf.Min(pathLength - 3.8f, front + .7f);
            Burst(pos, 26, 2.3f);
            for (int i = 0; i < 5; i++) KillBlue(i % BlueLanes);
        }

        public int DrainGateDamage()
        {
            int result = pendingDamage;
            pendingDamage = 0;
            return result;
        }

        void ResetBattle(bool live)
        {
            playing = live;
            questionActive = false;
            wave = 1;
            redCount = 400;
            blueCount = 100;
            killCount = pendingDamage = volleys = volleyHits = volleyTrials = volleyFired = 0;
            clock = sinceAnswer = pressureClock = battleTick = fireClock = salvoClock = 0;
            hitStop = flash = shake = boost = enrage = 0;
            visualHitStop = impactCooldown = 0;
            cachedRedDraws = cachedBlueDraws = cachedShadowDraws = 0;
            impacts.Clear();
            economy.Clear();recycledKind=0;upgradeCount=0;
            front = 19.0f;
            laneCursor = 0;
            for (int i = 0; i < RedCap; i++) {redS[i] = front - .25f - (i / RedLanes) * RedSpacing - (i % RedLanes) * .018f;redKinds[i]=KindFor(i);redHitTimes[i]=-1000;}
            for (int i = 0; i < BlueCap; i++) {blueS[i] = front + .38f + (i / BlueLanes) * BlueSpacing + (i % BlueLanes) * .03f;blueHitTimes[i]=-1000;}
            for (int i = 0; i < ParticleCap; i++) chips[i].duration = 0;
            for (int i = 0; i < CorpseCap; i++) corpses[i].duration = 0;
            for (int i = 0; i < BoltCap; i++) bolts[i].duration = 0;
            // Both pre-existing towers visibly operate in the title battle.
            towerRise[0] = towerRise[1] = 1;
            towerRecoil[0] = towerRecoil[1] = 0;
            towerLevel[0]=live?-1:1;towerLevel[1]=live?0:1;
            displayedReds = redCount;
            displayedBlues = blueCount;
        }

        void Update(){
            long before=HyeopgokArtProbe.Begin();
            try{UpdateArtFrame();}finally{HyeopgokArtProbe.End(1,before);}
        }
        void UpdateArtFrame()
        {
            if (!initialized) return;
            float realDt = Mathf.Min(Time.deltaTime, .04f);
            visualHitStop = Mathf.Max(0,visualHitStop-realDt);
            impactCooldown = Mathf.Max(0,impactCooldown-realDt);
            shake = Mathf.MoveTowards(shake, 0, realDt * .6f);
            flash = Mathf.MoveTowards(flash, 0, realDt * 9);
            redMaterial.SetFloat("_Flash", flash * .14f);
            hitStop -= realDt;
            float dt = hitStop > 0 ? 0 : realDt;
            clock += dt;
            if (playing && questionActive)
            {
                sinceAnswer += dt;
                // The HUD exposes the same grace, cadence and damage. One thoughtful
                // 20-second solve takes no damage; pressure starts at 22 seconds.
                if (sinceAnswer > questionGrace)
                {
                    pressureClock += dt;
                    if (pressureClock >= PressureInterval)
                    {
                        pressureClock -= PressureInterval;
                        pendingDamage += PressureDamage;
                        shake = Mathf.Max(shake,.10f);
                        Burst(PathPosition(pathLength - 1, 0, out _), 16, 1.6f);
                    }
                }
            }
            boost = Mathf.Max(0, boost - dt);
            enrage = Mathf.Max(0, enrage - dt);
            float drift = enrage > 0 ? .18f : boost > 0 ? -.10f : .028f + .003f * wave;
            if (!playing) drift = Mathf.Sin(clock * .24f) * .035f;
            front = Mathf.Clamp(front + drift * dt, 15.2f, pathLength - 3.5f);
            MoveSoldiers(dt);
            battleTick += dt;
            if (battleTick > .11f)
            {
                battleTick -= .11f;
                int lane = laneCursor++ % RedLanes;
                Vector3 contact = PathPosition(front + Range(-.22f,.12f), (lane-3.5f) * .24f, out _);
                Burst(contact + Vector3.up * .32f, 4, 1.6f);
                impacts.Contact(contact + Vector3.up * .34f,false);
                MarkHit(contact);
                blueHitTimes[lane%BlueLanes]=Time.time;
                if(laneCursor%4==0)impacts.DamageNumber(contact+Vector3.up*.10f,false);
                if (laneCursor % 2 == 0) KillRed(lane, .8f);
                if (laneCursor % 5 == 0) KillBlue(lane % BlueLanes);
                if (playing && laneCursor % 8 == 0) MgfSfx.Play("pop", .07f);
            }
            fireClock -= dt;
            if (fireClock <= 0)
            {
                fireClock = boost > 0 ? .18f : .48f;
                FireBolt(towerLevel[0]<0?1:laneCursor%2, false, true);
            }
            if (volleys > 0)
            {
                salvoClock -= dt;
                if (salvoClock <= 0)
                {
                    salvoClock = salvoSpacing;
                    int before = volleyFired * volleyHits / volleyTrials;
                    int after = (volleyFired + 1) * volleyHits / volleyTrials;
                    FireBolt(volleys % 2, true, after > before);
                    volleyFired++;
                    volleys--;
                }
            }
            UpdateTowers(realDt);
            UpdateBolts(dt);
            UpdateParticles(dt);
            RenderSoldiers();
            impacts.UpdateAndDraw(visualHitStop > 0 ? 0 : realDt);
            economy.UpdateAndDraw(realDt);
        }

        void MoveSoldiers(float dt)
        {
            float redSpeed = enrage > 0 ? 2.2f : .88f + wave * .032f;
            for (int i = 0; i < redCount; i++)
            {
                // Each lane has its own ordered queue. Its leader is the collider at
                // the enemy front; followers cannot overlap their preceding soldier.
                float limit = i < RedLanes ? front - .08f - .022f * (i % 3) : redS[i - RedLanes] - RedSpacing;
                redS[i] = Mathf.Min(limit, redS[i] + redSpeed * dt);
            }
            for (int i = 0; i < blueCount; i++)
            {
                float limit = i < BlueLanes ? front + .24f + .025f * (i % 2) : blueS[i - BlueLanes] + BlueSpacing;
                blueS[i] = Mathf.Max(limit, blueS[i] - (boost > 0 ? 2.2f : .92f) * dt);
            }
        }

        void KillRed(int lane, float force)
        {
            if (lane >= redCount || redS[lane] < 0) return;
            Vector3 direction;
            Vector3 pos = PathPosition(redS[lane], (lane - 3.5f) * .245f, out direction);
            AddCorpse(pos, -direction * Range(.9f,2.0f) * force, false, Mathf.Atan2(direction.x,direction.z) * Mathf.Rad2Deg,redKinds[lane]);
            economy.Drop(pos);
            for (int i = lane; i < redCount; i += RedLanes)
            {
                redS[i] = i + RedLanes < redCount ? redS[i + RedLanes] : -.2f - Range(0,.7f);
                redKinds[i]=i+RedLanes<redCount?redKinds[i+RedLanes]:KindFor(recycledKind++);
                redHitTimes[i]=i+RedLanes<redCount?redHitTimes[i+RedLanes]:-1000;
            }
            killCount++;
            // A battlefield kill milestone owns this accent; answer submission does
            // not. The simulation/HP/question clocks keep advancing during the
            // 45 ms held army pose, so this cannot grant extra thinking time.
            if (playing && killCount % 12 == 0 && impactCooldown <= 0)
            {
                visualHitStop = .045f;
                impactCooldown = .85f;
                shake = Mathf.Max(shake,.085f);
                impacts.Contact(pos+Vector3.up*.43f,true);
            }
        }

        void KillBlue(int lane)
        {
            if (lane >= blueCount) return;
            Vector3 direction;
            Vector3 pos = PathPosition(blueS[lane], (lane - 1.5f) * .28f, out direction);
            AddCorpse(pos, direction * Range(.6f,1.2f), true, Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg);
            for (int i = lane; i < blueCount; i += BlueLanes){
                blueS[i] = i + BlueLanes < blueCount ? blueS[i + BlueLanes] : pathLength + Range(0,.5f);
                blueHitTimes[i]=i+BlueLanes<blueCount?blueHitTimes[i+BlueLanes]:-1000;
            }
        }

        void MarkHit(Vector3 position)
        {
            // Only an impact writes this visual state; no HP, lane order or RNG
            // changes. Each soldier carries its own GPU flash timestamp.
            float now=Time.time,best=.62f,secondBest=.62f;int first=-1,second=-1;
            for(int i=0;i<redCount;i++){
                if(redS[i]<front-3.2f||redS[i]<0)continue;
                Vector3 p=PathPosition(redS[i],(i%RedLanes-3.5f)*.245f,out _);
                float dx=p.x-position.x,dz=p.z-position.z;
                float distance=dx*dx+dz*dz;
                if(distance<best){second=first;secondBest=best;first=i;best=distance;}
                else if(distance<secondBest){second=i;secondBest=distance;}
            }
            for(int k=0;k<2;k++){
                int i=k==0?first:second;if(i<0)continue;
                if(redKinds[i]>=2&&now-redHitTimes[i]>.35f){Vector3 p=PathPosition(redS[i],(i%RedLanes-3.5f)*.245f,out _);impacts.DamageNumber(p+Vector3.up*(redKinds[i]==3?1.9f:1.25f),true);}
                redHitTimes[i]=now;
            }
        }

        void AddCorpse(Vector3 position, Vector3 velocity, bool blue, float yaw,byte kind=0)
        {
            int slot = corpseCursor++ % CorpseCap;
            corpses[slot] = new Corpse {
                position = position + Vector3.up * .12f,
                velocity = velocity + Vector3.up * Range(1.6f,3.2f),
                age = 0, duration = Range(.55f,.85f), yaw = yaw,
                spin = Range(-150,150), blue = blue,kind=kind
            };
        }

        void Burst(Vector3 position, int count, float power)
        {
            for (int i = 0; i < count; i++)
            {
                int slot = chipCursor++ % ParticleCap;
                float angle = Range(0,Mathf.PI*2);
                byte kind = (byte)(i % 5 == 0 ? 2 : i % 2);
                chips[slot] = new Chip {
                    position = position,
                    velocity = new Vector3(Mathf.Cos(angle)*Range(.15f,power), Range(.7f,power*1.1f),Mathf.Sin(angle)*Range(.15f,power)),
                    age = 0, duration = kind == 2 ? Range(.45f,.85f) : Range(.2f,.55f),
                    size = kind == 2 ? Range(.19f,.32f) : Range(.035f,.11f),
                    rotation = Range(0,360), kind = kind
                };
            }
        }

        void FireBolt(int tower, bool heavy, bool hit)
        {
            int slot = boltCursor++ % BoltCap;
            Vector3 start = towerPositions[tower] + Vector3.up * 2.20f;
            Vector3 target = PathPosition(front - Range(.1f,2.0f), Range(-.9f,.9f), out _);
            if (!hit) target += new Vector3(tower == 0 ? -2.15f : 2.15f, 0, 0);
            target.y += .3f;
            bolts[slot] = new Bolt { start = start, target = target, previous = start, age = 0,
                duration = Vector3.Distance(start,target) / (heavy ? 24f : 19f), heavy = heavy, hit = hit };
            towerRecoil[tower] = .13f;
        }

        void UpdateBolts(float dt)
        {
            int count = 0;
            for (int i = 0; i < BoltCap; i++)
            {
                Bolt b = bolts[i];
                if (b.duration <= 0) continue;
                b.age += dt;
                float p = Mathf.Clamp01(b.age / b.duration);
                Vector3 position = Vector3.LerpUnclamped(b.start,b.target,p) + Vector3.up * (Mathf.Sin(p*Mathf.PI) * .45f);
                Vector3 direction = (b.target-b.start + Vector3.up*(Mathf.Cos(p*Mathf.PI)*Mathf.PI*.45f)).normalized;
                Quaternion rot = Quaternion.LookRotation(direction);
                float tail = Mathf.Min(b.heavy ? 2.0f : 1.65f,Vector3.Distance(b.start,position));
                boltMatrices[count] = Matrix4x4.TRS(position,rot,new Vector3(.080f,.080f,.66f));
                trailMatrices[count] = Matrix4x4.TRS(position-direction*(tail*.5f),rot,new Vector3(b.heavy?.095f:.074f,b.heavy?.095f:.074f,tail));
                count++;
                if (p >= 1)
                {
                    Burst(b.target,b.heavy ? 12 : 8,b.heavy ? 3.2f : 2.2f);
                    if (b.hit)
                    {
                        impacts.Contact(b.target,b.heavy);
                        MarkHit(b.target);
                        if(b.heavy)impacts.Explosion(b.target);
                        else if(i%3==0)impacts.DamageNumber(b.target,false);
                        KillRed((i+laneCursor)%RedLanes,b.heavy ? 1.7f : 1f);
                        if (b.heavy) KillRed((i+laneCursor+3)%RedLanes,1.5f);
                    }
                    b.duration = 0;
                }
                b.previous = position;
                bolts[i] = b;
            }
            Draw(boltMesh,goldMaterial,boltMatrices,count);
            Draw(trailMesh,boltTrailMaterial,trailMatrices,count);
        }

        void UpdateParticles(float dt)
        {
            int whiteCount = 0, goldCount = 0, dustCount = 0;
            for (int i = 0; i < ParticleCap; i++)
            {
                Chip p = chips[i];
                if (p.duration <= 0) continue;
                p.age += dt;
                if (p.age >= p.duration) { p.duration = 0; chips[i] = p; continue; }
                p.position += p.velocity * dt;
                p.velocity.y -= dt * (p.kind == 2 ? 1.5f : 9f);
                float fraction = p.age / p.duration;
                float size = p.size * (p.kind == 2 ? .7f + fraction * 1.1f : 1-fraction*.65f);
                size *= Mathf.Min(1,(1-fraction)*4);
                Matrix4x4 matrix = Matrix4x4.TRS(p.position,Quaternion.Euler(p.rotation+fraction*90,p.rotation,fraction*145),
                    new Vector3(size,p.kind == 1 ? size * 2.2f : size,size));
                if (p.kind == 0) whiteMatrices[whiteCount++] = matrix;
                else if (p.kind == 1) goldMatrices[goldCount++] = matrix;
                else dustMatrices[dustCount++] = matrix;
                chips[i] = p;
            }
            Draw(cubeMesh,whiteMaterial,whiteMatrices,whiteCount);
            Draw(cubeMesh,goldMaterial,goldMatrices,goldCount);
            Draw(cubeMesh,dustMaterial,dustMatrices,dustCount);
            for (int i = 0; i < CorpseCap; i++)
            {
                Corpse c = corpses[i];
                if (c.duration <= 0) continue;
                c.age += dt;
                c.position += c.velocity * dt;
                c.velocity.y -= dt*10;
                if (c.position.y < GroundY) { c.position.y = GroundY; c.velocity *= .4f; }
                if (c.age >= c.duration) c.duration = 0;
                corpses[i] = c;
            }
        }

        static byte KindFor(int i)
        {
            return (byte)(i%96==24?2:i%9==4?1:0);
        }

        void RenderSoldiers()
        {
            int nr=0,nb=0,ns=0,na=0,ng=0,bosses=0,wr=0,wb=0,wg=0,wBoss=0,hp=0;
            Quaternion facing=renderCamera.transform.rotation;
            for(int i=0;i<redCount;i++)
            {
                if(redS[i]<0)continue;
                float row=i/RedLanes;
                float lane=(i%RedLanes-3.5f)*.245f*(1+Mathf.Sin(row*.77f)*.12f)+Mathf.Sin(i*1.73f)*.055f+Mathf.Sin(row*.53f)*.085f;
                Vector3 direction;
                Vector3 p=PathPosition(redS[i],lane,out direction);
                p+=direction*(Mathf.Sin(i*2.113f)*.09f);
                float step=Mathf.Sin(clock*(enrage>0?16:11)+i*2.399f);
                p.y+=.024f+Mathf.Abs(step)*.035f;
                float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg+Mathf.Sin(i*1.29f)*12f;
                float attack=redS[i]>front-.8f?Mathf.Max(0,Mathf.Sin(clock*13+i))*16:0;
                float size=(1+Mathf.Sin(i*2.71f)*.075f)*.85f;
                byte kind=redKinds[i];
                Quaternion rotation=Quaternion.Euler(step*3+attack,yaw,step*5);
                Matrix4x4 matrix=Matrix4x4.TRS(p,rotation,new Vector3(1.01f,1.035f+step*.035f,1.01f)*size);
                if(kind==1){archerDrawHits[na]=redHitTimes[i];archerMatrices[na++]=matrix;}
                else if(kind==2){giantDrawHits[ng]=redHitTimes[i];giantMatrices[ng++]=matrix;}
                else if(kind==3&&bosses<4){bossDrawHits[bosses]=redHitTimes[i];bossMatrices[bosses++]=matrix;}
                else {redDrawHits[nr]=redHitTimes[i];redMatrices[nr++]=matrix;}
                float shadowSize=kind>=2?.70f:.29f;
                shadowMatrices[ns++]=Matrix4x4.TRS(new Vector3(p.x+.08f,GroundY+.005f,p.z-.08f),Quaternion.identity,new Vector3(shadowSize,1,shadowSize*.8f));
                if(kind>=2&&Time.time-redHitTimes[i]<1.1f&&hp<24)
                {
                    float height=kind==3?2.80f:2.10f;
                    Vector3 at=p+Vector3.up*height;
                    float health=Mathf.Clamp01((front-redS[i])*.24f+.13f);
                    hpBackgroundMatrices[hp]=Matrix4x4.TRS(at,facing,new Vector3(1.0f,.14f,1));
                    hpMatrices[hp]=Matrix4x4.TRS(at-renderCamera.transform.forward*.012f-renderCamera.transform.right*(1-health)*.46f,facing,new Vector3(.92f*health,.08f,1));
                    Vector3 right=new Vector3(direction.z,0,-direction.x);
                    swordMatrices[hp]=Matrix4x4.TRS(p+right*.66f+Vector3.up*1.28f+direction*.25f,rotation*Quaternion.Euler(82,0,0),new Vector3(.09f,.09f,.92f+Mathf.Sin(clock*8+i)*.06f));
                    hp++;
                }
            }
            displayedReds=nr+na+ng+bosses;
            for(int i=0;i<blueCount;i++)
            {
                if(blueS[i]>pathLength)continue;
                Vector3 direction;
                Vector3 p=PathPosition(blueS[i],(i%BlueLanes-1.5f)*.285f+Mathf.Sin(i*1.31f)*.023f,out direction);
                p+=direction*(Mathf.Sin(i*2.117f)*.055f);
                float step=Mathf.Sin(clock*12+i*2.399f);
                p.y+=.02f+Mathf.Abs(step)*.035f;
                float yaw=Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg+Mathf.Sin(i*1.37f)*7f;
                float size=(1+Mathf.Sin(i*2.67f)*.035f)*.85f;
                float attack=blueS[i]<front+1?Mathf.Max(0,Mathf.Sin(clock*14+i))*18:0;
                Matrix4x4 matrix=Matrix4x4.TRS(p,Quaternion.Euler(step*3+attack,yaw,step*5),new Vector3(.98f,1.055f+step*.04f,.98f)*size);
                if(blueS[i]>pathLength-.52f)whiteBlueMatrices[wb++]=matrix;
                else {blueDrawHits[nb]=blueHitTimes[i];blueMatrices[nb++]=matrix;}
                shadowMatrices[ns++]=Matrix4x4.TRS(new Vector3(p.x+.08f,GroundY+.005f,p.z-.08f),Quaternion.identity,new Vector3(.29f,1,.23f));
            }
            displayedBlues=nb+wb;
            // A short white silhouette replaces the previous lingering corpse.
            // Existing corpse RNG/physics is preserved; an 80 ms white flash
            // clears the contact band quickly enough for the four-point stars.
            for(int i=0;i<CorpseCap;i++)
            {
                Corpse c=corpses[i];if(c.duration<=0||c.age>.08f)continue;
                float t=c.age/.08f;
                Matrix4x4 m=Matrix4x4.TRS(c.position,Quaternion.Euler(t*65,c.yaw+c.spin*t,t*20),Vector3.one*(.85f*Mathf.Min(1,(1-t)*2)));
                if(c.blue)whiteBlueMatrices[wb++]=m;
                else if(c.kind==2)whiteGiantMatrices[wg++]=m;
                else if(c.kind==3)whiteBossMatrices[wBoss++]=m;
                else whiteRedMatrices[wr++]=m;
            }
            for(int i=0;i<2;i++)
            {
                float yaw=Mathf.Atan2(FrontPosition().x-towerPositions[i].x,FrontPosition().z-towerPositions[i].z)*Mathf.Rad2Deg;
                allyArcherMatrices[i]=Matrix4x4.TRS(towerPositions[i]+new Vector3(-.34f,1.80f,.03f),Quaternion.Euler(towerRecoil[i]*50,yaw,0),Vector3.one*(towerLevel[i]<0?0:.8f));
            }
            Draw(shadowMesh,shadowMaterial,shadowMatrices,ns);
            Draw(soldierMesh,redMaterial,redMatrices,nr,redDrawHits);Draw(blueSoldierMesh,blueMaterial,blueMatrices,nb,blueDrawHits);
            Draw(archerMesh,redMaterial,archerMatrices,na,archerDrawHits);Draw(giantMesh,redMaterial,giantMatrices,ng,giantDrawHits);Draw(bossMesh,redMaterial,bossMatrices,bosses,bossDrawHits);
            Draw(allyArcherMesh,blueMaterial,allyArcherMatrices,2);
            Draw(soldierMesh,silhouetteMaterial,whiteRedMatrices,wr);Draw(blueSoldierMesh,silhouetteMaterial,whiteBlueMatrices,wb);
            Draw(giantMesh,silhouetteMaterial,whiteGiantMatrices,wg);Draw(bossMesh,silhouetteMaterial,whiteBossMatrices,wBoss);
            Draw(hpMesh,hpBackgroundMaterial,hpBackgroundMatrices,hp);Draw(hpMesh,hpMaterial,hpMatrices,hp);
            Draw(trailMesh,swordMaterial,swordMatrices,hp);
            cachedRedDraws=nr;cachedBlueDraws=nb;cachedShadowDraws=ns;
        }

        void Draw(Mesh mesh, Material material, Matrix4x4[] matrices, int count,float[] hitTimes=null)
        {
            if (count == 0 || !mesh) return;
            if(hitTimes!=null)soldierProperties.SetFloatArray(HitTimeId,hitTimes);
            Graphics.DrawMeshInstanced(mesh,0,material,matrices,count,hitTimes==null?null:soldierProperties,
                ShadowCastingMode.Off,false,0,renderCamera,LightProbeUsage.Off);
        }

        void BuildPath()
        {
            pathLength = 0;
            for (int i = 0; i < PathSamples; i++)
            {
                float u = (float)i / (PathSamples-1)*(controls.Length-1);
                int k = Mathf.Min(controls.Length-2,Mathf.FloorToInt(u));
                float t = u-k;
                Vector3 a = controls[Mathf.Max(0,k-1)], b=controls[k], c=controls[k+1], d=controls[Mathf.Min(controls.Length-1,k+2)];
                path[i] = .5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                if (i > 0) pathLength += Vector3.Distance(path[i],path[i-1]);
                distances[i] = pathLength;
            }
        }

        Vector3 FrontPosition() { return PathPosition(front,0,out _); }

        Vector3 PathPosition(float distance, float lateral, out Vector3 forward)
        {
            if (distance < 3)
            {
                float blend = Mathf.Clamp01(distance/3);
                float branch = lateral < 0 ? -1.0f : .9f;
                Vector3 p = new Vector3(Mathf.Lerp(4.3f+branch,4.3f,blend),GroundY,9-distance);
                forward = new Vector3(-branch/3,0,-1).normalized;
                return p + new Vector3(-forward.z,0,forward.x) * lateral * .7f;
            }
            int lo = 0, hi=PathSamples-1;
            while (hi-lo>1)
            {
                int mid=(lo+hi)>>1;
                if (distances[mid]<distance) lo=mid; else hi=mid;
            }
            float amount = Mathf.Clamp01((distance-distances[lo])/Mathf.Max(.0001f,distances[hi]-distances[lo]));
            forward=(path[hi]-path[lo]).normalized;
            return Vector3.LerpUnclamped(path[lo],path[hi],amount)+new Vector3(-forward.z,0,forward.x)*lateral;
        }

        void BuildTowers()
        {
            GameObject bowAsset=Resources.Load<GameObject>("HyeopgokSasu/Models/crossbow");
            for(int i=0;i<2;i++)
            {
                towers[i]=new GameObject("성장하는 석궁탑 "+(i+1)).transform;
                towers[i].SetParent(transform,false);towers[i].position=towerPositions[i];
                towers[i].rotation=Quaternion.Euler(0,180,0);
                for(int level=0;level<3;level++)
                {
                    GameObject asset=Resources.Load<GameObject>("HyeopgokSasu/Models/tower_base_l"+(level+1));
                    if(!asset)asset=Resources.Load<GameObject>("HyeopgokSasu/Models/tower_base");
                    if(!asset)continue;
                    GameObject stage=Instantiate(asset,towers[i]);stage.transform.localPosition=Vector3.zero;
                    stage.transform.localRotation=Quaternion.identity;stage.transform.localScale=Vector3.one;
                    towerStages[i,level]=stage;
                    Renderer renderer=stage.GetComponentInChildren<Renderer>();towerRenderers[i,level]=renderer;
                    if(renderer){renderer.sharedMaterial=blueMaterial;renderer.shadowCastingMode=ShadowCastingMode.On;}
                    stage.SetActive(level==0);
                }
                if(bowAsset)
                {
                    GameObject bow=Instantiate(bowAsset,towers[i]);towerBows[i]=bow.transform;
                    bow.transform.localPosition=Vector3.up*1.62f;bow.transform.localRotation=Quaternion.identity;
                    Renderer r=bow.GetComponentInChildren<Renderer>();if(r){r.sharedMaterial=blueMaterial;r.shadowCastingMode=ShadowCastingMode.Off;}
                }
            }
        }

        void UpdateTowers(float dt)
        {
            Vector3 frontPosition=FrontPosition();
            for(int i=0;i<2;i++)
            {
                if(!towers[i])continue;
                towerRise[i]=Mathf.Min(1,towerRise[i]+dt*1.1f);
                towerRecoil[i]=Mathf.Max(0,towerRecoil[i]-dt);
                float t=towerRise[i],eased=1-Mathf.Pow(1-t,3);
                towers[i].position=towerPositions[i]+Vector3.up*((eased-1)*1.8f);
                towers[i].localScale=new Vector3(1,towerLevel[i]<0?.16f:1+Mathf.Sin(t*Mathf.PI)*.12f-towerRecoil[i]*.16f,1);
                for(int level=0;level<3;level++)
                {
                    GameObject stage=towerStages[i,level];if(!stage)continue;
                    bool active=level==Mathf.Max(0,towerLevel[i]);if(stage.activeSelf!=active)stage.SetActive(active);
                    if(active&&towerRenderers[i,level])
                    {
                        towerProperties.SetFloat("_Flash",towerLevel[i]<0?.7f:Mathf.Clamp01((1-t)*2.4f));
                        towerRenderers[i,level].SetPropertyBlock(towerProperties);
                    }
                }
                if(towerBows[i])
                {
                    towerBows[i].gameObject.SetActive(towerLevel[i]>=0);
                    Vector3 direction=frontPosition-towerBows[i].position;direction.y=0;
                    if(direction.sqrMagnitude>.001f)towerBows[i].rotation=Quaternion.LookRotation(direction)*Quaternion.Euler(-towerRecoil[i]*35,0,0);
                }
            }
        }

        static Mesh MakeBarMesh()
        {
            Vector3[] vertices={new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            Color[] colors={new Color(1,1,1,0),new Color(1,1,1,0),new Color(1,1,1,0),new Color(1,1,1,0)};
            Mesh mesh=new Mesh {name="전장 체력 막대",vertices=vertices,colors=colors,triangles=new[]{0,2,1,0,3,2}};
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        static Material MakeMaterial(Shader shader, Color color, bool unlit)
        {
            Material material=new Material(shader);
            material.SetColor("_Color",color);
            material.SetFloat("_Unlit",unlit?1:0);
            material.enableInstancing=true;
            return material;
        }

        static Mesh LoadSoldier(string model)
        {
            GameObject asset=Resources.Load<GameObject>("HyeopgokSasu/Models/"+model);
            if (!asset && model != "soldier") asset=Resources.Load<GameObject>("HyeopgokSasu/Models/soldier");
            if (asset)
            {
                MeshFilter[] filters=asset.GetComponentsInChildren<MeshFilter>();
                if (filters.Length>1)
                {
                    CombineInstance[] combines=new CombineInstance[filters.Length];
                    for (int i=0;i<filters.Length;i++)
                        combines[i]=new CombineInstance {mesh=filters[i].sharedMesh,transform=filters[i].transform.localToWorldMatrix};
                    Mesh combined=new Mesh {name="협곡 병사 결합 메시"};
                    combined.CombineMeshes(combines,true,true);
                    return combined;
                }
                if (filters.Length==1)
                {
                    // FBX meshes can carry -90-degree child rotation / .01 scale.
                    // Instanced draws bypass the imported hierarchy, so bake it once.
                    Mesh combined = new Mesh {name="협곡 병사 인스턴스 메시"};
                    CombineInstance[] combines = {
                        new CombineInstance {mesh=filters[0].sharedMesh,transform=filters[0].transform.localToWorldMatrix}
                    };
                    combined.CombineMeshes(combines,true,true);
                    combined.RecalculateBounds();
                    return combined;
                }
            }
            // Build remains playable if a local model import fails; QA screenshots will
            // expose the faceted placeholder so an import failure cannot hide silently.
            Mesh fallback=MakeChipMesh();
            Vector3[] vertices=fallback.vertices;
            for (int i=0;i<vertices.Length;i++) vertices[i]=new Vector3(vertices[i].x*.32f,vertices[i].y*.56f+.28f,vertices[i].z*.24f);
            fallback.vertices=vertices;
            fallback.RecalculateBounds();
            return fallback;
        }

        static Mesh MakeChipMesh()
        {
            // Octahedral chips double as faceted smoke puffs; 8 triangles each.
            Vector3[] p={Vector3.up*.5f,Vector3.down*.5f,Vector3.left*.5f,Vector3.right*.5f,Vector3.forward*.5f,Vector3.back*.5f};
            int[] faces={0,4,3,0,3,5,0,5,2,0,2,4,1,3,4,1,5,3,1,2,5,1,4,2};
            Vector3[] vertices=new Vector3[faces.Length];
            Color[] colors=new Color[faces.Length];
            int[] triangles=new int[faces.Length];
            for(int i=0;i<faces.Length;i++){vertices[i]=p[faces[i]]; colors[i]=new Color(1,1,1,0); triangles[i]=i;}
            Mesh mesh=new Mesh {name="협곡 파편",vertices=vertices,colors=colors,triangles=triangles};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static Mesh MakeShadowMesh()
        {
            Vector3[] vertices=new Vector3[9];
            Color[] colors=new Color[9];
            int[] triangles=new int[24];
            colors[0]=Color.white;
            for(int i=0;i<8;i++)
            {
                float t=i*Mathf.PI*.25f;
                vertices[i+1]=new Vector3(Mathf.Sin(t),0,Mathf.Cos(t));
                colors[i+1]=new Color(1,1,1,0);
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%8+1;
            }
            Mesh mesh=new Mesh {name="병사 접지 그림자",vertices=vertices,colors=colors,triangles=triangles};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static Mesh MakeBoltMesh()
        {
            Vector3[] vertices={new Vector3(0,0,.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(0,.5f,-.5f)};
            Color[] colors={new Color(1,1,1,0),new Color(1,1,1,0),new Color(1,1,1,0),new Color(1,1,1,0)};
            Mesh mesh=new Mesh {name="석궁 볼트",vertices=vertices,colors=colors,triangles=new[]{0,2,1,0,3,2,0,1,3,1,2,3}};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static Mesh MakeTrailMesh()
        {
            // A tapered cross remains a readable white streak from every view.
            Vector3[] vertices={new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(0,0,-.5f),
                new Vector3(0,-.5f,.5f),new Vector3(0,.5f,.5f),new Vector3(0,0,-.5f)};
            Color[] colors=new Color[6];
            for(int i=0;i<6;i++) colors[i]=new Color(1,1,1,0);
            Mesh mesh=new Mesh {name="석궁 흰 궤적",vertices=vertices,colors=colors,
                triangles=new[]{0,1,2,2,1,0,3,4,5,5,4,3}};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        float Range(float min,float max)
        {
            randomState^=randomState<<13;randomState^=randomState>>17;randomState^=randomState<<5;
            return min+(max-min)*(randomState&0x00ffffffu)/16777216f;
        }

        void OnDestroy()
        {
            if(redMaterial) Destroy(redMaterial); if(blueMaterial) Destroy(blueMaterial);
            if(whiteMaterial) Destroy(whiteMaterial); if(goldMaterial) Destroy(goldMaterial);
            if(dustMaterial) Destroy(dustMaterial); if(shadowMaterial) Destroy(shadowMaterial);
            if(cubeMesh) Destroy(cubeMesh); if(shadowMesh) Destroy(shadowMesh); if(boltMesh) Destroy(boltMesh);
            if(trailMesh) Destroy(trailMesh); if(soldierMesh) Destroy(soldierMesh); if(blueSoldierMesh) Destroy(blueSoldierMesh);
            if(impacts != null) impacts.Dispose();
            if(economy!=null)economy.Dispose();
            if(giantMesh)Destroy(giantMesh);if(archerMesh)Destroy(archerMesh);if(allyArcherMesh)Destroy(allyArcherMesh);if(bossMesh)Destroy(bossMesh);if(hpMesh)Destroy(hpMesh);
            if(boltTrailMaterial)Destroy(boltTrailMaterial);if(silhouetteMaterial)Destroy(silhouetteMaterial);if(swordMaterial)Destroy(swordMaterial);if(hpMaterial)Destroy(hpMaterial);if(hpBackgroundMaterial)Destroy(hpBackgroundMaterial);
        }
    }
}
