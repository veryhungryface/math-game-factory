# 초등 수학 게임 공장 (math-game-factory)

3시간마다 게임 1개를 자동 생산한다. 이 문서는 **모든 하위 에이전트가 반드시 지켜야 하는 계약**이다.

> **새 세션/새 모델로 이 공장을 이어받았다면 가장 먼저 읽어라:**
> 1. `docs/OPERATIONS.md` — 운영 매뉴얼 (크론·에이전트 CLI 규약·게이트·핫픽스/부활 절차·함정 목록)
> 2. `factory/state/HANDOVER.md` — 현재 상태·진행 중 작업·백로그 (**작업 상태가 바뀌면 갱신·커밋할 것**)
> 3. `docs/design-bible.html` + `docs/title-screen-spec.md` — 디자인 철학·타이틀 규격

## 저장소 구조

```
curriculum/2022-elementary-math.json   교육과정 원본 (수정 금지, 사람만 갱신)
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

1. **외부 CDN 금지.** `unpkg`, `jsdelivr`, `cdnjs`, Google Fonts 전부 금지. three.js가 필요하면 아래 vendor 방식을 쓴다. 폰트는 시스템 폰트 스택(`-apple-system, "Apple SD Gothic Neo", "Malgun Gothic", sans-serif`)을 쓴다.
2. **한 폴더 안에서 자기완결.** 다른 게임 폴더를 참조하지 않는다.
3. **경로는 상대경로.** `/assets/x.png`(절대) ❌ → `./assets/x.png` ✅. 배포 경로가 `/g/<slug>/`이기 때문이다.
4. **모바일 우선.** 390×844(iPhone)와 820×1180(iPad)에서 가로 스크롤이 생기면 안 된다. 터치 타깃 44px 이상. `touch-action`, `user-select`, `-webkit-tap-highlight-color` 처리 필수.
5. **콘솔 에러 0개.** 404 네트워크 요청 0개.
6. **60fps 목표, 30fps 미만이면 탈락.**
7. **`window.__GAME_TEST__` 훅을 반드시 노출한다** (아래 참조). QA가 이걸로 자동 플레이한다.
8. **한국어 UI.** 초등 5~6학년이 읽는 문장. 어려운 한자어 금지.
9. **오디오는 사용자 제스처 이후에만.** 자동재생 금지. 음소거 버튼 필수.
10. **정답이 틀리면 안 된다.** 문제 생성기의 정답은 수학적으로 100% 정확해야 한다. 부동소수점 비교 금지 — 분수는 분자/분모 정수로 다뤄라.

## ⚠️ 한국 교과서 표현 함정 (실제로 게임 하나를 폐기시킨 사례)

### 「○의 자리에서」 vs 「○의 자리까지」 — 완전히 다른 조작이다

| 표현 | 뜻 | 예: 225, 반올림 |
|---|---|---|
| **반올림하여 십의 자리까지** 나타내기 | 결과가 **십의 자리까지** 정확 | **230** |
| **십의 자리에서** 반올림하기 | **십의 자리 숫자를 보고** 판단 → 결과는 **백의 자리까지** | **200** |

즉 「○의 자리에서」는 그 자리 숫자를 기준으로 어림하고, **결과는 그 위 자리까지** 나타난다.

```
339을 십의 자리에서 올림하면?   → 400  (340 아니다)
739을 십의 자리에서 버림하면?   → 700  (730 아니다)
932를 십의 자리에서 올림하면?   → 1000 (940 아니다)
3400을 백의 자리에서 올림하면?  → 4000 (3400 아니다)
```

코드로는 이렇게 다르다 — 자릿수 인자가 **1 차이** 난다:
```js
const UP   = (n,p)=>{const m=10**p; return Math.ceil (n/m)*m;};
const DOWN = (n,p)=>{const m=10**p; return Math.floor(n/m)*m;};
const HALF = (n,p)=>{const m=10**p; return Math.round(n/m)*m;};

// 「반올림하여 십의 자리까지」 → p=1
HALF(225, 1) // 230
// 「십의 자리에서 반올림」     → p=2  (한 자리 위)
HALF(225, 2) // 200
```

**「○의 자리에서 어림한 결과가 원래 수와 같아지는」 예외 문제를 낼 때 주의:**
「백의 자리에서 올림」이 원래 값과 같으려면 백의 자리 **아래가 전부 0이어야** 한다(3000 ✅, 3400 ❌).

**구조적 해법 — 어림 문제 데이터 모델을 처음부터 이렇게 분리해라** (문자열로 뭉뚱그리다
이 함정에 빠진다):
```js
{ roundingMode: 'ceil'|'floor'|'halfUp',
  decisionPlace: 1,   // 어느 자리 숫자를 보고 판단하는가 (십의 자리=1)
  retainedPlace: 2,   // 결과가 어느 자리까지 남는가 (백의 자리=2)
  promptConvention: 'AT_PLACE'|'TO_PLACE' }  // 「~자리에서」 vs 「~하여 ~자리까지」
```
AT_PLACE 면 retainedPlace = decisionPlace + 1 이고, TO_PLACE 면 retainedPlace = 표기 자리.
문장 생성과 정답 계산이 같은 모델에서 나오게 하면 표현-수식 불일치가 원천 차단된다.

### 그 밖에 자주 틀리는 표현
- **이상/이하**는 경계 포함, **초과/미만**은 경계 제외. 선택지에 경계값을 넣어 이 차이를 시험해라.
- 「대략 몇백?」 같은 일상 표현은 보통 **반올림**을 뜻하지만, 문맥(자르기·포장·담기)에 따라 버림/올림이 강제된다. 문제 문장에 상황을 명시해라.
- 「약분하시오」와 「계산하시오」는 요구가 다르다. 기약분수를 요구할 거면 문장에 써라.

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
- 보유 애드온: `addons/controls/OrbitControls.js`, `addons/loaders/GLTFLoader.js`. 그 외 애드온은 없으니 **직접 구현**하거나 쓰지 마라.
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
  sampleProblems(n) {             // 문제 생성기에서 n개 표본 추출 (게임 진행과 무관)
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

## 품질 게이트

`factory/lib/qa.mjs`가 자동 검사 → `factory/prompts/40-review.md` 에이전트가 채점.
**총점 80점 미만이면 게시하지 않는다.** 1회 자동 수정 후 재검수, 그래도 미달이면 폐기하고 리포트만 보낸다.

배점: 교육과정 정합성 25 / 수학 정확성 25 / 재미(게임성) 20 / 비주얼 20 / 모바일·성능 10

## 하위 에이전트 실행 규칙

- LLM 호출은 **구독 CLI**로 한다: `claude -p`, `codex exec`. `ANTHROPIC_API_KEY`는 unset 한다.
- 이미지 생성은 `codex exec`의 이미지 생성 툴을 쓴다 (검증됨, 1장 ~60초).
- 병렬 작업은 서로 다른 파일만 건드린다. 같은 파일을 두 에이전트가 쓰면 안 된다.
- macOS에는 `timeout`이 없다. `gtimeout` 또는 백그라운드+wait 패턴을 써라.

## 하지 말 것

- 상표·캐릭터·에셋 복제 금지. **메커닉만 차용**하고 이름·아트는 오리지널로 만든다.
- `public/index.html`, `public/catalog.json` 직접 수정 금지 (빌드 산출물).
- `curriculum/` 직접 수정 금지.
