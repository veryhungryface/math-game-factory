<!--
사람 큐레이션 문서 (리서치 에이전트 작성, 2026-08-19 완결).
game-references.md 는 JSON에서 자동 생성되는 '전체 목록'이고, 이 파일은
인사이트·라이선스 규칙·상위 20개 컨셉을 담은 '요약판'이다. 자동 재생성으로
덮어써지지 않는다. 기획 에이전트는 이 파일을 우선 읽는다.
-->

# 초등 수학 교육게임 레퍼런스 조사 — 아이디어 광산

- 수집일: 2026-08-17~18 · 항목 수: **사이트/게임 133 + 메커닉 22 + 비주얼 레퍼런스 42 = 197**
- 기계가 읽는 원본: `game-references.json` (같은 폴더)
- **검증 원칙**: 모든 URL은 curl/WebFetch로 실제 접속 확인. 죽은 링크는 ❌로 명시하고 '시장 공백 신호'로만 기록.
- **저작권 원칙**: 게임 **규칙(메커닉)은 저작권 보호 대상이 아니지만**, 아트·캐릭터·사운드·코드·이름 같은 **표현은 보호 대상**이다. 우리는 어떤 항목에서도 상표·캐릭터·에셋을 베끼지 않고 **메커닉만 차용**한다.

---

## 0. 조사 전체에서 나온 10대 인사이트

1. **"문제 = 조작"이 정답이다.** Prodigy처럼 게임과 문제가 분리되면 문제는 세금이 된다. Count Control Legends(게이트 러너)·SplashLearn·ST Math처럼 계산 자체가 조준/조작인 게임이 이긴다.
2. **게이트 러너 메커닉은 이미 '수학 게임'으로 검증됐다.** Poki/CrazyGames 상위권의 Count Masters류는 ×2/÷2 게이트 고르기가 게임의 전부다. 게이트 값을 분수·소수·비율로 바꾸면 그대로 5~6학년 교재가 된다.
3. **교사 채택 공식 = 단원 매핑 + 학급 배포 + 실시간 모니터링.** 국내에서 이 3요소를 갖춘 비바샘 수학놀이터가 교실을 잡았고, 띵커벨은 '학생 로그인 없음(방번호 입장)'으로 마찰을 0으로 만들었다.
4. **콘텐츠 1회 입력 → N개 게임 자동 생성(Wordwall 구조) + 절차적 문항 생성(99math/MathsBot)**이 콘텐츠 비용을 지배한다. 문항 은행이 아니라 문항 '생성기'를 만들어야 한다.
5. **한국 시장에 공백이 크다**: e학습터·아이엠스쿨 2026-02 종료, 위두랑 종료 수순, 디지털교과서에 수학 없음, EBSMath 게임존은 측정·자료와가능성 영역이 비어 있음. '설치 없는 웹 + 교과서 단원 정밀 매핑' 포지션이 열려 있다.
6. **라이선스 횡재 목록**: 2048(MIT), Coup Ahoo(MIT), agar.io-clone(MIT), Matter.js/LittleJS/KAPLAY/Ecctrl(MIT), Babylon(Apache-2.0), PhET(CC-BY), Bruno Simon 포트폴리오(MIT+CC0 음악), Kenney/Quaternius(CC0). 이것만 조합해도 프로토타입 3~4개가 나온다.
7. **라이선스 지뢰 목록**: Hextris·clumsy-bird·TuxMath(GPL 전염), Shadertoy(기본 CC BY-NC-SA), Phaser 예제의 '에셋'(코드는 MIT지만 에셋은 상업 사용 불가), KenKen(상표), Suika(Aladdin X 상표), PBS Kids/Nitrome/Lusion(전면 저작권).
8. **저사양이 진짜 제약이다.** 학교 태블릿·학교 와이파이 기준으로 GameSnacks(2G에서 5초 로딩)와 js13kGames(13KB) 규율, Matter.js(2D, GPU 불요), Infinitown의 '유한 타일 랩핑' 기법을 표준으로 삼을 것.
9. **리텐션 장치는 이미 공식이 있다**: 일일 시드 문제(Wordle의 epoch 시드 패턴 — 서버 없이 반 전체가 같은 '오늘의 문제'), streak(Sudoku.com), 수집 메타(Blooket의 Blook), 인쇄 가능한 자격증(Timestables.co.uk).
10. **저연령 UX의 표준은 ST Math와 Chrome Music Lab Song Maker**: 텍스트 0, 설명 0, 조작이 곧 튜토리얼, 즉각적 시청각 피드백. 실패 페널티가 큰 IXL식 설계는 반면교사.

---

## 1. 교육용 게임 모음 사이트 (해외)

### 1-1. 반드시 뜯어볼 상위 레퍼런스
| 이름 | URL | 왜 봐야 하나 |
|---|---|---|
| Blooket | https://www.blooket.com/ | ⭐'정답→자원→별도 게임' 2단 구조. 약한 아이도 운으로 역전 → 참여율 폭발. Blook 수집 메타 |
| Gimkit | https://www.gimkit.com/ | ⭐정답 상금으로 업그레이드를 사는 복리 경제. '벌까 투자할까'가 반복 풀이를 위장 |
| Wordwall | https://wordwall.net/ | ⭐문항 1회 입력 → 30여 개 게임 템플릿 자동 생성. 우리 아키텍처의 원형 |
| Baamboozle | https://www.baamboozle.com/ | ⭐학생 기기 0대로 작동 + '점수 뺏기' 카드. 구현 난이도 최저, 교실 효과 최상 |
| Topmarks Hit the Button | https://www.topmarks.co.uk/maths-games/hit-the-button | ⭐설명 0초·60초 세션·최고기록. 분수/소수/비율 버전이 비어 있음 = 기회 |
| ST Math | https://www.mindeducation.org/programs/st-math/ | ⭐말·글 없는 시각 퍼즐로 수학 구조를 가르침. 교육 게임 설계의 교과서 |
| PhET (math) | https://phet.colorado.edu/en/simulations/filter?subjects=math | ⭐CC-BY 오픈소스 시뮬. Fraction Matcher·Proportion Playground가 5~6학년 직결 |
| Polypad (Mathigon) | https://polypad.amplify.com/ | ⭐최강 가상 교구 캔버스. '정답 없는 놀이터'의 표준 |
| Arcademics Grand Prix | https://www.arcademics.com/games/grand-prix | 실시간 멀티 레이스: 정답 속도 = 차 속도. 분수/소수 버전이 드묾 = 빈틈 |
| 99math | https://www.99math.com/ | 문항 자동 생성으로 교사 준비 0분. 수학 전용 Kahoot |
| Sudoku.com | https://sudoku.com/ | 일일 챌린지 + streak + 통계 대시보드 — 리텐션 UI 표본 |
| Khan Academy 한국어 | https://ko.khanacademy.org/math/kor-5th-2 | ⭐한국 5-2/6-2 학기 단원 코스가 실재(kor-6th-2 포함). 단원 매핑 레퍼런스 |

### 1-2. 퀴즈쇼/교실 플랫폼
Kahoot(https://kahoot.com/ — 음악·카운트다운 긴장감, 학생 화면에 문제 없음), Wayground(구 Quizizz, https://wayground.com/ — 자기 페이스, ⚠️리브랜딩 확인됨), Legends of Learning(https://www.legendsoflearning.com/ — 게임 마켓플레이스 전략), Kongregate(https://www.kongregate.com/ — 서드파티 게임 위 배지 메타 레이어).

### 1-3. 포털형
Coolmath Games(https://www.coolmathgames.com/ — '수학 이름을 단 오락실', 재미 우선의 반면교사이자 증거), ABCya(https://www.abcya.com/ — 학년 정렬의 표준), Math Playground(https://www.mathplayground.com/ — Thinking Blocks 막대모델, 학년별 grade_5_games.html/grade_6_games.html), Funbrain(아케이드 맵으로 게임 묶기), Hooda Math, Sheppard Software, Turtle Diary, Math Game Time, MathNook, Fun4theBrain, RoomRecess, Coolmath4Kids, Multiplication.com(단일 주제 집중의 성공례), Starfall(무텍스트 저연령 UX), CrazyGames math 태그(https://www.crazygames.com/t/math).

### 1-4. 가상 교구/도구
Toy Theater(https://toytheater.com/ — 마찰 0), Didax(https://www.didax.com/math/virtual-manipulatives.html), Math Learning Center 앱(https://www.mathlearningcenter.org/apps — fractions/geoboard/number-pieces 개별 URL 확인), Visnos(분수원·시계 시각화), MathsBot(⭐문제 생성기), MathsPad, Mathsframe, GeoGebra(⚠️상업 이용 별도 라이선스), Desmos(교사/학생 포털은 Amplify 도메인으로 이전), Illuminations(NCTM), Gynzy, Math is Fun.

### 1-5. 커리큘럼형 플랫폼
Prodigy(MMORPG, 게임·학습 분리의 한계), SplashLearn(문항=게임), Matific(⭐3분 에피소드 2,000개, 한국어 지원, 단원 정밀 매핑은 미흡), DreamBox(적응형), Zearn(CRA 단계·힌트 설계), IXL(실패 페널티 반면교사), XtraMath(그리드 채우기 습관 UI), MathFactLab, Timestables.co.uk(자격증 보상), BrainPOP, Khan Academy Kids, Scratch(창작·리믹스, CC BY-SA).

### 1-6. 퍼즐 형식(퍼블릭 도메인 규칙)
Nonograms(https://www.puzzle-nonograms.com/), 스도쿠, 계산 스도쿠(⚠️'KenKen'은 상표 — 이름 금지), BrainBashers, PuzzleScript(https://www.puzzlescript.net/ — MIT, 격자 퍼즐 프로토타이핑 도구), 칠교·펜토미노·마방진(퍼블릭 도메인 — EBSMath 게임존과 겹치지 않게 변형).

---

## 2. 한국 교육게임/에듀테크

### 2-1. 국가·공공
| 이름 | URL | 요점 |
|---|---|---|
| 똑똑! 수학탐험대 | https://www.toctocmath.kr/ | 교육부·KERIS. 교과 단원·차시 매핑 + AI 추천 + 탐험 서사·마을 꾸미기. **국내 사실상 표준 비교 대상** |
| EBSMath 게임존 | https://m.ebsmath.co.kr/mathquiz/Main | 칠교·펜토미노·마방진·스도쿠 무료 즉시 플레이. ⚠️측정·자료와가능성 영역 공백 = 우리 기회 |
| 에듀넷 티-클리어 | https://www.edunet.net/ | KERIS 자료 허브. 교육디지털원패스 연동 |
| 디지털교과서 | https://dtbook.edunet.net/ | ⚠️제공 교과에 **수학 없음**(사회/과학/영어) = 공백 |
| AIDT 안내(KERIS) | https://www.keris.or.kr/main/aidtMain.do | 수학이 AIDT 1차 교과 — 'AIDT 보완재' 포지셔닝 근거 |
| 하이러닝(경기) | https://hi.goe.go.kr/ | 초5~6 수학 전 차시 수업설계안 탑재 |
| 학교알리미 | https://www.schoolinfo.go.kr/ | 학교 규모 데이터(타깃 사이징) ⚠️EUC-KR 인코딩 |
| 울산수학문화관 등 | https://use.go.kr/usmcc/index.do | ⚠️'수학마당' 단일 사이트는 없음 — 시도별 수학체험센터로 분산(칠곡 gbe.kr/cgmath, 진주 gnmc.gne.go.kr/jjfm, 세종 edu.sje.go.kr/math) |
| ❌ e학습터 | cls.edunet.net | **2026-02-28 완전 종료, DNS 미해석 확인** — 시장 공백 신호 |
| ❌ 위두랑 | https://rang.edunet.net/main.do | 접속은 되나 서비스 종료 공지 게시 중 — 신규 의존 금지 |

### 2-2. 민간·상용
| 이름 | URL | 요점 |
|---|---|---|
| 비바샘 수학놀이터 | https://e.vivasam.com/themeplace/mathArcade/main | ⭐단원별 게임 + 학급 배포 + 실시간 모니터링 + 랭킹. **국내 1순위 벤치마크** (교사 인증 필요) |
| 띵커벨 | https://www.tkbell.co.kr/ | ⭐국내 1위 교실 퀴즈툴. 학생은 방번호만으로 입장(q1.tkbell.co.kr) — 마찰 0 |
| ZEP QUIZ | https://quiz.zep.us/en | ⭐퀴즈 + 아바타 성장 + 맵 탐험. 웹 즉시 실행 — 기술 스택이 우리와 가장 근접 |
| 퀴즈앤 | https://quizn.show/ | 한국형 Kahoot. 참가자 무로그인 |
| 클래스카드 | https://www.classcard.net/ | ⭐암기 세트→단계 반복→퀴즈배틀 루프(영어 전용) — 구구단/분수 감각 훈련에 구조 이식 가능 |
| 아이스크림 홈런 | https://www.home-learn.co.kr/ | 전용 태블릿 구독. 학부모 알림 이중 구조. '설치 없는 웹'의 반대편 |
| 밀크T초등 | https://www.milkt.co.kr/ | 천재교과서 직영. 교과서 판권이 해자 |
| 웅진스마트올 | https://smartall.wjthinkbig.com/index | ⚠️smartall.co.kr은 SSL 오류 — wjthinkbig 도메인이 공식 |
| 클래스팅 | https://www.classting.com/ | AI 튜터 '젤로' + 학급 소셜. 4,600여 학교 |
| 매쓰홀릭 | https://www.matholic.com/ | 오답 자동 재출제·쌍둥이 문제 생성(B2B). 교육부 가이드북 수록 사례 |
| 토도수학 | https://todoschool.com/en/math | ⭐한국개발 글로벌 성공. '부모 없이 혼자' 원칙, 무텍스트 UX. ⚠️웹 버전 없음 = 우리의 접근성 우위 |
| 아이스크림(교사용) | https://www.i-scream.co.kr/ | 차시 100% 정렬 수업자료 — '준비 시간 0'의 표준 |
| 인디스쿨 | https://indischool.com/ | ⭐교사 커뮤니티(교사 인증 필수). 자료는 게시자 저작물 — 개별 허락 필요. 홍보성 접근 금지, '무료 공개+기여'로 |
| 엔트리 | https://playentry.org/ | ⭐네이버커넥트. 출처 표기 시 교육적 이용 자유 — 프로토타입/확산 경로 |
| 째깍악어 | https://parent.tictoccroc.com/ | 돌봄 매칭(게임 아님). 학부모 접점 제휴 후보 |
| ❌ 아이엠스쿨 | school.iamservice.net | 2026-02-28 종료 — NHN EDU는 아이엠티처/아이엠클래스로 재편 |
| ❌ 노리(KnowRe) | knowre.co.kr | 2021 사업 중단. '기술이 좋아도 교실 유통 없이는 죽는다'는 교훈 |

---

## 3. 단순 캐주얼 게임 / 오픈소스 / 게임잼

### 3-1. 포털
itch.io HTML5(https://itch.io/games/html5 — 71만 건, ⚠️학생 직접 노출 금지), **itch.io math 태그(https://itch.io/games/tag-math — 3,462건, 경쟁 스캔용 최고 페이지)**, Poki(https://poki.com/ — '10초 내 첫 재미', 개발자 유통 프로그램), CrazyGames, Armor Games(업그레이드 경제), Nitrome(단일 규칙 + juice, 전면 저작권), GameSnacks(https://gamesnacks.com/ — **2G에서 5초 로딩** 성능 규율), Silvergames math(반면교사 최저선), Kongregate. ⚠️ldjam.com은 SSL 만료로 접속 불가 — LD 출품작은 itch 미러(https://itch.io/games/html5/tag-ludum-dare-57) 사용.

### 3-2. 게임잼 발굴 — 이 조사의 최대 수확
| 게임 | URL | 발견 |
|---|---|---|
| ⭐⭐ balaline (Gamedev.js 2025 우승) | https://ex0o.itch.io/balaline | 숫자들 위로 선 하나를 움직여 **양쪽 합이 같게** 가르기. 방정식이자 기하 게임. 판정은 외적 부호 하나(~100줄). 라이선스 없음 — 메커닉만 재구현 |
| ⭐⭐ Coup Ahoo (js13k 2024 2위) | https://js13kgames.com/2024/games/coup-ahoo | 주사위=화물=체력=공격력인 해적 로그라이크. **MIT — 즉시 포크 가능**(github.com/js13kGames/coup-ahoo). '평균과 가능성' 단원 최적 템플릿 |
| ⭐ Balance Ma'atters (Gamedev.js 2025) | https://leokuo0724.itch.io/balance-maatters | 양팔저울 2개를 동시에 균형 잡는 카드배틀 — 양팔저울은 이미 한국 초등 표준 교구 |
| ⭐ Pack (LD57) | https://plasmastarfish.itch.io/pack | 불규칙 물건을 가방에 채우는 무압박 퍼즐 — 도형·넓이·들이와 1:1 |
| Goop Snake (GMTK 2025 1위) | https://waeaves.itch.io/goop-snake | 자기 몸을 통과하는 뱀 — '아는 규칙 하나 뒤집기'의 모범 |
| Make Ten Deluxe | https://pancelor.itch.io/make-ten-deluxe | '합 10 만들기' 아케이드 35+모드 — **모드 목록 자체가 수와 연산 로드맵** |
| 13th Floor (js13k 2024 1위) | https://js13kgames.com/2024/games/13th-floor | 13KB 안의 진짜 WebGL 3D — 기술 기준점 |
잼 인덱스: js13kGames(https://js13kgames.com/ — 전 출품작 소스 공개 의무), Gamedev.js Jam(https://gamedevjs.com/jam/2025/ — 오픈소스 챌린지 트랙), GMTK 결과(https://itch.io/jam/gmtk-2025/results, gmtk-2024는 테마 'Built to Scale' = 측정·비율 아이디어 광산).

### 3-3. 오픈소스 코드 레퍼런스 (라이선스 명시)
| 저장소 | 라이선스 | 용도 |
|---|---|---|
| github.com/gabrielecirulli/2048 (play2048.co) | ⭐MIT | 타일형 수학 게임의 모델-뷰 분리 템플릿. 분수/소수 재타깃 용이 |
| github.com/owenashurst/agar.io-clone | ⭐MIT | Socket.IO 실시간 멀티 — '반 대항전' 레퍼런스. 봇 대전으로 축소 가능 |
| github.com/liabru/matter-js | ⭐MIT | 2D 물리 — 저울·머지·굴리기의 정답. GPU 불요 |
| github.com/KilledByAPixel/LittleJS | ⭐MIT | 초경량 엔진 + 예제 게임 50+ |
| github.com/kaplayjs/kaplay | ⭐MIT | 최저 인지부하 문법 — 학생 창작 파트의 정답(⚠️구 kaboom은 지원 종료) |
| github.com/straker/kontra | MIT | 13KB급 마이크로 라이브러리 |
| Phaser (phaserjs/phaser, labs.phaser.io) | ⚠️코드 MIT / **예제 에셋은 상업 사용 불가** | 코드만 쓰고 스프라이트 전부 교체 |
| PixiJS (pixijs.com/8.x/examples) | MIT (⚠️구 examples 저장소는 아카이브) | 렌더러 직접 제어 |
| github.com/wayou/t-rex-runner | BSD-3 (⚠️공룡 스프라이트는 Google 저작물) | 러너+퀴즈 접목 고전 템플릿 |
| Wordle 클론 (github.com/PavlikPolivka/wordle) | ⭐MIT | **epoch 결정론적 일일 시드** 패턴 — 서버 없는 '오늘의 문제' |
| moonfloof/suika-game | ⚠️NOASSERTION — 사용 전 LICENSE 정독 | matter.js 머지 구현 중 가장 읽기 쉬움. 'Suika' 이름·과일 아트 금지 |
| Hextris, clumsy-bird, TuxMath | ⚠️**GPL — 코드 복사 금지, 읽기만** | 구조 참고 |
| CarnegieLearning/MathFluency, github.com/topics/math-game | 저장소별 확인 | 채굴 경로 |

### 3-4. 초등생이 빠져드는 모바일 게임 → 핵심 루프와 수학 접목 (22개 메커닉, JSON `mechanics` 전체 수록)
| 메커닉 (원전) | 핵심 루프 | 대표 수학 접목 | 난이도 |
|---|---|---|---|
| 산술 게이트 러너 (Count Masters) | 달리며 ×/÷/+/- 게이트 선택, 군중 불리기 | 분수·소수 곱셈, 비례배분, ÷(1/2)의 직관 | 2 |
| 무한 러너 (Subway Surfers) | 3레인 회피 + 수집 + 기록 갱신 | 레인=정답 선택, 소수 자릿수 수집, 어림 게이트 | 3 |
| 호핑 (Crossy Road) | 한 칸씩 타이밍 도강 | 좌표평면, 대칭 거울 캐릭터, 배수 밟기 | 2 |
| 슬래시 (Fruit Ninja) | 드래그 한 번으로 베기+콤보 | 약수만 베기, 1/3 지점 자르기, 대칭축 절단 | 2 |
| 매치3 (Candy Crush) | 스왑→3연결→연쇄 | 동치분수 매치, 0.5=50%=1/2 표현 매치, 합10 | 2 |
| 머지 (수박게임/2048) | 같은 것 병합→상위 단계 | 1/8+1/8=1/4, 0.001→0.01→0.1, cm→m | 2 |
| 오토배틀 (Vampire Survivors) | 자동 공격 + 3택 성장 | ×1.5 vs +20 비교, 소수 배율 함정 선택지 | 3 |
| 덱빌딩 (Balatro) | (점수+보너스)×배수 부풀리기 | 곱셈·분배법칙, ×3/2 조커, 남은 덱 확률 | 2 |
| 방치형 (Cookie Clicker) | 클릭→자동화→복리 | 억·조 큰 수, 가성비 비교(비와 비율), 등비 비용 | 1 |
| 타워디펜스 (Bloons) | 배치·업그레이드로 웨이브 방어 | 사거리=원(반지름 2배→넓이 4배), 예산 비례배분 | 3 |
| 리듬 (FNF/태고) | 박자 타이밍 입력 | 음표=분수, BPM 3:2 비례 | 3 |
| 물리 발사 (Angry Birds) | 각도·힘 조절 발사 | 각도 측정, 거리 기록→표·그래프 | 3 |
| 로프 물리 (Cut the Rope) | 자르는 순서·타이밍 퍼즐 | 줄 길이 단위 환산, 무게중심=평균 | 3 |
| .io 성장 (agar/slither) | 먹고 커지고 먹히는 30초 판 | 반지름vs넓이 체감, 대소 즉시 판단, 분열=비례배분 | 4(봇이면 2) |
| 사회추론 (Among Us) | 임무+거짓말 색출 투표 | '수학 임포스터' 검산 색출, 진술 통계 추론 | 5 |
| 장애물 서바이벌 (Stumble Guys) | 다수 동시 탈락전 | 계산 관문, 생존자 수 그래프 | 4(고스트 3) |
| 아레나 (Brawl Stars) | 3분 팀전+트로피 | 배율 빌드 계산 (교실용으론 과함) | 5 |
| 실시간 퀴즈쇼 (Kahoot/Blooket) | 방 코드 입장→동시 풀이→순위 | 전 단원 범용 껍데기 + 2단 구조 | 3 |
| 수집/가챠 (Blooket/Prodigy) | 랜덤 뽑기→도감 | ⭐확률 공개→'이론vs실제 빈도' = 가능성 단원 그 자체 | 1 |
| 스택 (Stack/Helix) | 정밀 타이밍으로 쌓기 | 직육면체 겉넓이·부피 실시간 표시, 3면 투상 | 1 |
| 샌드박스 (Scratch/Polypad) | 만들고 공유·리믹스 | 대칭 도형 대회, 쌓기나무 투상 퀴즈 UGC | 3 |
| 숨은그림 (I Spy) | 조건 탐색 클릭 | '원기둥 전개도만 찾기', '평균보다 큰 값 찾기' | 1 |

---

## 4. 비주얼 "와우" 웹 3D 레퍼런스 (요점만 — 42개 전체는 JSON `visual_refs`)

**바로 쓰는 것(관대한 라이선스)**
- Bruno Simon 포트폴리오 https://bruno-simon.com/ — ⭐**MIT + Blender 파일 공개 + 음악 CC0**. 차 몰고 부딪히는 탐색 모델.
- three.js 공식 예제 https://threejs.org/examples/ (MIT): games_fps.html(물리엔진 0의 완성형 3D 루프), webgl_gpgpu_birds(공짜 새떼 연출), webgl_points_waves(20줄 앰비언트 배경), physics_rapier_instancing(숫자 블록 낙하), webgpu_compute_particles·webgpu_tsl_galaxy(⚠️WebGPU 필요 — WebGL 폴백 필수).
- Ecctrl https://ecctrl.app/ (MIT) — 모바일 조이스틱 내장 캐릭터 컨트롤러. drei https://drei.pmnd.rs/ — `<Sparkles>+<Float>` 두 줄로 마법 연출. R3F 예제 https://r3f.docs.pmnd.rs/getting-started/examples 의 'Game prototypes' 줄.
- 물리: **Matter.js(2D, 저사양 최강)** > Rapier(Apache-2.0, WASM 최속) > cannon-es(MIT, WASM 없음=학교 프록시 안전) > ammo.js(zlib, 최후 수단). enable3d(MIT)는 2D 로직+3D 장식 경로.
- Babylon.js Playground https://playground.babylonjs.com/ (Apache-2.0, 내장 GUI가 교육 게임에 강점). PlayCanvas https://playcanvas.com/explore (엔진 MIT, 로드 크기 최상급).
- 에셋: **Kenney https://kenney.nl/assets (CC0)**, **Quaternius https://quaternius.com/ (CC0, 리깅 캐릭터)**, Poly Pizza(CC-BY, 표기 필수).
- Chrome Music Lab Song Maker https://musiclab.chromeexperiments.com/Song-Maker/ — ⭐⭐초등 UX의 정점(무텍스트 격자+즉각 피드백), 오픈소스(Apache-2.0 계열).

**기법만 훔치는 것(전면 저작권)**
- Little Workshop Infinitown https://demos.littleworkshop.fr/infinitown — ⭐유한 타일을 랩핑해 무한 세계(최고 가성비 트릭). Keep Out! https://www.playkeepout.com/ — 브라우저 탭에서 진짜 3D 게임이 된다는 증명.
- Lusion https://lusion.co/ — 물리 솔버 없이 이징만으로 무게감. Jordan Breton https://jordan-breton.com/ — ⭐웨이포인트 점프 카메라(아이가 길을 잃지 않는다). Active Theory, makemepulse('light as air' 철학).
- oimo.io/works — 말랑한 물리 장난감 18종(문제 사이 보상 연출 아이디어). Sandspiel https://sandspiel.club/ — 목표 없는 창발 놀이.
- Codrops https://tympanus.net/codrops/ (글별 조건 확인): BatchedMesh 데모(서로 다른 숫자 타일 50개=드로우콜 1), Interactive Text Destruction(오답 폭발 연출), akella의 Pixel Distortion/Particle Rain(쿼드 1장 셰이더로 화면 살리기 — 저렴한 쪽 셰이더).
- ⚠️ Shadertoy https://www.shadertoy.com/ — 기본 CC BY-NC-SA + 고부하 레이마칭 다수. 단순 노이즈/그라디언트만.
- 학습: Three.js Journey https://threejs-journey.com/ (유료, 베이크드 라이팅 장이 학교 사양의 최대 지렛대), Three.js Workshops(akella).

---

## 5. 저작권·라이선스 요약 규칙

1. **메커닉(규칙)만 차용한다.** 게임 규칙은 저작권 보호 대상이 아니다. 단, 특정 **표현**(캐릭터, 아트, 사운드, 코드, 레벨 데이터, 이름·로고)은 보호 대상이므로 전부 자체 제작 또는 CC0/CC-BY 에셋 사용.
2. 상표 금지어: KenKen, Suika Game/수박게임(캐릭터 포함), Blook, Kahoot, Crossy Road·Subway Surfers 등 게임명·캐릭터명. 마케팅 문구에서 "~류(like)" 표현도 신중히.
3. 코드: **MIT/Apache/BSD/CC0만 복사 가능**(표기 의무 이행). **GPL은 읽기만**(폐쇄 배포 시 복사 금지). NOASSERTION은 정독 후 판단. Phaser '예제 에셋', Shadertoy 'NC' 조항 같은 **부분 라이선스 함정** 주의.
4. 국가·공공(똑똑수학탐험대, EBS, KERIS) 콘텐츠도 무단 재사용 불가 — 공공누리 유형 개별 확인.
5. 인디스쿨 자료는 게시자 개인 저작물, 클래스카드·띵커벨 등의 사용자 제작 세트도 마찬가지 — 구조만 참고.

---

## 6. ⭐ 초등 5~6학년 2학기 수학에 바로 쓸 만한 상위 20개 게임 컨셉

기준 단원 — **5-2**: ①수의 범위와 어림하기 ②분수의 곱셈 ③합동과 대칭 ④소수의 곱셈 ⑤직육면체 ⑥평균과 가능성 / **6-2**: ①분수의 나눗셈 ②소수의 나눗셈 ③공간과 입체 ④비례식과 비례배분 ⑤원의 넓이 ⑥원기둥·원뿔·구. (2022 개정, ko.khanacademy.org kor-5th-2/kor-6th-2 및 홈런 단원 요약으로 교차 확인)

| # | 제목안 | 차용 메커닉 (원전) | 수학 단원 | 왜 재밌는지 |
|---|---|---|---|---|
| 1 | **분수 게이트 대질주** | 산술 게이트 러너 (Count Control Legends) | 5-2 분수의 곱셈 | 달리는 중에 ×2/3 vs ×3/2 게이트를 3초 안에 골라야 한다. 계산이 곧 조작이고, 틀리면 군중이 눈앞에서 줄어든다. 검증된 Poki 상위권 메커닉 |
| 2 | **거꾸로 게이트** ("나눴는데 왜 커져?") | 게이트 러너 변형 | 6-2 분수의 나눗셈 | ÷(1/2) 게이트를 지나면 군중이 2배가 되는 순간의 인지 충격 — 분수 나눗셈 최대 오개념을 몸으로 교정 |
| 3 | **군중 갈림길 3:2** | 게이트 러너 + 분기 (Count Masters) | 6-2 비례식과 비례배분 | 군중을 두 다리로 3:2로 갈라 보내야 둘 다 관문을 통과 — 비례배분을 슬라이더 드래그 한 번으로 |
| 4 | **소수점 머지 공장** | 머지/드롭 (수박게임+2048, matter.js) | 5-2 소수의 곱셈 / 6-2 소수의 나눗셈 | 0.001 구슬 10개가 닿으면 0.01로 합쳐진다 — ×10/÷10 자릿값 이동을 물리 도파민으로. 통 넘치기 직전의 긴장 |
| 5 | **동치분수 팡팡** | 매치3 (Candy Crush) | 5-2 분수의 곱셈(약분·동치) | 1/2·2/4·3/6처럼 '값이 같은' 타일을 매치 — 연쇄 캐스케이드가 터질 때 약분이 손에 붙는다 |
| 6 | **반올림 사격장** | Hit the Button 스피드 + 러너 어림 게이트 | 5-2 수의 범위와 어림하기 | '반올림해서 3000이 되는 수' 게이트만 통과하는 60초 스피드런 — 설명 0초, 최고기록 갱신 욕구 |
| 7 | **대칭의 검** | 슬래시 (Fruit Ninja) | 5-2 합동과 대칭 | 날아오는 도형을 대칭축 그대로 베어야 점수 — 드래그 한 번의 손맛으로 대칭축 감각을 반복 훈련 |
| 8 | **짝꿍 도형 찾기(합동 임포스터)** | 숨은그림 + 사회추론 라이트 | 5-2 합동과 대칭 | 도형 무리에서 '합동이 아닌 가짜' 하나를 제한시간에 색출 — 뒤집기·돌리기를 눈으로 검산하는 긴장 |
| 9 | **상자 타워** | 스택 (Stack) | 5-2 직육면체 | 정밀 타이밍으로 쌓을 때마다 겉넓이·부피가 실시간 갱신 — 어긋난 만큼 잘려나가는 시각적 아픔이 곧 피드백 |
| 10 | **평균 저울 카드배틀** | 양팔저울 카드게임 (Balance Ma'atters) | 5-2 평균과 가능성 | 카드를 낼 때마다 저울이 기운다 — 평균 맞추기가 생존 조건. 양팔저울은 이미 교실 표준 교구라 설명 불필요 |
| 11 | **뽑기 연구소** | 수집/가챠 확률 공개 (Blooket Blook) | 5-2 평균과 가능성 | 확률표를 공개하고 뽑은 기록이 그래프로 쌓인다 — '이론 확률 vs 내 실제 빈도'를 도감 욕심으로 학습 |
| 12 | **주사위 함대전** | 주사위 로그라이크 (Coup Ahoo, **MIT 포크 가능**) | 5-2 평균과 가능성 | 주사위 눈이 체력이자 공격력 — 모든 전투가 기대값 비교인데 판돈이 커진다. 10분 런, 터치만 |
| 13 | **등호의 선** | 합 가르기 라인 퍼즐 (balaline, Gamedev.js 2025 우승) | 5-2 평균과 가능성(평균)·수 감각 | 선 하나를 돌려 양쪽 합을 같게 — 방정식이자 기하. 판정 코드 100줄로 우승한 검증 메커닉 |
| 14 | **쌓기나무 스캐너** | 샌드박스 + 투상 퀴즈 UGC (Scratch 공유 루프) | 6-2 공간과 입체 | 내가 쌓은 블록의 위/앞/옆 모습을 친구가 맞힌다 — 창작·출제·도전의 UGC 루프. three.js 박스면 충분 |
| 15 | **전개도 접기 공방** | 인터랙티브 전개도 (three.js net-folding, Polypad) | 6-2 원기둥·원뿔·구, 5-2 직육면체 | 전개도를 드래그하면 3D로 접히는 순간의 '아하' — 국내 웹에서 비어 있는 3D 니치 정면 공략 |
| 16 | **원의 성 타워디펜스** | 타워디펜스 (Bloons) | 6-2 원의 넓이 + 비례배분 | 사거리=원. 반지름 2배 업그레이드가 넓이 4배임을 웨이브 방어로 체감. 예산은 3:2 비례배분 |
| 17 | **넓이 먹기 아레나** | .io 성장 (agar.io, MIT 클론 + 봇) | 6-2 원의 넓이 | 나보다 작은 원만 먹을 수 있다 — 반지름·넓이·대소 비교가 생존 판단. 30초 한 판, 봇전이면 서버 불요 |
| 18 | **단가 마켓** | 경제 업그레이드 (Gimkit) + 방치형 | 6-2 소수의 나눗셈 | 1.5L에 3,600원 vs 0.8L에 2,000원 — 단위당 가격 계산으로 장사 밑천을 복리로 불린다. '벌까 투자할까' |
| 19 | **배수 조커 덱** | 점수 공식 덱빌딩 (Balatro) | 5-2 분수·소수의 곱셈 | (기본점+보너스)×배수 공식을 ×3/2, ×0.5 조커로 부풀린다 — ×0.5가 점수를 깎는 함정에서 소수 곱셈 크기 감각 획득. UI만으로 구현 최저 비용 |
| 20 | **오늘의 수학 (데일리)** | Wordle 일일 시드 + streak (Sudoku.com) | 5-2·6-2 전 단원 로테이션 | 서버 없이 epoch 시드로 반 전체가 같은 '오늘의 한 문제'를 받고, 결과를 이모지 격자로 공유 — 매일 아침 조회 시간 루틴을 노린다 |

**우선 구현 추천 순서**: ①(검증 메커닉+낮은 난이도) → ⑳(리텐션 뼈대, 난이도 1) → ⑨·⑰(3D 와우 대비 저비용) → ⑫(MIT 포크로 단기 승리) → ⑯(원의 넓이 단원 킬러).

---
*원본 데이터: 같은 폴더의 `game-references.json` (sites 133 / mechanics 22 / visual_refs 42, 전 항목 URL·연령·루프·훅·수학영역·난이도·라이선스 필드 포함)*
