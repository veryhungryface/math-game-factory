# 쌍둥이 요새 검증 재현

저장소 루트에서 실행한다. Node.js, npm 의존성 설치, 로컬 Chrome for Testing이 필요하다. 브라우저 기반 명령은 성능 간섭을 피하도록 순차 실행한다.

```sh
node factory/lib/qa.mjs twin-forts --out logs/manual-20260907-twin-forts/qa
node logs/manual-20260907-twin-forts/reproduce/visual-smoke.mjs
node logs/manual-20260907-twin-forts/reproduce/hint-smoke.mjs
FIRSTPLAY_OUT=logs/manual-20260907-twin-forts/firstplay-naive FIRSTPLAY_PLAY_MS=45000 node factory/lib/firstplay/harness.mjs twin-forts
node logs/manual-20260907-twin-forts/reproduce/live-smoke.mjs
```

`live-smoke.mjs`는 공개 도메인이 현재 로컬 게임과 같은 배포일 때 실행한다. 20개 게임 파일과 게시 meta.json의 응답 해시, 모바일 터치 시작/12:18 배분/출격 성공을 검사한다. 모든 스크립트의 Chrome 경로는 작성 호스트의 설치 경로이며 다른 환경에서는 설치된 실행파일로 조정한다.

독립 BigInt 및 실제 UI/정책 검증 스크립트와 지침은 인접 `../validation/`에 있다. `qa-initial-static-module`과 `qa-initial-navigation`은 인식 문제의 최초 실패, `*-before-retry-fix` 및 `validation/revision-1/`은 오답 재배치 수리 전 증거다. 최종 상태는 `../qa`, `../firstplay-naive`, `../review.json`, `../mathcheck.json`, `../artifact-manifest.json`으로 판단한다.
