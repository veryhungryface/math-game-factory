# 15개 게임 타이틀 화면 천편일률 진단과 차별화 설계

## 0. 결론

현재 15개 타이틀은 색과 문구만 다를 뿐, 거의 모두 `풀블리드 title.png → 상단 중앙 3D 낱글자 로고 → 필형 부제/단원 → 하단 중앙 68px 광택 버튼 → 밑줄 도움말`의 동일한 시선 동선을 쓴다. 배경은 14~22초 동안 천천히 확대되고, 로고 글자는 위에서 차례로 떨어지며, CTA는 2.4초마다 같은 펄스를 반복한다. 따라서 팔레트가 달라도 첫 1초의 실루엣과 정보 리듬이 같다.

이번 개편의 목표는 스킨 교체가 아니라 **시선의 시작점, 읽는 방향, 로고의 물성, 시작 행위**를 게임 메커닉에 맞게 바꾸는 것이다. 6개 구성 원형을 15개에 2~3개씩 배분한다.

## 1. 전수 진단

### 1.1 공통 골격

| 구조 요소 | 15개에서 반복되는 구현 | 결과 |
|---|---|---|
| 키 아트 | `title.png`를 `position:absolute; inset:0; width/height:100%; object-fit:cover`로 풀블리드 | 모든 표지가 같은 ‘배경 포스터’ 역할만 함 |
| 로고 위치 | 거의 전부 `top:max(52~54px, 7dvh)`, `left/right:14~16px`, 중앙 정렬 | 첫 시선이 항상 같은 좌표에 고정 |
| 로고 형태 | 굵은 한글 낱글자 `span`, 세로 그라디언트, 외곽선, 6~10px 압출 그림자, 전체 약 `-1.5~-2deg` | 글자 수와 색만 다르고 재질·방향·조형 언어가 같음 |
| 부제 | 로고 바로 아래 15~16px 여백, 반투명 블러 배경의 둥근 필 | 모든 게임이 같은 배지형 카피로 보임 |
| 단원 | 대다수 부제 아래 작은 한 줄, 일부만 하단 `title-meta` 필 | 정보 위계와 리듬이 거의 동일 |
| CTA | 하단 중앙, 최대 420px, 최소 높이 68px, 4px 테두리, 22px 라운드, 광택 그라디언트와 깊은 하단 그림자 | 세계관과 무관한 동일한 아케이드 버튼 |
| 보조 정보 | 숨겨진 최고 기록 필 → 단원/모드 필 → CTA → `어떻게 놀아요? 〉` | 하단이 동일한 체크리스트처럼 쌓임 |
| 등장 모션 | 배경 breathe/zoom, 낱글자 상단 낙하+오버슈트, 부제 지연 페이드, CTA 2.4초 펄스 | 게임 메커닉이 아니라 템플릿 자체를 예고함 |

### 1.2 게임별 현재 구현 비교

| 게임 | 로고 위치·형태 | 부제 처리 | CTA 형태·위치 | 정보 배치 | 등장 모션 |
|---|---|---|---|---|---|
| 반디탑 | 상단 중앙, 3글자 압출, `-1.6deg` | 로고 아래 필 | 하단 중앙 420×68px 광택 버튼 | 단원은 부제 아래, 최고 기록은 CTA 위 숨김 | 글자별 위 낙하, 배경 15s breathe, CTA 2.4s pulse |
| 원펼침 | 상단 중앙, 3글자 압출, `-1.8deg` | 로고 아래 필 | 동일 | 단원은 부제 아래, 최고 기록 숨김 | 동일 낙하, 배경 14s zoom, 동일 pulse |
| 네모공장 | 상단 중앙, 4글자 압출, `-1.5deg` | `titleTagline` 필 | 동일한 420×68px 버튼 | 단원은 로고 아래, 최고 기록 숨김 | 동일 낙하에 컨테이너 pop 추가, 배경 20s zoom·먼지 |
| 소수 스매시 | 상단 중앙, 5글자 압출, `-1.5deg` | 로고 아래 필 | 동일 | 단원은 부제 아래, 최고 기록 숨김 | 동일 낙하, 배경 14s breathe, 동일 pulse |
| 꿀몇잔 | 상단 중앙, 3글자 압출, `-1.5deg` | 로고 아래 필 | 동일 | **단원도 하단 필**로 CTA 위에 적층 | 동일 낙하, 배경 16s breathe, 동일 pulse; 별도 FX canvas |
| 쩍쩍 | 상단 중앙, 2글자 초대형 압출, `-2deg` | 로고 아래 필 | 동일 | 단원도 하단 필, 최고 기록 숨김 | 동일 낙하, 배경 16s breathe, 동일 pulse |
| 젤리 게이트 | 상단 중앙, 5글자 압출, `-1.5deg` | 로고 아래 필 | 동일 | 단원은 부제 아래, 최고 기록 숨김 | 동일 낙하, 배경 14s breathe, 동일 pulse |
| 말아봇 | 상단 중앙, 3글자 압출, `-1.5deg` | 로고 아래 필 | 동일 | 단원은 부제 아래, 최고 기록 숨김 | 동일 낙하, 배경 14s breathe, 동일 pulse |
| 어림 대시 | 상단 중앙, 5글자 압출, `-1.5deg` | 로고 아래 필 | 동일 | 긴 단원을 부제 아래 한 줄, 최고 기록 숨김 | 동일 낙하, 배경 14s breathe, 동일 pulse |
| 첨벙 | 상단 중앙, 2글자 압출, `-2deg` | 로고 아래 필 | 동일 | **관용도 2분할 선택+단원 필**을 CTA 위에 노출 | 동일 낙하, 배경 16s breathe, 동일 pulse |
| 가름돌 | 상단 중앙, 3글자 압출, `-1.5deg` | 로고 아래 필 | 동일 | **관용도 2분할 선택+단원 상태**를 CTA 위에 노출 | 동일 낙하, 배경 22s drift, 동일 pulse |
| 대칭 브레이커 | 상단 중앙, 6글자 압출, `-1.5deg` | 로고 아래 필 | 동일 | 단원은 부제 아래, 최고 기록 숨김 | 동일 낙하에 컨테이너 pop 추가, 배경 22s zoom, 동일 pulse |
| 쌓기나무 공방 | 상단 중앙, 7글자 압출, `-2deg` | 로고 아래 필 | 동일 | 단원은 부제 아래, 최고 기록 숨김 | 동일 낙하, 배경 14s breathe, 동일 pulse |
| 기우뚱 나무 | 상단 중앙, 6글자 압출, `-2deg` | 로고 아래 필 | 동일 | 단원은 부제 아래, 최고 기록 숨김 | 동일 낙하, 배경 14s breathe, 동일 pulse |
| 쓱말이 | 상단 중앙, 3글자 압출, `-2deg` | 로고 아래 필 | 동일 | 단원은 부제 아래, 최고 기록 숨김 | 동일 낙하, 배경 17s breathe, 동일 pulse |

진단상 실질적 예외는 네모공장의 먼지, 꿀몇잔의 FX canvas, 첨벙·가름돌의 시작 전 난이도 선택뿐이다. 전자는 장식 레이어라 골격을 바꾸지 못하고, 후자는 시작 화면을 설정 체크리스트로 만들어 유지 원칙과 충돌한다.

## 2. 유지할 불변 규칙

1. 게임을 시작시키는 주 CTA는 하나만 둔다. `어떻게 놀아요?`는 보조 텍스트 액션이며 주 CTA처럼 채우거나 펄스시키지 않는다.
2. CTA와 음소거, 도움말의 실제 클릭 영역은 최소 44×44px이다. 사물형 CTA도 투명 `button` hit box 전체가 이를 만족해야 한다.
3. 로고는 360×640px, 390×844px, 430×932px에서 키 아트와 4.5:1 이상의 대비를 확보한다. 복잡한 구간에는 국소 scrim 또는 2~4px 외곽선을 쓴다.
4. 시작 화면에 난이도·관용도·모드 체크리스트를 노출하지 않는다. 첨벙과 가름돌의 선택은 기본값으로 시작하고 첫 플레이 뒤 설정/재도전 화면으로 이동한다.
5. 세로 모바일을 기준으로 설계하고, `100dvh`, safe-area, `clamp()`를 사용한다. 가로 화면은 새 구성을 발명하지 않고 폭 640px의 세로 무대를 중앙 유지한다.
6. 기존 `assets/title.png`만 재사용한다. 새 이미지, 새 캐릭터 컷, 생성형 배경은 추가하지 않는다. CSS 복제 레이어, 마스크, 색면, 타이포, 간단한 DOM 도형은 허용한다.
7. `prefers-reduced-motion: reduce`에서는 진입 모션을 1ms 또는 단일 120ms 페이드로 바꾸고 무한 배경/CTA 모션을 끈다.

공통 구현 뼈대는 `.title-stage{position:fixed;inset:0;min-height:100svh;height:100dvh;overflow:hidden}`로 두되, 기존 `.title-logo-zone`과 `.title-cta-zone`의 좌표 토큰은 제거한다. 각 원형이 grid area와 reading order를 소유해야 한다.

## 3. 구성 원형 6종과 분배

| 원형 | 핵심 실루엣 | 배정 게임 | 수 |
|---|---|---|---:|
| A. 캐릭터 포스터형 | 키 아트 인물이 상부/중앙을 지배하고 로고가 하단 크레딧처럼 깔림 | 네모공장, 꿀몇잔, 쩍쩍 | 3 |
| B. 세로 족자형 | 한쪽 세로 축을 따라 제목과 정보가 위→아래로 흐름 | 원펼침, 쌓기나무 공방 | 2 |
| C. 대각 분할형 | 게임의 베기·축·레이싱 선이 화면을 대각/중앙으로 가름 | 소수 스매시, 대칭 브레이커, 어림 대시 | 3 |
| D. 오브젝트 조립형 | 큐브·수영판·열매가 글자 획/받침을 구성 | 반디탑, 첨벙, 기우뚱 나무 | 3 |
| E. 세계 진입형 | 레버·롤러처럼 세계 안의 사물을 작동하는 순간 시작 | 말아봇, 쓱말이 | 2 |
| F. 메커닉 타이포형 | 글자 자체가 곱셈·분할을 한 번 시연 | 젤리 게이트, 가름돌 | 2 |

원형은 레이아웃의 주 문법이다. 모든 게임의 CTA는 사물형으로 만들지만, E형만 화면 전체가 그 사물의 작동 동선에 종속된다.

## 4. 원형별 구현 규약

### A. 캐릭터 포스터형

- 아트는 `object-fit:cover`로 유지하되 인물 얼굴/손을 화면 35~62% 구간에 둔다.
- 로고 블록은 `bottom:clamp(118px,15dvh,154px)`에 두고 왼쪽 정렬한다. 검은 영화 크레딧 바를 만들지 말고 하단 38%에만 그라디언트 scrim을 둔다.
- 부제와 단원은 로고 위/옆의 작은 크레딧으로 분리한다. 필형 배지를 금지한다.
- CTA 사물은 오른쪽 아래에 겹쳐 두고 최소 `min-width:148px; min-height:56px`를 확보한다.

### B. 세로 족자형

- 제목을 2~4행 또는 세로 글자로 만들고 화면 한쪽 26~34% 폭을 점유한다.
- `writing-mode:vertical-rl`은 짧은 제목에만 사용하고, 긴 제목은 행을 쌓아 세로 리듬만 만든다. 스크린리더용 `aria-label`은 가로 읽기 그대로 유지한다.
- 반대편 66~74%는 아트의 인물/구조물을 가리지 않는다. 단원은 족자 끝의 낙관, CTA는 족자 손잡이/작업대 손잡이로 둔다.

### C. 대각 분할형

- `clip-path:polygon()` 색면 또는 `::before` 선으로 아트에 이미 존재하는 검/광축/레일을 연장한다.
- 로고를 선과 평행하게 `rotate()`하되 글자 자체 가독성을 위해 절대각 12deg를 넘지 않는다.
- 부제와 단원은 대각선의 서로 다른 면에 배치한다. CTA는 선의 종점에 둔다.

### D. 오브젝트 조립형

- 텍스트 DOM은 그대로 유지하되 `span::before/after`, `box-shadow`, 작은 CSS 도형으로 게임 오브젝트의 모서리·받침·축을 결합한다.
- 로고를 단일 베이스라인에 두지 않는다. 글자별 좌표는 CSS 변수 `--x`, `--y`, `--r`로 제어한다.
- CTA는 조립 결과의 마지막 부품이며, 탭 시 바로 시작된다.

### E. 세계 진입형

- 로고는 표지판/기계 라벨처럼 작게 두고, 중앙의 레버·롤러 조작부를 가장 큰 인터랙션으로 만든다.
- `button` 안에 손잡이/축/라벨 DOM을 넣는다. 시각물과 hit box를 분리하지 않는다.
- 진입 모션은 레버/롤러의 1회 작동 예고만 사용한다. 반복 펄스는 금지한다.

### F. 메커닉 타이포형

- 글자의 복제, 압축, 절단을 700~900ms 한 번만 수행해 게임 규칙을 말 없이 보여 준다.
- 초기/최종 프레임 모두 읽을 수 있어야 하며 중간 프레임은 `aria-hidden` 복제 레이어로 만든다.
- CTA는 글자 시연을 완성하는 게이트/쐐기/보석 받침으로 둔다.

## 5. 게임별 차별화 명세

### 5.1 반디탑 — D. 오브젝트 조립형

- **로고:** 화면 오른쪽 상단~중단에 `반/디/탑`을 3층 계단처럼 쌓는다. `반`은 `right:18px; top:12dvh`, `디`는 `right:58px; top:21dvh`, `탑`은 `right:98px; top:30dvh`; 각 글자 `rotate(-2deg)` 이하. 글자 사각 외곽과 하단 면을 `span::after{content:"";inset:8%;border:2px solid #e7ffcf;transform:translate(5px,6px);z-index:-1}`로 만들어 빛 큐브처럼 보이게 한다.
- **부제·단원:** 부제 ‘가린 칸이 들킨다’는 반디의 빛줄기 옆 `right:18px; top:43dvh; text-align:right`; 단원은 왼쪽 동굴 벽을 따라 `left:16px; bottom:20px`, 필 없이 11px 대문자형 정보로 둔다.
- **CTA:** 오른쪽 중하단의 **반디 탐조등 셔터**. 원형 60px 셔터와 ‘빛 비추기’ 라벨을 한 버튼으로 묶어 `width:176px;height:60px;border-radius:30px`; 원형 부분은 `radial-gradient()`로 발광한다.
- **등장 모션:** 세 글자 큐브가 아래층부터 `translateY(-24px)`로 내려와 한 번씩 맞물리고, 마지막에 전체 탑이 2px 가라앉는다(총 760ms).
- **`title.png` 재활용:** `object-position:50% 50%; transform:scale(1.02)`로 유지해 중앙 탑과 왼쪽 도마뱀을 모두 보존한다. 하단 전체 암막 대신 오른쪽 글자 뒤에만 `radial-gradient(ellipse at 82% 24%,rgba(3,20,12,.72),transparent 54%)`를 쓴다.

### 5.2 원펼침 — B. 세로 족자형

- **로고:** 왼쪽 28%에 ‘원 / 펼 / 침’을 세로로 배치한다. `.game-logo{position:absolute;left:14px;top:10dvh;display:grid;gap:-4px;font-size:clamp(54px,15vw,76px);transform:none}`. 먹색 평면 글자에 주홍색 3px 외곽선만 두고 기존 8단 압출은 제거한다.
- **부제·단원:** 부제는 왼쪽 족자 가장자리의 가로 띠로 `left:10px;top:49dvh;transform:rotate(-2deg)`; 단원은 제목 아래 42×42px 낙관처럼 두되 텍스트는 2행, `font-size:9px;line-height:1.15`.
- **CTA:** 화면 오른쪽 아래 그림 속 **두루마리 손잡이**. `button{right:20px;bottom:24px;width:190px;height:54px;border-radius:6px 28px 28px 6px}`에 ‘문양 펼치기’를 넣는다.
- **등장 모션:** 주홍 문양 띠가 `clip-path:inset(0 100% 0 0)`에서 펼쳐지고, 제목은 그 뒤 180ms에 먹이 번지듯 opacity만 오른다(총 850ms).
- **`title.png` 재활용:** `object-position:58% 48%`로 밀어 왼쪽 텍스트 축과 오른쪽 액자 사이를 확보한다. 인물 손과 원판은 가리지 않으며 왼쪽 30%에만 한지색 `rgba(232,221,199,.78)` 세로 veil을 둔다.

### 5.3 네모공장 — A. 캐릭터 포스터형

- **로고:** 하단 왼쪽에 `네모` 1행, `공장` 2행의 직각 블록 로고. `.game-logo{left:18px;bottom:132px;display:grid;justify-items:start;transform:none;line-height:.78}`; 글자 각도는 0deg, 모서리 반경 0에 가까운 압출로 박스 라벨처럼 만든다.
- **부제·단원:** 부제 ‘접어야 커진다’는 로고 위 `left:20px;bottom:250px`, 스탬프형 사각 라벨. 단원은 `right:16px;top:16px;width:94px;text-align:right`의 공장 작업표로 둔다.
- **CTA:** 우하단 **컨베이어 가동 스위치**. 빨간 토글 노브와 `공장 가동` 명판을 포함한 `button{right:18px;bottom:24px;width:172px;height:64px;border-radius:10px}`.
- **등장 모션:** 로고 네 글자가 평면 전개도처럼 시작해 각 글자 `rotateX()`가 아니라 seek-safe 2D `scaleX(.45) skewY()`에서 정사각 비율로 접힌다(720ms).
- **`title.png` 재활용:** `object-position:50% 42%; transform:scale(1.03)`로 로봇 머리와 든 상자를 중앙에 둔다. 하단 `linear-gradient(transparent 52%,rgba(34,20,6,.82) 86%)`로 로고만 받친다. 기존 먼지 레이어는 유지 가능하나 6개 이하로 제한한다.

### 5.4 소수 스매시 — C. 대각 분할형

- **로고:** 키 아트의 광검과 평행한 좌하→우상 대각선 위에 ‘소수 / 스매시’를 2단으로 놓는다. `.game-logo{left:8px;top:18dvh;width:92%;transform:rotate(-9deg);align-items:flex-start}`; ‘스매시’만 1.18배, 네온 마젠타 외곽선을 쓴다.
- **부제·단원:** 부제 ‘커지는 것만 톡!’은 검 위쪽의 어두운 면 `right:18px;top:12dvh`; 단원은 반대쪽 `left:16px;bottom:18px`에 수평으로 둔다.
- **CTA:** 광검 끝의 **충격 트리거**. `right:18px;bottom:84px;width:148px;height:58px;clip-path:polygon(10% 0,100% 0,90% 100%,0 100%)`, 문구 ‘베기’.
- **등장 모션:** 제목을 가로지르는 2px 빛선이 420ms에 지나가고, 통과 전/후 글자 복제 레이어가 `scale(1)`→`scale(1.08)`로 한 번 튕긴다.
- **`title.png` 재활용:** `object-position:50% 48%; transform:scale(1.04)`. 검의 실제 대각선을 가리지 말고 CSS 마젠타 선을 동일 각도로 8% 연장한다. 상단 빈 암부가 부제용 자연 scrim이다.

### 5.5 꿀몇잔 — A. 캐릭터 포스터형

- **로고:** 하단 왼쪽 테이블 전면에 ‘꿀몇잔’을 꿀 라벨처럼 `left:16px;bottom:136px;transform:rotate(-3deg)`로 둔다. 노란 채움+갈색 2px 외곽선, 압출은 3px로 축소한다.
- **부제·단원:** 부제는 캐릭터 반다나 위가 아닌 상단 하늘 `left:18px;top:18px`; 단원은 부제 아래 작은 2행 좌측 정렬. 둘 다 필을 제거하고 짙은 청록 텍스트+얕은 밝은 text-shadow를 쓴다.
- **CTA:** 중앙 아래의 **꿀단지 마개**. `button{left:50%;bottom:18px;transform:translateX(-50%);width:164px;height:58px;border-radius:50%}`에 육각 벌집 패턴과 ‘마개 열기’. 실제 타원은 `::before{inset:5px;border-radius:50%}`.
- **등장 모션:** 마개가 `rotate(-8deg)`→`rotate(5deg)`→0deg로 한 번 풀렸다 잠기고, 꿀 흐름 위에 1회 하이라이트가 내려간다(700ms).
- **`title.png` 재활용:** `object-position:50% 54%; transform:scale(1.01)`로 벌과 통, 잔을 모두 유지한다. 파란 하늘이 정보 여백이므로 상단 scrim을 제거한다. 기존 `titleFx`는 벌 3마리 이하의 짧은 경로에만 사용한다.

### 5.6 쩍쩍 — A. 캐릭터 포스터형

- **로고:** 하단 오른쪽 썰매 전면에 거대한 2글자 ‘쩍 / 쩍’을 수직 2단으로 `right:14px;bottom:118px;text-align:right;line-height:.72` 배치. 각 글자는 얼음판처럼 수평 균열선을 `linear-gradient()` 배경으로 가진다.
- **부제·단원:** 부제는 좌상단 `left:18px;top:18px`, 단원은 그 아래 2행. 흰색이 많은 아트이므로 남색 2px text-shadow와 반투명 청색 세로 선만 사용한다.
- **CTA:** 썰매 앞의 **얼음톱 손잡이**. `left:18px;bottom:22px;width:168px;height:56px;border-radius:28px 10px 10px 28px`, 문구 ‘한 번 자르기’.
- **등장 모션:** 첫 ‘쩍’이 가운데서 양쪽으로 5px 벌어지고 두 번째 ‘쩍’이 120ms 뒤 같은 동작을 한 후 원위치한다(균열 예고, 680ms).
- **`title.png` 재활용:** `object-position:48% 50%; transform:scale(1.02)`로 여우와 얼음 블록 행렬을 보존한다. 하단 오른쪽에만 `radial-gradient(at 88% 78%,rgba(3,35,62,.72),transparent 38%)`를 둔다.

### 5.7 젤리 게이트 — F. 메커닉 타이포형

- **로고:** 중앙 상단의 어두운 바다에 ‘젤리’는 큰 글자, ‘게이트’는 폭이 점차 줄어드는 3글자로 배치한다. `.game-logo{top:8dvh;left:50%;transform:translateX(-50%);display:grid}`; 각 글자 `--sx:1,.82,.64`로 게이트를 통과하며 작아지는 인상을 준다.
- **부제·단원:** 부제는 황금 게이트 바로 위 `top:34dvh`; 단원은 왼쪽 아래 산호 위 `left:14px;bottom:18px`, 필 없이 둔다.
- **CTA:** 화면 중하단 황금문과 겹치는 **게이트 문턱**. `left:50%;top:42dvh;transform:translateX(-50%);width:152px;height:54px;border-radius:28px 28px 8px 8px`, 문구 ‘통과하기’.
- **등장 모션:** ‘젤리’ 복제 3개가 게이트 방향으로 이동하며 `scale(.92)`, `scale(.78)`, `scale(.64)`로 줄고 최종 로고에 흡수된다(820ms, 1회).
- **`title.png` 재활용:** `object-position:50% 44%; transform:scale(1.01)`로 상단 암부와 중앙 게이트를 활용한다. 기존 전면 젤리 군집은 가리지 않도록 하단 중앙 UI를 54px 높이로 제한한다.

### 5.8 말아봇 — E. 세계 진입형

- **로고:** 왼쪽 상단 빈 종이에 공방 도면 라벨처럼 `left:18px;top:20px;transform:rotate(-1deg);font-size:clamp(46px,13vw,64px)` 배치. 단색 남색 글자, 얇은 이중선 프레임, 압출 없음.
- **부제·단원:** 부제 ‘딱 맞게 말아라’는 로고 밑 도면 주석선 끝에 둔다. 단원은 오른쪽 도구판 상단을 피해 `right:14px;top:20px;width:110px;text-align:right`.
- **CTA:** 화면 왼쪽 중단 빈 종이 위의 **공방 레버**가 화면의 주인공이다. `button{left:24px;top:35dvh;width:84px;height:190px;background:transparent}` 안에 14px 레일과 64px 빨간 손잡이를 만들고, 하단 명판에 ‘가동’ 표시. 전체 84×190px가 hit box다.
- **등장 모션:** 레버 손잡이가 위에서 아래로 36px 내려갔다 원위치하는 1회 예고(650ms). CTA 펄스와 배경 breathe는 제거한다.
- **`title.png` 재활용:** `object-position:50% 48%; transform:scale(1)`로 로봇과 말리는 띠를 전부 살린다. 왼쪽 상단의 넓은 종이 여백을 UI 영역으로 쓰며 별도 전체 scrim은 제거한다.

### 5.9 어림 대시 — C. 대각 분할형

- **로고:** 원근 레일 위에 ‘어림’을 좌측 레일, ‘대시’를 우측 레일처럼 2열 배치한다. `.game-logo{left:12%;right:12%;bottom:20dvh;display:flex;justify-content:space-between;transform:perspective(400px) rotateX(10deg)}` 대신 렌더 안정성을 위해 각 열에 `skewY(±4deg)`를 적용한다.
- **부제·단원:** 부제는 소실점 바로 아래 `top:38dvh;left:50%;transform:translateX(-50%)`; 긴 단원명은 `left:14px;top:16px;width:210px;line-height:1.25`로 2행 허용한다.
- **CTA:** 레일 중앙의 **출발 패드**. `left:50%;bottom:24px;transform:translateX(-50%);width:132px;height:64px;clip-path:polygon(18% 0,82% 0,100% 100%,0 100%)`, 문구 ‘발진’.
- **등장 모션:** 로고 두 열이 화면 아래에서 소실점 방향으로 14px 미끄러진 뒤 멈추고, 출발 패드의 청색 눈금이 한 칸만 앞으로 뛴다(740ms).
- **`title.png` 재활용:** `object-position:50% 54%; transform:scale(1.02)`로 함선과 레일 소실점을 유지한다. 기존 이미지 레일을 CSS 대각 가이드로 그대로 사용하고 추가 구분선은 넣지 않는다.

### 5.10 첨벙 — D. 오브젝트 조립형

- **로고:** ‘첨’은 6장의 수영판 더미처럼 수평 그림자를 반복하고, ‘벙’은 한 장이 빠져 오른쪽으로 8deg 기울어진 모습. `left:16px;top:13dvh;align-items:flex-end`; `span:first-child{box-shadow:0 5px #d84f3f,0 10px #b83c32}`, `span:last-child{transform:translateY(10px) rotate(8deg)}`.
- **부제·단원:** 부제는 로고 아래가 아니라 수면선 위 `right:16px;top:43dvh;text-align:right`; 단원은 좌하단 `left:14px;bottom:18px`.
- **CTA:** 오른쪽 아래 **빠진 수영판**. `right:16px;bottom:24px;width:156px;height:58px;border-radius:10px;background:#ed684f`; 손잡이 구멍은 `::after`로 만들고 문구 ‘판 끼우기’.
- **등장 모션:** ‘벙’과 CTA 판이 동시에 12deg 기울었다 수평으로 돌아오며 물방울 3개가 1회 튄다(680ms).
- **`title.png` 재활용:** `object-position:50% 52%; transform:scale(1)`로 중앙 오리와 더미를 유지한다. 상단 창문 여백에 로고를 두고 수영장 색을 탁하게 만드는 전체 scrim은 제거한다. **기존 관용도 segmented control은 타이틀에서 삭제**하고 기본값으로 시작한다.

### 5.11 가름돌 — F. 메커닉 타이포형

- **로고:** 중앙 보석의 균열을 기준으로 ‘가름’을 좌측, ‘돌’을 우측에 배치한다. `.game-logo{left:8%;right:8%;top:9dvh;display:grid;grid-template-columns:1fr 1fr}`; 가운데 6px 간격을 실제 균열처럼 남기고 자주/청록 서로 다른 면 색을 쓴다.
- **부제·단원:** 부제는 중앙 균열 아래 `top:31dvh;left:50%;transform:translateX(-50%)`; 단원은 좌하단 보석 단지 위가 아닌 `left:14px;bottom:18px`에 작은 두 줄로 둔다.
- **CTA:** 중앙 하단의 **보석 쐐기**. `left:50%;bottom:26px;transform:translateX(-50%);width:92px;height:72px;clip-path:polygon(50% 0,100% 100%,0 100%)`; 시각 문구는 아래 명판 ‘가르기’, 전체 hit box는 140×88px.
- **등장 모션:** 로고 전체가 중앙에서 좌우로 7px 갈라졌다 다시 2px만 닫히며 최종 균열을 남긴다(720ms). 파편은 DOM 6개 이하, 1회만 튄다.
- **`title.png` 재활용:** `object-position:50% 54%; transform:scale(1.03)`로 중앙 보석과 양쪽 단지를 모두 살린다. 텍스트는 상단 하늘 여백을 사용한다. **기존 관용도 segmented control은 타이틀에서 삭제**하고 재도전/설정으로 이동한다.

### 5.12 대칭 브레이커 — C. 대각 분할형

- **로고:** 중앙 광축을 기준으로 ‘대칭’을 왼쪽 청색 면, ‘브레이커’를 오른쪽 자홍 면에 거울처럼 배치한다. 두 그룹은 각각 `width:46%;top:14dvh`, 왼쪽 `text-align:right`, 오른쪽 `text-align:left`; 글자는 수평 유지하고 그룹만 `rotate(-4deg/4deg)`.
- **부제·단원:** 부제는 광축 위 `left:50%;top:43dvh;transform:translateX(-50%)`, 단원은 화면 상단 중앙 11px. 필 대신 광축과 같은 1px divider를 쓴다.
- **CTA:** 하단 받침의 **결정 잠금쇠**. `left:50%;bottom:22px;transform:translateX(-50%);width:176px;height:58px;clip-path:polygon(8% 0,92% 0,100% 50%,92% 100%,8% 100%,0 50%)`, 문구 ‘축 깨기’.
- **등장 모션:** 좌우 로고가 광축을 향해 18px 이동해 정확히 대칭을 만든 뒤, 140ms 후 각각 4px 바깥으로 튕겨 ‘깨짐’을 예고한다(780ms).
- **`title.png` 재활용:** `object-position:50% 30%; transform:scale(1.05)` 현행 초점을 유지한다. 중앙 광축을 가리지 않도록 UI를 좌우 분할하며, 좌우 색면은 각각 `rgba(0,180,255,.12)`와 `rgba(255,0,170,.12)`만 얹는다.

### 5.13 쌓기나무 공방 — B. 세로 족자형

- **로고:** 오른쪽 벽의 목재 판자처럼 `쌓기나무 / 공방` 두 묶음을 세로로 쌓는다. `.game-logo{right:12px;top:8dvh;width:112px;display:grid;gap:6px;transform:rotate(1deg);font-size:clamp(34px,9vw,48px);line-height:.9}`. 각 묶음은 밝은 목재 사각판 위 먹색 글자.
- **부제·단원:** 부제는 오른쪽 제목 아래의 작은 작업 지시서, 단원은 왼쪽 상단 창문 아래 `left:14px;top:18px;width:118px`.
- **CTA:** 하단 작업대의 **목공 바이스 손잡이**. `right:18px;bottom:22px;width:184px;height:58px;border-radius:8px 29px 29px 8px`, 문구 ‘의뢰 고정’.
- **등장 모션:** 제목 판자 2개가 위에서 10px씩 내려와 맞물리고, 마지막에 바이스 손잡이가 20deg 한 번 돌아간다(760ms).
- **`title.png` 재활용:** `object-position:48% 52%; transform:scale(1.01)`로 왼쪽 장인과 중앙 나무 탑을 보존한다. 오른쪽 도구/벽 구간에만 `linear-gradient(90deg,transparent,rgba(42,25,8,.64))`를 깐다.

### 5.14 기우뚱 나무 — D. 오브젝트 조립형

- **로고:** 나무의 가로 저울 막대 위에 ‘기우뚱’은 왼쪽 열매 더미, ‘나무’는 오른쪽 열매 더미처럼 얹는다. `.game-logo{left:8%;right:8%;top:13dvh;display:flex;justify-content:space-between;transform-origin:50% 100%}`; 글자 받침에 작은 토마토/배 색 원형 `::after`를 붙인다.
- **부제·단원:** 부제는 저울 축 바로 위 `top:37dvh;left:50%;transform:translateX(-50%)`; 단원은 상단 왼쪽 여백 `left:14px;top:16px`.
- **CTA:** 중앙 아래 캐릭터가 당기는 **저울 추 손잡이**. `left:50%;bottom:24px;transform:translateX(-50%);width:148px;height:60px;border-radius:30px`, 좌우에 토마토/배 원형을 달고 문구 ‘수평 맞추기’.
- **등장 모션:** 로고 전체가 시소처럼 `rotate(-5deg)`→`rotate(4deg)`→`rotate(0)` 한 번 기울었다 수평이 된다(820ms). 열매 2개만 6px 낙하한다.
- **`title.png` 재활용:** `object-position:50% 50%; transform:scale(1)`로 나무, 막대, 캐릭터를 모두 유지한다. 상단 복숭아색 여백은 그대로 텍스트 캔버스로 사용하고 전체 vignette를 제거한다.

### 5.15 쓱말이 — E. 세계 진입형

- **로고:** 우상단 재활용 컨테이너의 현판처럼 `right:18px;top:18px;transform:rotate(1deg);font-size:clamp(48px,14vw,68px)` 배치. 청록 단색 글자에 베이지 종이띠 그림자 3px만 둔다.
- **부제·단원:** 부제는 로고 아래 컨테이너 라벨, 단원은 왼쪽 상단 하늘에 `left:16px;top:20px;width:118px`로 분리한다.
- **CTA:** 화면 전경 캔의 실제 파란 손잡이와 겹치는 **포장 롤러 핸들**. `button{left:8px;bottom:18px;width:190px;height:96px;background:transparent}` 안에 지름 68px 노브와 ‘쓱 말기’ 명판을 둔다.
- **등장 모션:** 손잡이가 좌→우 34px 이동하며 로고 아래 종이띠가 `clip-path`로 0→100% 펼쳐진다(800ms, 1회). 배경 breathe와 CTA pulse는 제거한다.
- **`title.png` 재활용:** `object-position:52% 50%; transform:scale(1)`로 소녀의 손, 전경 캔, 롤러를 보존한다. CTA hit box는 전경 캔 손잡이에 정렬하고, 하늘에는 scrim을 넣지 않는다.

## 6. 공통 CSS/접근성 체크

```css
.title-primary {
  min-width: 44px;
  min-height: 44px;
  touch-action: manipulation;
  -webkit-tap-highlight-color: transparent;
}
.title-primary:focus-visible,
.title-howto:focus-visible,
.title-mute:focus-visible {
  outline: 3px solid #fff;
  outline-offset: 3px;
}
@media (prefers-reduced-motion: reduce) {
  .title-stage *, .title-stage *::before, .title-stage *::after {
    animation-duration: 1ms !important;
    animation-iteration-count: 1 !important;
    scroll-behavior: auto !important;
  }
}
```

- 사물형 CTA도 반드시 `<button type="button" class="title-primary">` 하나여야 한다. 장식 부품은 버튼 내부 `aria-hidden="true"`로 둔다.
- 로고의 시각 분할/세로 배열과 무관하게 `<h1 aria-label="정상 가로 제목">`을 유지한다.
- 최고 기록은 값이 있을 때만 단원 옆 작은 한 줄로 표시하고 별도의 필 한 층을 만들지 않는다.
- 도움말은 화면 가장자리의 44px hit area를 유지하되 CTA 바로 아래 고정하지 않는다. 각 원형의 빈 모서리로 이동해 하단 적층을 해체한다.
- 360×640px에서 로고, CTA, 도움말, 음소거가 서로 겹치지 않아야 한다. 높이 700px 이하에서는 카피를 삭제하지 말고 `clamp()`와 2행 단원으로 압축한다.

## 7. 구현 우선순위 — 사용자 체감 큰 순

1. **말아봇** — 거대 공방 레버가 기존 버튼 문법을 가장 즉시 깨뜨린다.
2. **대칭 브레이커** — 중앙 광축을 활용한 좌우 분할로 첫 실루엣이 완전히 달라진다.
3. **원펼침** — 세로 족자와 두루마리 CTA가 장르·시대를 한 화면에서 규정한다.
4. **소수 스매시** — 광검 대각선을 정보 구조로 바꾸는 효과가 크다.
5. **기우뚱 나무** — 로고 자체의 시소 동작이 메커닉을 가장 직관적으로 예고한다.
6. **쓱말이** — 전경 손잡이를 직접 작동시키는 세계 진입감이 강하다.
7. **가름돌** — 제목을 실제로 갈라 보이고 시작 전 관용도 UI도 함께 제거한다.
8. **첨벙** — 수영판 로고와 사물 CTA로 바꾸며 시작 전 모드 선택을 제거한다.
9. **젤리 게이트** — 축소되는 타이포가 분수 곱셈의 핵심을 말 없이 보여 준다.
10. **네모공장** — 하단 포스터 로고와 컨베이어 스위치로 캐릭터를 살린다.
11. **쩍쩍** — 초대형 2단 로고와 톱 손잡이가 현행 상단 로고를 크게 벗어난다.
12. **반디탑** — 큐브 계단 로고가 아트의 중앙 탑과 결합한다.
13. **어림 대시** — 레일 원근과 출발 패드가 강하지만 기존 아트 자체가 이미 역동적이다.
14. **쌓기나무 공방** — 우측 목재 세로판으로 정보 축을 바꾸는 안정적 개편이다.
15. **꿀몇잔** — 아트가 이미 강하므로 하단 라벨·단지 마개 중심의 정돈부터 적용한다.

이 순서는 개발 난이도가 아니라 **첫 1초에 ‘다른 게임’으로 느껴지는 변화량** 기준이다. 1~6번을 먼저 구현하면 6개 원형 중 A를 제외한 5개가 조기에 실물 검증되어, 이후 게임에 템플릿을 다시 복제하는 위험도 줄일 수 있다.
