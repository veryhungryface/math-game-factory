# 게를 꺼내 — 아트 출처

- `public/g/gereul-kkeonae/assets/title.png`: 초기 아트 단계의 2.5D 코발트 산호 안. 1차 검수에서 `ssak-geonjyeo`와 수중 암반·게 군집 물성이 같은 계열임이 확인돼 런타임에서 사용하지 않는 폐안 참고물로만 보존한다.
- `public/g/gereul-kkeonae/assets/cord-fiber.png`: 아트 단계 생성 상아색 꼬임줄 텍스처. `Resources/GereulKkeonae/cord-fiber.png`로 복사해 도자 줄통과 줄 재질에 사용하며, 로드 실패 시 상아색 `MgfLook.Lit` 재질로 폴백한다.
- `thumb.png`와 `square.png`: `validation/capture-runtime-covers.mjs`가 최종 Unity 타이틀 세계를 각각 1200×630, 1080×1080으로 직접 캡처한 표지다. 하단 조작 UI가 잘린 채 보이지 않도록 더 높은 안전 렌더에서 로고·세계 영역만 규격대로 잘랐다.
- 1차 수정 시각 재설계: 수중 배경을 완전히 제거하고, 노을빛 건식 조간대의 목재 갑판·철제 크레인·금속 도르래·긴 섬유 다발·절단 장력 다리를 Unity 프리미티브로 구성했다. 둥근 게 군집 대신 각진 작업게 한 마리만 남기고, 정답 사건은 도르래가 당겨져 처진 다리가 칸별로 펼쳐지는 물리 사건으로 바꿨다.
- 외부 CDN·상표·기존 캐릭터 자산 없음. 모든 새 작업장 오브젝트와 캐릭터는 Unity 코드로 제작했다.

## 검증 기록

- `validation/pointer-playthrough.mjs`: 답 훅 없이 실제 포인터로 정상 8/8과 첫 오답 뒤 재단 회복 8/8을 완주한다.
- `validation/b3-pointer-selftest.mjs`: 고정·순환·무작위 각 200판의 첫 연결 600건을 줄 뽑기→절단→연결 실제 포인터로 제출하고, 무입력 200판의 진도 0을 확인한다.
- 두 결과 JSON의 `artifactHashSha256`는 최종 WebGL 산출물 `7f06b3bbdde7…21271b4c`와 대응한다.
