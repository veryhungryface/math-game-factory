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
        readonly Matrix4x4[] redMatrices = new Matrix4x4[RedCap + CorpseCap];
        readonly Matrix4x4[] blueMatrices = new Matrix4x4[BlueCap + CorpseCap];
        readonly Matrix4x4[] shadowMatrices = new Matrix4x4[RedCap + BlueCap];
        readonly Matrix4x4[] whiteMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] goldMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] dustMatrices = new Matrix4x4[ParticleCap];
        readonly Matrix4x4[] boltMatrices = new Matrix4x4[BoltCap];
        readonly Matrix4x4[] trailMatrices = new Matrix4x4[BoltCap];
        readonly Vector3[] path = new Vector3[PathSamples];
        readonly float[] distances = new float[PathSamples];
        readonly Chip[] chips = new Chip[ParticleCap];
        readonly Corpse[] corpses = new Corpse[CorpseCap];
        readonly Bolt[] bolts = new Bolt[BoltCap];
        readonly Transform[] towers = new Transform[2];
        readonly float[] towerRise = new float[2];
        readonly float[] towerRecoil = new float[2];
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
        }
        struct Bolt
        {
            public Vector3 start, target, previous;
            public float age, duration;
            public bool heavy, hit;
        }

        Camera renderCamera;
        Mesh soldierMesh, blueSoldierMesh, cubeMesh, shadowMesh, boltMesh, trailMesh;
        Material redMaterial, blueMaterial, whiteMaterial, goldMaterial, dustMaterial, shadowMaterial;
        HyeopgokImpact impacts;
        int redCount = 400, blueCount = 100, chipCursor, corpseCursor, boltCursor, killCount, pendingDamage;
        int wave = 1, volleys, volleyHits, volleyTrials, volleyFired, laneCursor, displayedReds, displayedBlues;
        float pathLength, front, clock, sinceAnswer, pressureClock, battleTick, fireClock, salvoClock;
        float salvoSpacing = .035f;
        float enrage, hitStop, flash, shake, boost;
        float visualHitStop, impactCooldown;
        int cachedRedDraws, cachedBlueDraws, cachedShadowDraws;
        bool initialized, playing, questionActive;
        uint randomState = 0x5e3a07c1u;

        public int Kills { get { return killCount; } }
        public int Reds { get { return displayedReds; } }
        public int Blues { get { return displayedBlues; } }
        public float Shake { get { return shake; } }

        public void Init(Camera cam)
        {
            if (initialized) return;
            renderCamera = cam;
            Shader shader = Resources.Load<Shader>("HyeopgokSasu/Shaders/Horde");
            redMaterial = MakeMaterial(shader, new Color(.92f,.028f,.045f), false);
            blueMaterial = MakeMaterial(shader, new Color(.025f,.24f,.94f), false);
            whiteMaterial = MakeMaterial(shader, new Color(1,.995f,.95f), true);
            goldMaterial = MakeMaterial(shader, new Color(1,.59f,.028f), true);
            dustMaterial = MakeMaterial(shader, new Color(.89f,.81f,.66f), true);
            shadowMaterial = MakeMaterial(shader, new Color(.33f,.29f,.20f), true);
            soldierMesh = LoadSoldier("soldier");
            blueSoldierMesh = LoadSoldier("soldier_blue");
            cubeMesh = MakeChipMesh();
            shadowMesh = MakeShadowMesh();
            boltMesh = MakeBoltMesh();
            trailMesh = MakeTrailMesh();
            impacts = new HyeopgokImpact(cam,shader);
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
            wave = Mathf.Clamp(nextWave, 1, 10);
            int nextCount = Mathf.Min(RedCap, 400 + (wave - 1) * 22);
            for (int i = redCount; i < nextCount; i++) redS[i] = -.15f * (i - redCount + 1);
            redCount = nextCount;
        }

        public void BeginQuestion()
        {
            sinceAnswer = pressureClock = 0;
            questionActive = playing;
        }

        public void Reward(int padIndex, Vector3 pos, int hits, int trials)
        {
            if (!initialized) return;
            questionActive = false;
            enrage = 0;
            boost = 4.3f;
            hitStop = .05f;
            flash = 1;
            int tower = Mathf.Abs(padIndex) % 2;
            towerRise[tower] = .001f;
            towerRecoil[tower] = .25f;
            volleyTrials = Mathf.Clamp(trials, 1, 225);
            volleyHits = Mathf.Clamp(hits, 0, volleyTrials);
            volleyFired = 0;
            volleys = volleyTrials;
            salvoSpacing = Mathf.Clamp(2.8f / volleyTrials, .018f, .12f);
            salvoClock = 0;
            int added = Mathf.Min(8 + Mathf.RoundToInt(16f * volleyHits / volleyTrials), BlueCap - blueCount);
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
            front = 19.0f;
            laneCursor = 0;
            for (int i = 0; i < RedCap; i++) redS[i] = front - .25f - (i / RedLanes) * RedSpacing - (i % RedLanes) * .018f;
            for (int i = 0; i < BlueCap; i++) blueS[i] = front + .38f + (i / BlueLanes) * BlueSpacing + (i % BlueLanes) * .03f;
            for (int i = 0; i < ParticleCap; i++) chips[i].duration = 0;
            for (int i = 0; i < CorpseCap; i++) corpses[i].duration = 0;
            for (int i = 0; i < BoltCap; i++) bolts[i].duration = 0;
            // Both pre-existing towers visibly operate in the title battle.
            towerRise[0] = towerRise[1] = 1;
            towerRecoil[0] = towerRecoil[1] = 0;
            displayedReds = redCount;
            displayedBlues = blueCount;
        }

        void Update()
        {
            if (!initialized) return;
            float realDt = Mathf.Min(Time.deltaTime, .04f);
            visualHitStop = Mathf.Max(0,visualHitStop-realDt);
            impactCooldown = Mathf.Max(0,impactCooldown-realDt);
            shake = Mathf.MoveTowards(shake, 0, realDt * .6f);
            flash = Mathf.MoveTowards(flash, 0, realDt * 9);
            redMaterial.SetFloat("_Flash", flash * .68f);
            hitStop -= realDt;
            float dt = hitStop > 0 ? 0 : realDt;
            clock += dt;
            if (playing && questionActive)
            {
                sinceAnswer += dt;
                // The HUD exposes the same grace, cadence and damage. One thoughtful
                // 20-second solve takes no damage; pressure starts at 22 seconds.
                if (sinceAnswer > PressureGrace)
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
                if (laneCursor % 2 == 0) KillRed(lane, .8f);
                if (laneCursor % 5 == 0) KillBlue(lane % BlueLanes);
                if (playing && laneCursor % 8 == 0) MgfSfx.Play("pop", .07f);
            }
            fireClock -= dt;
            if (fireClock <= 0)
            {
                fireClock = boost > 0 ? .18f : .48f;
                FireBolt(laneCursor % 2, false, true);
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
            AddCorpse(pos, -direction * Range(.9f,2.0f) * force, false, Mathf.Atan2(direction.x,direction.z) * Mathf.Rad2Deg);
            for (int i = lane; i < redCount; i += RedLanes)
                redS[i] = i + RedLanes < redCount ? redS[i + RedLanes] : -.2f - Range(0,.7f);
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
            for (int i = lane; i < blueCount; i += BlueLanes)
                blueS[i] = i + BlueLanes < blueCount ? blueS[i + BlueLanes] : pathLength + Range(0,.5f);
        }

        void AddCorpse(Vector3 position, Vector3 velocity, bool blue, float yaw)
        {
            int slot = corpseCursor++ % CorpseCap;
            corpses[slot] = new Corpse {
                position = position + Vector3.up * .12f,
                velocity = velocity + Vector3.up * Range(1.6f,3.2f),
                age = 0, duration = Range(.55f,.85f), yaw = yaw,
                spin = Range(-150,150), blue = blue
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
                trailMatrices[count] = Matrix4x4.TRS(position-direction*(tail*.5f),rot,new Vector3(b.heavy?.052f:.039f,b.heavy?.052f:.039f,tail));
                count++;
                if (p >= 1)
                {
                    Burst(b.target,b.heavy ? 12 : 8,b.heavy ? 3.2f : 2.2f);
                    if (b.hit)
                    {
                        impacts.Contact(b.target,b.heavy);
                        KillRed((i+laneCursor)%RedLanes,b.heavy ? 1.7f : 1f);
                        if (b.heavy) KillRed((i+laneCursor+3)%RedLanes,1.5f);
                    }
                    b.duration = 0;
                }
                b.previous = position;
                bolts[i] = b;
            }
            Draw(boltMesh,goldMaterial,boltMatrices,count);
            Draw(trailMesh,whiteMaterial,trailMatrices,count);
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

        void RenderSoldiers()
        {
            if (visualHitStop > 0 && cachedRedDraws > 0)
            {
                Draw(shadowMesh,shadowMaterial,shadowMatrices,cachedShadowDraws);
                Draw(soldierMesh,redMaterial,redMatrices,cachedRedDraws);
                Draw(blueSoldierMesh,blueMaterial,blueMatrices,cachedBlueDraws);
                return;
            }
            int nr = 0, nb = 0, ns = 0;
            for (int i = 0; i < redCount; i++)
            {
                if (redS[i] < 0) continue;
                // Small visual staggering and wider helmets make neighbouring rows
                // overlap as a crowd. The ordered lane queues remain untouched.
                float lane = (i % RedLanes - 3.5f) * .245f + Mathf.Sin(i*1.73f)*.028f;
                Vector3 direction;
                Vector3 p = PathPosition(redS[i],lane,out direction);
                p += direction*(Mathf.Sin(i*2.113f)*.043f);
                // Separate gates: eight narrow lanes form two red streams for 3 metres.
                float step = Mathf.Sin(clock*(enrage>0 ? 16:11)+i*2.399f);
                p.y += .024f + Mathf.Abs(step)*.035f;
                float yaw = Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg+Mathf.Sin(i*1.29f)*5.5f;
                float size = 1+Mathf.Sin(i*2.71f)*.045f;
                redMatrices[nr++] = Matrix4x4.TRS(p,Quaternion.Euler(step*3,yaw,step*5),new Vector3(1.01f,1.035f+step*.035f,1.01f)*size);
                shadowMatrices[ns++] = Matrix4x4.TRS(new Vector3(p.x+.08f,GroundY+.005f,p.z-.08f),Quaternion.identity,new Vector3(.24f,1,.20f));
            }
            displayedReds = nr;
            for (int i = 0; i < blueCount; i++)
            {
                if (blueS[i] > pathLength) continue;
                Vector3 direction;
                Vector3 p = PathPosition(blueS[i],(i%BlueLanes-1.5f)*.285f+Mathf.Sin(i*1.31f)*.023f,out direction);
                p += direction*(Mathf.Sin(i*2.117f)*.035f);
                float step = Mathf.Sin(clock*12+i*2.399f);
                p.y += .02f+Mathf.Abs(step)*.035f;
                float yaw = Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg+Mathf.Sin(i*1.37f)*4.5f;
                float size = 1+Mathf.Sin(i*2.67f)*.035f;
                blueMatrices[nb++] = Matrix4x4.TRS(p,Quaternion.Euler(step*3,yaw,step*5),new Vector3(.98f,1.055f+step*.04f,.98f)*size);
                shadowMatrices[ns++] = Matrix4x4.TRS(new Vector3(p.x+.08f,GroundY+.005f,p.z-.08f),Quaternion.identity,new Vector3(.24f,1,.20f));
            }
            displayedBlues = nb;
            for (int i = 0; i < CorpseCap; i++)
            {
                Corpse c = corpses[i];
                if (c.duration <= 0) continue;
                float t = c.age / c.duration;
                Matrix4x4 matrix = Matrix4x4.TRS(c.position,Quaternion.Euler(t*135,c.yaw+c.spin*t,t*40),Vector3.one*(.9f*Mathf.Min(1,(1-t)*4)));
                if (c.blue) blueMatrices[nb++] = matrix; else redMatrices[nr++] = matrix;
            }
            Draw(shadowMesh,shadowMaterial,shadowMatrices,ns);
            Draw(soldierMesh,redMaterial,redMatrices,nr);
            Draw(blueSoldierMesh,blueMaterial,blueMatrices,nb);
            cachedRedDraws = nr; cachedBlueDraws = nb; cachedShadowDraws = ns;
        }

        void Draw(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
        {
            if (count == 0 || !mesh) return;
            Graphics.DrawMeshInstanced(mesh,0,material,matrices,count,null,
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
            GameObject asset=Resources.Load<GameObject>("HyeopgokSasu/Models/tower");
            if (!asset) return;
            for (int i=0;i<2;i++)
            {
                GameObject tower=Instantiate(asset,transform);
                tower.name="석궁 포탑 "+(i+1);
                towers[i]=tower.transform;
                towers[i].position=towerPositions[i];
                towers[i].rotation=Quaternion.Euler(0,180,0);
                Renderer[] renderers=tower.GetComponentsInChildren<Renderer>();
                for (int j=0;j<renderers.Length;j++)
                {
                    renderers[j].sharedMaterial=blueMaterial;
                    renderers[j].shadowCastingMode=ShadowCastingMode.On;
                }
            }
        }

        void UpdateTowers(float dt)
        {
            for (int i=0;i<2;i++)
            {
                if (!towers[i]) continue;
                towerRise[i]=Mathf.Min(1,towerRise[i]+dt*1.9f);
                towerRecoil[i]=Mathf.Max(0,towerRecoil[i]-dt);
                float t=towerRise[i];
                float eased=1-Mathf.Pow(1-t,3);
                towers[i].position=towerPositions[i]+Vector3.up*((eased-1)*1.8f);
                towers[i].localScale=new Vector3(1,1+Mathf.Sin(t*Mathf.PI)*.12f-towerRecoil[i]*.16f,1);
            }
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
            colors[0]=new Color(1,1,1,0);
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
        }
    }
}
