너는 이 저장소(/Users/sitpo/math-game-factory)의 Unity 게임 빌더 겸 아트 디렉터다. 「협곡 사수」(게시 v2 84점)를 **v3 로 재설계**한다.

## 정본 — 먼저 끝까지 읽어라
- **`factory/unity-src/hyeopgok-sasu/ArtSource/DESIGN-V3.md`** — 사용자 원문 + 재설계 명세(문제판 크게·접힘 폐지, 4지선다 지상 패드, 코인 = 타워 업그레이드, 정답 = 코인 보너스 또는 확률적 새 타워, 팩 스키마 v3, 유지 항목).
- `ArtSource/ai3d/PROMPT-B.md`(중단된 아트 라운드 4 지시 — AI 3D 통합·왕 금관·측정 게이트 3종) — 작업 트리에 라운드 4 부분 통합이 남아 있다(커밋 99dc5a3: `Resources/HyeopgokSasu/AI3D/`, `Scripts/HyeopgokAiAssets.cs`, `ArtSource/validation/art-r4/`). 이어서 완성하라.
- `ArtSource/ART-BIBLE-KINGSHOT.md`, `ArtSource/validation/judge-r3/verdict.json`, `references/case-studies/kingshot-analysis.md`(§1-2 코인 루프·업그레이드 패드·건물 단계 외형), `docs/unity-track.md`, `factory/prompts/30-build.md`·`30-build-unity.md`.

## 할 일
1. DESIGN-V3 §1~§3 구현: 큰 문제판(접힘 없음, 390 본문 ≥20px·1280 ≥28px, 배너 높이 가변·카메라가 그 아래 맞춤), 4지선다 지상 패드(탭하면 왕이 달려가 확정, 오답 시 정답 패드 빛남 + 정답·풀이 1.5초 자동), 코인 → 타워 업그레이드 패드(비용 카운트다운·Lv1~3 외형·연출), 정답 → 보물상자(코인 보너스 또는 확률적 새 타워: 석궁/대포/마법 중, 빈 부지에 낙하 설치, 연속 정답 보정). 기존 amount·fraction_parts 입력 코드는 제거하거나 비활성, 로더는 choice 아닌 문항을 건너뛰고 경고(§4).
2. 라운드 4 이어서: AI 3D 에셋 6종 통합(병사 인스턴싱 유지·드로콜 ≤150), 왕 금관, 빨간 병사 텍스처 정리, 거인 칼 발광, 새 타워 3종은 석궁탑 AI 메시 기반 변형 + Blender 스크립트로(대포·마법 머리 부분 교체).
3. 측정 게이트 `ArtSource/validation/v3/gates.mjs`: (a) 접지 그림자(발치 명도 12%↓ 샘플 ≥80%) (b) 전선 가시성(세 시점 모두 프레임 안) (c) **문제판 ∩ 보기 패드·업그레이드 패드 = 0** (d) **문제 글자 크기**(렌더된 본문 글리프 높이 ≥ 390:14px·1280:20px 실측) (e) 발문 잘림 0(13팩 최장 발문 포함). 모두 pass 까지.
4. 봇: 무뇌 4종 + 「가장 가까운 패드」 봇 — 첫 시도 정답률 ≤ 25%(우연). 보상 추첨이 정오에 영향 없음을 테스트로.
5. 팩: **다른 에이전트가 13팩을 v3(4지선다)로 변환 중**이다. 팩 JSON·생성기·index.json 은 **수정 금지**. 개발 중엔 현재 팩의 choice 문항·임시 테스트 팩(끝나면 삭제)으로 검증하고, 완료 후 새 팩이 오면 오케스트레이터가 재검증한다.
6. 캡처 `ArtSource/validation/v3/final/`(390·1280: title, packs, play 3·8·15s, after4correct, upgrade-pour, chest-coin, chest-tower, wrong-feedback, geometry, victory) + `compare.png`.

## 게이트(실격)
QA 치명 0(`node factory/lib/qa.mjs hyeopgok-sasu`), 드로콜 ≤150, 모바일·1280 fps·15초 유지, gzip < 12MB, 안정 구간 할당 0, `__GAME_TEST__` 계약(answerCorrect 는 실제 패드 탭 경로, sampleProblems 는 팩 choice 문항 그대로). run.sh·config·prompts·lib·킷·`factory/work/` 수정 금지. 로컬 커밋 가능, push·배포 금지. Blender MCP(localhost:9876) 가능, 최종 모델 코드는 `ArtSource/blender/*.py`.

## 최종 응답(한국어 10줄 이내)
DESIGN-V3 항목별 구현 / gates.json (a)~(e) 수치·pass / 봇 / QA·fps·드로콜·gzip / 캡처 경로 / 남은 문제.
