너는 이 저장소(/Users/sitpo/math-game-factory)의 Unity 게임 빌더다. 「협곡 사수」(`hyeopgok-sasu`) **2단계 라운드: 코인 경제 = 수학 입력**을 구현한다.
사용자 지시 「1→2」 중 1단계(비주얼·요소, 커밋 f67e1a5)는 끝났다. 이번이 2단계다.

## 먼저 읽을 것
- **정본 명세 `factory/unity-src/hyeopgok-sasu/ArtSource/DESIGN-PHASE2.md`** (붓는 양 = 답, `answer_mode` amount / fraction_parts / choice, 팩 스키마 v2, 게이트).
- `references/case-studies/kingshot-analysis.md` §1-2(코인 루프)·§4(수학 결합), 첨부 킹샷 프레임(코인이 등에 쌓이고 패드 숫자가 줄어드는 장면).
- 현재 소스 `factory/unity-src/hyeopgok-sasu/`(README·DESIGN·Scripts·ArtSource/packs 생성기·검증기), `docs/unity-track.md`, `factory/prompts/30-build.md`·`30-build-unity.md`, `curriculum/2022-middle-math.json` m2s2-u6/u7·expression_traps(중2 √ 금지, 「서로 다른」「임의로」, 복원만).

## 할 일
1. 코인 루프를 **수학 입력 장치**로: 처치 코인이 땅에 → 왕이 흡수 → 등의 코인 탑 → 답 패드에 서 있으면 한 닢씩 부어지고(길게 서면 가속, 탭=1닢) 숫자판 증가 → 걸어 나오면 확정(0.6초 확인 링, 다시 올라서면 추가 붓기 가능·덜기 불가). 정답=부은 코인이 건물 투자(레벨업·출격), 오답=코인 소실+정답·풀이 표시. 코인이 모자라면 싸워서 번다.
2. `fraction_parts`: 「모든 경우의 수」「사건이 일어나는 경우의 수」 두 패드(라벨은 팩이 정함) → 확정 시 두 패드 사이에 세로 분수 조립. `accept`: exact_parts / equivalent / reduced.
3. 팩 v2: 생성기·검증기 갱신, `m2s2-u6` 경우의 수 → 주로 amount, `m2s2-u7` 확률 → 주로 fraction_parts(exact_parts)+일부 reduced/choice(≤30%). 전수 재검증. 첫 문항은 작은 amount. 웨이브 설계가 「그 문항을 풀 코인을 벌 수 있음」을 보장. `packs/README.md` 에 answer_mode 3종·예시(초등·다른 단원 팩 작성 가이드).
4. v1 팩(answer_mode 없음)도 그대로 돌아야 한다(choice 로 처리) — 하위 호환 테스트.
5. 무뇌 봇: 무작위 양·항상 전부·항상 1닢·무입력·패드 위 오래 서기 → 첫 시도 정답률 ≤ 우연 수준. 결과 `ArtSource/bot-results.md`.
6. 1단계 비교에서 남은 눈에 띄는 비주얼 문제도 함께: ① 맵 가장자리 절벽이 베이지·회색 큰 상자로 보인다 → 킹샷처럼 둥근 층진 암석 덩어리(여러 크기, 윗면 따뜻·옆면 차가운 색, 나무가 가장자리를 덮음) ② 1280 가로에서 문제 배너·안내 글자가 너무 작다(모바일 기준 스케일과 따로 조정) ③ 가능하면 카메라를 조금 더 기울여(피치↓) 원근감.
7. `__GAME_TEST__`: `answerCorrect()` 는 실제 붓기 경로로, `sampleProblems` 는 v1 호환 필드(`answer` 문자열·`answerNumeric`·`choices` — amount/fraction 문항은 choices null 가능) 채움.
8. `node factory/lib/qa.mjs hyeopgok-sasu` 치명 0, fps·15초 게이트, gzip 12MB 미만. 첫 10초 훅(첫 처치 코인이 등에 쌓임 → 유령 손가락이 패드로 → 2닢 붓고 나오면 석궁탑) 캡처로 확인. `ArtSource/validation/phase2/` 에 캡처(붓는 중·분수 조립·오답 소실·승리).
9. meta.json: `version: 2` 유지, `qa` 리셋 상태 유지, description·howto·mechanic(`horde-defense-coin-pour`)·risks(4지선다 예외는 choice 문항에만 남는다고 갱신). 표지를 붓는 장면이 보이게 다시.
- 수정 금지: run.sh·config·prompts·lib·킷·`factory/work/`. 로컬 커밋 가능, push·배포 금지. Blender MCP(localhost:9876) 사용 가능.

## 최종 응답 (한국어, 10줄 이내)
구현 요약 / 팩 이관 결과(문항 수·모드 분포·검증) / 봇 결과 / QA total·fatal·fps / 산출 크기 / 캡처 경로 / 남은 위험.
