# 수학 게임 공장 타이틀 화면 디자인 시스템

> 적용 범위: 전 게임 공통 타이틀 화면  
> 기준 뷰포트: 모바일 세로 `390 × 844px`  
> 와이드 화면 콘텐츠 상한: 중앙 `640px` 컬럼  
> 핵심 원칙: **게임 세계가 먼저 보이고, 사용자가 할 일은 하나만 보인다.**

## 0. 디자인 목표

타이틀 화면은 설명 페이지가 아니라 게임 세계의 입구다. 화면을 열었을 때의 우선순위는 다음 한 문장으로 고정한다.

**키 아트 → 게임 로고 → 시작하기 → 필요한 경우에만 보조 정보**

첫 화면에는 게임 세계, 게임명, 단일 시작 행동만 강하게 노출한다. 기존 체크리스트의 정보는 삭제하지 않고 첫 실행 온보딩과 접이식 도움말로 이동한다.

**단, 「같은 공간 문법을 쓴다」는 옛 원칙은 2026-09-06 폐기됐다** — 그 문장이 24작을 형제로 만들었다. 아래 레이아웃 해부도는 **하나의 본보기**이고, 실제 구성은 게임마다 발명한다. 불변 규칙은 §7-A 에만 있다.

---

## 1. 레이아웃 해부도

### 1.1 공통 화면 골격

타이틀 루트는 `100dvh`를 채운다. 키 아트와 분위기 효과는 뷰포트 전체 폭을 사용하고, 텍스트와 버튼만 `max-width: 640px` 중앙 컬럼 안에 둔다.

```text
390 × 844 기준
┌──────────────────────────────┐ y=0
│  음소거 44×44          (우상단)│  시스템/안전 영역
│                              │
│      [로고 존 7%–31%]        │  중심 y ≈ 154
│       거대한 입체 로고         │
│          짧은 부제             │
│                              │
│  캐릭터/세계가 화면 좌우를 채움 │
│  ─ 중앙 시선 통로는 비워 둠 ─  │  키 아트 존: 화면 전체
│                              │
│                              │
│      [CTA 존 76%–96%]        │
│    상태 pill 또는 최고 기록     │  선택, 한 줄만
│       시작하기 346×68          │  유일한 강한 버튼
│      어떻게 놀아요? 〉          │  텍스트 링크
└──────────────────────────────┘ y=844
```

### 1.2 영역별 수치

| 영역 | 390×844 기준 | 와이드 규칙 | 역할 |
|---|---:|---|---|
| 풀블리드 키 아트 | `inset: 0`, 390×844 | 뷰포트 전체. 중앙 컬럼에 가두지 않음 | 세계관, 캐릭터, 깊이감 |
| 로고 존 | `top: max(54px, 7dvh)`, 높이 약 200px | 폭 `min(calc(100% - 32px), 600px)` | 로고와 한 줄 부제 |
| 로고 본문 | 권장 폭 330–358px, 44–64px 글자 | 최대 560px, 56–84px 글자 | 화면에서 가장 강한 문자 요소 |
| CTA 존 | `bottom: max(18px, env(safe-area-inset-bottom))` | 폭 `min(calc(100% - 32px), 520px)` | 상태 1줄, CTA 1개, 도움말 링크 |
| CTA | 폭 100%, 높이 68px | 최대 420px, 높이 72px | 유일한 주요 행동 |
| 음소거 | 우상단 12px, 44×44px | 콘텐츠 컬럼 우측선에 정렬 | 전역 보조 행동 |

`390×844`보다 낮은 화면에서는 로고를 먼저 축소하고(`clamp()`), CTA 높이는 64px 아래로 줄이지 않는다. 가로가 넓어져도 로고와 CTA 사이에 카드나 설명을 추가하지 않는다.

```css
.title-screen {
  --title-col: min(100vw, 640px);
  position: fixed;
  inset: 0;
  min-height: 100svh;
  height: 100dvh;
  overflow: hidden;
  isolation: isolate;
  background: #071020;
}

.title-ui {
  position: relative;
  z-index: 3;
  width: var(--title-col);
  height: 100%;
  margin-inline: auto;
  padding-inline: clamp(16px, 5vw, 32px);
  padding-top: env(safe-area-inset-top);
  padding-bottom: env(safe-area-inset-bottom);
  pointer-events: none;
}

.title-logo-zone {
  position: absolute;
  top: max(54px, 7dvh);
  left: 16px;
  right: 16px;
  min-height: 190px;
  display: grid;
  place-content: start center;
  text-align: center;
  pointer-events: auto;
}

.title-cta-zone {
  position: absolute;
  left: 50%;
  bottom: max(18px, env(safe-area-inset-bottom));
  width: min(calc(100% - 32px), 520px);
  transform: translateX(-50%);
  display: grid;
  justify-items: center;
  gap: 8px;
  pointer-events: auto;
}

@media (min-width: 641px) {
  .title-logo-zone { left: 24px; right: 24px; }
  .title-cta-zone { width: min(calc(100% - 48px), 520px); }
}

@media (max-height: 700px) {
  .title-logo-zone { top: max(38px, 5dvh); min-height: 150px; }
  .title-cta-zone { gap: 5px; }
}
```

### 1.3 온보딩 이동 규칙

타이틀 화면에서 체크리스트를 제거하되 내용은 다음 두 위치에 모두 보존한다.

1. **첫 실행:** `시작하기`를 누르면 타이틀이 240ms 동안 사라지고, 실제 플레이 장면 위에 3단계 스포트라이트 온보딩을 띄운다. 한 단계에는 행동 하나와 짧은 결과 하나만 쓴다. 사용자가 탭해 넘기며, 마지막 단계의 `해볼게요`로 게임을 시작한다. 자동으로 1.5초 뒤 사라지는 방식은 읽기 속도와 접근성 문제가 있으므로 사용하지 않는다.
2. **재방문:** 저장된 `onboardingSeen:<game-id>`가 있으면 CTA가 즉시 게임을 시작한다. CTA 아래의 `어떻게 놀아요? 〉` 텍스트 링크를 누르면 같은 3단계 안내를 언제든 다시 연다.

기존 3–4개 체크 항목은 의미 단위로 합쳐 3단계로 재편한다.

| 기존 정보 | 이동 위치 |
|---|---|
| 기본 조작 | 1단계: 조작 대상에 스포트라이트 |
| 선택/판단 규칙 | 2단계: 실제 선택지 위에 예시 표시 |
| 성공 조건/목표 | 3단계: 목표 HUD 또는 도착 지점 강조 |
| 특수 버튼·예외 규칙 | 해당 기능이 처음 등장할 때 1회 컨텍스트 팁. 도움말 전체 보기에도 수록 |

첫 화면에 남길 설명은 최대 한 줄, 24자 내외다. 이 한 줄조차 키 아트나 로고와 경쟁하면 생략한다. 온보딩은 체크박스 모양을 쓰지 않고, `1/3` 진행 표시와 `다음` 또는 화면 탭으로 진행한다.

---

## 2. 입체 한글 로고 CSS 레시피

> ⚠️ **먼저 §2.0 「로고 서체 다양화」를 읽어라.** 아래 §2.1–2.2 레시피를 그대로 복붙하는 것은
> **금지**다. 전 게임이 이 한 레시피를 복붙해 쓴 결과 "제목 폰트가 다 똑같다"는 실제 사용자
> 지적이 나왔다. 이 레시피는 **골격 예시**이지 기본값이 아니다.

### 2.0 로고 서체 다양화 (필수)

#### 왜

이 문서의 §2.2 레시피는 `font-family: "Black Han Sans", "Noto Sans KR", …` 로 시작하는데
**두 폰트 모두 저장소에 없다.** 웹폰트를 싣지 않으면 조용히 시스템 폰트로 폴백하므로,
코드 렌더 로고를 쓰는 게임이 전부 **같은 글자꼴 + 같은 그라디언트 + 같은 8px 압출**로
렌더된다. 색만 다를 뿐 자소 골격이 같으니 표지가 다 똑같아 보인다.

#### 단일 레시피 복붙 금지 조항

1. **`font-family` 를 그대로 복붙하지 마라.** 게임마다 `docs/title-typography-plan.md` 의
   배정표에서 지정한 폰트를 쓴다. 배정이 없는 신작은 배정표의 (폰트, 처리) 조합 중
   **아직 쓰이지 않은 쌍**을 골라 표에 추가하고 근거 한 줄을 남긴다.
2. **§2.2 의 8px 세로 압출 `text-shadow` 스택도 그대로 복붙하지 마라.** 배정표의
   처리(T1~T12: 각인 / 플랫 스티커 / 오프셋 / 점프 아치 / 세로 족자 / 원호 /
   이중 외곽선 / 텍스처 클리핑 / 슬랜트 / 계단 / 겹침)를 따른다. 압출(T1)은 전체에서
   2작까지만 허용한다.
3. **등장 모션도 갈라라.** 「위에서 낙하 + 오버슈트」가 전 게임 동일한 것도 천편일률의 일부다.
   각인은 찍히고, 점프는 아래에서 뛰어오르고, 등불은 켜진다.
4. **웹폰트는 「서브셋이 그 글자를 전부 덮을 때만」 쓴다.** 타이틀 로고·태그라인처럼 글자가
   고정된 곳이 가장 안전하다. 본문·UI·HUD·플레이 화면에 쓰려면 **거기 나올 수 있는 글자
   전부**(숫자·기호만 쓰는 HUD면 숫자·기호 서브셋으로 충분)를 서브셋에 넣고 시스템 폴백
   스택을 붙여라 — 덮지 못하면 한 문장 안에서 서체가 섞인다.
   (2026-09-06 정리: 옛 문장은 인게임 웹폰트를 금지했는데, 검수 4번은 인게임 서체 성격이
   없으면 감점한다. 금지가 아니라 **서브셋 커버리지 조건**이 규칙이다.)
5. **레이아웃·정보 위계·CTA 규격은 서체 때문에 흔들지 않는다.** 로고 존 좌표, CTA 68px,
   대비 4.5:1, 터치 44px 은 그대로다.

#### 서브셋 파이프라인

한글 폰트 원본은 풀셋 때문에 1~3MB다. 타이틀에서 실제 쓰는 글자만 뽑으면 **수 KB**로 떨어진다.
외부 CDN 금지 원칙을 지키면서 게임마다 다른 서체를 줄 수 있는 유일한 방법이다.

```bash
# 1회 준비
pip3 install --user fonttools brotli

# 게임별 서브셋 (제목 + 부제 + 이 서체로 렌더되는 문구만)
factory/lib/subset-font.sh \
  ~/fonts/DoHyeon-Regular.ttf \
  public/g/clang-stamp/assets/fonts/logo-dohyeon.woff2 \
  "꽝도장잘못찍으면찢어져 "
# → 1252 bytes
```

- 원본 TTF 는 **저장소에 커밋하지 않는다.** 서브셋 woff2 만 게임 폴더에 둔다(자기완결 원칙).
- **`OFL.txt` 를 `assets/fonts/` 에 같이 넣어라.** 라이선스 동봉은 OFL 1.1 의 요구사항이다.
- 폰트는 **OFL 또는 임베드 허용**만 쓴다. 확보한 8종과 출처는 `docs/title-typography-plan.md` §1.
- 서브셋 글자 목록에 **부제와 공백**을 빠뜨리지 마라. 빠진 글자만 시스템 폰트로 렌더된다.

#### 로딩 패턴

```css
/* 로고 전용 서체 — Do Hyeon (SIL OFL 1.1, ./assets/fonts/OFL-DoHyeon.txt) */
@font-face{
  font-family:"LogoDoHyeon";
  src:url("./assets/fonts/logo-dohyeon.woff2") format("woff2");
  font-weight:400; font-style:normal;
  font-display:swap;            /* 폰트가 늦어도 글자는 먼저 보인다 */
}
.game-logo{
  font-family:"LogoDoHyeon","Apple SD Gothic Neo","Malgun Gothic",sans-serif;
  font-weight:400;              /* 디스플레이 폰트는 대개 단일 웨이트다. 900 을 주면 가짜 볼드가 씌워진다 */
}
```

- 경로는 **상대경로**(`./assets/fonts/…`). 배포 경로가 `/g/<slug>/` 다.
- 시스템 폴백 스택을 반드시 붙여라. woff2 가 실패해도 게임이 읽혀야 한다.
- `font-weight` 를 원본 웨이트에 맞춰라. 단일 웨이트 폰트에 `900` 을 주면 브라우저가
  합성 볼드를 씌워 획이 뭉갠다.

#### 검증 (교체할 때마다)

```js
document.fonts.check('80px "LogoDoHyeon"')   // true 여야 한다
```

- `390×844` 와 `1280×800` 캡처를 **눈으로** 확인한다. 폴백으로 렌더돼도 화면은 멀쩡해 보이므로
  캡처만으로는 부족하다 — `document.fonts.check` 와 **자폭 비교**(웹폰트 vs 시스템 폴백의
  `measureText` 결과가 달라야 함)를 같이 본다.
- `woff2` 404 가 없어야 한다. `node factory/lib/qa.mjs <slug>` 의 실패 요청 0건으로 확인.

#### 서체별 함정 (파일럿 실측)

- **거친 손글씨·붓 계열은 자폭이 좁다.** East Sea Dokdo 는 약 `0.57em`. 고딕과 같은
  `font-size` 로 두면 글자가 절반 크기로 보인다. 서체를 바꾸면 **로고 본문 폭 330–358px
  (390 화면)** 를 다시 맞춰라(부록 A-1).
- **명조는 속공간이 좁다.** `-webkit-text-stroke` 5px 이상이면 `ㅇ`·`ㅁ` 안이 메워져
  글자가 덩어리가 된다. 먹선은 3px 안팎으로 묶고 판독은 바깥 림·글로우로 확보해라.
  8방향 하드 그림자로 테를 두르는 방식은 명조에서 금지다.
- **글자별 `--y` 오프셋은 px 가 아니라 em 으로.** 390 에서 맞춘 ±14px 이 1280 에서는
  과해져 글자끼리 부딪친다.
- **`animation-fill-mode:both` 에서 끝 키프레임에 `transform` 을 명시하지 마라.**
  `nth-child` 로 준 정적 transform 을 덮어쓴다. 끝에는 `opacity` 만 둔다.

### 2.1 마크업

각 음절을 `span`으로 분리한다. 줄바꿈은 게임 이름의 의미 단위에서만 허용하며, 2줄일 때 두 번째 줄은 첫 줄과 8–16% 겹쳐도 된다. 접근성 이름은 부모의 `aria-label`로 한 번만 제공하고 장식용 글자들은 숨긴다.

```html
<h1 class="game-logo" aria-label="젤리 게이트">
  <span aria-hidden="true" style="--r:-6deg;--y:7px;--s:1.02">젤</span>
  <span aria-hidden="true" style="--r:3deg;--y:-2px;--s:1.12">리</span>
  <span class="logo-gap" aria-hidden="true"></span>
  <span aria-hidden="true" style="--r:-3deg;--y:3px;--s:1.08">게</span>
  <span aria-hidden="true" style="--r:5deg;--y:-5px;--s:.98">이</span>
  <span aria-hidden="true" style="--r:-2deg;--y:2px;--s:1.10">트</span>
</h1>
<p class="game-subtitle">곱했는데 왜 줄어?</p>
```

### 2.2 복붙용 CSS

아래 레시피에서 게임별로 바꿀 값은 `--logo-top`, `--logo-mid`, `--logo-bottom`, `--logo-outline`, `--logo-side`, `--logo-side-dark`, `--logo-glow`뿐이다. 한 로고 안에서 색을 과도하게 늘리지 않는다.

```css
:root {
  --logo-top: #fff66a;
  --logo-mid: #ffc928;
  --logo-bottom: #ff8a19;
  --logo-outline: #48220d;
  --logo-side: #c84e13;
  --logo-side-dark: #70230f;
  --logo-glow: rgba(255, 198, 36, .42);
}

.game-logo {
  margin: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: nowrap;
  font-family: "Black Han Sans", "Noto Sans KR", "Apple SD Gothic Neo", sans-serif;
  font-size: clamp(46px, 13.4vw, 82px);
  font-weight: 900;
  line-height: .92;
  letter-spacing: -.10em;
  white-space: nowrap;
  filter: drop-shadow(0 16px 14px rgba(0, 0, 0, .34));
  transform: rotate(-1.5deg);
  transform-origin: 50% 60%;
}

.game-logo > span:not(.logo-gap) {
  --r: 0deg;
  --y: 0px;
  --s: 1;
  position: relative;
  display: inline-block;
  padding-inline: .025em;
  color: transparent;
  background: linear-gradient(
    180deg,
    var(--logo-top) 0%,
    var(--logo-mid) 48%,
    var(--logo-bottom) 100%
  );
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-stroke: clamp(2px, .75vw, 5px) var(--logo-outline);
  paint-order: stroke fill;

  /* 1–8px: 압출 측면, 10–18px: 바닥 그림자, 마지막: 색광 */
  text-shadow:
    0 1px 0 var(--logo-side),
    0 2px 0 var(--logo-side),
    0 3px 0 var(--logo-side),
    0 4px 0 var(--logo-side),
    0 5px 0 var(--logo-side-dark),
    0 6px 0 var(--logo-side-dark),
    0 7px 0 var(--logo-side-dark),
    0 8px 0 var(--logo-side-dark),
    0 11px 2px rgba(42, 13, 4, .60),
    0 16px 8px rgba(0, 0, 0, .46),
    0 22px 22px rgba(0, 0, 0, .34),
    0 0 26px var(--logo-glow);

  transform: translateY(var(--y)) rotate(var(--r)) scale(var(--s));
  transform-origin: 50% 80%;
  animation: logo-letter-in 720ms cubic-bezier(.18, 1.55, .34, 1) both;
  animation-delay: calc(var(--i, 0) * 65ms);
}

.game-logo > span:nth-child(1) { --i: 0; }
.game-logo > span:nth-child(2) { --i: 1; }
.game-logo > span:nth-child(3) { --i: 2; }
.game-logo > span:nth-child(4) { --i: 3; }
.game-logo > span:nth-child(5) { --i: 4; }
.game-logo > span:nth-child(6) { --i: 5; }

.game-logo .logo-gap {
  width: .22em;
  flex: 0 0 .22em;
}

.game-subtitle {
  margin: 15px 0 0;
  padding: 5px 13px 6px;
  border: 1px solid rgba(255, 255, 255, .34);
  border-radius: 999px;
  color: #fff8df;
  background: rgba(22, 13, 12, .46);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, .16), 0 5px 14px rgba(0, 0, 0, .24);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
  font: 800 clamp(12px, 3.5vw, 16px)/1.2 "Noto Sans KR", sans-serif;
  letter-spacing: -.02em;
  text-shadow: 0 2px 4px rgba(0, 0, 0, .72);
  animation: logo-subtitle-in 420ms 520ms ease-out both;
}

@keyframes logo-letter-in {
  0% {
    opacity: 0;
    transform: translateY(calc(var(--y) - 82px)) rotate(calc(var(--r) - 10deg)) scale(.55);
  }
  58% {
    opacity: 1;
    transform: translateY(calc(var(--y) + 8px)) rotate(calc(var(--r) + 3deg)) scale(calc(var(--s) * 1.10));
  }
  78% {
    transform: translateY(calc(var(--y) - 3px)) rotate(calc(var(--r) - 1deg)) scale(calc(var(--s) * .97));
  }
  100% {
    opacity: 1;
    transform: translateY(var(--y)) rotate(var(--r)) scale(var(--s));
  }
}

@keyframes logo-subtitle-in {
  from { opacity: 0; transform: translateY(-8px) scale(.92); }
  to   { opacity: 1; transform: translateY(0) scale(1); }
}

@media (prefers-reduced-motion: reduce) {
  .game-logo > span:not(.logo-gap),
  .game-subtitle { animation: none; }
}
```

#### 로고 제작 규칙

- 음절별 회전은 `-6deg`에서 `6deg`, 크기는 `.96`에서 `1.12`, 수직 이동은 `-6px`에서 `8px` 안에서만 변주한다.
- 첫 글자, 받침이 시각적으로 큰 글자, 핵심 단어의 첫 글자를 5–12% 크게 한다.
- 외곽선은 모바일에서 최소 2px, 보통 4px다. 키 아트 위에서 로고 윤곽이 사라지면 색을 바꾸기보다 외곽선을 먼저 굵게 한다.
- 압출 방향은 모든 글자에서 아래쪽으로 통일한다. 글자별 그림자 방향을 바꾸면 한 덩어리 로고로 보이지 않는다.
- 실제 HTML의 `nth-child`가 공백 span을 포함한다. 게임명이 더 길면 `--i`를 인라인으로 직접 지정하는 편이 안전하다.
- 폰트 로딩 전후 레이아웃 점프를 피하려면 굵은 한글 웹폰트를 로컬 제공하거나 사전 로드한다.

---

## 3. 단일 CTA 버튼 레시피

CTA 문구는 **게임 세계의 동사**를 쓴다(`공방 가동!`, `과수원 열기!`, `탐험 시작!`). 행동 의미가 즉시 이해되어야 한다는 것이 유일한 제약이고, 세계관 어휘가 떠오르지 않을 때의 폴백이 `시작하기`다 — 2026-09-06 정리 전에는 `시작하기`가 기본값이라고 적혀 있어 부록 A-4와 충돌했다. 한 화면에 이 스타일의 버튼은 정확히 하나만 둔다.

```html
<button class="title-start" type="button">
  <span>시작하기</span>
</button>
```

```css
.title-start {
  --cta-top: #fff27a;
  --cta-mid: #ffc62f;
  --cta-bottom: #ff8b1f;
  --cta-border: #54220d;
  --cta-depth: #a93d13;
  --cta-ink: #3b1907;

  position: relative;
  width: min(100%, 420px);
  min-height: 68px;
  padding: 0 28px;
  border: 4px solid var(--cta-border);
  border-radius: 22px;
  appearance: none;
  cursor: pointer;
  color: var(--cta-ink);
  background:
    linear-gradient(180deg, rgba(255,255,255,.58) 0 2px, transparent 2px 100%),
    linear-gradient(180deg, var(--cta-top) 0%, var(--cta-mid) 49%, var(--cta-bottom) 100%);
  box-shadow:
    0 7px 0 var(--cta-depth),
    0 10px 0 var(--cta-border),
    0 17px 26px rgba(0, 0, 0, .38),
    inset 0 3px 0 rgba(255, 255, 255, .55),
    inset 0 -3px 0 rgba(154, 48, 8, .20);
  font-family: "Black Han Sans", "Noto Sans KR", sans-serif;
  font-size: clamp(21px, 5.8vw, 27px);
  font-weight: 900;
  line-height: 1;
  letter-spacing: -.03em;
  text-shadow: 0 2px 0 rgba(255, 255, 255, .38);
  transform: translateY(0) scale(1);
  transform-origin: 50% 100%;
  animation: cta-pulse 2.4s ease-in-out 1.2s infinite;
  transition: transform 90ms ease-out, box-shadow 90ms ease-out, filter 120ms ease-out;
  touch-action: manipulation;
  -webkit-tap-highlight-color: transparent;
}

.title-start::after {
  content: "";
  position: absolute;
  inset: 5px 9px auto;
  height: 19px;
  border-radius: 999px;
  background: linear-gradient(180deg, rgba(255,255,255,.42), rgba(255,255,255,0));
  pointer-events: none;
}

.title-start > span { position: relative; z-index: 1; }

.title-start:hover {
  filter: saturate(1.08) brightness(1.04);
}

.title-start:active,
.title-start.is-pressed {
  transform: translateY(7px) scale(.985);
  box-shadow:
    0 1px 0 var(--cta-depth),
    0 3px 0 var(--cta-border),
    0 7px 13px rgba(0, 0, 0, .30),
    inset 0 3px 0 rgba(255, 255, 255, .38),
    inset 0 -2px 0 rgba(154, 48, 8, .18);
  animation-play-state: paused;
}

.title-start:focus-visible {
  outline: 4px solid #fff;
  outline-offset: 5px;
}

.title-start:disabled {
  cursor: default;
  filter: grayscale(.55) brightness(.78);
  opacity: .78;
  animation: none;
}

@keyframes cta-pulse {
  0%, 72%, 100% { transform: translateY(0) scale(1); }
  80% { transform: translateY(-2px) scale(1.025); }
  88% { transform: translateY(0) scale(1); }
  94% { transform: translateY(-1px) scale(1.012); }
}

@media (max-height: 700px) {
  .title-start { min-height: 64px; }
}

@media (prefers-reduced-motion: reduce) {
  .title-start { animation: none; transition-duration: 0ms; }
}
```

펄스는 계속 커졌다 작아지는 호흡이 아니라, 2.4초 중 마지막 일부에만 두 번 가볍게 튀는 방식이다. 터치 시 7px 내려가며 압출 그림자가 짧아져 실제로 눌리는 느낌을 만든다. 진동을 쓸 수 있다면 첫 유효 탭에만 `navigator.vibrate?.(18)`을 호출하며 필수 동작으로 의존하지 않는다.

---

## 4. 키 아트 요구사항

### 4.1 기존 `title.png` 풀블리드 재활용

현재 세로형 키 아트는 원본 비율이 대체로 2:3이며 일부는 `768×1152`다. `390×844` 화면은 원본보다 더 길고 좁다. 따라서 `object-fit: cover`를 적용하면 이미지 높이가 뷰포트에 맞고 원본의 좌우가 잘린다. `768×1152` 원본은 390×844에서 원본 폭의 약 69%만 보이므로, 캐릭터 얼굴과 필수 오브젝트는 중앙 약 64% 안전 폭 안에 두는 것을 권장한다.

기존 이미지의 “상단 1/3 여백”은 로고 자리로 사용한다. 기본 포지션은 `50% 50%`이며, 인물 얼굴이 잘리거나 로고 뒤로 핵심 요소가 들어갈 때만 게임별 CSS 변수 `--art-x`, `--art-y`, `--art-scale`을 조정한다.

```html
<div class="title-art" aria-hidden="true">
  <img class="title-art__image" src="./assets/title.png" alt="">
</div>
<div class="title-art__scrim" aria-hidden="true"></div>
<div class="title-art__vignette" aria-hidden="true"></div>
```

```css
.title-art {
  --art-x: 50%;
  --art-y: 50%;
  --art-scale: 1.01;
  position: absolute;
  inset: 0;
  z-index: 0;
  overflow: hidden;
  background: #071020;
}

.title-art__image {
  width: 100%;
  height: 100%;
  display: block;
  object-fit: cover;
  object-position: var(--art-x) var(--art-y);
  transform: scale(var(--art-scale));
  filter: saturate(1.06) contrast(1.03);
  animation: art-breathe 14s ease-in-out infinite alternate;
}

.title-art__scrim,
.title-art__vignette {
  position: absolute;
  inset: 0;
  z-index: 1;
  pointer-events: none;
}

.title-art__scrim {
  /* 위: 로고 판독, 중앙: 원화 보존, 아래: CTA 판독 */
  background:
    linear-gradient(180deg,
      rgba(4, 8, 18, .44) 0%,
      rgba(4, 8, 18, .16) 24%,
      rgba(4, 8, 18, .02) 46%,
      rgba(4, 8, 18, .12) 67%,
      rgba(4, 8, 18, .72) 100%),
    radial-gradient(ellipse 64% 27% at 50% 17%,
      rgba(4, 8, 18, .24),
      transparent 72%);
}

.title-art__vignette {
  background:
    linear-gradient(90deg,
      rgba(2, 5, 12, .40) 0%,
      transparent 13%,
      transparent 87%,
      rgba(2, 5, 12, .40) 100%),
    radial-gradient(ellipse at center,
      transparent 53%,
      rgba(2, 5, 12, .46) 112%);
  mix-blend-mode: multiply;
}

@keyframes art-breathe {
  from { transform: scale(var(--art-scale)) translate3d(0, 0, 0); }
  to   { transform: scale(calc(var(--art-scale) + .035)) translate3d(0, -0.6%, 0); }
}

@media (prefers-reduced-motion: reduce) {
  .title-art__image { animation: none; }
}
```

#### 기존 에셋별 점검 절차

1. `390×844`, `360×800`, `430×932`, `640×900`에서 캡처한다.
2. 로고를 숨긴 상태에서도 주인공 얼굴·손·핵심 게임 오브젝트가 잘리지 않는지 본다.
3. `--art-x`는 `44%–56%`, `--art-y`는 `44%–56%`, `--art-scale`은 `1–1.08` 안에서만 보정한다.
4. 이 범위를 넘어야 하면 원본 구도가 풀블리드에 맞지 않는 것이므로 새 키 아트를 만든다.
5. 스크림 불투명도는 게임별 ±`.10`까지 허용하되, 화면 전체를 회색이나 검정으로 덮어 원화의 채도를 죽이지 않는다.

### 4.2 새 키 아트 생성 프롬프트 가이드

새 이미지는 **로고와 버튼을 그려 넣지 않은 순수 배경 일러스트**로 만든다. 텍스트는 코드로 렌더링한다.

```text
모바일 세로 게임 타이틀용 풀블리드 키 아트, 2:3 비율, [게임 세계/장소].
[주인공과 조연 캐릭터 3~6명]이 화면의 왼쪽과 오른쪽 가장자리에서 안쪽을 향해
역동적으로 몸을 기울이며 프레임을 만든다. 얼굴과 실루엣은 가장자리를 빽빽하게 채우되
잘려도 자연스러운 반신 구도. 모든 캐릭터의 시선과 동작선은 중앙 상단을 향한다.
중앙 상단 30%에는 로고가 놓일 깨끗한 음영 공간을 남기고, 중앙 세로 통로에는 작은 핵심
게임 오브젝트만 배치한다. 하단 22%는 CTA가 읽히도록 디테일과 명암 대비를 낮춘다.
깊이감 있는 전경/중경/배경, 활기찬 만화 게임 일러스트, 과장된 표정, 선명한 색,
강한 실루엣, 영화적인 림라이트, 고품질 2D 애니메이션 키 비주얼.
텍스트 없음, 로고 없음, UI 없음, 버튼 없음, 워터마크 없음, 테두리 없음.
```

게임별 프롬프트에는 다음을 반드시 치환한다.

- `[게임 세계/장소]`: 공장, 숲, 우주, 성, 연구소처럼 즉시 구분되는 한 장소
- `[주인공과 조연 캐릭터 3~6명]`: 플레이 캐릭터와 게임 오브젝트를 구체적으로 명시
- 교과 개념은 교과서·수식 패널이 아니라 세계 안의 사물로 표현: 분수 게이트, 대칭 거울, 소수 블록 등
- 주인공 얼굴 안전 영역: 중앙 기준 좌우 32% 안쪽 또는 의도적으로 가장자리에서 잘리는 큰 전경
- 생성 해상도: 최소 `1024×1536`, 최종 납품은 PNG 또는 품질 85 이상 WebP

피해야 할 프롬프트 표현은 `centered hero`, `poster with title`, `character in the middle`, `UI mockup`이다. 이 표현들은 로고 공간을 침범하거나 가짜 텍스트를 생성하기 쉽다.

---

## 5. 정보 위계와 보조 컨트롤

### 5.1 노출 우선순위

1. 게임 로고
2. 시작하기 CTA
3. 키 아트의 주인공과 핵심 오브젝트
4. 한 줄 부제
5. 최고 기록 또는 진행 상태 pill
6. `어떻게 놀아요?` 텍스트 링크
7. 단원 표지, 음소거

### 5.2 단원 표지

- 첫 화면 최하단에 별도 카드로 두지 않는다.
- 로고 부제 아래 또는 CTA 위의 작은 한 줄 메타로 배치한다.
- 예: `5학년 2학기 · 분수의 곱셈`
- 글자 크기 11–12px, 굵기 700, 불투명도 `.72`, 최대 한 줄. 배경 pill은 원화가 복잡할 때만 쓴다.

### 5.3 최고 기록/진행 상태

- CTA 바로 위에 **하나의 pill**만 허용한다: `최고 24마리`, `최고 1,250점`, `3단계부터 계속`.
- 높이 28–32px, 좌우 패딩 12px, 글자 12px. 아이콘은 최대 하나.
- 신규 사용자처럼 값이 의미 없을 때 `최고 0점`을 보여 주지 않는다. 그 자리 자체를 숨긴다.
- 이어하기가 있으면 기록보다 이어하기 상태를 우선한다.

### 5.4 음소거

- 화면 우상단 안전 영역 안에 `44×44px` 투명 히트 영역으로 고정한다.
- 시각 아이콘은 20–22px, 배경은 검정 `rgba(0,0,0,.28)` 원형, 테두리는 흰색 `.18` 정도로만 처리한다.
- 텍스트 라벨은 화면에 노출하지 않고 `aria-label="소리 끄기"`/`"소리 켜기"`를 상태에 따라 갱신한다.
- CTA, 단원 pill과 한 줄로 묶지 않는다.

```css
.title-meta {
  min-height: 30px;
  padding: 6px 12px;
  border: 1px solid rgba(255,255,255,.24);
  border-radius: 999px;
  color: rgba(255,255,255,.92);
  background: rgba(5,10,20,.42);
  backdrop-filter: blur(5px);
  font-size: 12px;
  font-weight: 800;
  line-height: 1;
  text-shadow: 0 1px 3px rgba(0,0,0,.8);
}

.title-howto {
  min-height: 36px;
  padding: 7px 10px;
  border: 0;
  color: rgba(255,255,255,.86);
  background: transparent;
  font: 700 12px/1.2 "Noto Sans KR", sans-serif;
  text-decoration: underline;
  text-decoration-color: rgba(255,255,255,.35);
  text-underline-offset: 4px;
  text-shadow: 0 2px 5px rgba(0,0,0,.85);
}

.title-mute {
  position: absolute;
  z-index: 4;
  top: max(10px, env(safe-area-inset-top));
  right: max(12px, calc((100vw - min(100vw, 640px)) / 2 + 12px));
  width: 44px;
  height: 44px;
  display: grid;
  place-items: center;
  padding: 0;
  border: 1px solid rgba(255,255,255,.18);
  border-radius: 50%;
  color: #fff;
  background: rgba(0,0,0,.28);
  backdrop-filter: blur(5px);
  font-size: 21px;
}
```

---

## 6. 전 게임 공통 금지 목록

- 첫 화면에 체크리스트, 체크 아이콘 반복, 3줄 이상의 조작 설명을 노출하지 않는다.
- 카드 박스를 세로로 쌓지 않는다. 특히 `설명 카드 + 시작 버튼 카드 + 단원 카드` 구조를 금지한다.
- `시작`, `방법`, `설정`, `랭킹`처럼 같은 위계의 작은 버튼을 여러 개 두지 않는다.
- CTA와 경쟁하는 강한 색의 pill, 배지, 배너를 두지 않는다.
- 키 아트를 상단 1/3짜리 배너로 축소하고 나머지를 단색 UI 영역으로 채우지 않는다.
- 로고를 얇은 일반 시스템 폰트 한 줄과 단일 그림자로 끝내지 않는다.
- 키 아트 이미지 안에 게임명, 시작 버튼, 교과 단원명을 합성하지 않는다.
- 로고 뒤에 불투명 직사각형 카드를 두지 않는다. 판독성은 외곽선, 압출, 국소 스크림으로 해결한다.
- 최고 기록이 없는데 `0점`을 강조하지 않는다.
- 첫 방문 여부와 관계없이 매번 온보딩을 강제로 재생하지 않는다.
- 1.5초 자동 소멸 안내처럼 사용자가 읽기 전에 사라지는 정보를 사용하지 않는다.
- 44×44px보다 작은 터치 영역, 64px보다 낮은 주요 CTA를 사용하지 않는다.
- 장식 애니메이션을 동시에 세 종류 이상 재생하지 않는다. 기본은 키 아트 호흡, 로고 1회 등장, CTA 간헐 펄스까지다.
- `prefers-reduced-motion`을 무시하거나, 애니메이션 완료 전 CTA 입력을 막지 않는다.

---

## 7. 구현 완료 기준

> **2026-09-06 정리 — 무엇이 불변이고 무엇이 발명인가.**
> 이 문서의 옛 완료 기준은 「레이아웃과 구성을 바꾸지 마라」였고, 기획·검수는
> 「기존 타이틀 템플릿을 답습하면 실격」이라고 요구했다. 두 지시가 동시에 살아 있어서
> 빌드는 어느 쪽을 어겨도 지적을 받았다(감사 A C03·C04·C05).
> **불변인 것은 아래 「A. 불변 규칙」뿐이다. 화면 구성·로고 물성·시선 순서는
> 매 게임 발명한다** (`chosen.json` 의 `art_direction.title_composition` 이 계약).

### A. 불변 규칙 — 어떤 구성을 발명하든 이건 지킨다

- **주요 행동(시작) 버튼은 하나이며 높이 64px 이상.** 모드 선택은 온보딩 마지막 단계나 설정 토글로.
- **탭 가능한 모든 요소는 최소 44×44px** (도움말 링크 포함 — QA `mobile.touch` 게이트와 일치).
- **정보 위계**: 게임 세계 → 게임명 → 시작 행동이 1초 안에 읽힌다. 단원·기록은 보조 위계이고,
  값이 없을 때 빈 배지를 만들지 않는다.
- **키 아트/배경이 뷰포트를 채우고 빈 레터박스가 없다.** `390×844` 와 640px 이상 화면에서
  게임명·주인공·CTA가 겹치지 않는다.
- 기존 체크리스트의 모든 정보가 첫 실행 온보딩 또는 컨텍스트 팁에 매핑돼 있고,
  `어떻게 놀아요?` 로 다시 열 수 있다. 재방문 사용자는 한 번의 탭으로 즉시 플레이한다.
- 키보드 포커스, `aria-label`, 안전 영역, `prefers-reduced-motion` 지원.

### B. 매 게임 발명하는 것 — 여기서 답습하면 검수 must_fix high

화면 구성(시선 시작점·요소 배치), 로고의 **물성과 렌더 방식**, 시작 행위의 은유, 배경 처리,
환경 연출. **로고는 코드 렌더 텍스트여도 되고 생성 에셋이어도 된다**(§부록 C) —
둘 중 무엇을 쓸지는 이번 게임의 정체성이 정한다. 마찬가지로 **8px 압출·낱글자 회전 변주는
「축 스타일 하나의 레시피」지 완료 조건이 아니다**(§1.4 의 다양성 규정과 충돌했었다).
CTA 문구의 기본값은 「시작하기」가 아니라 **게임 세계의 동사**다(부록 A-4).

옛 문장 「게임별 개성은 키 아트·로고 팔레트·글자 리듬·CTA 문구·파티클로만 만들고
레이아웃과 정보 위계는 바꾸지 않는다」는 **폐기됐다.** 정보 위계(A)는 유지하되
레이아웃은 발명한다.

---

## 부록 A. 파일럿(말아봇)에서 확정된 보완 규칙
1. **로고 크기는 음절 수 비례로**: 레시피의 `clamp(46px,13.4vw,82px)`는 5~6음절 기준. 목표는 "로고 본문 폭 330–358px(390px 화면)". 음절 수 n일 때 `font-size ≈ clamp(46px, 340/n px 상당의 vw, 상한)` 감각으로 잡아라. 3음절이면 `clamp(72px,27.5vw,132px)` 수준.
2. **모든 탭 가능한 요소는 최소 44×44px**: 도움말 텍스트 링크 포함. QA mobile.touch 게이트와 일치.
3. **와이드 보정**: 세로 키 아트는 `@media (min-aspect-ratio:1/1)`에서 `object-position`의 세로 초점(--art-y)을 44~56% 안에서 조정해 얼굴 잘림을 피해라. 근본 해결은 §4.2 신규 아트.
4. **CTA 문구는 세계관 어휘로**: "시작하기" 대신 게임 세계의 동사(예: 공방 가동!, 과수원 열기!)를 써라.
5. **모드 선택(연습/장인 등)이 있던 게임**: 버튼을 늘리지 말고 온보딩 마지막 단계나 설정 토글로 옮겨 CTA는 하나를 유지해라.

## 부록 B. 로고 CSS 렌더링 주의 (확산 중 발견)
`background-clip:text` 그라디언트는 도색 순서상 `text-shadow` 압출 위 1~2px만 보인다. 화면에서 실제 "글자면 색"으로 보이는 것은 `--logo-side`, 옆면은 `--logo-side-dark`다. 팔레트를 고를 때 `--logo-side`를 글자면 색으로 잡아라. 또한 로고 요소가 `h1` 등 기존 전역 규칙(`.screen h1` 같은 높은 명시도 선택자)에 밟히지 않는지 계산된 font-size 를 반드시 확인해라 — 네모공장에서 로고가 절반 크기로 렌더된 실제 사고 원인이었다.

---

## 부록 C. 생성 에셋 타이틀 방법론 (파일럿 검증 완료 — 기우뚱 나무 382fa87)
로고를 **생성 에셋**으로 만들기로 했다면 이 절차를 따른다. (코드 렌더 로고와 생성 에셋 로고는
둘 다 유효한 선택지다 — §7-B. 2026-09-06 전에는 이 부록이 "표준"이라 §7의 코드 렌더 완료 조건과 충돌했다.)
1. **생성 (codex 이미지 도구)**: 게임당 4종 — 로고(한글 제목, 게임 세계 재질/질감, 만화 압출), CTA 버튼(세계관 사물+문구), 모바일 세로 키 아트(1024×1536, 텍스트 금지, 로고 존 상단 비움), PC 가로 키 아트(1536×1024, 텍스트 금지).
2. **크로마 규약**: 로고·CTA는 순수 마젠타 #FF00FF 단색 배경, 그림자·글로우 금지. 도구가 반환한 경로만 사용.
3. **한글 정확성 게이트**: 생성 직후 자가 판독 — 오탈자·획 뭉개짐이면 최대 3회 재생성, 판독 결과를 보고에 기록. (파일럿 실측: 로고 3회, 버튼 1회 재생성으로 성공)
4. **에셋화 (grok)**: `python3 factory/lib/chroma-cut.py` — 마젠타 색거리 알파 + 경계 디스필(프린지 제거) + 타이트 크롭. 어두운 배경 위 확대 스크린샷으로 프린지 0 확인이 검증 항목.
5. **통합**: 로고·CTA = 이미지 에셋(등장 모션·:active 눌림은 CSS), 배경은 orientation 미디어쿼리로 모바일/PC 분기. 로고 ≤1MB, 배경 ≤1.2MB.
6. 신규 게임은 파이프라인 art 단계에서 이 방법론을 기본으로 따른다.

### 부록 C 보강 (말아봇 CTA 사고에서)
- 자가 판독 게이트는 로고처럼 큰 글자에서는 잘 작동하지만, **버튼 명판처럼 작은 글자는 뭉개짐을 놓치기 쉽다**. CTA류의 짧은 문구(2~4자)는 이미지에 글자를 굽지 말고 **사물 에셋(무자 버전) + 코드 렌더 텍스트 오버레이**를 기본으로 해라. 오케스트레이터는 최종 스크린샷에서 모든 한글을 직접 재판독한다.
