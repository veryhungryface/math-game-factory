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
        const int TroopKinds = 3, TroopCapacity = RedCap + BlueCap;
        // Wider queues and deterministic per-instance offsets break the solid army
        // carpet without changing path authority, kill cadence or collision rules.
        const float RedSpacing = .445f, BlueSpacing = .440f, GroundY = .27f;
        readonly float[] redS = new float[RedCap];
        readonly float[] blueS = new float[BlueCap];
        readonly float[] redHitTimes=new float[RedCap],blueHitTimes=new float[BlueCap];
        readonly float[] giantDrawHits=new float[RedCap],bossDrawHits=new float[4];
        readonly MaterialPropertyBlock soldierProperties=new MaterialPropertyBlock();
        readonly MaterialPropertyBlock hitProperties=new MaterialPropertyBlock();
        static readonly int HitTimeId=Shader.PropertyToID("_HitTime");
        static readonly int TeamId=Shader.PropertyToID("_Team");
        static readonly int PhaseId=Shader.PropertyToID("_InstancePhase");
        static readonly int AttackId=Shader.PropertyToID("_Attack");
        readonly byte[] redKinds=new byte[RedCap],blueKinds=new byte[BlueCap];
        readonly Mesh[] troopMeshes=new Mesh[TroopKinds];
        readonly Matrix4x4[][] troopMatrices={new Matrix4x4[TroopCapacity],new Matrix4x4[TroopCapacity],new Matrix4x4[TroopCapacity]};
        readonly float[][] troopHitTimes={new float[TroopCapacity],new float[TroopCapacity],new float[TroopCapacity]};
        readonly float[][] troopTeams={new float[TroopCapacity],new float[TroopCapacity],new float[TroopCapacity]};
        readonly float[][] troopPhases={new float[TroopCapacity],new float[TroopCapacity],new float[TroopCapacity]};
        readonly float[][] troopAttacks={new float[TroopCapacity],new float[TroopCapacity],new float[TroopCapacity]};
        readonly int[] troopCounts=new int[TroopKinds];
        readonly Matrix4x4[][] allyTroopMatrices={new Matrix4x4[BlueCap+1],new Matrix4x4[BlueCap+1],new Matrix4x4[BlueCap+1]};
        readonly float[][] allyTroopHitTimes={new float[BlueCap+1],new float[BlueCap+1],new float[BlueCap+1]};
        readonly float[][] allyTroopTeams={new float[BlueCap+1],new float[BlueCap+1],new float[BlueCap+1]};
        readonly float[][] allyTroopPhases={new float[BlueCap+1],new float[BlueCap+1],new float[BlueCap+1]};
        readonly float[][] allyTroopAttacks={new float[BlueCap+1],new float[BlueCap+1],new float[BlueCap+1]};
        readonly int[] allyTroopCounts=new int[TroopKinds];
        readonly Matrix4x4[][] whiteTroopMatrices={new Matrix4x4[CorpseCap],new Matrix4x4[CorpseCap],new Matrix4x4[CorpseCap]};
        readonly int[] whiteTroopCounts=new int[TroopKinds];
        readonly int[] hitCandidates=new int[12];
        readonly float[] hitCandidateDistances=new float[12];
        readonly Matrix4x4[] shadowMatrices = new Matrix4x4[RedCap + BlueCap + 1];
        readonly Matrix4x4[] whiteMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] goldMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] dustMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] boltMatrices = new Matrix4x4[BoltCap];
        readonly Matrix4x4[] trailMatrices = new Matrix4x4[BoltCap];
        readonly Matrix4x4[] giantMatrices=new Matrix4x4[RedCap];
        readonly Matrix4x4[] bossMatrices=new Matrix4x4[4];
        readonly Matrix4x4[] whiteGiantMatrices=new Matrix4x4[CorpseCap];
        readonly Matrix4x4[] whiteBossMatrices=new Matrix4x4[CorpseCap];
        readonly Matrix4x4[] allyArcherMatrices=new Matrix4x4[TowerCount];
        readonly Matrix4x4[] hpBackgroundMatrices=new Matrix4x4[24];
        readonly Matrix4x4[] hpMatrices=new Matrix4x4[24];
        readonly Matrix4x4[] swordMatrices=new Matrix4x4[24];
        readonly Vector3[] path = new Vector3[PathSamples];
        readonly float[] distances = new float[PathSamples];
        readonly Chip[] chips = new Chip[ParticleCap];
        readonly Corpse[] corpses = new Corpse[CorpseCap];
        readonly Bolt[] bolts = new Bolt[BoltCap];
        public const int TowerCount=3;
        public static readonly Vector3[] UpgradePads={
            new Vector3(.92f,1.23f,-4.35f),new Vector3(.98f,1.23f,3.42f),new Vector3(-2.52f,1.23f,-4.22f)
        };
        readonly Transform[] towers = new Transform[TowerCount];
        readonly float[] towerRise = new float[TowerCount];
        readonly float[] towerDrop = new float[TowerCount];
        readonly float[] towerRecoil = new float[TowerCount];
        readonly float[] upgradeClock = new float[TowerCount];
        readonly GameObject[,,] towerStages=new GameObject[TowerCount,3,3];
        readonly Renderer[,,] towerRenderers=new Renderer[TowerCount,3,3];
        readonly Material[,] towerMaterials=new Material[3,3];
        readonly Transform[] towerBows=new Transform[TowerCount];
        readonly MaterialPropertyBlock towerProperties=new MaterialPropertyBlock();
        readonly int[] towerLevel=new int[TowerCount];
        readonly int[] towerType=new int[TowerCount];
        readonly int[] upgradeRemaining=new int[TowerCount];
        readonly Vector3[] towerPositions = {
            new Vector3(1.78f, 1.2f, -3.72f), new Vector3(1.82f, 1.2f, 4.62f),new Vector3(-3.14f,1.2f,-3.72f)
        };
        readonly Vector3[] controls = {
            // Exact terrain control points from ArtSource/blender/build_models.py.
            new Vector3(4.3f,GroundY,9), new Vector3(4.3f,GroundY,5),
            new Vector3(4.15f,GroundY,1), new Vector3(3.7f,GroundY,-3.5f),
            new Vector3(2.7f,GroundY,-5.9f), new Vector3(.2f,GroundY,-7.1f),
            new Vector3(-2.6f,GroundY,-6.6f), new Vector3(-4,GroundY,-4.8f),
            new Vector3(-4.1f,GroundY,-1)
        };
        static readonly float[] ContactSockets={-.90f,-.30f,.30f,.90f};

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
        Mesh cubeMesh, shadowMesh, boltMesh, trailMesh;
        Mesh giantMesh,giantGlowMesh,enemyAiTroopMesh,allyAiTroopMesh,allyArcherMesh,bossMesh,hpMesh;
        Material redMaterial, blueMaterial, troopMaterial,allyTroopMaterial,giantMaterial,giantGlowMaterial, whiteMaterial, goldMaterial, dustMaterial, shadowMaterial;
        Material chestWoodMaterial,chestGoldMaterial;
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
        int recycledKind,upgradeCount,upgradePad=-1;
        float chestLife;
        Transform chestRoot,chestLid;
        HyeopgokRules rules;
        HyeopgokRewardRng rewardRng=new HyeopgokRewardRng(1);
        HyeopgokChestReward lastChest;
        Vector3 artSoldierShadowWorld;
        bool artSoldierShadowValid;
        float questionGrace=PressureGrace;
        public float QuestionGrace=>questionGrace;
        bool initialized, playing, questionActive;
        uint randomState = 0x5e3a07c1u;

        public int Kills { get { return killCount; } }
        public int BuiltTowers {get {int n=0;for(int i=0;i<TowerCount;i++)if(towerLevel[i]>=0)n++;return n;}}
        public int UpgradeLevel=>upgradeCount;
        public bool UpgradePouring=>upgradePad>=0;
        public bool ChestOpen=>chestLife>0;
        public HyeopgokChestReward LastChest=>lastChest;
        public int Reds { get { return displayedReds; } }
        public int Blues { get { return displayedBlues; } }
        public float Shake { get { return shake; } }
        // Camera framing may use a small wide-screen lead beyond the actual melee;
        // simulation and the diagnostic gate retain the exact contact point.
        public Vector3 FrontWorld {
            get {
                Vector3 p=FrontPosition();
                if((float)Screen.width/Mathf.Max(1,Screen.height)>1.2f)p.z-=2.0f;
                return p;
            }
        }
        public Vector3 ArtFrontWorld => FrontPosition();
        public Vector3 ArtShadowWorld=>artSoldierShadowValid?artSoldierShadowWorld:FrontPosition();
        public Vector3 ArtFrontPoint(int socket)=>PathPosition(front,ContactSockets[Mathf.Clamp(socket,0,ContactSockets.Length-1)],out _);
        public bool TryGetArtSoldierShadow(out Vector3 point){point=artSoldierShadowWorld;return artSoldierShadowValid;}
        public Vector3 CollectionPoint {
            get {
                Vector3 p=FrontPosition();
                return new Vector3(Mathf.Clamp(p.x,HyeopgokRules.MinX,HyeopgokRules.MaxX),1.24f,Mathf.Clamp(p.z,HyeopgokRules.MinZ,HyeopgokRules.MaxZ));
            }
        }
        public int Coins { get { return economy==null?0:economy.Coins; } }
        public int DepositRemaining { get { return economy==null?0:economy.Remaining; } }
        public bool Depositing { get { return economy!=null&&economy.Depositing; } }
        public void SetKing(Transform king){if(economy!=null)economy.SetKing(king);}
        public void SetEconomyRules(HyeopgokRules value){rules=value;economy.SetRules(value);}
        public void PourCoin(int pad){economy.Pour(HyeopgokRules.Pads[pad]);}
        public int TowerLevelAt(int at)=>at>=0&&at<TowerCount?towerLevel[at]:-1;
        public int TowerTypeAt(int at)=>at>=0&&at<TowerCount?towerType[at]:-1;
        public int UpgradeRemainingAt(int at)=>at>=0&&at<TowerCount?upgradeRemaining[at]:0;
        public void CopyTowerState(ref int[] levels,ref int[] types,ref int[] remaining){
            if(levels==null||levels.Length!=TowerCount){levels=new int[TowerCount];types=new int[TowerCount];remaining=new int[TowerCount];}
            for(int i=0;i<TowerCount;i++){levels[i]=towerLevel[i];types[i]=towerType[i];remaining[i]=upgradeRemaining[i];}
        }
        public static int UpgradePadAt(Vector3 p){for(int i=0;i<TowerCount;i++){Vector3 d=p-UpgradePads[i];d.y=0;if(d.sqrMagnitude<.72f*.72f)return i;}return -1;}
        // Synchronous QA command uses actual kills, grounded coins and their pickup
        // trajectory. It never assigns a wallet or calls answer adjudication.
        public void SimulateEarnedCoins(int needed){
            int target=Mathf.Min(HyeopgokRules.CarryCapacity,Coins+Mathf.Max(0,needed));
            for(int tick=0;tick<400&&Coins<target;tick++){
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
            troopMaterial = HyeopgokAiAssets.CreateMaterial("enemy_soldier",Color.white,0);
            allyTroopMaterial = HyeopgokAiAssets.CreateMaterial("ally_soldier",Color.white,1);
            Color teamRed=MgfLook.Hex("#D0060C").linear;
            Color teamRedDark=MgfLook.Hex("#8C1A20").linear;
            Color teamBlue=MgfLook.Hex("#0C73D5").linear;
            Color teamBlueDark=MgfLook.Hex("#0658C7").linear;
            troopMaterial.SetColor("_TeamRed",teamRed);
            troopMaterial.SetColor("_TeamRedDark",teamRedDark);
            troopMaterial.SetColor("_TeamBlue",teamBlue);
            troopMaterial.SetColor("_TeamBlueDark",teamBlueDark);
            allyTroopMaterial.SetColor("_TeamRed",teamRed);
            allyTroopMaterial.SetColor("_TeamRedDark",teamRedDark);
            allyTroopMaterial.SetColor("_TeamBlue",teamBlue);
            allyTroopMaterial.SetColor("_TeamBlueDark",teamBlueDark);
            whiteMaterial = MakeMaterial(shader, new Color(1,.995f,.95f), true);
            goldMaterial = MakeMaterial(shader, new Color(1,.59f,.028f), true);
            goldMaterial.SetFloat("_Metallic",.82f);
            dustMaterial = MakeMaterial(shader, new Color(.89f,.81f,.66f), true);
            shadowMaterial = new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/Contact"));Color shadowTint=MgfLook.Hex("#244A48").linear;shadowTint.a=.76f;shadowMaterial.SetColor("_Color",shadowTint);shadowMaterial.enableInstancing=true;
            silhouetteMaterial=MakeMaterial(shader,Color.white,true);silhouetteMaterial.SetFloat("_Flash",1);
            swordMaterial=MakeMaterial(shader,new Color(1,.08f,.18f),true);
            hpMaterial=MakeMaterial(shader,new Color(.91f,.08f,.09f),true);
            hpBackgroundMaterial=MakeMaterial(shader,new Color(.14f,.16f,.19f),true);
            boltTrailMaterial=MakeMaterial(shader,new Color(.33f,.77f,1),true);
            // These FBX assets share neutral armour. Team colour is a per-instance
            // shader property, so both armies remain exactly three troop batches.
            enemyAiTroopMesh=HyeopgokAiAssets.LoadInstancedMesh("enemy_soldier");
            allyAiTroopMesh=HyeopgokAiAssets.LoadInstancedMesh("ally_soldier");
            if(enemyAiTroopMesh)for(int kind=0;kind<TroopKinds;kind++)troopMeshes[kind]=enemyAiTroopMesh;
            else {
                troopMeshes[0]=LoadSoldier("troop_sword","soldier");
                troopMeshes[1]=LoadSoldier("troop_spear","soldier");
                troopMeshes[2]=LoadSoldier("troop_shield","soldier");
            }
            giantMesh=HyeopgokAiAssets.LoadInstancedMesh("giant");if(!giantMesh)giantMesh=LoadSoldier("giant");
            giantMaterial=HyeopgokAiAssets.CreateMaterial("giant",Color.white,0);
            giantGlowMesh=HyeopgokAiAssets.LoadInstancedMesh("giant_blade_glow");
            giantGlowMaterial=HyeopgokAiAssets.CreateAdditiveMaterial("giant_blade_glow",new Color(1,.22f,.045f,.78f));
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
            BuildChest();
            initialized = true;
            ResetBattle(false);
        }

        public void Begin(int rewardSeed)
        {
            if (!initialized) return;
            rewardRng.Reset(rewardSeed);
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
            if(finalBoss)redKinds[120]=4;
        }

        public void BeginQuestion(float seconds=24)
        {
            sinceAnswer = pressureClock = 0;
            // Preserve an 18-second reading/solving window, then make the live
            // defence matter during the middle of long pour questions instead of
            // hiding all gate pressure inside their final 14 seconds.
            questionGrace=Mathf.Clamp(seconds*.5f,PressureGrace,Mathf.Max(PressureGrace,seconds-12));
            questionActive = playing;
        }

        public HyeopgokChestReward Reward(int padIndex, Vector3 pos, int hits, int trials,int streak,float speed01)
        {
            if (!initialized) return new HyeopgokChestReward{kind=HyeopgokRewardKind.None,towerSlot=-1};
            questionActive = false;
            enrage = 0;
            boost = 4.3f;
            hitStop = .05f;
            flash = 1;
            int tower=FirstInstalledTower(Mathf.Abs(padIndex));
            if(tower>=0)towerRecoil[tower] = .25f;

            bool hasEmpty=false;for(int i=0;i<TowerCount;i++)if(towerLevel[i]<0)hasEmpty=true;
            bool forceTower=Application.absoluteURL.Contains("reward=tower"),forceCoin=Application.absoluteURL.Contains("reward=coin");
            lastChest=rewardRng.Roll(true,streak,hasEmpty,speed01,forceCoin,forceTower);
            if(lastChest.kind==HyeopgokRewardKind.Tower){
                int slot=-1;for(int i=0;i<TowerCount;i++)if(towerLevel[i]<0){slot=i;break;}
                lastChest.towerSlot=slot;if(slot>=0)InstallTower(slot,(int)lastChest.towerType);
            }else if(rules!=null){
                lastChest.coinBonus=rules.AddRewardCoins(lastChest.coinBonus);
                economy.CelebrateReward(pos,rules.King,lastChest.coinBonus);
            }
            ShowChest(pos,lastChest);

            volleyTrials = Mathf.Clamp(trials, 1, 225);
            volleyHits = Mathf.Clamp(hits, 0, volleyTrials);
            volleyFired = 0;
            volleys = volleyTrials;
            salvoSpacing = Mathf.Clamp(2.8f / volleyTrials, .018f, .12f);
            salvoClock = 0;
            int added = 0;
            for (int i = blueCount; i < blueCount + added; i++)
            {
                blueS[i] = pathLength + (i - blueCount) * .10f;
                blueKinds[i]=(byte)(i%TroopKinds);
                blueHitTimes[i]=-1000;
            }
            blueCount += added;
            // The front retreats visibly; the next frames advance the relieved blue army.
            front = Mathf.Max(15.3f, front - 1.25f);
            Burst(pos + Vector3.up * .2f, 36, 3.5f);
            Burst(FrontPosition(), 70, 4.0f);
            if (playing) MgfSfx.Play("whoosh", .20f);
            return lastChest;
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
            economy.Clear();recycledKind=0;upgradeCount=0;upgradePad=-1;chestLife=0;lastChest=new HyeopgokChestReward{kind=HyeopgokRewardKind.None,towerSlot=-1};
            if(chestRoot)chestRoot.gameObject.SetActive(false);
            front = 19.0f;
            laneCursor = 0;
            for (int i = 0; i < RedCap; i++) {redS[i] = front - .25f - (i / RedLanes) * RedSpacing - (i % RedLanes) * .018f;redKinds[i]=KindFor(i);redHitTimes[i]=-1000;}
            for (int i = 0; i < BlueCap; i++) {blueS[i] = front + .38f + (i / BlueLanes) * BlueSpacing + (i % BlueLanes) * .03f;blueKinds[i]=(byte)(i%TroopKinds);blueHitTimes[i]=-1000;}
            for (int i = 0; i < ParticleCap; i++) chips[i].duration = 0;
            for (int i = 0; i < CorpseCap; i++) corpses[i].duration = 0;
            for (int i = 0; i < BoltCap; i++) bolts[i].duration = 0;
            // Both pre-existing towers visibly operate in the title battle.
            for(int i=0;i<TowerCount;i++){towerRise[i]=1;towerDrop[i]=towerRecoil[i]=upgradeClock[i]=0;towerType[i]=i%3;towerLevel[i]=live?(i==1?0:-1):1;upgradeRemaining[i]=towerLevel[i]>=0&&towerLevel[i]<2?UpgradeCost(towerLevel[i]):0;ApplyTowerVisibility(i);}
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
            troopMaterial.SetFloat("_Flash",flash*.14f);
            hitStop -= realDt;
            float dt = hitStop > 0 ? 0 : realDt;
            clock += dt;
            if (playing && questionActive)
            {
                sinceAnswer += dt;
                // The HUD exposes the same grace, cadence and damage. A quick solve
                // stays safe; a long solve now visibly spends gate HP before timeout.
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
                Vector3 contact=Vector3.zero;
                // Four fixed sockets span the whole collision line. Their small
                // deterministic longitudinal offsets keep the stars from reading
                // as a single repeated sprite while remaining allocation-free.
                for(int socket=0;socket<ContactSockets.Length;socket++)
                {
                    float longitudinal=(socket%2==0?-.18f:.14f)+Mathf.Sin((laneCursor+socket)*1.37f)*.08f;
                    contact=PathPosition(front+longitudinal,ContactSockets[socket],out _);
                    Burst(contact+Vector3.up*.30f,2,1.35f);
                    impacts.Contact(contact+Vector3.up*.38f,socket==1&&laneCursor%5==0);
                    blueHitTimes[(lane+socket)%BlueLanes]=Time.time;
                }
                MarkHit(PathPosition(front,0,out _));
                if(laneCursor%3==0)impacts.DamageNumber(contact+Vector3.up*.16f,false);
                if (laneCursor % 2 == 0) KillRed(lane, .8f);
                if (laneCursor % 5 == 0) KillBlue(lane % BlueLanes);
                if (playing && laneCursor % 8 == 0) MgfSfx.Play("pop", .07f);
            }
            fireClock -= dt;
            if (fireClock <= 0)
            {
                fireClock = boost > 0 ? .18f : .48f;
                FireBolt(FirstInstalledTower(laneCursor), false, true);
            }
            if (volleys > 0)
            {
                salvoClock -= dt;
                if (salvoClock <= 0)
                {
                    salvoClock = salvoSpacing;
                    int before = volleyFired * volleyHits / volleyTrials;
                    int after = (volleyFired + 1) * volleyHits / volleyTrials;
                    FireBolt(FirstInstalledTower(volleys), true, after > before);
                    volleyFired++;
                    volleys--;
                }
            }
            UpdateTowers(realDt);
            UpdateChest(realDt);
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
            AddCorpse(pos, direction * Range(.6f,1.2f), true, Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg,blueKinds[lane]);
            for (int i = lane; i < blueCount; i += BlueLanes){
                blueS[i] = i + BlueLanes < blueCount ? blueS[i + BlueLanes] : pathLength + Range(0,.5f);
                blueKinds[i]=i+BlueLanes<blueCount?blueKinds[i+BlueLanes]:(byte)(i%TroopKinds);
                blueHitTimes[i]=i+BlueLanes<blueCount?blueHitTimes[i+BlueLanes]:-1000;
            }
        }

        void MarkHit(Vector3 position)
        {
            // Only an impact writes this visual state; no HP, lane order or RNG
            // changes. Each soldier carries its own GPU flash timestamp.
            float now=Time.time;
            for(int slot=0;slot<hitCandidates.Length;slot++){hitCandidates[slot]=-1;hitCandidateDistances[slot]=float.MaxValue;}
            for(int i=0;i<redCount;i++){
                if(redS[i]<front-3.2f||redS[i]<0)continue;
                Vector3 p=PathPosition(redS[i],(i%RedLanes-3.5f)*.245f,out _);
                float dx=p.x-position.x,dz=p.z-position.z;
                float distance=dx*dx+dz*dz;
                if(distance>=hitCandidateDistances[hitCandidateDistances.Length-1])continue;
                int insert=hitCandidateDistances.Length-1;
                while(insert>0&&distance<hitCandidateDistances[insert-1]){
                    hitCandidateDistances[insert]=hitCandidateDistances[insert-1];
                    hitCandidates[insert]=hitCandidates[insert-1];insert--;
                }
                hitCandidateDistances[insert]=distance;hitCandidates[insert]=i;
            }
            for(int k=0;k<hitCandidates.Length;k++){
                int i=hitCandidates[k];if(i<0)continue;
                if(redKinds[i]>=3&&now-redHitTimes[i]>.35f){Vector3 p=PathPosition(redS[i],(i%RedLanes-3.5f)*.245f,out _);impacts.DamageNumber(p+Vector3.up*(redKinds[i]==4?1.9f:1.25f),true);}
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
            if(tower<0||tower>=TowerCount||towerLevel[tower]<0)return;
            int slot = boltCursor++ % BoltCap;
            Vector3 start = towerPositions[tower] + Vector3.up * 2.20f;
            Vector3 target = PathPosition(front - Range(.1f,2.0f), Range(-.9f,.9f), out _);
            if (!hit) target += new Vector3(tower%2 == 0 ? -2.15f : 2.15f, 0, 0);
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
            // Three readable regular silhouettes dominate the line. Giants stay
            // sparse; the final boss is assigned explicitly in SetStage.
            return i%112==55?(byte)3:(byte)(i%TroopKinds);
        }

        void RenderSoldiers()
        {
            for(int kind=0;kind<TroopKinds;kind++){troopCounts[kind]=0;allyTroopCounts[kind]=0;whiteTroopCounts[kind]=0;}
            artSoldierShadowValid=false;
            int ns=0,ng=0,bosses=0,wg=0,wBoss=0,hp=0,redVisible=0,blueVisible=0;
            Quaternion facing=renderCamera.transform.rotation;
            for(int i=0;i<redCount;i++)
            {
                if(redS[i]<0)continue;
                redVisible++;
                int formationRow=i/RedLanes,formationLane=i%RedLanes;
                byte kind=redKinds[i];
                // Keep the contact ranks solid and readable, then open deterministic
                // breathing lanes farther back. Simulation still owns every soldier;
                // this only stops the distant queue reading as one red carpet.
                if(!ShowFormationMember(formationRow,formationLane,front-redS[i],kind))continue;
                float row=i/RedLanes;
                float lane=(i%RedLanes-3.5f)*.300f*(1+Mathf.Sin(row*.77f)*.08f)+HashSigned(i,11)*.18f;
                Vector3 direction;
                Vector3 p=PathPosition(redS[i],lane,out direction);
                p+=direction*(HashSigned(i,29)*.18f);p.y+=.024f;
                float sinceHit=Time.time-redHitTimes[i];
                if(sinceHit>=0&&sinceHit<.06f)p-=direction*(1-sinceHit/.06f)*Mathf.Lerp(.16f,.28f,Hash01(i,71));
                float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg+HashSigned(i,47)*6f;
                float size=.76f*Mathf.Lerp(.94f,1.06f,Hash01(i,83));
                float phase=Hash01(i,101)*Mathf.PI*2;
                float attacking=redS[i]>front-.95f?1:0;
                Quaternion rotation=Quaternion.Euler(0,yaw,0);
                Matrix4x4 matrix=Matrix4x4.TRS(p,rotation,Vector3.one*size);
                if(kind<3)AppendTroop(kind,matrix,redHitTimes[i],0,phase,attacking);
                else {
                    float step=Mathf.Sin(clock*(enrage>0?16:11)+phase);
                    float attack=attacking>0?Mathf.Max(0,Mathf.Sin(clock*13+phase))*16:0;
                    matrix=Matrix4x4.TRS(p+Vector3.up*Mathf.Abs(step)*.035f,Quaternion.Euler(step*3+attack,yaw,step*5),Vector3.one*size);
                    if(kind==3){giantDrawHits[ng]=redHitTimes[i];giantMatrices[ng++]=matrix;}
                    else if(bosses<4){bossDrawHits[bosses]=redHitTimes[i];bossMatrices[bosses++]=matrix;}
                }
                float shadowSize=kind>=3?.70f:.31f;
                shadowMatrices[ns++]=Matrix4x4.TRS(new Vector3(p.x+.08f,GroundY+.005f,p.z-.08f),Quaternion.identity,new Vector3(shadowSize,1,shadowSize*.8f));
                // Probe a quiet rear rank instead of the impact socket: the latter
                // is intentionally covered by white hit flashes, coins and damage
                // numbers. This is the exact centre of its instanced contact disc.
                if(kind>=3&&Time.time-redHitTimes[i]<1.1f&&hp<24)
                {
                    float height=kind==4?2.80f:2.10f;
                    Vector3 at=p+Vector3.up*height;
                    float health=Mathf.Clamp01((front-redS[i])*.24f+.13f);
                    hpBackgroundMatrices[hp]=Matrix4x4.TRS(at,facing,new Vector3(1.0f,.14f,1));
                    hpMatrices[hp]=Matrix4x4.TRS(at-renderCamera.transform.forward*.012f-renderCamera.transform.right*(1-health)*.46f,facing,new Vector3(.92f*health,.08f,1));
                    Vector3 right=new Vector3(direction.z,0,-direction.x);
                    swordMatrices[hp]=Matrix4x4.TRS(p+right*.66f+Vector3.up*1.28f+direction*.25f,rotation*Quaternion.Euler(82,0,0),new Vector3(.09f,.09f,.92f+Mathf.Sin(clock*8+i)*.06f));
                    hp++;
                }
            }
            for(int i=0;i<blueCount;i++)
            {
                if(blueS[i]>pathLength)continue;
                blueVisible++;
                int formationRow=i/BlueLanes,formationLane=i%BlueLanes;
                byte kind=blueKinds[i];
                if(!ShowFormationMember(formationRow,formationLane,blueS[i]-front,kind))continue;
                Vector3 direction;
                float lane=(i%BlueLanes-1.5f)*.340f+HashSigned(i,137)*.18f;
                Vector3 p=PathPosition(blueS[i],lane,out direction);
                p+=direction*HashSigned(i,149)*.18f;p.y+=.02f;
                float sinceHit=Time.time-blueHitTimes[i];
                if(sinceHit>=0&&sinceHit<.06f)p+=direction*(1-sinceHit/.06f)*Mathf.Lerp(.14f,.24f,Hash01(i,157));
                float yaw=Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg+HashSigned(i,163)*6f;
                float size=.76f*Mathf.Lerp(.94f,1.06f,Hash01(i,179));
                float phase=Hash01(i,193)*Mathf.PI*2;
                float attacking=blueS[i]<front+1?1:0;
                Matrix4x4 matrix=Matrix4x4.TRS(p,Quaternion.Euler(0,yaw,0),Vector3.one*size);
                AppendAllyTroop(kind,matrix,blueHitTimes[i],1,phase,attacking);
                shadowMatrices[ns++]=Matrix4x4.TRS(new Vector3(p.x+.08f,GroundY+.005f,p.z-.08f),Quaternion.identity,new Vector3(.31f,1,.25f));
            }
            // One quiet sentry shares the existing ally/shadow instanced draws. It
            // gives the base a readable guard and a deterministic, effect-free
            // soldier contact-shadow sample away from the collision flashes.
            Vector3 sentry=new Vector3(-2.52f,1.23f,-2.58f);
            AppendAllyTroop(0,Matrix4x4.TRS(sentry,Quaternion.Euler(0,145,0),Vector3.one*.72f),-1000,1,.35f,0);
            Vector3 sentryShadow=new Vector3(sentry.x+.06f,sentry.y+.005f,sentry.z-.06f);
            shadowMatrices[ns++]=Matrix4x4.TRS(sentryShadow,Quaternion.identity,new Vector3(.72f,1,.58f));
            Vector3 screenRight=renderCamera.transform.right;screenRight.y=0;screenRight.Normalize();
            artSoldierShadowWorld=sentryShadow+screenRight*.16f;artSoldierShadowValid=true;
            // A short white silhouette replaces the previous lingering corpse.
            // Existing corpse RNG/physics is preserved; a 60 ms white flash
            // clears the contact band quickly enough for the four-point stars.
            for(int i=0;i<CorpseCap;i++)
            {
                Corpse c=corpses[i];if(c.duration<=0||c.age>.06f)continue;
                float t=c.age/.06f;
                Matrix4x4 m=Matrix4x4.TRS(c.position,Quaternion.Euler(t*65,c.yaw+c.spin*t,t*20),Vector3.one*(.85f*Mathf.Min(1,(1-t)*2)));
                if(c.kind==3)whiteGiantMatrices[wg++]=m;
                else if(c.kind==4)whiteBossMatrices[wBoss++]=m;
                else {int kind=Mathf.Min(TroopKinds-1,c.kind);whiteTroopMatrices[kind][whiteTroopCounts[kind]++]=m;}
            }
            for(int i=0;i<TowerCount;i++)
            {
                float yaw=Mathf.Atan2(FrontPosition().x-towerPositions[i].x,FrontPosition().z-towerPositions[i].z)*Mathf.Rad2Deg;
                allyArcherMatrices[i]=Matrix4x4.TRS(towerPositions[i]+new Vector3(-.34f,1.80f,.03f),Quaternion.Euler(towerRecoil[i]*50,yaw,0),Vector3.one*(towerLevel[i]<0?0:.8f));
            }
            Draw(shadowMesh,shadowMaterial,shadowMatrices,ns);
            for(int kind=0;kind<TroopKinds;kind++)DrawTroop(kind);
            DrawAllyTroops();
            Draw(giantMesh,giantMaterial,giantMatrices,ng,giantDrawHits);Draw(giantGlowMesh,giantGlowMaterial,giantMatrices,ng);
            Draw(bossMesh,redMaterial,bossMatrices,bosses,bossDrawHits);
            Draw(allyArcherMesh,blueMaterial,allyArcherMatrices,TowerCount);
            for(int kind=0;kind<TroopKinds;kind++)Draw(troopMeshes[kind],silhouetteMaterial,whiteTroopMatrices[kind],whiteTroopCounts[kind]);
            Draw(giantMesh,silhouetteMaterial,whiteGiantMatrices,wg);Draw(bossMesh,silhouetteMaterial,whiteBossMatrices,wBoss);
            Draw(hpMesh,hpBackgroundMaterial,hpBackgroundMatrices,hp);Draw(hpMesh,hpMaterial,hpMatrices,hp);
            Draw(trailMesh,swordMaterial,swordMatrices,hp);
            displayedReds=redVisible;displayedBlues=blueVisible;
            cachedRedDraws=redVisible;cachedBlueDraws=blueVisible;cachedShadowDraws=ns;
        }

        void AppendTroop(int kind,Matrix4x4 matrix,float hitTime,float team,float phase,float attack)
        {
            kind=Mathf.Clamp(kind,0,TroopKinds-1);int at=troopCounts[kind]++;
            troopMatrices[kind][at]=matrix;troopHitTimes[kind][at]=hitTime;troopTeams[kind][at]=team;
            troopPhases[kind][at]=phase;troopAttacks[kind][at]=attack;
        }

        void AppendAllyTroop(int kind,Matrix4x4 matrix,float hitTime,float team,float phase,float attack)
        {
            int at=allyTroopCounts[0]++;
            allyTroopMatrices[0][at]=matrix;allyTroopHitTimes[0][at]=hitTime;allyTroopTeams[0][at]=team;
            allyTroopPhases[0][at]=phase;allyTroopAttacks[0][at]=attack;
        }

        void DrawAllyTroops()
        {
            int count=allyTroopCounts[0];Mesh mesh=allyAiTroopMesh?allyAiTroopMesh:troopMeshes[0];if(count<=0||!mesh)return;
            soldierProperties.SetFloatArray(HitTimeId,allyTroopHitTimes[0]);soldierProperties.SetFloatArray(TeamId,allyTroopTeams[0]);
            soldierProperties.SetFloatArray(PhaseId,allyTroopPhases[0]);soldierProperties.SetFloatArray(AttackId,allyTroopAttacks[0]);
            Graphics.DrawMeshInstanced(mesh,0,allyTroopMaterial,allyTroopMatrices[0],count,soldierProperties,ShadowCastingMode.Off,false,0,renderCamera,LightProbeUsage.Off);
        }

        void DrawTroop(int kind)
        {
            int count=troopCounts[kind];if(count<=0||!troopMeshes[kind])return;
            soldierProperties.SetFloatArray(HitTimeId,troopHitTimes[kind]);
            soldierProperties.SetFloatArray(TeamId,troopTeams[kind]);
            soldierProperties.SetFloatArray(PhaseId,troopPhases[kind]);
            soldierProperties.SetFloatArray(AttackId,troopAttacks[kind]);
            Graphics.DrawMeshInstanced(troopMeshes[kind],0,troopMaterial,troopMatrices[kind],count,soldierProperties,
                ShadowCastingMode.Off,false,0,renderCamera,LightProbeUsage.Off);
        }

        static float Hash01(int value,int salt)
        {
            unchecked{uint x=(uint)value*747796405u+(uint)salt*2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;x=(x>>22)^x;return(x&0x00ffffffu)/16777216f;}
        }
        static float HashSigned(int value,int salt)=>Hash01(value,salt)*2-1;

        static bool ShowFormationMember(int row,int lane,float depth,byte kind)
        {
            if(kind>=3||depth<1.35f)return true;
            if(depth<5f)return (row+lane*2)%3!=0;
            return (row*2+lane*3)%3==0;
        }

        void Draw(Mesh mesh, Material material, Matrix4x4[] matrices, int count,float[] hitTimes=null)
        {
            if (count == 0 || !mesh) return;
            if(hitTimes!=null)hitProperties.SetFloatArray(HitTimeId,hitTimes);
            Graphics.DrawMeshInstanced(mesh,0,material,matrices,count,hitTimes==null?null:hitProperties,
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

        void BuildChest(){
            Shader shader=Resources.Load<Shader>("HyeopgokSasu/Shaders/Horde");
            chestWoodMaterial=MakeMaterial(shader,MgfLook.Hex("#7a401e").linear,false);chestGoldMaterial=MakeMaterial(shader,MgfLook.Hex("#f3bd35").linear,false);chestGoldMaterial.SetFloat("_Metallic",.72f);
            chestRoot=new GameObject("정답 보물상자").transform;chestRoot.SetParent(transform,false);
            MgfLook.Prim(PrimitiveType.Cube,"상자 몸통",new Vector3(0,.28f,0),new Vector3(1.05f,.48f,.70f),chestWoodMaterial,chestRoot,false);
            MgfLook.Prim(PrimitiveType.Cube,"상자 금테",new Vector3(0,.31f,-.36f),new Vector3(.17f,.52f,.035f),chestGoldMaterial,chestRoot,false);
            chestLid=new GameObject("열리는 상자 뚜껑").transform;chestLid.SetParent(chestRoot,false);chestLid.localPosition=new Vector3(0,.55f,.34f);
            MgfLook.Prim(PrimitiveType.Cube,"상자 뚜껑",new Vector3(0,.09f,-.34f),new Vector3(1.08f,.22f,.72f),chestWoodMaterial,chestLid,false);
            MgfLook.Prim(PrimitiveType.Cube,"뚜껑 금테",new Vector3(0,.10f,-.70f),new Vector3(.18f,.24f,.035f),chestGoldMaterial,chestLid,false);
            chestRoot.gameObject.SetActive(false);
        }
        void ShowChest(Vector3 position,HyeopgokChestReward reward){
            chestLife=1.5f;chestRoot.gameObject.SetActive(true);chestRoot.position=new Vector3(position.x,1.25f,position.z);chestRoot.rotation=Quaternion.Euler(0,renderCamera.transform.eulerAngles.y+180,0);chestRoot.localScale=Vector3.one*.84f;chestLid.localRotation=Quaternion.identity;
        }
        void UpdateChest(float dt){
            if(chestLife<=0||!chestRoot)return;chestLife=Mathf.Max(0,chestLife-dt);float age=1.5f-chestLife;
            chestRoot.localScale=Vector3.one*.84f*(1+Mathf.Sin(Mathf.Min(1,age/.28f)*Mathf.PI)*.18f);
            chestLid.localRotation=Quaternion.Euler(-Mathf.SmoothStep(0,72,Mathf.InverseLerp(.18f,.58f,age)),0,0);
            if(chestLife<=0)chestRoot.gameObject.SetActive(false);
        }

        void BuildTowers()
        {
            GameObject bowAsset=Resources.Load<GameObject>("HyeopgokSasu/Models/crossbow");
            string[] names={"crossbow","cannon","magic"};
            for(int type=0;type<3;type++)for(int level=0;level<3;level++){
                string id="tower_"+names[type]+"_lv"+(level+1);
                towerMaterials[type,level]=HyeopgokAiAssets.CreateMaterial("tower_shared",Color.white,1);
            }
            for(int i=0;i<TowerCount;i++)
            {
                towers[i]=new GameObject("보상 타워 부지 "+(i+1)).transform;
                towers[i].SetParent(transform,false);towers[i].position=towerPositions[i];
                towers[i].rotation=Quaternion.Euler(0,180,0);
                bool anyAi=false;
                for(int type=0;type<3;type++)for(int level=0;level<3;level++){
                    string id="tower_"+names[type]+"_lv"+(level+1);
                    GameObject stage=HyeopgokAiAssets.InstantiateModel(id,towerPositions[i],towers[i].rotation,1,towers[i],towerMaterials[type,level],true);
                    if(stage){stage.transform.localPosition=Vector3.zero;stage.transform.localRotation=Quaternion.identity;anyAi=true;}
                    towerStages[i,type,level]=stage;towerRenderers[i,type,level]=stage?stage.GetComponentInChildren<Renderer>(true):null;
                    if(stage)stage.SetActive(false);
                }
                if(!anyAi){
                    for(int level=0;level<3;level++){
                        GameObject asset=Resources.Load<GameObject>("HyeopgokSasu/Models/tower_base_l"+(level+1));if(!asset)asset=Resources.Load<GameObject>("HyeopgokSasu/Models/tower_base");if(!asset)continue;
                        GameObject stage=Instantiate(asset,towers[i]);stage.transform.localPosition=Vector3.zero;stage.transform.localRotation=Quaternion.identity;stage.transform.localScale=Vector3.one;
                        towerStages[i,0,level]=stage;towerRenderers[i,0,level]=stage.GetComponentInChildren<Renderer>();if(towerRenderers[i,0,level])towerRenderers[i,0,level].sharedMaterial=blueMaterial;stage.SetActive(false);
                    }
                    if(bowAsset){GameObject bow=Instantiate(bowAsset,towers[i]);towerBows[i]=bow.transform;bow.transform.localPosition=Vector3.up*1.62f;Renderer r=bow.GetComponentInChildren<Renderer>();if(r)r.sharedMaterial=blueMaterial;}
                }
            }
        }

        static int UpgradeCost(int level)=>level<=0?15:level==1?30:0;
        int FirstInstalledTower(int offset){for(int k=0;k<TowerCount;k++){int i=(Mathf.Abs(offset)+k)%TowerCount;if(towerLevel[i]>=0)return i;}return -1;}
        void InstallTower(int slot,int type){
            if(slot<0||slot>=TowerCount)return;towerType[slot]=Mathf.Clamp(type,0,2);towerLevel[slot]=0;upgradeRemaining[slot]=UpgradeCost(0);
            towerRise[slot]=1;towerDrop[slot]=4.8f;towerRecoil[slot]=.28f;ApplyTowerVisibility(slot);Burst(towerPositions[slot],48,3.4f);
        }
        void ApplyTowerVisibility(int slot){
            for(int type=0;type<3;type++)for(int level=0;level<3;level++)if(towerStages[slot,type,level])towerStages[slot,type,level].SetActive(towerLevel[slot]>=0&&type==towerType[slot]&&level==towerLevel[slot]);
            if(towerBows[slot])towerBows[slot].gameObject.SetActive(towerLevel[slot]>=0&&towerType[slot]==0);
        }
        public bool TickUpgrade(Vector3 king,Vector3 target,float dt){
            int previous=upgradePad;int at=UpgradePadAt(king),destination=UpgradePadAt(target);bool stopped=(king-target).sqrMagnitude<.012f;
            upgradePad=at>=0&&at==destination&&stopped&&towerLevel[at]>=0&&towerLevel[at]<2?at:-1;
            if(upgradePad<0){if(previous>=0)upgradeClock[previous]=0;return previous!=upgradePad;}
            int slot=upgradePad;upgradeClock[slot]-=dt;
            if(upgradeClock[slot]<=0&&rules!=null&&rules.SpendUpgradeCoin()){
                upgradeClock[slot]=.105f;economy.Pour(UpgradePads[slot]);upgradeRemaining[slot]=Mathf.Max(0,upgradeRemaining[slot]-1);
                towerRecoil[slot]=.08f;
                if(upgradeRemaining[slot]==0){towerLevel[slot]++;upgradeCount++;upgradeRemaining[slot]=UpgradeCost(towerLevel[slot]);towerRise[slot]=.001f;ApplyTowerVisibility(slot);Burst(towerPositions[slot]+Vector3.up*.5f,42,3.2f);MgfSfx.Play("correct",.25f);}
                return true;
            }
            return previous!=upgradePad;
        }

        void UpdateTowers(float dt)
        {
            Vector3 frontPosition=FrontPosition();
            for(int i=0;i<TowerCount;i++)
            {
                if(!towers[i])continue;
                towerRise[i]=Mathf.Min(1,towerRise[i]+dt*1.1f);
                towerRecoil[i]=Mathf.Max(0,towerRecoil[i]-dt);
                float t=towerRise[i],eased=1-Mathf.Pow(1-t,3);
                towerDrop[i]=Mathf.MoveTowards(towerDrop[i],0,dt*7.2f);
                towers[i].position=towerPositions[i]+Vector3.up*((eased-1)*1.8f+towerDrop[i]);
                towers[i].localScale=new Vector3(1,towerLevel[i]<0?.16f:1+Mathf.Sin(t*Mathf.PI)*.12f-towerRecoil[i]*.16f,1);
                for(int type=0;type<3;type++)for(int level=0;level<3;level++)
                {
                    GameObject stage=towerStages[i,type,level];if(!stage)continue;
                    bool active=towerLevel[i]>=0&&type==towerType[i]&&level==towerLevel[i];if(stage.activeSelf!=active)stage.SetActive(active);
                    if(active&&towerRenderers[i,type,level])
                    {
                        towerProperties.SetFloat("_Flash",Mathf.Clamp01((1-t)*2.4f));
                        towerRenderers[i,type,level].SetPropertyBlock(towerProperties);
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

        static Mesh LoadSoldier(string model,string fallbackModel="soldier")
        {
            GameObject asset=Resources.Load<GameObject>("HyeopgokSasu/Models/"+model);
            if(!asset&&model!=fallbackModel)asset=Resources.Load<GameObject>("HyeopgokSasu/Models/"+fallbackModel);
            if(!asset&&fallbackModel!="soldier")asset=Resources.Load<GameObject>("HyeopgokSasu/Models/soldier");
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
            if(trailMesh) Destroy(trailMesh);for(int i=0;i<troopMeshes.Length;i++)if(troopMeshes[i]){bool duplicate=false;for(int j=0;j<i;j++)if(troopMeshes[j]==troopMeshes[i])duplicate=true;if(!duplicate)Destroy(troopMeshes[i]);}
            if(impacts != null) impacts.Dispose();
            if(economy!=null)economy.Dispose();
            if(giantMesh)Destroy(giantMesh);if(allyArcherMesh)Destroy(allyArcherMesh);if(bossMesh)Destroy(bossMesh);if(hpMesh)Destroy(hpMesh);
            if(allyAiTroopMesh)Destroy(allyAiTroopMesh);if(giantGlowMesh)Destroy(giantGlowMesh);
            if(boltTrailMaterial)Destroy(boltTrailMaterial);if(silhouetteMaterial)Destroy(silhouetteMaterial);if(swordMaterial)Destroy(swordMaterial);if(hpMaterial)Destroy(hpMaterial);if(hpBackgroundMaterial)Destroy(hpBackgroundMaterial);
            if(troopMaterial)Destroy(troopMaterial);if(allyTroopMaterial)Destroy(allyTroopMaterial);if(giantMaterial)Destroy(giantMaterial);if(giantGlowMaterial)Destroy(giantGlowMaterial);
            for(int type=0;type<3;type++)for(int level=0;level<3;level++)if(towerMaterials[type,level])Destroy(towerMaterials[type,level]);
            if(chestWoodMaterial)Destroy(chestWoodMaterial);if(chestGoldMaterial)Destroy(chestGoldMaterial);
        }
    }
}
