# 꿀 도둑 — 생성 자산·소리 기록

- 런타임 배경·벌·벌집·칼·UI는 모두 `Scripts/KkulDodukView.cs`의 Unity 프리미티브와 런타임 생성 텍스처로 만든다.
- 외부 네트워크, CDN, 원격 폰트, 외부 오디오 파일은 사용하지 않는다.
- 효과음은 사용자 제스처 뒤 `MgfSfx`가 생성하는 코드 합성음만 사용한다.
- `public/g/kkul-doduk/assets/title.png`는 아트 단계의 생성 참고 이미지이며 게임 배경에는 사용하지 않는다.
- 최종 `thumb.png`(1200×630)와 `square.png`(1080×1080)는 `validation/capture-runtime-covers.mjs`가 실 GPU Chrome에서 실제 Unity 타이틀을 직접 캡처한 것이다. 생성 표지를 최종 표지로 사용하지 않는다.
- 타이틀은 실제 런타임 밀랍 입구와 두 조각으로 갈라지는 봉인을 사용한다.

검증 산출물은 `validation/` 아래에 둔다. `math-audit.mjs`는 생성 풀과 오개념 값의 우연 일치 0건을, `b3-pointer-bots.mjs`는 실제 브라우저 포인터 입력 정책을 검사한다. `capture-runtime-covers.mjs`가 표지의 출처와 재현 방법이다.
