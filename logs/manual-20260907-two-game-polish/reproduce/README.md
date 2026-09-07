# 두 게임 미세 디자인 수정 재현

저장소 루트에서 실행한다. 실제 GPU를 공유하므로 QA/캡처/첫플레이는 순차 실행한다. 아래 `<slug>`는 `twin-forts` 또는 `sunbasket-farm`이다.

```sh
node logs/manual-20260907-two-game-polish/reproduce/freeze.mjs <slug>
python3 logs/manual-20260907-two-game-polish/reproduce/check-source-pack.py <slug>
node logs/manual-20260907-two-game-polish/reproduce/capture.mjs <slug>
node factory/lib/qa.mjs <slug> --out logs/manual-20260907-two-game-polish/<slug>/qa
FIRSTPLAY_OUT=logs/manual-20260907-two-game-polish/firstplay FIRSTPLAY_PLAY_MS=45000 node logs/manual-20260907-two-game-polish/reproduce/firstplay-gpu.mjs <slug>
node logs/manual-20260907-two-game-polish/reproduce/verify-production-files.mjs
node logs/manual-20260907-two-game-polish/reproduce/capture.mjs <slug> --live
```

첫 플레이는 원래 무작위 포인터 정책을 그대로 쓰며 SwiftShader 강제 플래그만 제거했다. 현재 게임 경로 밖으로 이동하는 링크는 탐침 대상에서 제외하고, 실제 공개 캡처의 마지막 홈 이동으로 별도 검증한다. 이는 범위 제한이며 사용자 이해·학습 효과를 증명하지 않는다. 캡처 스크립트는 읽기 전용 상태/투영 좌표 관찰 후 실제 버튼·터치·마우스로 제출하고 수확한다. 정답/단계 변경 훅은 사용하지 않는다.

수학과 핵심 채점 함수의 동일성은 기존 배포본과 비교하며, 동일 소스의 과거 대량 검산은 새로 실행했다고 쓰지 않는다. ZIP은 기존 GLB/Blender와 새 런타임 참고 소스의 일치를 각각 확인한다.
