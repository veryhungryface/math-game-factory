# Unity 트랙 — 정본 문서

> 중학교 게임은 HTML 캔버스가 아니라 **Unity 6000.3.24f1(arm64)로 만들고 WebGL로 빌드**해
> `public/g/<slug>/`에 게시한다. 이 트랙을 쓰는 이유는 3D 조명·재질·연출 품질이다(Coral Rescue 수준).
> 게임 폴더 계약·하드 불변 5·품질 게이트는 `CLAUDE.md` 그대로다. 이 문서는 **Unity로 그 계약을 지키는 방법**만 다룬다.
> 최초 작성: 2026-09-26 (킷·빌드 스크립트·스모크 QA 실측)

---

## 1. 구조 한눈에

```
factory/unity/
  kit/                         매 빌드마다 워크스페이스에 동기화되는 공용 코드 (정본 — 워크스페이스 사본을 고치지 마라)
    MgfKit/Runtime/            MgfBridge · MgfContracts(IMgfGame/MgfState/MgfProblem/MgfJson) · MgfLook · MgfText · MgfPointer · MgfSfx · MgfFx
    MgfKit/Plugins/WebGL/      MgfBridge.jslib  (C# → JS)
    MgfKit/Editor/             MgfBuild.cs (배치 빌드 진입점) · MgfSetup.cs (프로젝트 설정)
    MgfKit/Resources/MgfKit/   Shaders/*.shader (Mgf/Lit·Unlit·Alpha·Additive·Sky) · Fonts/MgfKR-Bold.otf (+OFL)
    WebGLTemplates/MGF/        index.html  (전체 화면 캔버스 · 한국어 로딩 · 음소거 · window.__GAME_TEST__)
  setup-workspace.sh           공용 워크스페이스 생성·점검 (멱등)
  build.sh                     게임 1개 빌드 → public/g/<slug>/ 배치
  load-probe.mjs               로딩 시간 측정 (goto → __GAME_TEST__.ready)
  smoke/Scripts/SmokeGame.cs   킷 검증용 스모크 게임 (구조 예시로 읽어라)

factory/unity-src/<slug>/      ★ 게임별 Unity 소스의 정본 (git에 남는다 — 검산·검수 에이전트가 C#을 읽는다)
~/UnityProjects/MGF-Workspace  공용 Unity 프로젝트 (git 밖. Library를 따뜻하게 유지해 빌드를 빠르게 한다)
```

한 번에 **게임 하나만** 워크스페이스 `Assets/Game/`에 들어간다. `build.sh`가 매번 소스 폴더로 교체한다.
그래서 **Unity 빌드는 동시에 두 개를 돌릴 수 없다**(워크스페이스가 하나다).

### 1.1 게임 소스 폴더 `factory/unity-src/<slug>/`

| 경로 | 필수 | 내용 |
|---|---|---|
| `Scripts/*.cs` | ✅ | `namespace Mgf.<Slug>` (예: `Mgf.PrimeForge`). **`IMgfGame`을 구현한 MonoBehaviour가 정확히 1개**(부트스트랩) |
| `Resources/<Slug>/…` | ⬜ | 텍스처(png)·모델(fbx)·오디오. 코드에서 `Resources.Load<Texture2D>("<Slug>/wood")` (확장자 없이) |
| `Editor/*.cs` | ⬜ | 임포트 설정 같은 에디터 스크립트(빌드에 안 들어감) |
| `mgf.json` | ⬜ | `{"title":"…","bg":"#RRGGBB","scene":"Assets/Game/Scenes/Main.unity"}` — title 없으면 `meta.json`의 title |
| `ArtSource/` | ⬜ | 원화·프롬프트 기록. **Unity로 복사하지 않는다**(build.sh가 제외) |

- `.meta` 파일은 넣지 마라(워크스페이스가 만들고 보존한다). 예외: 직접 만든 씬을 쓰는 경우.
- **아트 단계 이미지를 텍스처로 쓰는 법:** `public/g/<slug>/assets/foo.png` → `factory/unity-src/<slug>/Resources/<Slug>/foo.png`로 복사 → `Resources.Load<Texture2D>("<Slug>/foo")` → `MgfLook.Lit(Color.white, tex: t)`.
- 3D 모델은 **FBX**로 넣는다. 워크스페이스에 glTF 임포터(glTFast)가 없다 — GLB는 Blender로 FBX로 바꿔라.

---

## 2. 브리지 계약

### 2.1 게임(C#)이 할 일 — 이게 전부다

```csharp
namespace Mgf.PrimeForge {
  public class PrimeForgeGame : MonoBehaviour, Mgf.IMgfGame {
    [System.Serializable] class State : Mgf.MgfState { public int combo; }  // score,lives,level,phase,solved 상속
    readonly State st = new State();
    void Awake() { /* 씬 전체를 코드로 만든다 */ MgfBridge.Register(this); }
    public void TestStart()         { /* 인트로 건너뛰고 playing */ }
    public void TestAnswerCorrect() { /* 실제 정답 경로와 같은 함수 */ }
    public void TestAnswerWrong()   { /* 실제 오답 경로와 같은 함수 */ }
    public string StateJson()       => JsonUtility.ToJson(st);
    public string ProblemBankJson() => MgfJson.Bank(listOfMgfProblem);   // 부팅 때 1회
  }
}
```

- `MgfBridge.Register(this)`: 문제 은행을 JS에 푸시하고 `ready`를 알린다. **Awake/Start에서 1회**.
- 상태 푸시: 0.2초마다(바뀌었을 때만) + `MgfBridge.NotifyChanged()` 호출 시 그 프레임 끝 + 명령 처리 직후.
- **`StateJson()`에 시간·타이머처럼 입력 없이 계속 바뀌는 값을 넣지 마라.** QA `input.real`은 "상태 문자열이 바뀌었나"를
  입력 반응의 근거로 쓴다. 타이머가 들어 있으면 무입력 변화와 입력 반응을 구분 못 해 게이트가 의미를 잃는다.
- `MgfProblem`: `{id, prompt, choices, answer, answerNumeric, unitConcept}` (CLAUDE.md 스키마). 비객관식이면 `choices`를
  비워 두면 JS에서 `null`이 된다. 수치로 못 나타내면 `answerNumeric = double.NaN`(JS에서 필드 생략).
- **고유 문항 300개 이상**을 부팅 때 만들어라(`sampleProblems`는 이 은행에서 비복원 추출한다). QA 다양성 검사는
  괄호 안 문자열을 지우고 비교하므로 `(14 + 6) × 4` 같은 괄호식은 핵심 문장이 겹쳐 보일 수 있다 — 괄호식만으로 은행을 채우지 마라.
- 문제 문장·정답·선택지를 **같은 데이터 모델**에서 만든다(CLAUDE.md 어림 모델 규칙 동일). 정수/분수는 정수로 계산.

### 2.2 JS 쪽 (템플릿이 한다 — 게임이 손댈 것 없음)

`window.__GAME_TEST__`는 **동기 API**다.

| 멤버 | 동작 |
|---|---|
| `ready` | `createUnityInstance` 완료 **그리고** C#이 `Register`를 알린 뒤 `true` |
| `start()` / `answerCorrect()` / `answerWrong()` | `unityInstance.SendMessage('MgfBridge','OnCmd','{"t":"start"}')` — SendMessage는 동기라 **반환 전에 C#이 실행되고 상태가 푸시된다** |
| `getState()` | 마지막 푸시 상태 사본. 최소 `{score,lives,level,phase,solved}` 기본값을 채운다 |
| `sampleProblems(n)` | 푸시된 은행에서 `min(n, 은행 크기)`개를 무작위 비복원 추출(동기). QA는 n=40/17/63으로 부른다 |
| `getLayout()` | `{land, playW, playH, canvas:'fullscreen', lowGfx}` — QA `layout.wide1280`용. 가로면 `html.land`도 붙는다 |

- JS → C#: `SendMessage('MgfBridge','OnCmd', json)`, `t ∈ start | correct | wrong | mute(on) | ping`.
- C# → JS: `MgfBridge.jslib`의 `MGF_Ready / MGF_PushState / MGF_PushBank / MGF_InitialMuted / MGF_LowGfx` → `window.__MGF_HOST__`.
- `MgfBridge` GameObject는 씬과 무관하게 `RuntimeInitializeOnLoad(BeforeSceneLoad)`로 자동 생성된다(이름 고정).

### 2.3 템플릿이 보장하는 것 (`WebGLTemplates/MGF/index.html`)

- 전체 화면 캔버스, 가로 스크롤 없음(390×844·820×1180·2000px 검증), `touch-action:none`·`user-select:none`·`-webkit-tap-highlight-color`.
- 한국어 로딩 화면(진행률 %) — `ready`가 되면 사라진다. 로딩 실패도 한국어 문구(`alert` 없음).
- **모든 경로 상대경로**(`Build/…`), 외부 CDN 없음, `https://` 문자열 없음(QA `static.nocdn`).
- `devicePixelRatio` 상한 2. **음소거 버튼**(48px, `#mgf-mute.mute` — QA 입력 검사에서 "게임 크롬"으로 제외됨) →
  C#이 `AudioListener.volume`을 처리, `localStorage`에 기억(try/catch). 게임은 `MgfBridge.MuteChanged` 이벤트로 추가 반응 가능.
- 오디오는 브라우저 정책상 **첫 사용자 제스처 뒤에만** 난다(Unity가 첫 입력 때 AudioContext를 깨운다).
- `preserveDrawingBuffer: true` — QA가 캔버스 픽셀을 읽는다(`input.real` 픽셀 비교·`visual.notblank`). 끄면 합성 뒤 버퍼가 비어 읽힌다.
- 로컬 QA 서버가 `.wasm`을 `application/wasm`이 아닌 형식으로 주면 Unity가 스트리밍 컴파일을 포기하고 정상 폴백하면서
  `console.error`를 두 줄 찍는다. 템플릿의 `printErr`가 **그 두 메시지만** `console.warn`으로 낮춘다(다른 에러는 그대로 error).
  기본 빌드(gzip+폴백)는 스트리밍을 안 써서 이 경로 자체를 타지 않는다.
- **저사양 모드(lowGfx):** WebGL 렌더러 이름이 SwiftShader/software/llvmpipe면(또는 `?lowgfx=1`) 렌더 픽셀을 약 36만으로 묶고
  C#(`MgfBridge.LowGfx`)이 MSAA를 끄고 그림자 해상도를 낮춘다(그림자는 유지). **실 GPU에서는 켜지지 않는다.**
  첫플레이 하네스(`factory/lib/firstplay/harness.mjs`)는 항상 swiftshader로 띄우므로 **첫플레이 캡처는 이 모드로 찍힌다.**

---

## 3. 빌드

```bash
bash factory/unity/setup-workspace.sh          # 최초 1회(멱등). 없으면 만들고, 있으면 점검·재설정
bash factory/unity/build.sh <slug>             # 소스 = factory/unity-src/<slug>/
bash factory/unity/build.sh <slug> <src_dir>   # 다른 소스 폴더
node factory/unity/load-probe.mjs <slug> [--throttle 20] [--runs 3]   # 로딩 시간
node factory/lib/qa.mjs <slug>                 # 자동 QA (HTML 게임과 동일)
```

`build.sh` 순서: 워크스페이스 점검(없으면 setup) → 킷 동기화 → `unity-src/<slug>` → `Assets/Game/` 교체(.meta 보존,
`ArtSource/`·`mgf.json`·`*.md` 제외) → `unity build <ws> --target WebGL --execute-method MgfBuild.Perform …` 배치 빌드 →
`public/g/<slug>/`의 `index.html`·`Build/`만 교체(**`meta.json`·`thumb.png`·`square.png`·`assets/`는 보존**) → 파일 크기 요약.

| 종료 코드 | 의미 | stdout |
|---|---|---|
| 0 | 성공 | `[build] 성공: <slug> · N초 · bootstrap=…` + 파일별 크기 |
| 1 | 인자·환경 오류 | 이유 |
| 2 | **컴파일 에러** | `Assets/Game/Scripts/X.cs(142,29): error CS0103: …` 줄 목록 (`Assets/Game/` = 소스 폴더 상대경로) |
| 3 | 빌드 실패(부트스트랩 0개/2개, 셰이더 에러 등) | `MGF_BUILD_RESULT {json}` + `[MGF]`·Exception 줄 + 로그 경로 |
| 4 | Unity가 워크스페이스를 열고 있다 | 해당 프로세스 목록 — 에디터를 닫고 다시 |

- 로그: `~/UnityProjects/MGF-Workspace/Logs/build-<slug>-<시각>.log`. 요약 한 줄 `MGF_BUILD_RESULT {…}`.
- 씬은 **코드로 생성**한다: `MgfBuild`가 빈 씬에 `Main Camera`(태그·AudioListener) + `Game` 오브젝트(부트스트랩 컴포넌트)를 넣어
  `Assets/MgfGenerated/Main.unity`로 저장한다. `mgf.json`의 `scene`을 주면 그 씬을 쓴다(비권장).
- 환경변수 스위치: `MGF_UNITY_COMPRESSION=gzip|none|brotli`, `MGF_UNITY_STRIP=Low|Medium|High`,
  `MGF_UNITY_OPT=DiskSizeLTO|DiskSize|BuildTimes|RuntimeSpeed|RuntimeSpeedLTO`, `MGF_UNITY_BUILD_TIMEOUT`(초, 기본 1800),
  `MGF_UNITY_WORKSPACE`, `MGF_UNITY_VERSION`.

### 3.1 워크스페이스 설정 (MgfSetup — 빌드마다 다시 적용)

| 항목 | 값 | 이유 |
|---|---|---|
| 렌더 파이프라인 | **내장(Built-in)**, URP 아님 | Coral Rescue와 같다. 빌드가 빠르고 산출물이 작다. 서피스 셰이더로 PBR+그림자 충분 |
| 색공간 / API | Linear / WebGL2(GLES3)만 | 조명 품질. Linear는 WebGL2 전용 |
| 입력 | 구 Input Manager (`activeInputHandler=0`), Input System 패키지 없음 | 크기·단순성. `MgfPointer`가 마우스·터치 통합 |
| 패키지 | uGUI 2.0(TextMeshPro 포함) + 필수 모듈(물리·파티클·오디오·애니메이션·UI) | `setup-workspace.sh`의 manifest가 정본 |
| TMP | Essential Resources를 `.unitypackage`에서 직접 풀어 넣음 → 킷 한글 폰트로 **동적 SDF 폰트 에셋**(`Assets/MgfGenerated/MgfKR SDF.asset`)을 만들어 TMP 기본 폰트로 지정, 영문 LiberationSans·이모지 리소스 제거 | 기본 폰트가 한글이 되고 .data 1.7MB 절감 |
| 스플래시 | 끔(Unity 6 Personal 허용) | |
| 스크립팅 | IL2CPP(WebGL 유일), Release, OptimizeSize, 관리 코드 스트리핑 Medium, 엔진 코드 스트리핑 On, 예외 Explicit | High 스트리핑은 wasm −0.6MB뿐이라 리플렉션 위험 대비 이득이 작다 |
| Wasm 코드 최적화 | **DiskSizeLTO** | 아래 실측 |
| 압축 | **gzip + decompressionFallback** | 아래 실측. `Content-Encoding` 헤더 없이 어떤 정적 서버에서도 뜬다 |
| 파일명 | 내용 해시(`nameFilesAsHashes`) | `vercel.json`에서 `/g/*/Build/*`를 immutable 1년 캐시 |
| 메모리 | 초기 64MB, 기하 증가, 최대 1024MB | |
| 품질 | 그림자 All/High/거리 40/캐스케이드 1, MSAA 4, 픽셀 라이트 4, vSync 0 | 런타임 `MgfLook.Quality()`가 다시 설정 |
| 프레임 | `Application.targetFrameRate = -1` | WebGL은 rAF로 돈다. 값을 주면 setTimeout 루프로 바뀌어 오히려 끊긴다 |

### 3.2 실측 (2026-09-26, Apple M4, 스모크 게임)

**빌드 시간**

| 상황 | 시간 |
|---|---|
| 워크스페이스 최초 생성(`setup-workspace.sh`) | 16초 |
| IL2CPP 캐시 없음(`Library/Bee` 삭제) 전체 빌드 | 88초 |
| C# 코드 변경 후 빌드(DiskSizeLTO) | 72~77초 |
| 코드 그대로·템플릿/에셋만 변경 | 7~13초 |
| 컴파일 에러 보고까지 | 4초 |

**코드 최적화 × 크기 (무압축 wasm / 빌드 시간)**

| codeOptimization | wasm | 빌드 |
|---|---|---|
| BuildTimes (Unity 기본값) | 21.9MB | 47초 |
| RuntimeSpeedLTO | 23.3MB | 120초 |
| DiskSize | 20.1MB | 60초 |
| **DiskSizeLTO (채택)** | **17.9MB** | 77~84초 |

**압축 × 전송량 × 로딩 (load-probe, 390×844, 빈 캐시)**

| 압축 | Build 합계 | 로컬 ready | 20Mbps ready | 10Mbps ready |
|---|---|---|---|---|
| Disabled | 23.8MB | 0.3~0.6초 | 18.2초 | — |
| **gzip + fallback (채택)** | **7.9MB** (wasm 6.1 · data 1.7 · js 0.1) | 0.5~0.8초 | 4.2초 | 7.5초 |

> 원래 지시는 "압축 Disabled(헤더 없이 뜨게)"였다. gzip + `decompressionFallback`도 **헤더 없이 뜬다**(로더가 JS로 푼다) —
> 같은 목적을 지키면서 전송량이 1/3이고, QA `static.assets`(게임 폴더 12MB 미만, 비치명)를 통과한다.
> Disabled는 Vercel이 `.wasm`을 전송 압축해 주는지에 기대야 하고 실측하지 않았다. 되돌리려면 `MGF_UNITY_COMPRESSION=none`.
> 대가: wasm 스트리밍 컴파일을 못 쓴다(해제 뒤 인스턴스화). M4에서는 차이를 측정할 수 없었다.

---

## 4. 게임 에이전트(codex/claude)가 지킬 규칙

1. **씬은 코드로 만든다(부트스트랩 방식).** `Awake`에서 카메라·빛·오브젝트·UI를 전부 생성한다. `.unity`/`.prefab` YAML을
   손으로 쓰거나 고치지 마라. 에디터 GUI를 열지 마라(배치 빌드가 막힌다 — 종료 코드 4).
2. **재질은 `MgfLook`으로.** `Shader.Find("Standard")`·`new Material(Shader.Find(...))`는 WebGL 빌드에서 셰이더가 스트리핑돼
   분홍/검정 화면이 된다(Coral Rescue 첫 빌드 실사고). `MgfLook.Lit(color, smoothness, metallic, emission, tex)`,
   `Unlit`, `Alpha`, `Additive`, `Sky(top, horizon, bottom)`, `Sun(euler, color)`, `Camera(pos, lookAt, fov)`,
   `Block(...)`(모서리 둥근 상자 — 프리미티브 큐브보다 훨씬 덜 시제품 같다), `RoundedBox`, `SoftDot`.
   직접 셰이더를 쓰면 `Resources/<Slug>/`에 두고 `Resources.Load<Shader>`로 불러라. `Lit`·`Unlit`은 정점색을 쓰지 않는다
   (정점색 없는 메시가 검게 나오는 사고 방지). `Alpha`·`Additive`는 정점색을 곱한다(파티클·LineRenderer용).
3. **입력은 `MgfPointer`로.** `Down/Held/Up/Position`, `DownHit(cam, out hit)`(콜라이더 필요), `OnPlane(cam, y, out p)`.
   QA `input.real`은 브라우저가 만든 진짜 마우스/터치 이벤트로 캔버스 격자(가로 30·50·70%, 세로 32·52·72%)를 누른다 —
   **그 영역에 반응하는 오브젝트가 있어야** 하고, 반응은 상태(`StateJson`) 또는 화면 변화로 드러나야 한다.
   `TestAnswerCorrect()`가 실제 입력 경로와 다른 함수를 부르면 검수에서 감점된다.
4. **한국어 텍스트는 `MgfText`로.** `MgfText.World(text, pos, size, color, parent)`(3D), `MgfText.Ui(text, anchor, offset, size, color)`
   (화면 UI, 390×844 기준 스케일). 킷 폰트는 NotoSansKR Bold 서브셋(**KS X 1001 한글 2350자** + ASCII + 수학 기호 `×÷−±≤≥≠√π∠△□○` 등,
   330KB, SIL OFL 1.1 — `MgfKR-OFL.txt` 동봉, 예약 이름 "Source" 미사용). 처음 쓰는 글자는 그 프레임에 SDF를 굽느라 잠깐 멈추니
   부팅 때 `MgfText.Prewarm("…")`. 2350자 밖의 글자(예: 똠·햏 같은 희귀 음절)는 □로 나온다 — 필요하면
   `python3 -m fontTools.subset <NotoSansKR-Bold.otf> --text-file=…`로 킷 폰트를 다시 만들어라(원본: `~/UnityProjects/SeoksoeSal/Assets/Resources/Fonts/`).
   분수·거듭제곱처럼 조판이 필요한 식은 TMP 리치 텍스트(`<sup>`, `<sub>`, `<size>`)나 메시로 직접 그려라(KaTeX는 이 트랙에서 못 쓴다).
5. **연출은 `MgfFx`·`MgfSfx`로.** `MgfFx.Punch(t)`, `Shake(cam)`, `Burst(pos, color)`, `Glow(pos, color)` — 파티클 시스템 1개를
   `Emit`으로 재사용한다. `MgfSfx.Play("tap|pop|correct|wrong|win|lose|whoosh")` — 합성음, 에셋 불필요.
6. **3D 모델 조달(선택).** 기본은 코드 메시(`MgfLook.Block`, 프리미티브 조합, 절차적 메시). 더 필요하면
   Blender MCP로 모델링 → FBX, 또는 이미지 생성 → Meshy 등 이미지→3D → Blender에서 최적화(삼각형 3~5만 이하, 텍스처 1~2K) → FBX를
   `Resources/<Slug>/`에. 유료 생성은 비용·원본을 `ArtSource/`에 기록. Coral Rescue 워크플로(`/Users/sitpo/dev/unity-connection-test/Workflow/StreetLab/`)의 함정을 먼저 읽어라.
7. **카메라는 종횡비에 맞춰라.** 캔버스는 항상 전체 화면이다(세로 390×844 ~ 초와이드 2000×1045). `MgfLook.FitWidth(cam, baseFov, minAspect)`를
   `Update`에서 부르면 세로 화면에서도 가로 폭이 잘리지 않는다.
8. **HTML을 고치지 마라.** `index.html`은 빌드 산출물(템플릿)이다. 로딩 화면·음소거·훅은 킷이 맡는다. 게임 UI는 Unity 안에서.
9. **성능 예산 (모바일 390×844 기준):** 드로콜 150 이하, 삼각형 20만 이하, 실시간 그림자 빛 1개(태양), 추가 픽셀 라이트 ≤3,
   텍스처 합계 16MB 이하(1~2K), 후처리 없음(내장 파이프라인·HDR 끔), `Update`에서 `new Material`/`new Mesh`/`Instantiate` 반복 금지
   (풀링), `GameObject.Find` 금지, 문자열 조립은 바뀔 때만(`TMP.text` 매 프레임 대입 금지). 빌드 합계(gzip) 12MB 이하.
   QA `perf.fpsdecay`(15초 뒤 60% 유지)를 떨어뜨리는 것은 쌓이는 오브젝트·파티클 시스템·코루틴이다.
10. **실패 상태 + 무뇌 게이트, 하단 n지선다 금지** — CLAUDE.md 하드 불변 그대로. 3D 공간의 선택 오브젝트도 "보기 3개를 누르는"
    구조면 같은 규칙에 걸린다(스모크 게임은 인프라 검증용이라 예외 — 따라 하지 마라).

---

## 5. 스모크 QA 결과 (2026-09-26)

`factory/unity/smoke`(5-1-1 혼합 계산, 둥근 블록 3개 + 그림자 + TMP 한글 + 파티클)를 `public/g/unity-smoke/`로 빌드해
`node factory/lib/qa.mjs unity-smoke` 실행. 확인 뒤 `public/g/unity-smoke/`는 삭제했다.

| 항목 | 결과 |
|---|---|
| 총계 | **44/44 통과 · 치명 0** (실 GPU: ANGLE Metal, Apple M4) |
| `run.ready` | 로컬 0.3~0.8초 (qa.mjs 대기 45초/20초 변경 불필요) |
| `math.*` | 호출별 40→40, 17→17, 63→63 · 다양성 89~95% |
| `input.real` | mouse · 4번째 격자 제스처에서 감지 (진짜 pointer 이벤트 → Unity `Input` → 레이캐스트) |
| `perf.fps` / `perf.fps1280` | 60~82fps / 72~85fps |
| `perf.fpsdecay` | 유지율 85~132% |
| 콘솔 에러 · 실패 요청 | 0 · 0 |
| `static.assets` | 11.9MB (이 중 Unity 산출물 7.9MB, 나머지는 임시 표지 PNG) |
| swiftshader 강제(`QA_FORCE_SWIFTSHADER=1`) | 44/44 · 치명 0 · 48fps / 35fps (저사양 모드 작동). 저사양 모드가 없을 때는 8fps로 치명 |
| 첫플레이 하네스(swiftshader) | `readyOk=true` · 콘솔 에러 0 · 캔버스 탭·드래그 동작 |

---

## 6. 알려진 함정

- **셰이더 스트리핑:** `Shader.Find`로 찾는 내장 셰이더는 빌드에 없을 수 있다 → 킷 셰이더(Resources)만 써라.
- **`Application.targetFrameRate`를 60으로 두지 마라**(WebGL에서 rAF 대신 setTimeout 루프). 킷이 -1로 둔다.
- **puppeteer `setViewport`가 isMobile을 바꾸면 페이지가 새로 고쳐진다.** QA의 1280/1440/2000 캡처는 다시 로드된 Unity(타이틀)다.
  Unity는 로딩이 1초 안쪽이라 문제없지만, `desktop*.png`에 게임 화면이 안 나온다고 버그로 보지 마라.
- **워크스페이스는 하나다.** Unity 빌드 두 개를 동시에 돌리지 마라. 에디터 GUI로 열어 두면 빌드가 종료 코드 4로 막힌다.
- **`Assets/MgfKit`·`Assets/WebGLTemplates`를 워크스페이스에서 고치면 다음 빌드 때 덮어써진다.** 정본은 `factory/unity/kit/`.
- **TMP 컴포넌트는 `AddComponent` 즉시 기본 폰트를 잡는다** — 기본 폰트가 킷 한글 폰트라 경고가 없다. `Assets/TextMesh Pro/Resources/Fonts & Materials`를
  되살리면 LiberationSans(영문 전용)가 섞인다.
- **JsonUtility**는 최상위 배열·Dictionary·프로퍼티를 직렬화하지 못한다. 상태는 `[Serializable]` 클래스의 public 필드로.
  `double.NaN`은 `MgfJson.Bank`가 `null`로 바꾼다.
- **Unity 로더의 `console.error` 두 줄**(wasm 스트리밍 MIME) — 템플릿이 경고로 낮춘다(2.3). 그 외 `console.error`는 QA 치명이다.
  C#의 `Debug.LogError`/예외도 `console.error`로 나간다.
- **로딩 시간:** 로컬은 1초 미만이라 qa.mjs·첫플레이 하네스(goto 45초·ready 20초)에 여유가 크다. 실사용자는 20Mbps에서 약 4초, 10Mbps에서 약 7.5초.
- **저사양 모드는 첫플레이 캡처에 나온다.** 하네스가 swiftshader로 띄우므로 MSAA 없음·낮은 그림자 해상도·낮은 렌더 해상도로 찍힌다.
  비주얼 판정은 qa.mjs의 `mobile.png`(실 GPU)도 함께 봐라.
- **Unity 버전은 6000.3.24f1 고정**(`MGF_UNITY_VERSION`). 6000.3.25f1 업그레이드 알림이 떠도 워크스페이스·문서·실측을 함께 갱신하기 전엔 올리지 마라.
