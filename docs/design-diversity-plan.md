# 인게임 디자인 언어 전수 감사 + 레퍼런스 기반 분화 계획

작성 2026-08-29 · 대상 게시작 24작 (`public/catalog.json`) · 게임 코드 미수정 (감사·계획 문서)

> **⚠️ 2026-09-05 추기 — 이 문서의 처방(축 10종 중 택1 + 직전 5작 중복 금지 + 축당 정원 3작)이
> 「가족 단위 닮음」을 제도화했다.** 신작이 축 H(블루프린트)를 재사용해 파도 검문소와 같은
> 계열이 됐고 타이틀도 기존 템플릿을 답습해, 사용자가 "심각한 문제, 독창적인 것을 원한다"고
> 직접 지적했다(`docs/loop-engineering.md` §7.8 네 번째 재발). **축은 이제 고를 메뉴가 아니라
> 참고 어휘 사전이고**(`references/game-references.json` → `art_directions.status: "vocabulary"`),
> 기본값은 **매 게임 고유 비주얼 정체성 발명**이다. 아래 §3.1 축 배정표와 §3.3 정원 규칙은
> **사용 이력**으로만 읽어라 — 배정 근거로 쓰지 마라.

## 0. 결론 한 줄

판형과 타이틀은 갈렸지만 **인게임은 아직 한 벌의 템플릿이다.** 24작 전부가
`상단 어두운 알약 칩 HUD → 크림색 문제 카드 → AI 페인팅 배경 위 플레이 → 하단 금색 광택 CTA`
라는 동일한 5단 띠를 쓴다. 최근 9작은 색만 다를 뿐 **CSS 선언의 47~78%가 문자 그대로 같다.**
원인은 취향이 아니라 파이프라인에 박혀 있는 세 줄이다 — `20-art.md` 의 스타일 고정 문구,
`30-build.md` 의 "게임 화면 폴리시", `references/game-references.json` 에 아트 디렉션 축이
아예 없다는 사실.

---

## 1. 전수 감사 — 공유 DNA

측정 방법: 24작 × (390×844 플레이 + 1280×800 플레이/정답/오답) 재캡처 후 육안 판정 +
`index.html` 의 색·CSS 선언·연출 키워드 기계 추출 + 캡처 픽셀의 지배색/명도 통계.
캡처·추출 스크립트와 원자료(컨택트 시트 7장, `extract.json`, `pixel-stats.json`, `similarity.json`)는
세션 스크래치패드에 격리돼 있다:
`/private/tmp/claude-501/-Users-sitpo-math-game-factory/6fc2aed9-8feb-465e-b495-5fd623c5b8db/scratchpad/design-audit/`
(재현: `node capture-design.mjs && node extract.mjs && node pixel-stats.mjs && node similarity.mjs`)

### 1.1 수치로 본 공유 DNA

| # | 공유 항목 | 수치 |
|---|---|---|
| 1 | `assets/bg.png` (AI 생성 회화풍 배경 일러스트)를 인게임 플레이 화면에 깖 | **24 / 24** |
| 2 | 부품에 `box-shadow: 0 3px 0 <어두운색>` (딱딱한 하단 그림자 = 스티커/캔디 문법) | **23 / 24** (예외 decimal-smash) |
| 3 | 부품 상단에 `inset 0 1~2px 0 rgba(255,255,255,.2~.8)` 광택 하이라이트 | **22 / 24** (예외 roll-bot, tri-cut-tower) |
| 4 | 화면 상단 전폭 HUD 띠 (`#hud`/`#topBar`/`#hudIn`/`#top`) | **24 / 24** |
| 5 | HUD 부품이 `.pill` / `.chip` 클래스의 둥근 알약 칩 | **18 / 24** |
| 6 | `.pill`/`.chip` 이 `border-radius:12~14px` + `border:2px solid` + `font-weight:800~900` 조합 | **17 / 24** — 색 값만 다르고 나머지 선언이 동일 |
| 7 | 우측 상단 원형 음소거 버튼 | **24 / 24** (같은 자리·같은 크기) |
| 8 | HUD 바로 아래 크림/반투명 "문제 카드" 라운드 사각형 | **24 / 24** |
| 9 | 하단 액션 버튼 + 그 버튼을 칠하는 따뜻한 밝은색 `linear-gradient` | **23 / 24** (예외 rounding-dash) |
| 10 | 본문 타이포가 전부 `-apple-system` 시스템 스택 (게임 고유 서체 0) | **24 / 24** |
| 11 | 캔버스 텍스트가 `800~900` 굵기 산세리프 | **19 / 24** (나머지 5작은 템플릿 리터럴이라 미검출) |
| 12 | 정답 연출 = 파티클 + 점수 팝업 + 화면 흔들림 조합 (`star`/`particle`/`shake`/`flash` 동시 출현) | **24 / 24** |
| 13 | `combo` 카운터 존재 | **24 / 24** |
| 14 | 호박색·금색 강조색 (H 32~54, S 80~100) 보유 | **23 / 24** |
| 15 | 크림·따뜻한 오프화이트 패널색 (#f4~#fff 계열, H 28~48) 보유 | **22 / 24** |
| 16 | AI 페인팅풍 동물·아동 마스코트가 플레이 화면에 상주 | **17 / 24** |
| 17 | 웹폰트를 **타이틀에만** 쓰고 인게임은 시스템 폰트로 돌아감 | 웹폰트 보유 9작 전부 |

**같은 hex를 그대로 공유하는 사례** (팔레트가 다르다면 나올 수 없는 값):

- `#fff27a` — overlay-snap, twice-cut, boundary-rush, stone-hop, tide-checkpoint, clang-stamp, glass-puff (**7작**)
- `#ff8b1f` — overlay-snap, twice-cut, boundary-rush, stone-hop, tide-checkpoint (**5작**)
- `#a93d13` — overlay-snap, twice-cut, splash-flat, honey-cups (**4작**)

파티클·불꽃 연출 상수가 게임 간에 복사된 흔적이다.

### 1.2 축별 판정

**a. 색 문법.** 명도 자체는 갈렸다(캡처 평균 휘도 0.107 rounding-dash ~ 0.789 boundary-rush).
문제는 색상환 분포다 — 24작 중 **주황~노랑(H30-60)이 지배색인 게임 10작, 시안~파랑(H180-240)이
지배색인 게임 8작**으로 두 덩어리에 몰려 있고, 어느 쪽이든 강조색은 결국 호박색이다(23/24).
`factory/state/queue.json` 의 `palette_history` 를 보면 무드 문장은 "사프란 해바라기 들판",
"모노크롬 먹선 한지", "대양 관제탑" 처럼 다양하게 **선언**돼 있고 `bg` 도 `#FFE27A`, `#CFF6FF`,
`#C8D9CC` 처럼 밝게 잡혀 있는데, 실제 빌드된 게임의 최빈 hex는 `#111111`, `#17324d`, `#3a1f0a`
처럼 어두운 값이다. **기획서의 팔레트가 빌드에서 관철되지 않는다** — HUD·패널·버튼이 항상
어두운 반투명으로 덮어쓰기 때문이다.

**b. HUD 문법.** 24/24가 상단 전폭 띠. 그 안의 부품 CSS가 사실상 한 파일에서 복사됐다:

```css
/* boundary-rush */ .pill{background:rgba(34,37,43,.88);border:2px solid #2446D8;border-radius:14px;
  padding:6px 10px;font-weight:900;font-size:13px;...;box-shadow:inset 0 1.5px 0 rgba(255,255,255,.22),0 3px 0 rgba(16,18,22,.45)}
/* cell-latch  */ .pill{background:rgba(44,24,16,.88);border:2px solid rgba(232,180,35,.55);border-radius:13px;
  padding:5px 9px;font-weight:900;font-size:12.5px;...;box-shadow:inset 0 1.5px 0 rgba(255,255,255,.22),0 3px 0 rgba(28,16,10,...)}
/* lantern-disk*/ .pill{background:rgba(28,36,34,.82);border:2px solid rgba(242,193,78,.55);border-radius:14px;
  padding:6px 10px;font-weight:900;font-size:13px;...;box-shadow:inset 0 1.5px 0 rgba(255,255,255,.22),0 3px 0 rgba(16,20,18,...)}
```

색 값 3개를 빼면 완전히 같은 선언이다. 정보 구조도 같다 — 좌: 점수, 중: 콤보, 우: 목숨(하트/점),
최우: 원형 음소거.

**c. UI 부품.** `border-radius` 최빈값이 24작 중 21작에서 **13~18px** 구간, 테두리는 전부 `2px solid`,
그림자는 전부 `0 3px 0` 하드 오프셋 + 상단 광택 inset. 캔버스 `roundRect` 반경도 8~16 구간에 몰린다.
"게임 UI답게"의 정의가 하나뿐이라 24작이 같은 스티커 재질을 쓴다.

**d. 배경 처리.** 24/24가 생성 일러스트 배경. 그 위에 반투명 패널을 얹는 것도 24/24.
플랫 컬러·종이 질감·격자·무배경을 쓴 게임이 **0작**이다. circle-unfurl(모노크롬 한지)만이
유일하게 회화풍에서 벗어났고, 그마저도 그 위에 같은 알약 HUD가 올라간다.

**e. 피드백 연출.** 24/24가 같은 3종 세트 — 입자 버스트 + 점수 팝업 + 화면 흔들림.
`shake`(24작) · `flash`(24작) · `star/particle`(24작) · `combo`(24작) 이 전부 동시 출현한다.
게임 고유의 정답 사건(예: 유리가 부풀어 터진다 / 자물쇠가 딸깍 걸린다)이 아니라 **범용 축하 효과**다.

**f. 마스코트.** 17/24에 AI 생성 동물·아동 캐릭터가 상주하고 전부 같은 렌더 스타일이다 —
두꺼운 외곽선 + 소프트 셀 셰이딩 + 채도 높은 원색. 다른 게임에 옮겨 놓아도 이질감이 없다.
이것이 "한 가족처럼 보인다"는 인상의 가장 직접적인 원인이다.

### 1.3 유사도 클러스터

색을 제거한 CSS 선언 집합의 자카드 유사도 (같은 템플릿에서 파생됐는지를 재는 지표):

```
78.0%  cell-latch ~ glass-puff          65.4%  overlay-snap ~ stone-hop
74.6%  overlay-snap ~ twice-cut         65.2%  tide-checkpoint ~ clang-stamp
70.2%  overlay-snap ~ tide-checkpoint   65.2%  twice-cut ~ stone-hop
69.3%  stone-hop ~ tide-checkpoint      64.1%  twice-cut ~ lantern-disk
69.2%  stone-hop ~ lantern-disk         64.1%  overlay-snap ~ clang-stamp
69.1%  twice-cut ~ tide-checkpoint      63.9%  overlay-snap ~ lantern-disk
68.2%  tide-checkpoint ~ lantern-disk   63.2%  twice-cut ~ clang-stamp
66.2%  stone-hop ~ clang-stamp          62.9%  clang-stamp ~ lantern-disk
```

- **클러스터 A — "최신 템플릿" 9작**: overlay-snap, twice-cut, boundary-rush, stone-hop,
  tide-checkpoint, clang-stamp, cell-latch, lantern-disk, glass-puff.
  상호 47~78%. **이 9작이 정확히 최근에 만들어진 9작이다** — 템플릿이 시간이 갈수록
  수렴하고 있다는 뜻이다. 개선이 아니라 악화 추세다.
- **클러스터 B — 중간 2작**: circle-unfurl, wobble-grove (44.5%).
- **클러스터 C — 개별작**: tri-cut-tower, roll-bot, symmetry-breaker, jelly-gate, rounding-dash,
  ice-snap, honey-cups, splash-flat, wrap-pop, decimal-smash, bandi-tower, cube-merge-factory,
  split-stone. 평균 19~26%. CSS는 갈렸지만 **위 1.1의 공유 DNA 17개 항목은 그대로 공유한다.**

---

## 2. 레퍼런스 대조 — 우리는 원작의 아트를 버렸다

### 2.1 원작 아트 디렉션 vs 우리 구현

| 게임 | `mechanic_origin` | 원작의 실제 아트 디렉션 | 우리 구현 | 판정 |
|---|---|---|---|---|
| overlay-snap | Stack + Topmarks + Fruit Ninja | Stack: **배경 일러스트 0.** 단색 그라디언트가 높이에 따라 색상만 바뀜, 무텍스처 블록, HUD는 거대한 얇은 숫자 하나. Topmarks: 흰 바탕 + 플랫 컬러 버튼 | 벽돌 공장 회화 배경 + 알약 HUD + 마스코트 | ✗ 정반대 |
| clang-stamp | Topmarks + Stack | 위와 동일 | 목재 창고 회화 배경 | ✗ 정반대 |
| twice-cut | Fruit Ninja + Cut the Rope | FN: 어두운 대나무 도장, **과즙 스플래터 자체가 연출의 주인공**, 손그림 붓 UI, 금색 콤보 서체 | 축제 천막 회화 + 범용 파티클 | △ 스플래터 개성 실종 |
| decimal-smash | Fruit Ninja | 위와 동일 | 네온 연구실 | △ (네온은 개성이나 원작과 무관) |
| ice-snap / wobble-grove | Cut the Rope | **골판지 상자 디오라마.** 손그림 사탕, 분필 낙서 배경, 판지 갈색 + 사탕 빨강 | 빙하 회화 / 과수원 회화 | ✗ 판지 물성 미차용 |
| stone-hop | Crossy Road / Frogger | **무텍스처 저폴리 보크셀**, 정사영 카메라, 외곽선 0, 그림자 1개, 장난감 색면 | 연못 회화 배경 + 캐릭터 일러스트 | ✗ |
| boundary-rush / roll-bot | Vampire Survivors | **의도적 저해상 픽셀 아트**, 어두운 타일 바닥, 조밀한 픽셀 VFX, 구형 PC RPG식 픽셀 프레임 UI | 경기장 회화 / 장난감 공방 회화 | ✗ |
| lantern-disk | Bloons TD / Kingdom Rush | 두꺼운 외곽선 2D 카툰, 손그림 배너·나무 두루마리 패널, 탑다운 세미아이소 | 야경 연못 회화 | △ |
| cube-merge-factory / roll-bot | Suika / Threes! / 2048 | Threes!: **순수 플랫 파스텔 카드**, 여백이 절반, 얇은 타이포, 일러스트 0 | 공장 회화 배경 | ✗ |
| jelly-gate | Count Masters / Join Clash | 하이퍼캐주얼 — 민무늬 그라디언트 하늘, 무텍스처 캡슐 군중, **거대한 플랫 게이트 사각형 + 굵은 콘덴스드 숫자** | 심해 네온 터널 | △ |
| rounding-dash | Subway Surfers / Run 3 | SS: 그래피티·스트리트 아트, 채도 높은 원색 셀셰이딩. Run 3: **순수 검은 공허 + 플랫 색면 타일** | 네온 하이웨이 | △ |
| cell-latch | PhET Area Model / Proportion Playground | **흰 실험실 바탕**, 원색 컨트롤, 일러스트 0, 정보가 곧 그래픽 | 극장 커튼 회화 | ✗ 정반대 |
| circle-unfurl | Polypad (Mathigon) | **흰 캔버스**, Mathigon 팔레트(산호·틸·보라)의 플랫 교구 타일, 가는 격자, 장식 0 | 모노크롬 한지 (24작 중 가장 성공적인 이탈) | ○ |
| tri-cut-tower | Minecraft / Polypad | 16px 블록 텍스처 / 위와 동일 | 공방 회화 | △ |
| bandi-tower | Helix Jump | **배경 0.** 단일 그라디언트 위 헬릭스 하나, 광택 플라스틱 캔디 색 | 수정 동굴 회화 | ✗ |
| honey-cups | Sandspiel | **1px 입자 셀룰러 오토마타**, 검은 캔버스, UI는 원소 팔레트 띠 하나뿐 | 해바라기 들판 회화 | ✗ 정반대 |
| splash-flat | oimo.io Pudding + Blooket | oimo: **순백 배경**, 순색 젤리, 크롬 0. Blooket: 플랫 벡터 블룩 | 항구 회화 | ✗ |
| tide-checkpoint / split-stone | balaline (Gamedev.js 2025) | 잼 게임 — **선 하나의 미니멀 라인 아트**, 단일 강조색, 거대한 여백 | 관제탑 회화 / 보물온실 회화 | ✗ |
| wrap-pop | Hexa Sort | 플랫 육각 타일, 채도 높은 단색, 민무늬 밝은 배경 | 해안 포장소 회화 | ✗ |
| symmetry-breaker | Stack + Slice Master | 위 Stack 참조 | 심해 네온 성소 | △ |
| glass-puff | 네모시티 도형 구조대 | 교육용 웹 — 플랫 도형, 흰 작업면 | 유리 공방 회화 | ✗ |

**요약: 24작 중 19작의 원작이 "배경 일러스트가 없거나 극단적으로 미니멀"하다.**
그럼에도 우리는 24/24에 AI 회화 배경을 깔았다. 메커닉은 차용했는데 아트 디렉션은 통째로 버리고,
그 자리를 공장 기본값 한 벌로 채웠다. 이것이 "여러 레퍼런스를 참고해야지"라는 지적의 실체다.

### 2.2 레퍼런스 광산에 없는 것

`references/game-references.json` 을 열어 보면 원인이 명확하다:

- `mechanics` (28개) 필드: `name / origin / core_loop / math_ideas / web_difficulty / notes`
  → **아트 디렉션 필드가 아예 없다.** 원작의 색·형태·타이포·피드백 개성이 기록되지 않는다.
- `visual_refs` (46개): Bruno Simon, three.js 예제, Shadertoy, Codrops, Lusion, Active Theory …
  → **46개 전부 WebGL/three.js 테크 데모다.** "파티클 웨이브", "GPGPU 플로킹" 같은 *기법* 목록이지
  *아트 디렉션* 목록이 아니다. 여기서 무엇을 고르든 색·질감·타이포는 결정되지 않는다.
- `sites` (143개) 중 아트를 언급한 항목 11개뿐.

즉 기획 에이전트는 "이번 게임의 그림은 어떤 결이어야 하는가"를 물어볼 대상이 없다.
없으니 `20-art.md` 의 고정 문구가 그대로 24번 반복된다.

### 2.3 신설할 비주얼 축 10개

초등 수학 게임 시장에서 톤을 갈라 주는 축으로, 각 축은 **팔레트·질감·타이포·부품·피드백**까지
한 벌로 결정된다. 축당 최대 3작.

| 축 | 이름 | 질감·팔레트 | 타이포 | 부품 문법 | 피드백 개성 | 참고 (메커닉 아닌 아트만) |
|---|---|---|---|---|---|---|
| **A** | 종이공작·보드게임 | 무광 판지·펠트·원목. 인쇄 종이결. 채도 중간, 그림자는 종이 오프셋 2~4px | 굵은 그로테스크 산세리프, 보드게임 규칙서 느낌 | 다이컷 카드, 나무 말, 펀치아웃 토큰. 광택 금지 | 종이 넘김, 말이 딸깍 놓임, 토큰 튕김 | Tearaway, Paper Mario: Origami King, Azul·Patchwork 보드 아트, Cut the Rope 판지 디오라마 |
| **B** | 플랫 벡터 교구 | **배경 일러스트 없음.** 흰/연회색 캔버스 + 3~5색 플랫 | 기하학 산세리프, 여백 크게 | 3px 단색 스트로크, 그림자 0, radius 0 또는 999px 양극단 | 도형 자체가 변형·정렬되는 것이 곧 피드백 | Mathigon Polypad, PhET, Threes!, Mini Metro |
| **C** | 네온 아케이드 | 순흑 + 전기 시안·마젠타, 블룸, 스캔라인 | 아케이드 콘덴스드, 자간 넓게 | 발광 하이라이트 테두리, 면 없음 | 잔광 트레일, 글리치, 비트 싱크 | Geometry Dash, Rez, Tron, Beat Saber |
| **D** | 클레이·스톱모션 | 지문 자국 점토, 이음매, 완전 무광. 무지 배경지 + 깊은 스튜디오 그림자 | 손으로 눌러 만든 듯한 둥근 서체 | 살짝 비뚤어진 윤곽, 24fps 저더 | 눌림·늘어남(squash&stretch)이 전부. 파티클 금지 | Aardman, Claybook, LocoRoco, Vignettes |
| **E** | 레트로 픽셀 | 하드 픽셀 그리드, 16색 램프, 디더링, 안티에일리어싱 0 | 비트맵 폰트 (코드로 그린 5×7) | 1px 외곽선, 구형 PC RPG 프레임 | 스프라이트 깜빡임, 픽셀 파편, 팔레트 스왑 | Downwell, Celeste, Stardew Valley, Vampire Survivors |
| **F** | 인쇄·리소그래프 | 2~3 별색 잉크 + 어긋난 견짜기, 하프톤 망점, 종이 결 | 세리프 헤드라인 + 작은 캡션 | 잉크 얼룩, 판형 여백, 괘선 | 잉크 번짐·도장 자국·판 어긋남 | Genesis Noir, Obra Dinn(1bit 디더), 리소 진, 구 교과서 도해 |
| **G** | 칠판·문구·공책 | 슬레이트 녹색·짙은 회색 판, 분필 가루, 모눈 공책, 연필·자·컴퍼스, 포스트잇 | 손글씨 + 연필 캡션 | 분필 선, 모눈 격자, 종이 클립·테이프 | 분필로 그어 지움, 지우개 자국, 도장 찍힘 | 실제 교실 칠판, Scribblenauts, 실험 노트 |
| **H** | 기술 도면·블루프린트 | 남색 위 시안 선, 또는 벨럼 위 먹선. 치수선·화살표·아이소메트릭 모눈 | 모노스페이스 라벨, 대문자 | 헤어라인 스트로크, 면 채움 없음, 인출선 | 치수선이 뻗어 나가 값을 확정 짓는 연출 | Poly Bridge, Opus Magnum, Besiege, 실제 제도 도면 |
| **I** | 직물·자수·펠트 | 니트 조직, 펠트 절단면, 박음질 스티치, 실 늘어짐 | 자수 놓은 듯한 두꺼운 서체 | 스티치 테두리, 단추, 지퍼 | 실이 풀리고 다시 꿰매짐, 천이 늘어짐 | Yoshi's Woolly World, Kirby's Epic Yarn, Unravel |
| **J** | 유리·스테인드글라스 | 납선 검은 윤곽 + 보석색 반투명 면, 배광(backlit), 굴절 | 얇은 대문자 세리프 | 납선 두께가 곧 테두리, 면은 반투명 | 빛이 통과·굴절, 유리가 금 가고 다시 채워짐 | Gris, Monument Valley, Sagrada 보드게임 |

> 축 C는 이미 3작이 점유(decimal-smash, jelly-gate, rounding-dash)했다. **신규 배정 금지.**
> 대신 세 작품 간 내부 분화가 필요하다 — 3.3 참조.

---

## 3. 분화 계획

### 3.1 축 배정표 (24작)

| 축 | 게임 | 팔레트 방향 | HUD·부품 재문법 | 정답/오답 연출 개성 | 원작 참고 |
|---|---|---|---|---|---|
| **A** 종이공작 | **stone-hop** 폴짝이 | 크라프트 갈색 + 원목 + 이끼 초록, 강조는 잉크 빨강 1색 | HUD를 **판지 탭 3장**으로 (칩 폐기). 돌은 펀치아웃 토큰, 두께 있는 측면 노출 | 말이 칸에 **딸깍 앉으며 판지 눌림**. 파티클 대신 종이 부스러기 3~4장 | Crossy Road의 무텍스처 장난감 형태 + Azul 보드 토큰 |
| | **ice-snap** 쩍쩍 | 골판지 갈색 + 흰 판지 + 얼음 대신 **투명 셀로판 파랑** | 골판지 상자 무대 프레임. HUD는 상자에 붙은 **테이프 라벨** | 판지가 **가위로 잘리는 절단면** 노출, 길이 눈금은 자 스티커 | Cut the Rope 원작 판지 디오라마 회복 |
| | **wrap-pop** 쓱말이 | 모래빛 크라프트 + 코발트 도장 잉크 + 산호 라벨 | 전개도가 **실제 종이 띠**. HUD는 포장 송장 양식 | 종이가 **말리며 풀칠 이음선이 맞물림**. 성공음은 테이프 뜯는 소리 | Hexa Sort 플랫 타일 + 실제 포장 공작 |
| **B** 플랫 벡터 | **cell-latch** 칸자물쇠 | **배경 일러스트 제거.** 흰 캔버스 + Mathigon식 산호/틸/보라 3색 | HUD 폐기 → 상단 **가는 괘선 한 줄**에 텍스트만. 칸은 순수 플랫 사각형 | 두 곱이 같아지는 순간 **두 직사각형이 서로 겹쳐 정렬**되는 것 자체가 연출 | PhET Area Model, Polypad |
| | **splash-flat** 첨벙 | 흰 배경 + 이론값 파랑 / 실제값 주황 2색 대비 | HUD를 **인포그래픽 범례**로. 수치는 축 라벨 | 막대가 **차트처럼 재배열**되며 평균선이 그어짐 | oimo.io 순백 배경, Blooket 플랫 블룩 |
| **C** 네온 (신규 금지) | **decimal-smash** 소수 스매시 | 현행 유지하되 **CRT 스캔라인 + 브루탈리즘 굵은 프레임**으로 하위 분화 | 알약 칩 → 각진 사각 프레임, radius 0 | 슬래시 궤적이 **잔광으로 수식을 남김** (Fruit Ninja 스플래터의 네온 번역) | Rez, Fruit Ninja 궤적 |
| | **jelly-gate** 젤리 게이트 | 유기적 발광 젤라틴 — 곡선·굴절 위주, 직선 금지 | HUD를 **젤리 방울**로 (반경 999px, 물컹 이징) | 게이트 통과 시 군중이 **젤리처럼 뭉개졌다 복원** | Count Masters 무텍스처 군중 |
| | **rounding-dash** 어림 대시 | 검은 공허 + **와이어프레임 벡터 라인 only** (면 채움 금지) | HUD는 HUD가 아니라 **수직선 위 눈금 라벨**로 흡수 | 레인 판정이 **선의 굵기 변화**로만 표현 | Run 3 검은 공허, Tron |
| **D** 클레이 | **glass-puff** 유리를 불어 | 무지 배경지 (연회색) + 점토 테라코타·크림·청록 | HUD를 **점토 명패**로. 광택 inset 제거, 완전 무광 | 부풀림이 **squash & stretch 그 자체**. 파티클 전부 제거, 실패는 점토가 주저앉음 | Claybook, Aardman |
| | **roll-bot** 말아봇 | 플라스틴 원색 + 24fps 저더 애니메이션 | 칩 → **점토 버튼**(눌린 자국 남음) | 회전체가 **손으로 빚어지듯** 단계별로 형성 | LocoRoco, Vignettes |
| **E** 픽셀 | **boundary-rush** 경계폭주 | 16색 램프, 어두운 타일 바닥 + 형광 젬 색 | 구형 PC RPG **1px 프레임 창**. 폰트는 코드로 그린 비트맵 | 픽셀 파편 + **팔레트 스왑 깜빡임** (파티클 금지) | Vampire Survivors 원작 회복 |
| | **honey-cups** 꿀몇잔 | 검은 캔버스 + 꿀 황금 입자 1색 | UI는 Sandspiel식 **원소 팔레트 띠 하나** | 꿀이 **1px 입자로 실제로 흘러 쌓임** — 그것 말고 다른 연출 없음 | Sandspiel 원작 회복 |
| **F** 인쇄·리소 | **twice-cut** 두 번 잘라 | 별색 2도 (먹 + 형광 주황), 견짜기 어긋남 3px | HUD를 **판형 여백의 괘선 정보**로. 칩 폐기 | 자를 때 **잉크가 번지고 판이 어긋남** (Fruit Ninja 스플래터의 인쇄 번역) | 리소 진, Genesis Noir |
| | **circle-unfurl** 원펼침 | 현행 먹선 한지 유지·강화. 석간주 1색만 유지 | 유일하게 남은 알약 HUD를 **판심(版心) 괘선**으로 교체 | 둘레 리본이 **먹이 종이에 스미듯** 펼쳐짐 | 구 교과서 도해, Obra Dinn 1bit |
| **G** 칠판·문구 | **clang-stamp** 꽝도장 | 인주 빨강 + 먹 검정 + 갱지. 도장은 문구다 | HUD를 **결재란 표**로. 버튼은 스탬프패드 | **도장이 실제로 찍히고 잉크가 눌려 번짐**. 실패는 인주가 흐릿 | 실제 인장·전표, 서류 양식 |
| | **tri-cut-tower** 쌓기나무 공방 | 슬레이트 녹색 칠판 + 분필 흰색 + 노란 분필 강조 | HUD를 **칠판 구석 판서**로. 투상도는 분필 3면도 | 쌓기나무가 **분필로 그어지고 지우개로 지워짐** | 실제 교실 칠판, 초등 교구 |
| | **cube-merge-factory** 네모공장 | 모눈 공책 + 연필 회색 + 포스트잇 3색 | HUD를 **공책 여백 메모**로. 상자는 절취선 도안 | 전개도가 **접히는 선을 따라 실제로 접힘** (파티클 폐기) | 공작 도안, 실험 노트 |
| **H** 블루프린트 | **tide-checkpoint** 파도 검문소 | 남색 해도 + 시안 등심선 + 주홍 부표 1색 | HUD를 **해도 범례 박스**로 (모노스페이스) | 경계가 **치수선처럼 뻗어 나가** 범위를 확정 | balaline 라인 미니멀 회복, 실제 해도 |
| | **split-stone** 가름돌 | 벨럼 크림 + 먹선 + 두 몫만 별색 2색 | HUD 폐기 → **인출선 라벨** | 가름선이 **제도 컴퍼스처럼** 그어지고 양쪽 치수가 자동 기입 | Opus Magnum, 제도 도면 |
| **I** 직물·펠트 | **overlay-snap** 겹쳐딱 | 펠트 원색 3~4장 + 무명천 바탕. **회화 배경 제거** | HUD를 **천에 박음질된 라벨**로. 칩 폐기 | 두 조각이 겹칠 때 **스티치가 꿰매지며 고정**. 어긋나면 실이 풀림 | Kirby's Epic Yarn, Stack의 무배경 미니멀리즘 |
| | **wobble-grove** 기우뚱 나무 | 니트 조직 + 펠트 열매 + 실 늘어짐 | 모빌 줄이 **실제 실**. HUD는 옷핀에 꽂힌 태그 | 균형이 맞으면 **실 장력이 팽팽해지며 정지**, 틀리면 실이 늘어져 처짐 | Unravel, Cut the Rope 로프 물성 |
| **J** 유리·스테인드글라스 | **bandi-tower** 반디탑 | 납선 검정 + 보석색 반투명 면 + 반딧불 배광 | HUD를 **납선 프레임 패널**로 | 가려진 칸이 **빛을 통과시키며** 드러남 (반딧불 = 배광 그 자체) | Gris, Sagrada |
| | **symmetry-breaker** 대칭 브레이커 | 스테인드글라스 만다라 — 납선 대칭축이 곧 접는 선 | 현행 네온 칩 → **납선 카르투슈**로 | 접힘·회전이 **유리판이 겹쳐 색이 섞이는** 것으로 표현 | Monument Valley, 로제트 창 |
| | **lantern-disk** 등불을 켜 | 등롱 유리 + 종이 창호 배광. 밤 배경은 유지하되 **회화 → 실루엣 + 배광**으로 | 사거리 원을 **유리 등롱의 광원 원**으로. HUD는 등롱에 걸린 목패 | 불이 켜질 때 **유리를 통과한 빛이 범위를 물들임** | Kingdom Rush 배너 UI의 유리 번역 |

### 3.2 1차 대상 6작 (우선순위)

클러스터 A(9작)의 유사도 상위 쌍을 전부 끊는 최소 집합. **여섯 작품이 서로 다른 축으로 흩어진다.**

| 순위 | 게임 | 현 최고 유사쌍 | 배정 축 | 작업 범위 | 예상 작업량 |
|---|---|---|---|---|---|
| 1 | **cell-latch** 칸자물쇠 | 78.0% (glass-puff) | B 플랫 벡터 | **배경 에셋 폐기** + HUD 전면 교체 + 부품 재작성 + 연출 교체 | **대** — bg/hero/curtain/lock 재생성 불필요(제거), CSS 전면 |
| 2 | **glass-puff** 유리를 불어 | 78.0% (cell-latch) | D 클레이 | 배경 재생성(무지 배경지) + 광택 제거 + 파티클 폐기 후 squash&stretch | **대** |
| 3 | **overlay-snap** 겹쳐딱 | 74.6% (twice-cut) | I 직물·펠트 | 배경 재생성 + 마스코트 재생성 + HUD/부품 + 연출 | **대** |
| 4 | **twice-cut** 두 번 잘라 | 74.6% (overlay-snap) | F 인쇄·리소 | 배경 재생성 + 2도 별색 재도색 + 절단 연출 교체 | **중~대** |
| 5 | **tide-checkpoint** 파도 검문소 | 70.2% (overlay-snap) | H 블루프린트 | 배경 재생성(해도) + HUD를 범례로 + 치수선 연출 | **중~대** |
| 6 | **stone-hop** 폴짝이 | 69.3% (tide-checkpoint) | A 종이공작 | 배경 재생성 + 판지 토큰 재질 + 연출 | **중~대** |

1차 완료 후 클러스터 A 잔여 최고 쌍은 **clang-stamp ~ lantern-disk 62.9%** 로 떨어진다.

**2차 대상 3작** (클러스터 A 잔여): boundary-rush(E 픽셀) · clang-stamp(G 칠판·문구) ·
lantern-disk(J 유리). 셋 다 **대** — 픽셀/칠판/유리는 전부 배경·부품·폰트를 새로 만들어야 한다.

**3차 (재스킨 수준)** — 배경 구조는 유지하고 색·HUD·부품 문법만 바꾸면 되는 작품:

| 작업량 | 게임 |
|---|---|
| **소** (색·HUD·부품만) | circle-unfurl (이미 축에 근접), decimal-smash, jelly-gate, rounding-dash (축 C 내부 분화만), split-stone |
| **중** (+ 배경 처리·연출 일부) | wrap-pop, ice-snap, splash-flat, wobble-grove, symmetry-breaker, bandi-tower, cube-merge-factory, tri-cut-tower, honey-cups, roll-bot |

> 전체 24작 중 **1차 6 + 2차 3 = 9작이 "대"**, 10작이 "중", 5작이 "소".

### 3.3 축 C(네온) 내부 분화 — 신규 배정 없이 3작을 가르는 법

세 작품이 모두 "어두운 배경 + 형광"이라 축 안에서 또 뭉친다. 하위 재질을 강제한다:

- **decimal-smash**: CRT 스캔라인 + **각진 브루탈리즘 프레임**(radius 0), 잔광 궤적
- **jelly-gate**: **곡선만** — 직선·직각 금지, 굴절·물컹 이징, radius 999px
- **rounding-dash**: **와이어프레임 선만** — 면 채움 전면 금지, 굵기 변조로 정보 전달

---

## 4. 파이프라인 재발 방지 — 조항 초안

> 아래는 **문안 초안**이다. 이번 임무에서 `factory/prompts/` 는 수정하지 않았다.

### 4.1 `factory/prompts/20-art.md` — 스타일 고정 문구 폐기

**현행 (재발의 직접 원인, 3행):**

```
- 스타일 고정 문구를 모든 프롬프트에 붙인다:
  `flat vector game art, thick clean outlines, bold saturated colors, soft cel shading,
   mobile game asset, crisp edges, centered composition`
```

이 한 줄이 24작 전부의 에셋을 같은 화풍으로 만든다. 대체 문안:

```markdown
## ⚠️ 고정 스타일 문구 금지 (24작이 같아 보인 원인)

모든 게임에 같은 스타일 문구를 붙이지 마라. 그 대신 `chosen.json` 의
`art_direction.visual_axis` 가 지정하는 축의 **스타일 스탠자를 그대로 프롬프트에 넣어라.**
축과 스탠자는 `references/visual-axes.json` 에 있다. 예:

- A 종이공작 : `die-cut matte cardstock and felt, printed paper grain, no gloss,
                offset paper drop shadow, tabletop board game piece`
- B 플랫벡터 : `flat vector diagram, plain white background, 3px uniform stroke,
                no shading, no illustration, no texture, editorial infographic`
- E 픽셀     : `16-color pixel art, hard pixel grid, visible dithering, no antialiasing,
                1px outline, 32x32 sprite sheet style`
...

**금지어**: 축이 명시적으로 요구하지 않는 한 `soft cel shading`, `bold saturated colors`,
`thick clean outlines` 를 쓰지 마라. 이 셋이 24작을 한 가족으로 만들었다.

**배경 에셋**: 축 B·C·E·H 는 **배경 일러스트를 만들지 않는다.** 이 축들은 코드가 그리는
플랫 면·격자·공허가 배경이다. `bg.png` 를 요구하는 축은 A·D·F·G·I·J 뿐이다.

**마스코트**: 마스코트는 선택이다. 최근 6작 중 4작 이상이 마스코트를 썼다면 이번 게임은
마스코트 없이 간다 — 슬롯 컨텍스트의 `recent_mascot_count` 를 확인해라.
```

`title.png` 의 "닌텐도 게임 패키지 아트처럼" 지시도 축별 구도로 갈라야 한다
(예: B는 포스터가 아니라 도해, F는 표지 조판, E는 타이틀 스프라이트).

### 4.2 `factory/prompts/30-build.md` — "게임 화면 폴리시" 를 축 조건부로

**현행 (알약 칩 문법을 문자로 지시하는 4행):**

```
- **HUD와 패널을 '게임 UI'답게**: 브라우저 기본 느낌 금지. 라운딩 크게(12px+),
  2px 안팎의 또렷한 외곽선 또는 이중 보더, 미세한 상단 하이라이트.
- **버튼은 눌리는 느낌**: :active 에서 아래로 2~3px + 그림자 축소. 면/테두리 2톤.
```

이것이 `border-radius:13~14px + border:2px + inset 광택 + 0 3px 0` 을 24번 재생산했다. 대체 문안:

```markdown
## HUD·부품 문법은 비주얼 축이 정한다 (템플릿 금지)

"게임 UI답게"의 정의는 하나가 아니다. `art_direction.visual_axis` 의 **부품 문법**을 따라라.

| 축 | HUD 형태 | radius | 테두리 | 그림자 | 광택 |
|---|---|---|---|---|---|
| A 종이공작 | 판지 탭·펀치아웃 토큰 | 2~6px | 종이 절단면 | 오프셋 2~4px | **금지** |
| B 플랫벡터 | 괘선 한 줄 또는 HUD 없음 | 0 또는 999px | 3px 단색 | **금지** | **금지** |
| C 네온 | 발광 테두리, 면 없음 | 축 하위 규칙 | 1px 발광 | 외곽 글로우 | 금지 |
| D 클레이 | 점토 명패 | 불규칙 | 없음(윤곽이 곧 형태) | 깊은 소프트 | **금지** |
| E 픽셀 | 1px 프레임 창 | **0** | 1px 하드 | 1px 오프셋 | 금지 |
| F 인쇄 | 판형 괘선·판심 | 0 | 헤어라인 | **금지** | 금지 |
| G 칠판 | 판서·공책 여백 | 0 | 분필선/괘선 | 금지 | 금지 |
| H 블루프린트 | 범례 박스·인출선 | 0 | 헤어라인 | 금지 | 금지 |
| I 직물 | 박음질 라벨 | 8~12px | 스티치 점선 | 천 그림자 | 금지 |
| J 유리 | 납선 카르투슈 | 축 규칙 | 납선 3~5px | 배광 | 굴절만 |

**금지**: `box-shadow: 0 3px 0` + `inset 0 1.5px 0 rgba(255,255,255,…)` 조합은 축 A·I 이외에서
쓰지 마라. 현재 23/24작이 이 조합을 쓰고 있다.

**금지**: `.pill` / `.chip` 이라는 클래스명과 그 CSS를 다른 게임에서 복사해 오지 마라.
빌드 전 `grep -c 'border-radius:1[34]px' public/g/*/index.html` 로 자기 검열해라.

**타이포**: 인게임 본문·수치를 `-apple-system` 시스템 스택으로만 두지 마라 (현재 24/24).
축이 서체 성격(비트맵/세리프/손글씨/모노스페이스)을 지정하면 **코드로 렌더하거나**
타이틀용 웹폰트를 인게임에도 써라.
```

**배경 체크리스트 항목도 수정 필요:**

```
현행: - [ ] 배경이 단색이 아니다 (그라디언트·패럴랙스·별·파티클)
개정: - [ ] 배경이 이 게임의 비주얼 축에 맞는가.
        축 B·C·E·H 에서 배경 일러스트를 깔았다면 실격.
        축 A·D·F·G·I·J 에서 배경이 밋밋하면 실격.
```

### 4.3 `factory/prompts/10-design.md` — `visual_reference` 를 아트 디렉션으로

현행은 `references/game-references.md` 의 비주얼 레퍼런스(=WebGL 테크 데모 46개)에서 하나를
고르게 한다. 기법 목록이라 아트 디렉션이 결정되지 않는다. 대체 문안:

```markdown
2. **비주얼 축을 하나 고르고, 원작의 아트 디렉션을 조사해라.**
   `art_direction` 안에 다음 세 필드를 반드시 넣어라:

   - `visual_axis`: `references/visual-axes.json` 의 축 id 하나 (A~J).
     **슬롯 컨텍스트의 `axis_usage` 에서 이미 3작이 쓴 축은 고를 수 없다.**
   - `origin_art_notes`: `mechanic_origin` 으로 지목한 원작이 **실제로 어떤 그림인지**
     3문장. 색·질감·타이포·피드백 중 무엇을 가져오고 무엇을 버리는지 명시.
     (상표·에셋 복제 금지 — 조형 언어만 차용한다.)
   - `anti_reference`: 이번 게임이 **닮지 않아야 할** 우리 게시작 2개와 그 이유 한 줄.

   ⚠️ 지금까지 24작 중 19작이 원작의 아트 디렉션을 통째로 버렸다. Stack·Threes!·Polypad·
   PhET·Sandspiel·Helix Jump·oimo·balaline 은 전부 **배경 일러스트가 없는** 게임인데
   우리는 24/24에 AI 회화 배경을 깔았다. 원작이 미니멀하면 미니멀하게 가라.

3. **팔레트는 빌드에서 관철돼야 한다.** `palette` 에 밝은 배경을 적어 놓고 실제 게임은
   어두운 반투명 패널로 덮는 사고가 반복됐다(`palette_history` 의 밝은 `bg` vs 실제 최빈 hex).
   `palette` 의 첫 색이 **실제 화면 픽셀의 최빈색이어야 한다**고 생각하고 골라라.
```

### 4.4 `factory/prompts/40-review.md` — 검수 게이트에 분화 항목 추가

```markdown
## 비주얼 20점 중 8점은 "분화"에 배정한다

- **축 준수 (4점)**: `art_direction.visual_axis` 의 부품 문법표를 실제로 지켰는가.
  금지된 그림자·radius·광택을 썼으면 0점.
- **선례 비유사 (4점)**: 아래를 실제로 실행해 보고 보고해라.
  ```bash
  node factory/lib/design-similarity.mjs <slug>   # 신설 필요
  ```
  기존 게시작 중 **CSS 선언 자카드 유사도가 45%를 넘는 게임이 하나라도 있으면 이 항목 0점**
  이고, 총점과 무관하게 **재작업 대상**이다. (현재 최신 9작이 47~78% 구간에 있다.)

추가 실격 조건:
- 인게임 캡처의 최빈색 3개가 기존 게시작과 2개 이상 정확히 일치하면 실격.
- `.pill`/`.chip` CSS를 다른 게임에서 복사해 왔으면 실격.
```

### 4.5 `references/game-references.json` — 아트 디렉션 축 신설

```markdown
- `mechanics[]` 에 `art_direction` 필드를 추가한다 — 원작의 색·질감·타이포·피드백 개성 3문장.
  기존 28개 메커닉 전부를 소급 채운다.
- `visual_refs` 는 현재 46개 전부 WebGL 테크 데모다. 이름을 `tech_refs` 로 바꾸고,
  **`art_refs` 를 신설**해 위 A~J 10개 축과 축당 참고작 3~5개를 기록한다.
- `factory/prompts/50-reference-scout.md` 의 수집 임무에 "아트 디렉션 레퍼런스 2~5개"를 추가한다
  — 기법이 아니라 **색·질감·타이포·피드백**을 적게 한다.
- 신설 파일 `references/visual-axes.json`: 축 id / 이름 / 스타일 스탠자(영문, 이미지 프롬프트용) /
  부품 문법표 / 배경 정책 / 참고작. `20-art.md`·`30-build.md`·`10-design.md` 가 공통 참조한다.
```

### 4.6 슬롯 선택기 — 축 사용 이력 추적

`factory/lib/pick-slot.mjs` 는 이미 `avoid_palette_moods` 를 넘긴다(최근 6작의 mood/bg).
그런데 mood 문장은 다양했는데도 결과물이 같았다 — **문장이 아니라 구조를 추적해야 한다.**

```markdown
- `queue.json` 에 `axis_history: [{slug, visual_axis}]` 를 추가한다.
- 슬롯 컨텍스트에 `axis_usage: {A:3, B:2, ...}` 와 `recent_mascot_count` 를 넣는다.
- 3작이 찬 축은 후보에서 제외한다 (하드 제약, 권고 아님).
```

---

## 5. 실행 순서 제안

1. `references/visual-axes.json` 신설 (10축 · 스타일 스탠자 · 부품 문법표) — 이후 모든 작업의 기준
2. `factory/lib/design-similarity.mjs` 신설 (CSS 선언 자카드) + 검수 게이트 연결
3. `20-art.md` · `30-build.md` · `10-design.md` · `40-review.md` 조항 반영 (4장 문안)
4. 1차 6작 재스킨 — cell-latch → glass-puff → overlay-snap → twice-cut → tide-checkpoint → stone-hop
5. 2차 3작 (boundary-rush · clang-stamp · lantern-disk)
6. 3차 15작 (소·중 작업량, 축 배정표 3.1 기준)

각 재스킨은 게임 로직·문제 생성기를 건드리지 않는다 — **색·부품·배경·연출만** 바꾼다.
`window.__GAME_TEST__` 계약과 수학 정확성은 그대로 유지되므로 QA는 비주얼 항목만 재채점하면 된다.
