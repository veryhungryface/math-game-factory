# 쌍둥이 요새 독립 검증 증거

검증자는 생산 게임 코드를 수정하지 않았다. 수학 판정은 `scripts/oracle.mjs`의 독립 BigInt 계산으로, 실제 플레이는 마우스·터치·키보드 입력으로 수행했다. 게임 훅은 상태 읽기에만 사용했다. 정답 훅·내부 상태 수정으로 캠페인을 진행하지 않았다.

- `math-source-report.json`: 공개 풀 596개 + 튜토리얼, 33,341개 판정, 실제 생성기 5,000캠페인/일반40,000문항, 정책별 우연 기준.
- `revision-1/ui`: 정상9작전, 일반 실수1회 후9작전, 실패3회/재시작, 저장/새로고침, 다섯 화면 크기. 이 시점의 retry 손잡이 비활성화 결함은 findings에 남겼다.
- `revision-2/recovery`: 수정된 retry에서 별도 복구 버튼 없이 터치/정밀버튼/키보드로 바로 수정. 한 번 실수한 캠페인을 실제 UI로 9작전까지 완료하고 승리 장식 저장을 확인했다.
- `revision-2/bots`: 7정책 × 일반 첫시도3회 실제 UI. 정답으로 수정하여 다음 문항으로 이동한 시도는 정책의 첫시도 통계에서 제외했다. 각0/3은 모집단 정답률 추정치가 아니다.
- `revision-2/game-under-test.js`, `style-under-test.css`, `artifact-manifest.json`: 실제 완주·복구·정책 입력을 검증한 판본. 최종판과의 차이는 전장 탭 안내용 단일 재사용 DOM/독립 pointerdown 핸들러/CSS뿐이며 배분·판정·점수·시도 기록 함수는 동일하다.
- `../hint`: 최종판의 실제 전장 탭 리플/손잡이 안내, 8초 무입력, 해설 보존 및 복구 확인. 호스트가 실행하고 독립 검증자가 원본 로그와 이미지4장을 열람했다.
- `../qa`, `../firstplay-naive`: 최종판 자동 QA와 정답을 모르는 자동 탐침. 전체 첫 플레이 프레임은 순서대로 독립 열람하여 review에 판정한다. 아동 사용자 테스트를 의미하지 않는다.
- `../mathcheck.json`, `../review.json`: 최종 산출물 SHA와 연결한 최종 판정.

## 재현

저장된 `.mjs`는 실제 수행한 검증 스크립트다. 이 디렉터리로 옮기면서 `static-server.mjs`의 상대 import 깊이만 수정했다. 저장 경로는 원래 실행의 `scratchpad/twin-forts-validation/`를 유지하므로 원본 증거 디렉터리를 덮어쓰지 않는다. 저장된 mathcheck 마감 스크립트는 이 디렉터리의 고정된 math-source-report를 읽는다.

저장소 루트에서 다음을 실행한다. 브라우저 스크립트는 다른 Puppeteer 작업이 없는 때에만 실행하며 `TWIN_FORTS_BROWSER_SLOT=granted`가 필요하다. 브라우저 실행 경로는 해당 작업의 macOS Chrome for Testing 경로로 고정되어 있다.

```bash
node logs/manual-20260907-twin-forts/validation/scripts/audit-math.mjs
TWIN_FORTS_BROWSER_SLOT=granted node logs/manual-20260907-twin-forts/validation/scripts/run-ui.mjs
TWIN_FORTS_BROWSER_SLOT=granted node logs/manual-20260907-twin-forts/validation/scripts/run-recovery.mjs
TWIN_FORTS_BROWSER_SLOT=granted node logs/manual-20260907-twin-forts/validation/scripts/run-bots.mjs
```

`complete-mathcheck.mjs`는 현재 게임의 모든 파일 SHA가 호스트 artifact-manifest와 일치할 때만 최종 QA의 한국어 프롬프트를 다시 파싱/계산하고 mathcheck를 갱신한다. 게시 후 meta.qa가 바뀌면 게시 전 manifest와 달라지는 것은 정상이며 이 스크립트는 그 상태를 거부한다.
