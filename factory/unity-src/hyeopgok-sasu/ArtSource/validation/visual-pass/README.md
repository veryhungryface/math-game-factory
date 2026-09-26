# 협곡 사수 비주얼 강화 검증

`baseline/`은 이번 비주얼 수정 이전의 WebGL 빌드를 실행한 실제 캡처다. 각 이미지와 함께 `capture-report.json`에 게임 상태·Build SHA-256·브라우저 오류·GPU 렌더러를 기록한다. 문제 판정과 팩은 수정하지 않는다.

## 재현

저장소 루트에서:

```sh
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/visual-pass-capture.mjs final
python3 factory/unity-src/hyeopgok-sasu/ArtSource/validation/visual-pass/make_artifacts.py
node factory/lib/qa.mjs hyeopgok-sasu --out factory/unity-src/hyeopgok-sasu/ArtSource/validation/visual-pass/qa
```

캡처와 공식 QA는 성능 측정이 겹치지 않도록 순서대로 실행한다. 캡처 스크립트는 임의의 빈 로컬 포트와 독립 헤드리스 Chrome 한 개를 사용하고 종료 시 둘 다 닫는다. `factory/work/`에 쓰지 않는다. 표지 합성은 기본적으로 `artifacts/`에만 출력한다. 검토한 표지를 게임 폴더에 복사하려면 `make_artifacts.py --install-public`을 명시하며 그 복사 해시도 provenance에 기록된다.

- `playing-*`: 시작 훅 호출 후 약 1.2초.
- `battle-*`: 시작 훅 호출 후 약 7초. 캡처 자체의 시간이 더해지므로 정밀한 동일 프레임 비교는 아니다.
- `impact-early/mid/late-*`: 실제 게임의 정답 훅 호출 후 연속 캡처. 게임 화면을 연출하기 위해 추가 병력·이펙트나 편집 상태를 주입하지 않는다.
- `reward-*`: 정답 연출 뒤 새 문제 장면.
- 제목·문제·온보딩·성공 피드백은 원래 게임의 화면이다. 게임 캔버스나 코드를 숨기거나 수정하지 않는다.

## 표지와 비교판

표지는 게임과 같은 navy/cream/gold 및 `MgfKR-Bold.otf`로 제목을 얹는다. 가로 표지는 1200×630 원본 상단 HUD 띠만 제목으로 대체한다. 정사각 표지는 1280×800 실제 장면의 중앙 전장을 잘라 원래 비율 그대로 확대해 제목 밑에 배치한다. 원래 게임 장면의 병사·지형·광원·이펙트를 재생성·칠하기·추가 합성하지 않는다. `artifacts/provenance.json`은 원본 스크린샷 경로와 해시, crop 좌표, 결과 크기와 해시를 기록한다.

`reference-comparison.png`는 제공된 두 레퍼런스에서 소셜 화면 장식을 일부 잘라 전장 부분을 표시하고, 실제 390px 게임 전장과 나란히 놓는다. `before-after-mobile.png`와 `before-after-desktop.png`는 UI를 포함한 전체 캡처로 회귀를 확인한다. 이미지 비율은 유지한다.

## 개선 전 관찰

`baseline/battle-390.png` 및 `baseline/playing-1280.png` 직접 열람 기준: U자 구도와 적 관문·병영·왕·석궁·점선 패드는 명확하다. 절벽은 길게 이어진 단색 수직 판이며 풀밭의 큰 색면 안에는 미세 음영이 없다. 병사는 규칙적인 8열 격자처럼 보이고, 390px에서는 투구·방패보다 빨간 알갱이 패턴이 앞선다. 맞닿은 전선에서 흰 섬광·연기·금빛 파편의 지속적 폭발이 약하다. 레퍼런스와 비교 시 층진 절벽·풀/흙 표면의 변화·군집 식생·장비 실루엣·유기적인 떼 압축·연속 타격이 주요 차이다.

기존 수정인 압박 시간 표시·타이틀 첫 입력 시작·온보딩 왕 포인팅·정답/목표 HUD는 baseline에서 확인되며 이번 비주얼 작업에서 보존 대상이다.

## 최종 검토 (2026-09-27)

`final/` 28장과 두 레퍼런스의 나란한 비교판을 직접 열람했다. 최종 빌드의 wasm은 `1ce0d0…`, data는 `0166d4…`이며 전체 SHA-256은 `final/capture-report.json`에 있다. Metal Apple M4 실 GPU에서 콘솔·페이지 예외·실패 요청 0건이다. 이 캡처는 성능 시험이나 실제 iPhone 검증을 대신하지 않는다. 공식 QA 결과는 root가 별도 실행한 `qa/report.json`이 정본이다.

- 개선 확인: 둥근 빨간 투구와 각진 파란 투구/넓은 방패가 구별된다. 병사 위치 편차로 종전의 격자성이 줄고 떼의 면적이 채워진다. 절벽에 층띠와 모서리 밝음, 풀밭에 면별 색 변화, 가장자리에 활엽수/침엽수 군집이 생겼다.
- 타격 확인: 모바일 `impact-mid-390.png` 및 가로 `impact-mid-1280.png`에서 흰 볼트 궤적과 투구 위 흰 별, 둥근 연기, 금빛 파편이 함께 읽힌다. 1차 캡처에서 전선 앞을 가리던 나무는 최종에서 제거되어 충돌목이 드러난다.
- 남은 차이: 레퍼런스의 사선 원근 구도와 더 큰 석궁/병영 규모, 넓게 겹친 불규칙한 충돌 전선, 세밀한 표면 명암과 부드러운 접촉 그림자에는 아직 차이가 있다. 현재 절벽은 층띠가 있어도 긴 직선 판 외형이 남고, 넓은 배경의 풀 음영은 삼각형/사각형 경계가 읽힌다. 완전히 같은 시각 품질이라고 평가하지 않는다.
- 표지 확인: `public/g/hyeopgok-sasu/thumb.png`(1200×630, 246,086 bytes)와 `square.png`(1080×1080, 592,688 bytes)는 이 최종 렌더의 실제 캡처다. 제목 이외의 병력·지형·타격 효과를 새로 그리지 않았다. 정사각 crop은 적 관문 두 곳부터 U자 전선까지 보존한다. root가 두 표지를 직접 보고 확정했다.
- 크기: 표지 설치 직후 게임 폴더 총 9,552,892 bytes. 이미 gzip인 `.unityweb`은 그대로 세고 나머지 파일에 gzip level 9를 적용한 전송 크기 합은 9,069,121 bytes(8.649 MiB)다. 공식 QA의 `static.assets`는 원본 파일 크기 합 기준이므로 별도로 읽는다.

최종 비교 경로: `artifacts/reference-comparison.png`, `artifacts/before-after-mobile.png`, `artifacts/before-after-desktop.png`. 표지 출처는 `artifacts/provenance.json`. `interim-1/`과 `interim-artifacts-1/`, `interim-artifacts-crop/`은 전선 나무/섬광 높이 보정 이전의 중간 증거이며 최종 결과로 사용하지 않는다.
