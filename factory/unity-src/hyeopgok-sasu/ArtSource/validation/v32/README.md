# 협곡 사수 v3.2 검증

이 디렉터리는 빌드된 `public/g/hyeopgok-sasu/` WebGL을 블랙박스로 검증한다. 게임·팩·
팩 인덱스를 수정하지 않고, 모든 실행 전후에 팩 SHA-256을 대조한다. 훅이나 상태 필드가
없거나 장면을 관찰하지 못하면 통과로 추정하지 않고 실패한다.

## 실행

저장소 루트에서:

```bash
bash factory/unity/build.sh hyeopgok-sasu
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v32/gates.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v32/performance.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v32/capture.mjs
node factory/lib/qa.mjs hyeopgok-sasu
node factory/unity/load-probe.mjs hyeopgok-sasu --runs 3
```

- `gates.mjs` → `gates.json`, `gate-frames/`, `choice-frames/`.
- `performance.mjs` → `performance.json`.
- `capture.mjs` → `final/` 33장과 `final/report.json`.
- `V32_KEEP_FRAMES=0`은 일반 게이트 스크린샷만 끄며 보기 실측 증거는 유지한다.
- `--quick`/`V32_SKIP_LONGEST=1`은 13팩 최장 발문을 건너뛰는 개발용 모드로, (e)는 반드시 실패한다.
- (j)의 권위 입력은 같은 디렉터리의 Editor 산출물 `bots.json`이다.
- `V32_BROWSER_BOT_SMOKE=1`은 브라우저 무뇌 봇 4종을 각 1회 추가 실행한다. 이 스모크는
  보조 진단이며 780런 권위 판정을 대체하지 않는다.

## (a)~(g): 3개 맵 각각 판정

`v32map=1|2|3`은 실제 제품 맵 빌더를 그대로 띄우는 비주얼 픽스처다. (h)~(j)에서는
이 쿼리를 사용하지 않는다.

- baseline: 3맵 × 2판형 × 3/8/15초 = 18건.
- 최장 발문: 3맵 × 13팩 × 2판형 = 78건.
- 보상 라벨: 3맵 × 2판형 = 6건.
- 보기 픽스처: 3맵 × 7종 × 2판형 = 42건.

다음 v3 기준을 그대로 쓴다: 접지 음영 12% 이상·통과율 80% 이상, 전선 6% 안전 여백,
1280 전장 너비 70% 이상, UI/라벨 교차 0, 본문 잉크 높이 390에서 14px·1280에서
20px 이상, 접힘·말줄임·잘림·overflow·대체 글리프 0.

## (h)~(i): 픽스처 없는 자연 진행

390×844 한 판을 실제 `answerCorrect()` 패드 경로로 30문제 진행한다. 매 10문제 끝에
`boss → recovered → perk → transition`을 모두 관찰하고, 영구 강화는 `perkRects` 안을 CDP
터치한다. 체크포인트는 `2/사막/1`, `3/설원/1`, `4/초원/2`다. 이후 실제
오답 패드 경로로 HP를 0으로 만들어 `phase=gameover`, `endReason=gate_hp_zero`를 확인한다.

(i)는 또한 팩/문항 은행 불변, 처음 30문항 ID 미중복, 패드 4개, 맵 사이 코인·타워
초기화, HP +30 회복 연출, 스테이지 4 난이도 계수 1.25를 기록한다. 스테이지 1은
difficulty 1~2가 10문항 중 7개 이상, 스테이지 3은 3~4가 적어도 1개여야 한다.

## (j): 780런 봇 + 브라우저 보조 스모크

권위 판정은 Editor가 만든 `bots.json`의 13팩 × 12시드 × 5전략 = 780런이다. `idle`,
`random`, `fixed`, `nearest`는 각각 156/156회 스테이지 3 전에 성문 HP 0으로 끝나야 하고,
`perfect`는 156/156회 스테이지 3 이상에 도달해야 한다. 모든 전략은 실제
`CommandChoicePad` 경로를 써야 하며, 10·20·30문제 연속 진행, 성문 HP 0 단독 종료,
맵 순환, 난이도 곡선도 함께 통과해야 한다. `bots.json`이 없거나 자체 `pass=false`면 (j)는
실패한다.

`V32_BROWSER_BOT_SMOKE=1`을 주면 `idle`, `random`, `fixed`, `nearest`를 새 WebGL 판에서
각 1회 실제 터치로 추가 실행한다. 선택은 정답·보기 문자열을 읽지 않으며 결과는
`browserSmoke.auxiliary=true`로 기록된다. 이 4회는 환경 진단용이라 (j)의 780런 통계를
부풀리거나 대신하지 않는다.

## 성능·용량

3맵 × 2판형 6회를 각각 warmup 4초 + 연속 15초 측정한다. 전체/앞 4초/뒤 4초 FPS는
30 이상, 뒤/앞 유지율은 60% 이상, 프레임별 WebGL draw 최댓값은 150 이하여야 한다.
`HyeopgokGame.Update`, `HyeopgokBattle.Update`, `HyeopgokEnvironment.LateUpdate` 안정 구간의 할당은
콜백별 0 byte여야 한다. 용량은 게임 폴더의 모든 비숨김 파일을 각각 gzip-9(mtime 0)로
압축한 합이 **12,000,000 byte 미만**이다.

## 최종 캡처 33장

- `battle-{grass,desert,snow}-{390,1280}.png` 6장.
- `boss-wave-*`, `region-recovered-*`, `permanent-upgrade-choice-*` 각 2장.
- `upgrade-seq-1.png` ~ `upgrade-seq-6.png` 6장. 코인을 실제로 모아 업그레이드 패드를
  터치한 뒤 `upgradeSeqSerial/Phase/Progress`를 대기한다.
- `tower-gallery-{390,1280}.png` 2장.
- `fire-{crossbow,cannon,magic}-lv{1,2,3}.png` 9장.
- `game-over-result-*`, `best-record-*` 각 2장.

`v32showcase=towers|fire-<type>-<level>`가 무시되면 스크린샷만 존재해도 실패한다. 각 프레임은
캡처 시점의 `getState()`, SHA-256, 바이트 크기, 판정 사유를 `final/report.json`에 남긴다.
