# 나비 재봉소 — 원화·검증 기록

## 생성 에셋 기록과 런타임 사용 여부

- `title.png`: `factory/work/art.json`의 1차 생성물 원본. 1차 검수에서 실제 Unity 플레이 세계와 불일치가 확인되어 **v2 런타임에서는 사용하지 않는다**. 비교·출처 기록으로만 보존한다.
- `hero-reference.png`: `factory/work/art.json`의 `hero-reference` 생성물 원본. 흰 배경의 2시점 모델 참고로만 쓰며 런타임 스프라이트로 참조하지 않는다.
- 게시 표지 `public/g/nabi-jaebongso/thumb.png`, `square.png`는 v2 최종 WebGL의 실제 Unity 타이틀을 직접 캡처한다.

생성 프롬프트와 크기·검사 결과의 정본은 `factory/work/art.json`이다. 외부 URL·CDN·추가 런타임 이미지가 없다.

## 런타임 시각 정본

밝은 민트·산호·라벤더 빛이 통과하는 거대 고치 내부, 비대칭 섬유 리본과 장력 고리, 부정형 실크 막, 도자 컴퍼스·셔틀, 원본 삼각형·원호·자는 모두 `NabiJaebongsoView.cs`가 로컬 메시와 프리미티브로 만든다. 타이틀도 생성 회화 없이 같은 Unity 세계를 그대로 보여 준다. 작도 평면은 카메라와 평행해 원이 원으로 보이고, 정답 위치 스냅이나 제출 전 정답색은 없다. 정답 사건은 실크 막이 자라고 셔틀이 학생이 그린 변을 따라 실을 엮는 봉합이다.

## 재현 검증

로컬 서버(`npm run serve`)가 켜진 상태에서:

```bash
node factory/unity-src/nabi-jaebongso/ArtSource/validation/b3-bot-selftest.mjs
node factory/unity-src/nabi-jaebongso/ArtSource/validation/pointer-playthrough.mjs
FIRSTPLAY_PLAY_MS=45000 node factory/lib/firstplay/harness.mjs nabi-jaebongso
node factory/lib/qa.mjs nabi-jaebongso
```

- `b3-bot-results.json`: 연타·4단 순환·무작위·무입력 각 200판, 문항별 첫 제출률과 완주율.
- `pointer-playthrough-results.json`: 실제 pointer 이벤트로 타이틀→고정 연습→6날개 클리어, 실제 원호 구간 밖 가상 교점 회귀 거부, 대응 순서 오답→수선, 화면에 표시된 합동 순서만 독립 계산한 두 번째 판을 기록한다. 문제은행의 `answer`는 읽지 않는다.
- `pointer-playthrough/*.png`: 위 흐름의 증거 프레임.
- `firstplay/`: 45초 첫 플레이 하네스 프레임(실행 후 생성).

문제 은행 360개는 부팅 때 `NabiRules.Audit`가 ID·발문 유일성, 부등변, 삼각형 부등식, 서로 다른 두 반지름, `initialWrongEdge` 우연 정답 0건, `correspondence_order` 교환 우연 정답 0건을 전수 검사한다.

## 1차 수정 (2026-10-25)
- 타이틀 중앙 정적 판 → 코드로 만든 투광 실크 시연막(원호 둘 → 교점 두 선 → 실크막 성장 → 봉합 매듭, 5.2초 반복). 생성 이미지는 런타임에 쓰지 않는다.
- `validation/layout-check.mjs` — 세로/가로 배치 회귀(원본 꼭짓점이 발문 아래·밑변이 안내 위) + 고정 연습 B→C 외 변 거절 회귀. 결과 `layout-check-results.json`, 캡처 `layout-check/`.
- `validation/capture-runtime-covers.mjs` — thumb/square 를 시연 완성 시점(3.9초)에 런타임 캡처.

## 2차 수정 (2026-10-25)
- 첫 BC 폭 잡기를 44 CSS px 이상 레일로 확장하고, 6초 무진전/실패 2회 뒤 `B를 누른 채 C에서 떼기`와 완주 유령손을 같이 보인다.
- 820×1180 태블릿을 별도 분기해 발문→원본판→대상판→도구 안내 안전영역을 분리한다.
- 어두운 제도실/금속 압인 계열을 제거하고, 밝은 섬유 터널/부정형 막/도자 셔틀 왕복으로 배경·부품·보상 세 축을 바꾼다.
