# 제비 특급 (jebi-teukgeup) — 아트·검증 기록

## 런타임 아트
- 배경 일러스트 없음. 하늘은 카메라에 붙은 세로 그라디언트 판(코드 생성 텍스처), 구름은 구 여러 개를 `CombineMeshes`로 합친 뭉게구름 메시.
- 원형 역·내접원은 절차 원환(`JebiMesh.Torus`), 기구·종·부리는 회전체(`JebiMesh.Lathe`), 구·원기둥도 절차 메시
  (물리 모듈이 빠진 빌드에서 `GameObject.CreatePrimitive`가 콜라이더 에러를 내므로 쓰지 않는다).
- 우편 제비(오리지널): 구 8개 + 원뿔 부리 + 두 갈래 꼬리 판 + 우편 가방. 우편차: 둥근 상자(`MgfLook.RoundedBox`) + 바퀴 2개.
- 효과음은 전부 합성(`JebiAudio`: 눈금 딸깍·고리 금속음·종·장력파·발차기·그물·거절음 + 킷 `MgfSfx`).
- 글꼴: 킷 MgfKR(NotoSansKR Bold 서브셋, OFL).

## 아트 단계 생성 이미지(게임 런타임에서는 쓰지 않음)
`public/g/jebi-teukgeup/assets/title.png` · `assets/hero.png` 는 아트 단계(codex 내장 이미지 생성)가 기획서
`art_direction.assets_needed` 프롬프트로 만든 참고 원화다(`factory/work/art.json`). 최종 `thumb.png`(1200×630)와
`square.png`(1080×1080)는 2차 수정 뒤 `capture-covers.mjs`로 최종 WebGL 타이틀을 직접 캡처한다.
- hero: "Single original plump swallow courier with a short forked tail, orange beak and small cream mailbag, standing on a tiny two-wheel postal cart …"
- thumb / square: "dense miniature aerial postal network: round cream balloon stations linked by taut straight tangent cables …"
런타임은 기획서 background_policy(평면 배경 일러스트 금지)에 따라 참고 이미지를 장면에 깔지 않고, 제비 모델의 형태 참고로만 썼다.

## 검증 스크립트 (`validation/`)
- `common.mjs` — 정적 서버·크롬·지급 눈금 좌표(JebiView.cs 와 같은 식).
- `capture.mjs` — 390×844 · 1280×800 실제 포인터 장면 캡처(`shots/`).
- `pointer-bots.mjs` — B3 무뇌 봇(고정·순환·무작위 각 200판, 점검역 q 유지·q 배제 각 120판, 무입력 200판 + 90초 방치) → `bot-results.json`.
- `playthrough.mjs` — 정답을 아는 흐름 검증(연습 12→7, 8/8 도착, 1역 수리 후 7/8 도착, 고리 0 종료) → `playthrough-*.json`.
# 1차 수정 시각 재설계 기록 (2026-10-29)

- 생성 이미지나 외부 에셋은 추가하지 않았다. 모든 형상·재질·애니메이션은 Unity 런타임 코드로 구성한다.
- 비교 근거: `public/g/sil-taneun-geomi/thumb.png`와 실제 QA 모바일 프레임을 직접 대조했다.
- 수정 정체성: 부드러운 파스텔 저폴리 부품과 동물 활주 보상에서, 짙은 청록의 각진 법랑 항공 관제판·황동 회전부·직조 케이블·경로 도장 연쇄로 전환했다.
- 정답 사건: 제비가 줄을 타는 대신 원형 관제판 회전 → 양쪽 날개 접힘 → 도장 3개 연쇄 인쇄가 중심이 된다. 정답 화면 흔들림은 쓰지 않는다.
- 가독성: 플레이 중 배경 제비·구름을 HUD와 미니 노선 금지 영역에서 빼고, 상단 HUD와 미니 노선 뒤에 불투명한 남청 법랑 바탕을 둔다.

# 2차 수정 시각·증거 재설계 기록 (2026-10-29)

- 검수에서 `baeyul-mireo`와 겹친 전면 플랫 작업면·금속 도장 사건을 폐기했다. 화면 가장자리까지 이어지는 밝은 하늘,
  깊이가 다른 기구 역·구름, 목재 부유 갑판과 현수 프레임을 플레이 장면에도 유지한다.
- 정답 사건은 프레스/도장이 아니라 `장력파 → 세 분기 잠금쇠 체결 → 노선등 순차 점등`이다. 우편차는 줄을 활주하지 않으며
  정답 화면 흔들림도 쓰지 않는다.
- 지급 눈금은 크림색 레일 폭을 30→72 논리 px로 늘리고 1~24 숫자를 레일 안쪽의 12~14 px 남색으로 옮겼다.
- 세로 플레이에서는 장식 구름·기구·군조를 숨기고, 가로에서도 안전 가장자리만 허용해 HUD·발문·미니 노선 겹침을 막는다.
- `pointer-bots.mjs`의 artifact 해시는 QA와 같은 `index.html + Build/*` 규칙을 쓰고, `MGF_RUN_ID`를 결과에 보존한다.
- 최종 표지는 생성 원화가 아니라 `capture-covers.mjs`가 최종 WebGL 타이틀을 직접 캡처한다.

# 3차 수정 학습 경로 표현 보완 (2026-10-29)

- 문제은행의 Pair·Sum 발문뿐 아니라 실제 도면 성질 명패, 3역 뒤 개념 다리, 연습 정답 설명도
  `원 O 밖의 점 P에서 원 O에 그은 두 접선`으로 통일했다. 코드 속 정답만 맞고 화면 설명은 목적어가
  빠져 있던 학습 경로 불일치를 없앤 수정이며, 핵심 조작·판정·시각 사건은 바꾸지 않았다.
