# 박쥐 깨워 (bakjwi-kkaewo) — 아트·검증 기록

## 런타임 아트
- 월드·캐릭터는 전부 Unity 코드 메시다(배경 일러스트 없음 — 기획서 `background_policy`).
  - 돌잠 박쥐: 구·캡슐 프리미티브 조합(몸·접힌 날개 주름·살구색 얼굴·코·귀·발). 접힌 날개 = 실제 자료로 그린 삼각형 메시.
  - 동굴: 둥근 지층 블록(`MgfLook.Block`) + 다공질 구 + 석순 + 주황 광물 + 먼지(가산 쿼드).
  - 팔레트: `#675083 #9B82B1 #FFC59A #F8F1DA #DA7248 #34283F` (기획서 palette 그대로).
- 아트 단계 생성물(`public/g/bakjwi-kkaewo/`): `thumb.png` 1200×630, `square.png` 1080×1080,
  `assets/hero.png`(박쥐 참조 시트), `assets/title.png`(세로 키 아트). 모두 codex 내장 이미지 생성(gpt-image),
  프롬프트는 `factory/work-lanes/20261018-000819-L1/chosen.json` `art_direction.assets_needed`, 결과 기록은 같은 폴더 `art-*.json`.
  런타임은 `hero`·`title` PNG 를 쓰지 않는다(배경 일러스트 금지 정책 — 참조용).
- UI 패턴 참고: Animata Split Reveal(타이틀 아치가 가운데서 갈라짐) · Icon Ripple(시작점 링) · Counter(점수 카운트업).
  코드·패키지는 쓰지 않고 C# 보간으로 재구현.

## 검증 (`validation/`)
| 파일 | 내용 |
|---|---|
| `run-headless.sh` + `HeadlessCheck.cs` | Unity 없이 `BakjwiRules.cs` 를 컴파일해 은행 검산·독립 실수 검산·오답 우연 참 전수·덱 마스크 분포·B3 봇(정책별 200판, 일부 2000판) |
| `headless-results.json` | 위 결과 |
| `pointer-selftest.mjs` | 실제 mouse 이벤트로 두 판 실플레이(첫 장면 과잉 선택 → 깃 손실 → 같은 장면 수정), 연습 게이트, 경계, 실포인터 무뇌 봇 |
| `pointer-selftest-results.json` | 위 결과 |
| `probe.mjs` | 화면 캡처(타이틀·연습·획·판정·SAS·AA·결과, 390×844 / 1280×800) |

### 실루엣(눈대중) 차단
그림은 실제 자료대로 그린다. 그래서 닮지 않은 날개는 기준과 그림의 세 각 차가 3.5° 이내(AA 는 3~5°)인 근접 반례만 쓴다
(SAS 끼인각 반례만 예외 — 오개념 확인용, 장면당 최대 1). 그림의 세 각을 ±1° 로 읽는 초인적 눈대중 정책도 숙련 통과 9%,
±2° 는 0%, ±4° 는 우연 수준이다 — 수치를 읽어야 통과한다.
