# 협곡 사수 아트 라운드 4 검증

게임 코드와 팩을 바꾸지 않는 WebGL 블랙박스 검증 도구다. 저장소 루트에서 실행한다.

```bash
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r4/gates.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r4/capture.mjs
python3 factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r4/make-compare.py final
```

`gates.mjs`는 390×844(DPR 2)와 1280×800(DPR 1)에서 시작 후 3·8·15초를 잰다. 실제 스크린샷 픽셀과 `window.__HYEOPGOK_ART_DEBUG__`의 읽기 전용 투영값을 결합하며 결과는 `gates.json`, 원본 프레임은 `gate-frames/`에 남긴다. 디버그 훅 누락·형식 오류·필수 범주(왕/병사/건물/나무) 누락·범주별 가시 표본 0개는 모두 실패다.

그림자 표본의 `foot`은 모델 픽셀이 아니라 **발치 바로 아래 지면의 접촉 그림자 중심**이어야 한다. `ring`/`ground`는 같은 지면의 UI·모델·다른 그림자를 피한 투영점이며 프레임 안에 최소 3개가 있어야 한다. 390 캡처에서는 CSS 좌표에 PNG/viewport `scaleX`, `scaleY`를 적용하며 이 값도 각 snapshot에 기록한다.

`capture.mjs` 기본 출력은 `final/`이다. 부분 재실행은 `ONLY=judge,run,pour,geo` 중 필요한 값을 쉼표로 지정한다. 요구 파일은 `title`/`packs`/`play-{3s,8s,15s,after4correct}`/`pour`/`fraction`/`geometry`/`victory` 각각의 390·1280 PNG다. 기하 화면은 메모리 응답 fixture만 쓰며 실행 전후 팩 SHA가 같은지 `final/report.json`에 기록한다.

