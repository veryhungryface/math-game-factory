# 게를 꺼내 — 아트 출처

- `public/g/gereul-kkeonae/assets/title.png`: 아트 단계 생성 2.5D 코발트 산호 구조 키 아트. 최종 게임은 `background_policy`에 따라 이 이미지를 배경으로 쓰지 않고, 같은 팔레트의 Unity 로우폴리 월드를 코드로 구성한다.
- `public/g/gereul-kkeonae/assets/cord-fiber.png`: 아트 단계 생성 상아색 꼬임줄 텍스처. `Resources/GereulKkeonae/cord-fiber.png`로 복사해 도자 줄통과 줄 재질에 사용하며, 로드 실패 시 상아색 `MgfLook.Lit` 재질로 폴백한다.
- `thumb.png`와 `square.png`: `validation/capture-runtime-covers.mjs`가 최종 Unity 타이틀 세계를 각각 1200×630, 1080×1080으로 직접 캡처한 표지다. 하단 조작 UI가 잘린 채 보이지 않도록 더 높은 안전 렌더에서 로고·세계 영역만 규격대로 잘랐다.
- 외부 CDN·상표·기존 캐릭터 자산 없음. 산호게·산호 절벽·줄통·그물은 Unity 프리미티브와 코드 UI로 제작했다.

## 검증 기록

- `validation/pointer-playthrough.mjs`: 답 훅 없이 실제 포인터로 정상 8/8과 첫 오답 뒤 재단 회복 8/8을 완주한다.
- `validation/b3-pointer-selftest.mjs`: 고정·순환·무작위 각 200판의 첫 연결 600건을 줄 뽑기→절단→연결 실제 포인터로 제출하고, 무입력 200판의 진도 0을 확인한다.
- 두 결과 JSON의 `artifactHashSha256`는 최종 WebGL 산출물 `7f06b3bbdde7…21271b4c`와 대응한다.
