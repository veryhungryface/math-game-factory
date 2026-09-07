# 햇살 바구니 재현

저장소 루트에서 실행한다. GPU 검증은 서로 순차 실행한다.

```sh
node factory/lib/qa.mjs sunbasket-farm --out logs/manual-20260907-sunbasket-farm/qa
FIRSTPLAY_OUT=logs/manual-20260907-sunbasket-farm/firstplay-naive FIRSTPLAY_PLAY_MS=45000 node logs/manual-20260907-sunbasket-farm/reproduce/firstplay-gpu.mjs sunbasket-farm
```

첫 플레이는 기존 공용 탐침의 무작위 포인터 정책을 사용한다. 공용 코드를 수정하지 않고 로컬 복사본에서 SwiftShader 강제 플래그3개를 제거해 실제 GPU를 사용하며, 다른 페이지로 이동하는 링크를 대상에서 제외한다. 게임 내부 조작을 보는 범위 제한이다. 수학 정답/문제 상태를 읽지 않고 `ready`만 기다린다. 실제 어린이 사용자 테스트나 45초 이해도 증거로 간주하지 않는다.

실제 입력과 파일 검증:

```sh
node logs/manual-20260907-sunbasket-farm/reproduce/visual-smoke.mjs
python3 logs/manual-20260907-sunbasket-farm/reproduce/check-source-pack.py
node logs/manual-20260907-sunbasket-farm/reproduce/verify-production-files.mjs
node logs/manual-20260907-sunbasket-farm/reproduce/live-ui.mjs
```

`build-proof/full-ui.mjs`와 `final-layout.mjs`는 빌더 실행본을 보존하면서 새 위치에 맞게 import 깊이와 출력 경로만 고쳤다. 재실행하려면 별도 터미널에서 `python3 -m http.server 8787 --directory public`로 서버를 띄운다. full-ui의 blur 검사는 합성 이벤트이며, 실제 탭 전환은 독립 검수의 별도 증거를 따른다.
