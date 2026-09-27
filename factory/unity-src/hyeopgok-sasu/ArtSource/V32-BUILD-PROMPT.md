너는 이 저장소(/Users/sitpo/math-game-factory)의 Unity·Blender 게임 빌더 겸 아트 디렉터다. 게시된 「협곡 사수」 v3.1(84점)을 **v3.2 로 확장**한다.

## 정본 — 먼저 끝까지 읽어라
- **`factory/unity-src/hyeopgok-sasu/ArtSource/DESIGN-V3.2-ENDLESS.md`** — 사용자 원문 2개(「죽을 때까지 해야지 / 다른 맵에서 플레이」, 「타워 업그레이드되는 맛이 안 느껴짐」)와 명세 §1~§5: 성문 HP 0 만 게임 오버, 10문제 스테이지 + 보스 웨이브 + 「지역 수복」, 맵 3종(초원 협곡 / 사막 강 다리 / 설원 요새, 길 모양 U·S·두 갈래) 순환 + 루프 난이도, 스테이지 사이 영구 강화 3택1, 난이도 곡선, **업그레이드 손맛 시퀀스(붓기 → 히트스톱 → 청사진 → 오버슈트 → 먼지·빛기둥 → 펀치 줌 → 팡파르 → 스탯 팝업), 레벨별 외형 대변화·전투 발사 패턴 변화, MAX**.
- `ArtSource/DESIGN-V3.md`(v3 계약 — 유지), `ART-BIBLE-KINGSHOT.md`, `references/case-studies/kingshot-analysis.md`(강·다리·사막·설원 바이옴, 건물 단계 외형), `docs/unity-track.md`, `factory/prompts/30-build.md`·`30-build-unity.md`.
- 첨부: 킹샷 강·다리·사막 프레임, 기지 성장 프레임, 현재 우리 화면.

## 할 일
1. DESIGN-V3.2 §1~§5 전부 구현. 새 맵 2종은 Blender 스크립트(`ArtSource/blender/v32_maps.py`, 재현 가능)로 지형 생성 + 기존 메시 재사용·팔레트 재틴트(사막: 사암·선인장·고사목·모래 / 설원: 눈 덮인 바위·침엽수 눈모자·얼음 강·차가운 조명). Blender MCP(localhost:9876) 사용 가능.
2. 타워 3종 × Lv1~3 외형(목조 → 석재+천 → 성가퀴+금테·깃발·룬, 레벨마다 크기 +20%)과 레벨별 발사 패턴. 업그레이드 시퀀스 1초 안팎.
3. 측정: `ArtSource/validation/v32/gates.mjs` — 기존 v3 게이트 (a)~(g)를 **맵 3종 모두**에서 pass + (h) 게임 오버는 성문 HP 0 에서만(10·20·30문제 도달해도 계속) (i) 맵 전환 정상(스테이지 1→2→3→1, 상태·패드·팩 문항 연속성) (j) 무뇌 봇(무입력·무작위·고정·최근접)은 스테이지 3 이전에 성문 붕괴, 정답 봇은 스테이지 3 이상 도달. 수치 `gates.json`.
4. 캡처 `ArtSource/validation/v32/final/`: 맵 3종 × (390·1280) 전투, 보스 웨이브, 지역 수복·강화 선택, 업그레이드 시퀀스 6프레임(`upgrade-seq-1..6`), 타워 3종×Lv1~3 나란히, 레벨별 전투 발사, 게임 오버 결과·최고 기록.
5. `public/g/hyeopgok-sasu/meta.json`: version 4, description·howto 갱신, qa 리셋 `{"score":0,"gate":80,"passed":false,"reviewed_at":"","notes":[]}`.

## 게이트(실격)
QA 치명 0(`node factory/lib/qa.mjs hyeopgok-sasu`), 드로콜 ≤150, 모바일·1280 fps·15초 유지, **gzip < 12MB**(현재 11.49MB — 텍스처 압축·중복 제거로 여유 확보), 안정 구간 할당 0, `__GAME_TEST__`(getState 에 stage·map·loop 추가, answerCorrect 는 실제 패드 탭, sampleProblems 는 팩 그대로). 팩 JSON·생성기·index.json·run.sh·config·prompts·lib·킷·`factory/work/` 수정 금지. 로컬 커밋 가능, push·배포 금지.

## 최종 응답(한국어 10줄 이내)
§1~§5 구현 / gates (a)~(j) / 용량·fps·드로콜 / 캡처 경로 / 남은 문제.
