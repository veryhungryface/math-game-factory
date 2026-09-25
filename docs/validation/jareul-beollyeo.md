# 자를 벌려 — 제작 검증 기록

2026-09-09 KST. 대상은 `public/g/jareul-beollyeo/{index.html,engine.js,meta.json}`과 기존 표지·에셋이다. 게임은 자의 너비 잠금·옮기기, 조건에 맞지 않는 각목 밀기, 삼각자 배치·회전으로 답을 제출한다. 게시나 배포는 수행하지 않았고 `meta.qa`는 초기 미게시 상태를 유지한다.

## 자동 QA와 실행 환경

요청한 원본 명령을 실제로 실행했다.

```sh
node factory/lib/qa.mjs jareul-beollyeo
FIRSTPLAY_PLAY_MS=45000 node factory/lib/firstplay/harness.mjs jareul-beollyeo
```

두 명령 모두 게임 로드 전에 `listen EPERM: operation not permitted 127.0.0.1`로 종료코드 1을 반환했다. [원본 QA 로그](../../logs/jareul-beollyeo-build/browser-qa-official.txt), [원본 firstplay 로그](../../logs/jareul-beollyeo-build/browser-firstplay-official.txt)에 그대로 보존했다. **원본 HTTP 실행을 통과했다고 주장하지 않는다.**

기존 저장소에서 사용한 제한 환경용 방식으로, 로컬 파일 탐색과 Chromium CDP pipe 실행만 바꾸어 동일 QA 및 firstplay를 추가 실행했다. [환경 어댑터](../../logs/jareul-beollyeo-build/browser-loader.mjs)는 생산용 하네스의 검사·점수·임계값을 수정하지 않는다. 최종 명령:

```sh
node --loader ./logs/jareul-beollyeo-build/browser-loader.mjs factory/lib/qa.mjs jareul-beollyeo --out logs/jareul-beollyeo-build/browser-qa-post
```

**최종 보조 QA: 42/44, 치명적 결함 0, 종료코드 0.** 콘솔 오류·미처리 예외·실패 요청은 각각 0이다. 표지 1200×630 / 1080×1080, 교육과정 코드, 상대경로, 훅, 실제 입력 반응 검사를 통과했다. 문제 표본은 요청한 40/17/63개를 모두 반환했고, 마지막 표본의 핵심 발문 다양성은 61/63(97%)이다. [최종 보고서](../../logs/jareul-beollyeo-build/browser-qa-post/report.json).

### 성능 제한 — 충족 확인이 아님

최종 제한 환경의 single-process SwiftShader 렌더링에서 모바일 중앙값은 **17fps**, 1280 재측정 중앙값은 **19fps**였다. 같은 실행의 머신 벤치는 10fps였다. QA는 기존 보정 규칙에 따라 두 FPS 검사를 비치명 **보류**로 처리했다(보정값 68/76fps). 보정값을 실제 게임 FPS로 보고하지 않는다.

레이아웃·DOM 조회·잔상 배열의 프레임별 할당을 제거한 뒤 성능 탐색의 15초 전후 원자료는 **15→13fps, 비율 0.87**이고, 최종 QA 재측정은 **17→16fps, 비율 0.94**였다. 다만 하네스는 앞 구간부터 낮아 `measurable:false`로 저하율 판정을 생략했다. **실 GPU 환경에서 실제 30fps 및 장기 유지 여부는 미검증**이다. [최종 QA 성능 원자료](../../logs/jareul-beollyeo-build/browser-qa-post/report.json).

## 실제 포인터 플레이

[실입력 검사기](../../logs/jareul-beollyeo-build/browser-playthrough.mjs)는 화면 지시문과 렌더된 꼭짓점·자·삼각자 위치를 읽어 수학 판단을 독립 계산한다. 답 제출에는 Chromium의 진짜 포인터 입력만 사용한다. `answerCorrect`, `answerWrong`, `start` 훅이나 생성기의 정답 필드를 사용하지 않았다. 상태 훅은 진행 관찰에만 사용했다.

실제 90초 타이머 수정 후 결과:

| 경로 | 결과 | 남은 기회 | 남은 게임 시간 | 검사 루프 실제 경과 |
|---|---:|---:|---:|---:|
| 연습 후 정상 진행 | 10/10 클리어 | 3 | 53.37초 | 38.02초 |
| 첫 일반 실수 후 다시 해결 | 10/10 클리어 | 2 | 56.23초 | 33.32초 |

정상 경로 첫 시도 10/10, 복구 경로 첫 시도 9/10이다. 실제 경과에는 검사 통신·마지막 화면 촬영이 포함된다. 포인터는 down 65 / move 425 / up 65회 모두 `isTrusted`, 비신뢰 이벤트는 0회였다. 오류·외부 HTTP 요청·가로 스크롤은 0이다. [최종 실입력 보고서](../../logs/jareul-beollyeo-build/browser-playthrough-final/report.json), [정상 결과](../../logs/jareul-beollyeo-build/browser-playthrough-final/normal-final.png), [실수 후 결과](../../logs/jareul-beollyeo-build/browser-playthrough-final/one-mistake-recovery-final.png).

검사 중 우측 꼭짓점으로 옮긴 삼각자의 회전 손잡이가 화면 밖으로 나가 입력이 막히는 결함을 발견했다. 화면 안에 연결 손잡이를 두고 손가락의 각도 변화만 적용하도록 고쳤다. 올바른 각으로 자동 회전하지 않는다. 수정 후 삼각자를 오른쪽 가장자리에 놓아 원래 팔 끝을 화면 밖으로 보낸 상태에서, 보이는 연결 손잡이를 잡아 실제로 회전하는 회귀 검사를 통과했다. [발견 당시 증거](../../logs/jareul-beollyeo-build/browser-offscreen-handle/normal-final.png), [수정 후 입력 증거](../../logs/jareul-beollyeo-build/browser-playthrough-final/edge-proxy-reachable.png).

추가 실제 입력 검증:

- 잘못된 옮기기 3회로 기회가 3→2→1→0이 되어 종료했다. 이후 드래그·탭과 10초 대기에도 기회·점수·종료 상태가 유지됐다. [종료 보고서](../../logs/jareul-beollyeo-build/browser-final/report.json).
- 22.5초 실제 대기 후 두 번째 각목을 먼저 선택·완료했다. 최초 각목은 미완료 상태로 남고, 완료 수는 1·기회는 3이었다. [동시 작업 보고서](../../logs/jareul-beollyeo-build/browser-second-piece/report.json).
- 고정 연습에서 5 cm 너비를 4 cm 변에 잘못 대도 기회 3·시간 90·점수 0·동결을 유지했다. 설명 후 5→5를 직접 맞추면 연습을 끝내고 진행했다. [연습 오답 보고서](../../logs/jareul-beollyeo-build/browser-tutorial-error/report.json).

## 첫 플레이와 판형

최종 소스에 대해 45초 firstplay를 새로 실행하여 **15프레임, ready=true, 콘솔·페이지 오류 0**을 기록했다. [최종 프레임 목록](../../logs/jareul-beollyeo-build/browser-firstplay-post/jareul-beollyeo/manifest.json). 프레임 10·11·12를 직접 열어 입력 위치의 리플, 실제 대상으로 향하는 화살표, 거절 문구가 바뀌는 것을 확인했다. 무작위 학생은 이 실행에서 연습을 완료하지 않았다. 이 결과를 이해도 판정 `yes`나 초보자의 완주 증거로 대체하지 않는다.

390×844, 820×1180, 1280×800, 1920×1080과 보조 844×390 화면을 캡처했다. 가로 스크롤은 없고 1280 이상 및 844×390에서는 `land`가 활성화된다. 초기 작은 데스크톱 도형과 모바일 하단 문구 겹침은 수정했다. [최종 QA 모바일](../../logs/jareul-beollyeo-build/browser-qa-post/mobile.png), [1280 화면](../../logs/jareul-beollyeo-build/browser-qa-post/desktop-1280.png), [짧은 가로 화면](../../logs/jareul-beollyeo-build/browser-final/play-844x390.png). 844×390은 높이 제한으로 도형이 작으며, 전체 완주 입력은 390×844에서 검증했다.

## B3 — 무뇌 입력 4종 × 200판

엔진 담당자가 DOM 스텁 VM에서 **수정하지 않은 생산용 pointer 핸들러**에 좌표 입력을 보내 각 200판을 실행했다. 실제 브라우저에서 800판을 실행한 것은 아니다. 정답 훅·정답 판정의 직접 호출을 쓰지 않았다. [검사기](../../logs/jareul-beollyeo-build/engine-spatial-bots.cjs), [판별 원자료](../../logs/jareul-beollyeo-build/engine-spatial-bots.json).

| 입력 정책 | 첫 시도 정답률 | 우연 수준 상한 | 완주율 | 첫 제출 문항 |
|---|---:|---:|---:|---:|
| 연타 | 0% | 25% | 0% | 0 |
| 순환 | 0% | 25% | 0% | 0 |
| 무작위 | 12.40% (15/121) | 25% | 0% | 121 |
| 무입력 | 0% | 0% | 0% | 0 |

제출이 0건인 정책의 0%는 학습 진도 0을 뜻한다. 무응답 문항을 무작위 정확도의 분모에 넣지 않았고, 별도로 제시된 문항은 연타·순환·무입력 각 400개, 무작위 414개로 기록했다. 25%는 초기 3개 변쌍+버리기 판단에서 정확한 끝점 위치를 무상으로 준 보수적 상한이다. 무작위 봇의 한 판 최대 완료 수는 1개였고, 모든 정책의 완주율은 0%였다. 추가 공간 입력 정책인 항상 버리기와 고정 전체 측정도 0/200이었다.

더 강한 **좌표 제약 없는 엔진 입력 의도** 스트레스 검사도 따로 재실행했다. 연타 0/200(0%)/25%, 순환 0/200(0%)/25%, 무작위 31/269(11.52%)/25%, 무입력 0%/0%였고 모든 정책의 완주율은 0%였다. 첫 일반 문항은 표시가 있는 `같은 변-다른 밑변-같은 변`의 배치로 고정해 학습 진입을 명확히 하고, 후속 작업 2∼4는 정확히 이등변 2개+세 변이 다른 삼각형 1개를 섞는다. [추가 입력 의도 원자료](../../logs/jareul-beollyeo-build/engine-bots.json).

## 수학·상태 전수 검사

[엔진 전수 보고서](../../logs/jareul-beollyeo-build/engine-audit.json): 기본 삼각형 351개, 매개변수·모드 조합 14,742개, 완전한 해답 경로 18,624개에서 잘못된 삼각형 0·우연히 참인 오답 0. 정상 500판과 첫 일반 실수 후 500판이 모두 목표에 도달했다. 정삼각형의 이등변 포함, 세 각을 모두 확인해야 하는 예각 판정, 동시 작업, 종료 직전 수학 피드백 보존을 검사했다. [독립 검토](../../logs/jareul-beollyeo-build/review-findings.md)는 정수 분류와 실제 회전 판정·레이아웃 유지 경로를 별도로 확인한다.

## 저장소 반영 제한

커밋을 시도했지만 `.git/index.lock` 생성이 `Operation not permitted`로 거부됐다. 따라서 커밋·푸시가 완료되지 않았다. [오류 원문](../../logs/jareul-beollyeo-build/commit.txt). 이 기록은 자동 승인 검토의 거절이 아니라 파일시스템 쓰기 권한 제한이다.
