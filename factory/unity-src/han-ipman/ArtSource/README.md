# 한 입만 — 아트 출처

- `public/g/han-ipman/assets/hero.png`: 아트 단계가 생성한 오리지널 작은 맥 손님의 모델링 참고 이미지. 런타임은 이 이미지를 배경으로 쓰지 않고 구·캡슐·둥근 상자 프리미티브로 다시 만든다.
- `public/g/han-ipman/assets/title.png`: 아트 단계가 생성한 1024×1536 코코아 진열장 키 아트. 최종 Unity 타이틀은 같은 팔레트·진열장·케이크·맥을 실시간 3D로 구성한다.
- `public/g/han-ipman/thumb.png`, `square.png`: 최종 WebGL 타이틀을 각각 1200×630, 1080×1080 실제 브라우저 프레임으로 캡처한 카탈로그 이미지다. 생성 원화는 표지로 쓰지 않는다.

생성 프롬프트와 크기는 `factory/work/chosen.json`의 `art_direction.assets_needed`, 실제 반환 경로와 용량은 `factory/work/art.json`이 정본이다. 외부 상표·기존 캐릭터를 참조하지 않았다.

## 검증

```bash
node factory/unity-src/han-ipman/ArtSource/validation/capture-runtime-covers.mjs
node factory/unity-src/han-ipman/ArtSource/validation/pointer-playthrough.mjs
node factory/unity-src/han-ipman/ArtSource/validation/dumb-bots.mjs
```

- `capture-runtime-covers.mjs`: 최종 WebGL 타이틀을 정확한 두 규격으로 직접 캡처하고 해시를 남긴다.
- `pointer-playthrough.mjs`: 독립 평가자가 저장된 화면 캡처만 보고 `q|계산 근거`를 입력한다. 스크립트는 `problemId`·상태의 `prompt`·문제은행·정답 훅을 읽지 않으며 실제 포인터 드래그로 연습+수선 판+무실수 둘째 판을 완주한다.
- `dumb-bots.mjs`: 연타·순환·무작위·무입력 봇을 각각 200판 실행하고 첫 시도 정답률/완주율/진도를 기록한다.
