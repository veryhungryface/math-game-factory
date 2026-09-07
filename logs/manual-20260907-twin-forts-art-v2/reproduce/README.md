# 쌍둥이 요새 아트 v2 재현

저장소 루트에서 실행한다. 브라우저 검증은 순서대로 실행하며 같은 GPU에서 동시 측정하지 않는다. Puppeteer full Chrome 경로는 실행 호스트에 맞춰 바꾼다.

```sh
node logs/manual-20260907-twin-forts-art-v2/reproduce/visual-smoke.mjs
node factory/lib/qa.mjs twin-forts --out logs/manual-20260907-twin-forts-art-v2/qa
FIRSTPLAY_OUT=logs/manual-20260907-twin-forts-art-v2/firstplay-naive FIRSTPLAY_PLAY_MS=45000 node logs/manual-20260907-twin-forts-art-v2/reproduce/firstplay-gpu.mjs twin-forts
TWIN_FORTS_BROWSER_SLOT=granted node logs/manual-20260907-twin-forts-art-v2/reproduce/run-recovery.mjs
node logs/manual-20260907-twin-forts-art-v2/validation/verify-math-regression.mjs
node logs/manual-20260907-twin-forts-art-v2/reproduce/live-smoke.mjs
```

`getState()`는 관찰에만 사용한다. 배분과 제출은 실제 포인터/터치/키 입력으로 수행한다. 답은 화면의 총원과 비를 읽어 별도 정수 오라클로 계산한다. 순진한 첫 플레이 탐침은 어린이 실사용 연구를 대체하지 않는다.

공용 firstplay 하네스는 SwiftShader를 강제하며 이번 실행은 t33에서 정지했다. 해당 부분 시퀀스는 `firstplay-naive-software-incomplete/`에 보존했다. 로컬 `firstplay-gpu.mjs`는 동일 정책을 유지하며 소프트웨어 렌더링 강제 옵션 세 개만 제거했다. 공용 하네스는 수정하지 않았다.
