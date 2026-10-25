# 한 입만 — 아트 출처

- `public/g/han-ipman/assets/hero.png`: 아트 단계가 생성한 오리지널 작은 맥 손님의 모델링 참고 이미지. 런타임은 이 이미지를 배경으로 쓰지 않고 구·캡슐·둥근 상자 프리미티브로 다시 만든다.
- `public/g/han-ipman/assets/title.png`: 아트 단계가 생성한 1024×1536 코코아 진열장 키 아트. 최종 Unity 타이틀은 같은 팔레트·진열장·케이크·맥을 실시간 3D로 구성한다.
- `public/g/han-ipman/thumb.png`, `square.png`: 아트 단계가 생성한 초기 카탈로그 이미지. 최종 검증 뒤 실제 런타임 캡처로 교체할 수 있다.

생성 프롬프트와 크기는 `factory/work/chosen.json`의 `art_direction.assets_needed`, 실제 반환 경로와 용량은 `factory/work/art.json`이 정본이다. 외부 상표·기존 캐릭터를 참조하지 않았다.

## 검증

```bash
node factory/unity-src/han-ipman/ArtSource/validation/pointer-playthrough.mjs
node factory/unity-src/han-ipman/ArtSource/validation/dumb-bots.mjs
```

- `pointer-playthrough.mjs`: 테스트 정답 훅 없이 실제 포인터 드래그로 연습+수선 판+무실수 판을 완주한다.
- `dumb-bots.mjs`: 연타·순환·무작위·무입력 봇을 각각 200판 실행하고 첫 시도 정답률/완주율/진도를 기록한다.
