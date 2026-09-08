# 독립 검수 재현

격리 작업 폴더 `math-game-factory-sunbasket-v3`의 루트에서 실행한다. 출력은 이 review 폴더에만 쓴다. 아래 브라우저 명령은 제작자와 부모의 GPU 슬롯 반환 및 최종 소스 동결 뒤 순서대로 실행한다.

```bash
node factory/lib/qa.mjs sunbasket-farm --out logs/manual-20260908-sunbasket-v3/review/qa
FIRSTPLAY_OUT=logs/manual-20260908-sunbasket-v3/review/firstplay FIRSTPLAY_PLAY_MS=45000 node logs/manual-20260908-sunbasket-v3/review/firstplay-gpu.mjs sunbasket-farm
SUNBASKET_V3_BROWSER_SLOT=granted node logs/manual-20260908-sunbasket-v3/review/run-ui.mjs
python3 logs/manual-20260908-sunbasket-v3/review/audit-assets.py
node logs/manual-20260908-sunbasket-v3/review/verify-final-math.mjs logs/manual-20260908-sunbasket-v3/review/qa/report.json <최종-artifact-manifest.json>
```

`verify-final-math.mjs`는 인자가 없으면 최종 합격을 만들지 않고 `invariance-preliminary.json`에 현재 소스 비교만 남긴다. `baseline/`은 HEAD 변경이나 제작 중 소스에 따라 다시 만들지 않는다. 핵심 함수 변경이 발견되면 실제 diff와 유효 입력 증거를 별도로 검토해야 한다.

`run-ui.mjs`는 이전 독립 수확 하네스를 재사용하여 새 모델을 실제 입력으로 검증한다. 공개 수치에서 직접 계산해 한 번 틀린 일반 주문을 수정하고 9주문을 완주하며, 144작물·중복수확·4개 뷰포트·저장 재로드·실제 탭 전환의 입력 해제를 확인한다. 최대144를 포함하는 고정 난수열은 정상 UI 실행의 출제 재현용이다. 첫플레이와 퇴화정책 정확도 증거로 쓰지 않는다.

`firstplay-gpu.mjs`는 공용 하네스에서 상대 import를 옮기고 강제 SwiftShader 플래그 세 개를 제거한 실행본이다. 게임 밖으로 나가는 링크만 후보에서 제외한다. 정답·게임 상태를 읽지 않으며 ready만 확인한다. 모든 프레임을 실제로 본 범위와 탐침의 진행 한계를 리뷰에 기록한다. 이전 감사의 재사용과 이번 실행의 범위는 구분한다.

`audit-assets.py`는 GLB/ZIP 바이트·부품 구조를 점검한다. 그 결과만으로 모델 품질·재사용 편의성·실시간 성능을 통과시키지 않는다. 해당 화면과 실제 로드/입력 증거가 별도로 필요하다.
