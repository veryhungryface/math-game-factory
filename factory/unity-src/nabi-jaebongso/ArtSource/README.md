# 나비 재봉소 — 원화·검증 기록

## 실제 에셋

- `title.png`: `factory/work/art.json`의 `title` 생성물 원본. Unity `Resources/NabiJaebongso/title.png`와 `public/g/nabi-jaebongso/assets/title.png`에 같은 파일을 사용한다. 타이틀 전용이며 로드 실패 시 코드 하늘 월드가 그대로 보인다.
- `hero-reference.png`: `factory/work/art.json`의 `hero-reference` 생성물 원본. 흰 배경의 2시점 모델 참고로만 쓰며 런타임 스프라이트로 참조하지 않는다.
- 게시 표지 `public/g/nabi-jaebongso/thumb.png`, `square.png`는 아트 단계 생성물이다.

생성 프롬프트와 크기·검사 결과의 정본은 `factory/work/art.json`이다. 외부 URL·CDN·추가 런타임 이미지가 없다.

## 런타임 시각 정본

수레국화색 하늘, 상아 고치와 실망, 사프란 꽃, 나비 몸통·삼각 날개, 원본 삼각형·컴퍼스·원호·자는 모두 `NabiJaebongsoView.cs`가 로컬 메시와 프리미티브로 만든다. 작도 평면은 카메라와 평행해 원이 원으로 보이고, 정답 위치 스냅이나 제출 전 정답색은 없다.

## 재현 검증

로컬 서버(`npm run serve`)가 켜진 상태에서:

```bash
node factory/unity-src/nabi-jaebongso/ArtSource/validation/b3-bot-selftest.mjs
node factory/unity-src/nabi-jaebongso/ArtSource/validation/pointer-playthrough.mjs
FIRSTPLAY_PLAY_MS=45000 node factory/lib/firstplay/harness.mjs nabi-jaebongso
node factory/lib/qa.mjs nabi-jaebongso
```

- `b3-bot-results.json`: 연타·4단 순환·무작위·무입력 각 200판, 문항별 첫 제출률과 완주율.
- `pointer-playthrough-results.json`: 실제 pointer 이벤트로 타이틀→고정 연습→6날개 클리어, 대응 순서 오답→수선, 두 번째 첫 제출 실패→gameover.
- `pointer-playthrough/*.png`: 위 흐름의 증거 프레임.
- `firstplay/`: 45초 첫 플레이 하네스 프레임(실행 후 생성).

문제 은행 360개는 부팅 때 `NabiRules.Audit`가 ID·발문 유일성, 부등변, 삼각형 부등식, 서로 다른 두 반지름, `initialWrongEdge` 우연 정답 0건, `correspondence_order` 교환 우연 정답 0건을 전수 검사한다.
