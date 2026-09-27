# 협곡 사수 v3 검증

이 디렉터리는 빌드된 WebGL을 블랙박스로 측정한다. 팩 파일은 읽기만 하며, 최장 발문 검증은 Chrome 요청 응답에서 기존 문항 순서만 메모리상 바꾼다. 모든 실행은 전후 SHA-256이 같은지 확인한다. 프로브가 없거나 측정할 수 없으면 통과로 추정하지 않고 실패한다.

## 실행

저장소 루트에서 다음 순서로 실행한다.

```bash
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v3/gates.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v3/bots.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v3/capture.mjs
```

- `gates.mjs`는 `gates.json`, 선택적으로 `gate-frames/`, 그리고 보기 실측 증거 `fix1/choices-*.png` 14장을 만든다. `V3_KEEP_FRAMES=0`으로 일반 게이트 프레임 저장만 끌 수 있으며 `fix1` 증거는 항상 현재 실행으로 덮어쓴다. `--quick`은 개발용으로 13팩 최장 발문을 건너뛰며, 이 경우 (e)는 의도적으로 실패한다.
- `bots.mjs`는 기본 Unity 워크스페이스 `/Users/sitpo/UnityProjects/MGF-Workspace`를 실행한다. 다른 위치는 `--workspace PATH` 또는 `MGF_UNITY_WORKSPACE`로 지정한다. 이미 생성된 v3 결과만 검사하려면 `--report PATH`를 쓴다.
- `capture.mjs`는 `final/`에 390×844와 1280×800 각각의 12개 장면, `final/report.json`, 그리고 전체 실행 성공 시 `compare.png`를 만든다. 일부만 다시 찍을 때는 `ONLY=title,packs,play,after4correct,upgrade,chest,wrong,geometry,victory`를 쓴다.
- 최종 공용 QA 결과는 `final/qa/report.json`, 별도 15초 성능·드로콜·할당·gzip 결과는 `performance.json`에 보존한다.

## 읽기 전용 WebGL 프로브 계약

`window.__HYEOPGOK_V3_DEBUG__.snapshot()`을 정본으로 쓴다. 함수 자체나 `getSnapshot()`도 허용하며, 이전 이름 `__HYEOPGOK_ART_DEBUG__`는 임시 호환 경로다. 모든 좌표는 **CSS 픽셀, 좌상단 원점**이다. `{normalized:true}`를 붙인 0..1 좌표도 받지만 새 구현은 CSS 픽셀을 내보내야 한다.

```js
{
  version: 3,
  shadowSamples: [
    {
      id: "king",
      type: "king" | "soldier" | "building" | "tree",
      visible: true,
      foot: {x, y},
      ring: [{x, y}, {x, y}, {x, y}] // 모델과 UI가 없는 인접 지면 3점 이상
    }
  ],
  frontLine: {
    visible: true,
    points: [{x, y}] // 전선 대표점 전부가 화면 6% 안쪽
  },
  battlefieldRect: {x, y, width, height}, // 전장 기준 월드 범위의 투영 사각형
  question: {
    prompt: "실제 표시 중인 전체 발문",
    panelRect: {x, y, width, height},
    bodyRect: {x, y, width, height},
    renderedTextRect: {x, y, width, height},
    isFolded: false,
    hasEllipsis: false,
    isTruncated: false,
    isOverflowing: false,
    visibleCharacters: 42,
    totalCharacters: 42,
    glyphs: [
      {
        id: "char-0",
        char: "삼",
        kind: "body",
        visible: true,
        rect: {x, y, width, height},
        backgroundPoints: [{x, y}] // 선택 사항. 글자 밖 같은 배경색 지점
      }
    ]
  },
  choices: [
    {
      id: "choice-0",
      text: "−{frac:3/4}",
      bodyRect: {x, y, width, height},
      renderedTextRect: {x, y, width, height},
      isTruncated: false,
      isOverflowing: false,
      visibleCharacters: 3,
      totalCharacters: 3,
      glyphs: [
        {id: "choice-0-char-0", char: "−", kind: "choice", visible: true, rect: {x, y, width, height}}
      ]
    }
  ], // v3choices=1에서 실제 TMP 보기 4개
  choicePadRects: [
    {id: "choice-0", visible: true, active: true, rect: {x, y, width, height}}
  ], // 플레이 중 정확히 4개
  upgradePadRects: [
    {id: "upgrade-0", visible: true, active: true, rect: {x, y, width, height}}
  ], // 플레이 중 1개 이상
  worldLabels: [
    {id: "choice-0", kind: "choice", visible: true, active: true, rect: {x, y, width, height}},
    {id: "upgrade-0", kind: "upgrade", visible: true, active: true, rect: {x, y, width, height}},
    {id: "reward", kind: "reward", visible: false, active: false, rect: {x, y, width, height}}
  ] // 고정 순서: choice 4, upgrade 3, reward 1
}
```

글리프 `rect`는 TextMeshPro 문자 정보로 계산한 실제 렌더 영역이어야 한다. 하네스는 그 영역의 스크린샷 픽셀에서 주변 배경과 다른 잉크 행을 다시 찾아 높이를 잰다. 단순 `fontSize`나 프로브가 주장하는 높이는 판정에 쓰지 않는다.

캡처에는 `__GAME_TEST__.getState()`의 기존 필드 외에 다음 읽기 전용 상태가 필요하다.

```js
{
  attempts, solved, coins, phase, lastCorrect,
  nextUpgradeCost,
  upgradeAffordable,
  upgradePhase: "idle" | "pouring" | "complete",
  upgradePouring: false,
  rewardSerial,
  rewardPhase,
  rewardKind: "coin" | "tower",
  rewardTowerType: "crossbow" | "cannon" | "magic",
  chestOpen,
  feedbackVisible,
  wrongFeedback,
  victory
}
```

`answerCorrect()`와 `answerWrong()`은 상태를 직접 바꾸는 우회 명령이 아니라 실제 보기 패드 탭과 같은 왕 이동·도착·확정 경로를 실행해야 한다. 캡처 하네스는 그 명령 뒤 `attempts`/`solved` 변화를 기다리며, 업그레이드는 강한 원근에서도 실제 패드 안쪽으로 역투영되는 `upgradePadRects` 내부 후보점을 CDP로 터치한다. 상자 종류는 강제하지 않고 자연 추첨에서 두 종류가 나올 때까지 새 판을 시도한다.

## 봇 결과 계약

Editor의 `Mgf.HyeopgokSasu.HyeopgokBotSelfTest.Run`은 `MGF_HYEOPGOK_VALIDATION_OUT/bot-results.json`에 다음 v3 구조를 쓴다. 각 팩별 행을 둘 수 있으나 각 행은 고정 시드 200판이어야 한다.

```json
{
  "schemaVersion": 3,
  "gamesPerBot": 200,
  "results": [
    {
      "id": "fixed | cycle | random | idle | nearest",
      "blind": true,
      "games": 200,
      "submitted": 200,
      "correct": 47,
      "firstAttemptRate": 0.235,
      "progress": false
    }
  ],
  "rewardOrthogonality": {
    "testedSeeds": 20,
    "comparisons": 2000,
    "answerMismatches": 0,
    "questionOrderMismatches": 0,
    "rewardOutcomeVariants": 2,
    "pass": true
  },
  "pass": true
}
```

`fixed`, `cycle`, `random`, `idle` 네 무뇌 전략과 `nearest` 모두 첫 확정 정답률이 25% 이하여야 한다. `idle`은 제출과 진도가 0이어야 한다. reward RNG 검사는 같은 문항/입력 시퀀스를 서로 다른 보상 시드로 대조해 정오·문항 순서 불일치가 0이면서 코인/타워 두 보상 결과가 실제로 갈리는지를 확인한다.

## `gates.json` 요약 구조

`gates` 아래의 키는 최종 보고서 항목과 일치한다.

- `a_groundShadow`: 발치가 인접 지면보다 12% 이상 어두운 표본 비율과 유형별 누락. 각 시점과 전체 모두 80% 이상이어야 한다.
- `b_frontLineVisibility`: 두 판형의 3/8/15초 여섯 시점에서 전선 대표점이 6% 안전 여백 안에 있는지 기록한다. 1280에서는 `battlefieldRect.width / viewport.width`가 모든 시점에 70% 이상이어야 한다.
- `c_zeroUiObstruction`: 문제판과 보기 4개·업그레이드 패드의 교차 면적 합. 모든 측정에서 0이어야 한다.
- `d_renderedGlyphHeight`: 스크린샷 실측 본문 글리프 최솟값. 390은 14px, 1280은 20px 이상이다.
- `e_longestPromptClipping`: 13팩 각각의 최장 choice 발문을 두 판형에서 렌더한 26건. 패널/본문 포함 관계, 접힘·말줄임·overflow, 문자 수를 보존한다.
- `f_zeroWorldLabelOverlap`: 정상 플레이의 보기 4개·업그레이드 1개 이상과 실제 정답 보상 장면을 두 판형에서 측정한다. 활성 월드 라벨은 모두 화면 안에 있고 서로의 교차 면적이 0이어야 한다.
- `g_choiceRendering`: 실팩의 `70°`, `12 cm²`, `2:3`, `−{frac:3/4}`, `23.8`, `0`·`1`·분수 혼합, 14자 최장 보기를 두 판형에서 렌더한다. 원문 일치, 컨테이너 포함, overflow·잘림·라벨 겹침·`□`/`�` 대체 글리프 0, 14개 캡처와 팩 SHA 불변을 확인한다.

`overall.pass`는 일곱 게이트, 브라우저 오류 0, 13팩 v3 사전조건, 팩 해시 불변을 모두 만족할 때만 `true`다.
