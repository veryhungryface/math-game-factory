# 초등 수학 게임 공장 (math-game-factory)

**하루 1작을 목표로 크론이 2시간마다 생산을 시도한다(성공하면 그날은 종료).** 이 문서는 **모든 하위 에이전트가 반드시 지켜야 하는 계약**이다.

> 2026-09-06 체제 전환: 「틱마다 신작」을 폐기했다. 오늘(KST) 이미 게시한 게임이 있으면
> `factory/run.sh` 최상단 가드가 신규 생산을 건너뛴다. 하루 최대 12번 시도하되 첫 성공에서 멈춘다.
> 합격작이 없으면 그날 0개여도 된다 — 미검증을 통과시켜 생산량을 채우는 것이 더 나쁘다.
> 근거: `docs/gpt6-factory-audit-20260905.md` §5.

> **새 세션/새 모델로 이 공장을 이어받았다면 가장 먼저 읽어라:**
> 1. `docs/OPERATIONS.md` — 운영 매뉴얼 (크론·에이전트 CLI 규약·게이트·핫픽스/부활 절차·함정 목록)
> 2. `docs/loop-engineering.md` — 이 공장의 5겹 피드백 루프(L0~L4)·횡단 원칙·모델 티어링·루프 설계 체크리스트. **게이트를 손대거나 새 자동화를 붙이기 전에 필독**
> 3. `factory/state/HANDOVER.md` — 현재 상태·진행 중 작업·백로그 (**작업 상태가 바뀌면 갱신·커밋할 것**)
> 4. `docs/design-bible.html` + `docs/title-screen-spec.md` — 디자인 철학·타이틀 규격
> 5. `docs/character-bible.md` — **고정 캐스트 「자눈 측량대」 5인 정본** (2026-09-06 확정). 에셋은 `public/vendor/cast/` + `manifest.json`. 캐릭터를 새로 발명하지 마라 — 캐스팅해라

## 저장소 구조

```
curriculum/2022-elementary-math.json   교육과정 원본 (수정 금지, 사람만 갱신)
curriculum/textbook-structure.json     교과서·익힘책 구조 (16권 전수: 단원·차시·문제 유형·발문 스타일)
curriculum/textbook-crosswalk.json     units 24개 ↔ 교과서 권·단원 매핑
curriculum/sources/                    교과서 원문 (저작권 — .gitignore. 로컬에만 존재)
references/game-references.json        레퍼런스 게임/메커닉 광산
factory/                               생산 하네스 (파이프라인 코드)
  run.sh                               1회 생산 사이클 진입점 (cron이 호출)
  prompts/*.md                         각 단계 에이전트 프롬프트
  lib/*.mjs                            슬롯 선택 / 허브 빌드 / QA / 리포트
  state/queue.json                     생산 이력 + 다음 슬롯
public/                                Vercel 정적 배포 루트
  index.html                           허브 (자동 생성 — 직접 수정 금지)
  catalog.json                         카탈로그 (자동 생성 — 직접 수정 금지)
  vendor/                              공용 라이브러리 (three.module.js 등)
  g/<slug>/                            게임 1개 = 폴더 1개
```

## 게임 폴더 계약 (하드 요구사항)

`public/g/<slug>/` 안에 반드시:

| 파일 | 필수 | 설명 |
|---|---|---|
| `index.html` | ✅ | 자기완결형 플레이 가능 게임. 외부 CDN 금지 |
| `meta.json` | ✅ | 아래 스키마 준수 |
| `thumb.png` | ✅ | 1200×630 허브 카드용 가로 이미지 |
| `square.png` | ✅ | 1080×1080 정사각 공유용 이미지 — 디스코드 등에 게시할 때 쓴다 |
| `assets/` | ⬜ | 생성 이미지·사운드. 경로는 **상대경로만** |

### 절대 규칙

1. **외부 CDN 금지.** `unpkg`, `jsdelivr`, `cdnjs`, Google Fonts 전부 금지. three.js가 필요하면 아래 vendor 방식을 쓴다. 폰트의 기본값은 시스템 폰트 스택(`-apple-system, "Apple SD Gothic Neo", "Malgun Gothic", sans-serif`)이다. 정체성이 다른 서체를 요구하면 **게임 폴더 안의 서브셋 `woff2`**(`factory/lib/subset-font.sh`)를 쓸 수 있다 — 금지되는 것은 외부 호스팅(CDN·Google Fonts)이지 웹폰트 자체가 아니다. 단 서브셋이 그 자리에 나올 글자를 전부 덮어야 하고 시스템 폴백 스택을 붙여야 한다(`docs/title-screen-spec.md` §1.4-4).
2. **한 폴더 안에서 자기완결.** 다른 게임 폴더를 참조하지 않는다.
3. **경로는 상대경로.** `/assets/x.png`(절대) ❌ → `./assets/x.png` ✅. 배포 경로가 `/g/<slug>/`이기 때문이다.
4. **모바일 우선.** 390×844(iPhone)와 820×1180(iPad)에서 가로 스크롤이 생기면 안 된다. 터치 타깃 44px 이상. `touch-action`, `user-select`, `-webkit-tap-highlight-color` 처리 필수.
5. **콘솔 에러 0개.** 404 네트워크 요청 0개.
6. **60fps 목표, 30fps 미만이면 탈락. 그리고 오래 돌려도 유지돼야 한다** — QA 가 15초 방치 후
   저하율도 잰다(앞 구간의 60% 미만이면 탈락). 매 프레임 재생성되는 `Image`·오프스크린 캔버스·
   DOM 노드, 지워지지 않는 파티클 배열·리스너가 전형적 원인이다.
7. **`window.__GAME_TEST__` 훅을 반드시 노출한다** (아래 참조). QA가 이걸로 자동 플레이한다.
8. **한국어 UI.** **이 게임의 슬롯 학년**(`factory/work/slot.json` 의 `unit.grade`)이 읽는 문장.
   학년은 고정값이 아니라 슬롯이 정한다 — 3학년 슬롯이면 3학년 어휘다. 어려운 한자어 금지.
9. **오디오는 사용자 제스처 이후에만.** 자동재생 금지. 음소거 버튼 필수.
10. **정답이 틀리면 안 된다.** 문제 생성기의 정답은 수학적으로 100% 정확해야 한다. 부동소수점 비교 금지 — 분수는 분자/분모 정수로 다뤄라.

## ⚠️ 한국 교과서 표현 함정 (요약 — 3줄)

1. **어림 발문은 「올림/버림/반올림하여 ○의 자리까지」 형식으로만 쓴다.**
   「○의 자리에서 올림/버림/반올림」은 아이스크림 2022 개정 교과서 16권 전수 확인에서 **0회**이고,
   3·4학년에서는 같은 말이 **곱셈의 자리 올림(carry)** 을 뜻해 의미가 정반대로 읽힌다.
   두 형식은 자릿수도 한 자리 어긋난다 — 225 를 「반올림하여 십의 자리까지」는 **230**,
   「십의 자리에서 반올림」은 **200**이다. `35-mathcheck` 가 정규식으로 자동 검출해 fail 시킨다.
2. **이상/이하는 경계 포함, 초과/미만은 경계 제외.** 선택지에 경계값을 넣어 이 차이를 시험해라.
3. **답 형식을 문장에 명시해라.** 「약분하시오」와 「계산하시오」는 요구가 다르다 —
   기약분수·대분수·소수 몇째 자리까지를 원하면 발문에 써라. 안 쓰면 정답이 여러 개가 된다.

어림 문제를 만들 때는 문장 생성과 정답 계산이 **같은 데이터 모델**에서 나오게 해라
(문자열로 뭉뚱그리면 이 함정에 빠진다):

```js
{ roundingMode: 'ceil'|'floor'|'halfUp',
  decisionPlace: 1,   // 어느 자리 숫자를 보고 판단하는가 (십의 자리=1)
  retainedPlace: 2,   // 결과가 어느 자리까지 남는가 (백의 자리=2)
  promptConvention: 'TO_PLACE' }   // AT_PLACE 는 발문으로 쓰지 않는다 (모델에는 남겨 둔다)
```

> **표현 관습의 정본은 `curriculum/textbook-structure.json` 의 `style_guide` · `expression_traps`
> 와 단원별 `textbook_caveats` 다.** 문제 생성기를 짜기 전에 반드시 열어라 —
> 소수 자리 어림(「반올림하여 소수 둘째 자리까지」), 상황어가 강제하는 어림 방법
> (「남김없이 모두 담으려면」=올림 / 「만들 수 있는 만큼만」=버림), 단위·기호 표기가 전부 거기 있다.
> `curriculum/textbook-crosswalk.json` 이 `unit_id` ↔ 교과서 권·단원·차시를 잇는다.
> (원문 md 는 저작권 때문에 미커밋 — 로컬 `curriculum/sources/textbook-icecream/` 에만 있다.)

### three.js 사용법 (vendor)

`public/vendor/` 에 three.js가 이미 들어 있다. 애드온(`OrbitControls` 등)은 `'three'` 를 bare specifier로 import 하므로 **import map이 필수**다. 아래를 그대로 써라.

```html
<script type="importmap">
{
  "imports": {
    "three": "../../vendor/three.module.js",
    "three/addons/": "../../vendor/addons/"
  }
}
</script>
<script type="module">
  import * as THREE from 'three';
  import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
  // ...
</script>
```

- `three.module.js` 는 옆의 `three.core.js` 를 상대경로로 가져간다. 둘 다 있어야 한다.
- 보유 애드온: `addons/controls/OrbitControls.js`, `addons/loaders/GLTFLoader.js`(+의존 `addons/utils/BufferGeometryUtils.js`·`SkeletonUtils.js`, 2026-09-08 추가 — 그 전 게임 4작은 각자 `assets/lib/`에 사본을 동봉해 우회했다). 그 외 애드온은 없으니 **직접 구현**하거나 쓰지 마라.
- import map은 `<script type="module">` **보다 먼저** 나와야 한다.
- 3D 게임은 `renderer.setPixelRatio(Math.min(devicePixelRatio, 2))` 로 모바일 성능을 지켜라.

### KaTeX 사용법 (vendor) — 수식을 진짜 수식처럼 렌더링할 때

분수·소수·식 표현을 캔버스 `fillText` 로 밋밋하게 그리지 말고, `public/vendor/katex/`
를 써서 실제 수학 조판으로 렌더링해라. 특히 분수·대분수·거듭제곱처럼 plain text로
표현하기 어색한 게임에 강력 추천한다.

```html
<link rel="stylesheet" href="../../vendor/katex/katex.min.css">
<script src="../../vendor/katex/katex.min.js"></script>
```
(`type="module"` 아닌 일반 스크립트. 로드되면 전역 `window.katex` 가 생긴다.)

**패턴 — 캔버스 위에 DOM 오버레이로 얹기** (캔버스 안에 직접 그릴 방법은 없다):
```js
const layer = document.createElement('div');
layer.style.cssText = 'position:absolute;inset:0;pointer-events:none;overflow:hidden';
document.body.appendChild(layer);

function makeMathEl(tex, fontPx) {
  const el = document.createElement('div');
  el.style.cssText = `position:absolute;font-size:${fontPx}px;will-change:transform`;
  katex.render(tex, el, { throwOnError: false });
  layer.appendChild(el);
  return el;
}
// 매 프레임: el.style.transform = `translate(${x}px,${y}px) scale(${s})`; 만 갱신
// katex.render() 는 텍스트가 바뀔 때만 다시 호출해라 — 매 프레임 호출하면 느려진다(60fps 못 지킴).
```

- `\times`(곱셈), `\frac{a}{b}`(분수), `\div`(나눗셈) 등 LaTeX 문법을 그대로 쓴다. "8.2×4" 같은
  plain text는 "8.2 \\times 4" 로 변환해라.
- 한글 단위("마리", "cm" 등)는 LaTeX 문자열 밖에 별도 텍스트로 둬라 — 수식 안에 한글을 억지로
  넣지 마라.
- `{throwOnError:false}` 필수 — 변환 실수로 깨진 LaTeX가 게임 전체를 죽이면 안 된다.
- 폰트는 `public/vendor/katex/fonts/*.woff2` 만 들어 있다(용량 절약). 최신 브라우저는
  woff2만 요청하므로 문제없다 — **이 벤더 폴더 자체는 건드리지 마라.**
- 비활성/화면 밖 슬롯의 수식 DOM 엘리먼트는 반드시 숨기거나 제거해라. 안 그러면 쌓여서
  성능이 떨어진다.

### meta.json 스키마

```json
{
  "slug": "fraction-crossing",
  "title": "분수 크로싱",
  "tagline": "12자 이내의 후킹 문구",
  "grade": 5,
  "semester": 2,
  "unit": { "id": "g5s2-u2", "order": 2, "title": "분수의 곱셈" },
  "standards": ["[6수01-05]"],
  "concepts": ["진분수의 곱셈", "약분"],
  "misconceptions_targeted": ["분모끼리 더한다"],
  "mechanic": "hopper",
  "mechanic_origin": "Crossy Road",
  "tech": ["canvas2d"],
  "description": "2~3문장 소개",
  "howto": ["방향키 또는 화면 탭으로 이동", "정답 통나무만 밟아라"],
  "playtime_min": 5,
  "difficulty": 3,
  "thumb": "thumb.png",
  "thumb_square": "square.png",
  "created_at": "2026-08-17T23:00:00+09:00",
  "version": 1,
  "qa": { "score": 0, "gate": 80, "passed": false, "reviewed_at": "", "notes": [] }
}
```

`standards` 안의 코드는 반드시 `curriculum/2022-elementary-math.json`에 실재하는 코드여야 한다. QA가 대조 검사한다.

### window.__GAME_TEST__ 훅 (필수)

게임 로드 완료 시 다음을 노출한다. QA 자동화가 이것 없이는 통과할 수 없다.

```js
window.__GAME_TEST__ = {
  ready: true,                    // 초기화 완료 플래그
  start() {},                     // 인트로 스킵하고 즉시 플레이 시작
  getState() {                    // 현재 상태
    return { score: 0, lives: 3, level: 1, phase: "playing", solved: 0 };
  },
  answerCorrect() {},             // 정답을 강제로 처리 (점수 증가 검증용)
  answerWrong() {},               // 오답을 강제로 처리
  // 선택: 범용 pointer 제스처로 닿지 않는 특수 조작만. 반드시 "실제 이벤트"로 재현할 것.
  // 내부 판정 함수를 직접 부르면 실입력 게이트를 무력화하는 것이라 검수에서 감점된다.
  simulateInput() {},
  sampleProblems(n) {             // 문제 생성기에서 n개 표본 추출 (게임 진행과 무관)
                                  // QA 는 n=40/17/63 으로 세 번 부른다. n 을 존중해라.
    return [{
      id: "p1",
      prompt: "3/4 × 2/3 = ?",    // 학생에게 보이는 문제 문장
      choices: ["1/2", "5/7", "6/7", "2/3"],  // 객관식이면 배열, 아니면 null
      answer: "1/2",              // 정답 (choices 안에 반드시 존재)
      answerNumeric: 0.5,         // 수치로 환산한 정답 (검증용)
      unitConcept: "진분수의 곱셈"
    }];
  }
};
```

## 계약 다이어트 — 하드 불변 5 (2026-09-07)

2026-09-06 해방 실험에서 **계약 없이 자유롭게 만든 빌드 3작이 정규 빌드를 블라인드 매력
평가 3-0(8.9 vs 6.5)으로 이겼고, 감점 사유는 전부 계약 강제 항목**이었다(캐스팅 의무·HUD
문법표·화면 요소 나열식 강제). 동시에 자유 빌드가 실제로 망가진 지점은 **실패 상태 부재 ·
3지선다 · fps 저하** 셋뿐이었다 — 그건 우리 게이트가 옳았다는 증거다.
자발 준수율 100%였던 항목(AT_PLACE 회피 · 정수 연산 · CDN 금지 · 한국어 · 오디오 제스처)은
강제하지 않아도 지켜지므로 프롬프트에서 뺐다.

그래서 **예외 없는 규칙은 아래 5개뿐**이고(정본: `factory/prompts/_invariants.md`),
나머지는 전부 **결과 기준**이거나 검수 가점이다:

1. **규격 파일** — `index.html`·`meta.json`·`thumb.png`·`square.png` + 실재하는 성취기준 코드
2. **`window.__GAME_TEST__` 훅 6종**
3. **실패 상태 + 무뇌 게이트(학습 진도 기준)** — 무뇌 입력이 **첫 시도 정답률을 우연 수준 이상으로
   못 끌어올려야** 한다. **완주율은 기준이 아니다** — 완주했어도 첫 시도 정답률이 우연 수준이면 실격이다
   (구 기준 「완주율 25% 이하」를 대체한다)
4. **조작 = 수학 판단 직결** — 하단 n지선다 버튼 금지(**3지선다 포함**)
5. **외부 CDN 금지 · 상대경로**

디자인은 나열식 문법표 대신 **결과 기준 한 문장**으로 판정한다:
> 화면 전체가 하나의 일관된 디자인 시스템으로 보여야 하고, 카탈로그의 어느 게시작과도 다른 시스템이어야 한다.

**캐스트(자눈 측량대 5인)는 선택이다** — 게임 세계가 캐스트와 맞을 때만 쓰고, 안 쓴다고 감점하지 않는다.
쓰기로 했으면 `docs/character-bible.md` 의 톤·시그니처 색을 지켜라. 새 캐릭터 발명은 여전히 금지다.

## 품질 게이트

**수동 게시 예외(2026-09-07):** 사용자가 점수 미달 게시를 명시적으로 지시한 경우만
`publish-game.mjs --manual-approval '<사유>'`를 쓴다. 실제 점수와 `qa.passed=false`를 보존하고
`qa.manual_release`에 별도 기록한다. 자동 생산의 80점 게이트는 유지한다. 상세는 OPERATIONS §5.

`factory/lib/qa.mjs`가 자동 검사 → `factory/prompts/40-review.md` 에이전트가 채점.
**총점 80점 미만이면 게시하지 않는다.** 미달 시 "수정→재QA→재첫플레이→재검산→재검수" 루프를 **최대 3회**(`MAX_FIX_ROUNDS`) 돌리고, 그래도 미달이면 폐기하고 리포트만 보낸다. 게이트 자체는 불변이다.

게시 조건은 점수 하나가 아니다 (2026-09-06 P0-3 — run.sh 가 집행한다):

| 조건 | 통과 기준 |
|---|---|
| 검수 | `passed: true` · `reject_immediately: false` |
| 자동 QA | 치명적 결함 0 (종료코드 0) |
| 독립 수학 검산 | `verdict: "pass"` — **`unknown`(산출물 누락)은 통과가 아니다** |
| 미해결 지적 | `must_fix` 중 `severity: "high"` **0건** |
| 첫 플레이 증거 | 호스트가 캡처한 프레임이 있어야 한다. 없으면 「미검증」 → 게시 보류 |

수정 최대 3회는 **비용 상한**이지 품질 보증이 아니다. 다음 검사에 들어가려면 러너의 정상 종료·**실제 산출물 변경**(호스트가 해시로 확인)·지적별 해결 증거가 있어야 한다. 러너가 402/인증/쿼터로 죽거나 아무것도 안 고쳤으면 러너를 1회 대체하고, 그래도 실패면 **인프라 실패**로 회차를 즉시 중단한다 — 안 고친 코드를 다시 채점하지 않는다.

배점: 교육과정 정합성 25 / 수학 정확성 25 / 재미(게임성) 20 / 비주얼 20 / 모바일·성능 10

`qa.mjs` 는 44개 이진 검사이고 그중 약 18~20개가 `fatal`(하나라도 실패하면 즉시 탈락)이다.
개수는 매 실행 고정이 아니다 — 조기 종료·캔버스 유무·동적 FPS 판정 때문에 갈리므로,
**숫자를 외우지 말고 `report.json` 의 `total`/`fatal` 을 읽어라.**

- **`input.real`** — `answerCorrect()` 를 우회해 **진짜 pointer 이벤트**로 게임이 반응하는지 본다.
  터치가 죽은 게임이 훅 덕분에 통과한 사고에서 나왔다.
- **`perf.fps` / `perf.fps1280`** — 1.5초 표본 3회의 중앙값. **2026-09-07부터 실 GPU 측정이 정본**이다:
  헤드리스 크롬을 플래그 없이 띄워 ANGLE/Metal 을 잡으면 그 수치를 그대로 쓰고,
  실 GPU 를 못 잡을 때만 swiftshader 로 폴백해 머신 여유 보정을 적용한다(보정값도 30 미만이면 fatal).
  `report.render.mode` 가 `gpu` 인지 `swiftshader` 인지 알려 준다.
- **`perf.fpsdecay`** — 15초 방치 후 뒤 구간이 앞 구간의 60% 미만이면 fatal.
  "처음엔 59fps, 30초 뒤엔 16fps" 인 게임을 한 번의 중앙값으로는 못 잡아서 만든 항목이다.
  누적되는 DOM·리스너·파티클 배열, 매 프레임 재생성되는 `Image`/오프스크린 캔버스가 전형적 원인이다.

자세한 내용은 `docs/loop-engineering.md` 의 "자동 QA는 44개 이진 검사다" 절.

## 하위 에이전트 실행 규칙

- LLM 호출은 **구독 CLI**로 한다: `claude -p`, `codex exec`. `ANTHROPIC_API_KEY`는 unset 한다.
- 이미지 생성은 `codex exec`의 이미지 생성 툴을 쓴다 (검증됨, 1장 ~60초).
- 병렬 작업은 서로 다른 파일만 건드린다. 같은 파일을 두 에이전트가 쓰면 안 된다.
- macOS에는 `timeout`이 없다. `gtimeout` 또는 백그라운드+wait 패턴을 써라.

## 하지 말 것

- 상표·캐릭터·에셋 복제 금지. **메커닉만 차용**하고 이름·아트는 오리지널로 만든다.
- `public/index.html`, `public/catalog.json` 직접 수정 금지 (빌드 산출물).
- `curriculum/` 직접 수정 금지.
