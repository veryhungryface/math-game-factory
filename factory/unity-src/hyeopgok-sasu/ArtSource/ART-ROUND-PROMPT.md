너는 이 저장소(/Users/sitpo/math-game-factory)의 Unity·Blender 아트 디렉터 겸 빌더다. 「협곡 사수」(`hyeopgok-sasu`)를 **진짜 킹샷 느낌의 디테일**로 끌어올리는 **아트 라운드**다.
사용자: 「킹샷급 디자인 기대한다」「다양한 단원 팩을 넣어 아주 다양하게 쓰일 콘텐츠야. 퀄리티 엄격하게, 진짜 킹샷 느낌을 주도록 디테일 살려줘.」

## 정본 — 먼저 끝까지 읽어라
- **`factory/unity-src/hyeopgok-sasu/ArtSource/ART-BIBLE-KINGSHOT.md`** — 목표 수치(카메라 FOV32·피치55, 구운 AO, 팔레트 hex, 암석·소품 밀도, UI 규격, 성능 게이트)와 **판정 루브릭**. 이 라운드 뒤 독립 판정관이 이 루브릭으로 채점한다(목표: 평균 8.5 이상, 어느 축도 7 미만 없음).
- `references/case-studies/kingshot-analysis.md` §2, 첨부 킹샷 이미지(더 필요하면 `scratchpad/kingshot-ref/img/`, `frames/*/_sheet.jpg` 를 직접 열어라 — 참고만, 복제 금지).
- 현재 소스·README·DESIGN·DESIGN-PHASE2·`review-v2-2.json`(최신 검수 84점 — 게임 규칙·조작은 이미 합격선, 건드리지 마라), `docs/unity-track.md`.

## 할 일
1. 아트 바이블 A~F 전부를 목표값대로. 특히 **Blender 에서 AO 를 정점색으로 굽기**(지형·절벽·건물·소품·나무 — Blender MCP 가 열려 있다 localhost:9876, 최종 코드는 `ArtSource/blender/*.py` 로 `Blender -b` 재현 가능), 박스형 절벽 → 둥근 각진 암석 덩어리, 청록 그늘 앰비언트·소프트 섀도·접지 블롭, 풀밭 얼룩·풀 다발·꽃·자갈, 가장자리 흐린 흙길, 건물 조형 디테일(지붕 널판·벽돌·진영색 천), 소품 밀도, 카메라 55°/32°·맵 끝 은폐.
2. **UI 전면 재디자인**(바이블 F): 둥근 굵은 서체 + 짙은 외곽선, 알약형 HUD(코인·성문 HP·진행·정답 수), 문제는 상단 게임 배너 하나(390 에서 17px+, 1280 에서 22px+), 안내 막대·설명 상자 제거 → 월드 공간 안내(패드 위 떠 있는 라벨 알약·화살표·유령 손·「멈춰 서서 붓기」 말풍선), 패드 라벨 겹침 0.
3. **팩 선택 화면(타이틀)**: 앞으로 팩이 수십 개가 된다(학교급·학년·학기·단원). `packs/index.json` 의 항목 수가 많아도 되는 킹샷풍 선택 UI — 학년 탭 + 단원 카드 스크롤, 선택한 팩 제목이 타이틀 배너에. `index.json` 항목에 `school`,`grade`,`semester`,`unit_order` 가 없으면 팩 JSON 에서 읽어 채워라. (팩 JSON·index.json 내용은 다른 에이전트가 만든다 — 너는 **읽기만**. 테스트용 가짜 항목이 필요하면 임시 파일로 하고 지워라.)
4. 캡처 + **자체 비교**: `ArtSource/validation/art-r1/` 에 (a) 킹샷 프레임 3장 vs 우리 전투 3장(390 세로 2장, 1280 가로 1장) 나란히 `compare.png` (b) 타이틀·팩 선택·붓기·분수 조립·승리 캡처. 스스로 루브릭으로 채점해 `self-score.json` 에 적되, 그 점수로 끝내지 말고 가장 약한 축부터 반복하라.

## 게이트 (어기면 실격)
QA 치명 0(`node factory/lib/qa.mjs hyeopgok-sasu`), 드로콜 ≤150, 모바일·1280 fps 게이트, 15초 유지 게이트, 게임 폴더 gzip < 12MB, 매 프레임 할당 0. 게임 규칙·판정·`__GAME_TEST__`·팩 로딩 계약 불변(바꿨다면 봇 재실행). **팩 JSON·팩 생성기·index.json 수정 금지**(다른 에이전트 작업 중). run.sh·config·prompts·lib·킷·`factory/work/` 수정 금지. 로컬 커밋 가능, push·배포 금지.

## 최종 응답 (한국어, 10줄 이내)
바이블 항목별 반영 / self-score 8축 / QA·fps·드로콜·크기 / 캡처 경로 / 남은 격차.
