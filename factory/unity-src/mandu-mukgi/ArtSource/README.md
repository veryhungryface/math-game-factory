# 만두 묶기 (mandu-mukgi) — 아트·검증 기록

## 에셋 출처
- 런타임 화면의 모든 3D 오브젝트(코발트 차양·주문 패·컨베이어·증기 레인·찜통·도자기 봉인·번호 트레이·회수컵·노점 까치·반죽)는
  `Scripts/ManduWorld.cs`·`ManduTray.cs` 의 코드 메시/프리미티브로 만든다. 텍스처도 절차 생성(젖은 슬레이트 바닥, 밀가루 점무늬).
- 효과음은 `Scripts/ManduSfx.cs` 합성음(외부 음원 없음). 글꼴은 킷 NotoSansKR 서브셋(OFL).
- `public/g/mandu-mukgi/thumb.png`·`square.png`·`assets/title.png`·`assets/magpie-reference.png` 는 아트 단계가 codex 이미지 생성으로 만든
  키 아트·캐릭터 참고도다(프롬프트: `factory/work-lanes/20261011-014611-L1/art-*.json`). 런타임은 이 이미지를 읽지 않는다(까치는 참고도의
  검은 날개·흰 배·코발트 앞치마·산호색 집게를 프리미티브로 옮겼다). 상표·기존 캐릭터 복제 없음.

## 검증 스크립트(`validation/`)
| 파일 | 내용 |
|---|---|
| `b3-bot-selftest.mjs` | B3 무뇌 봇 8정책 × 200판. 실제 빌드 + puppeteer 진짜 마우스 획. 통계는 C# getState 그대로 |
| `band-diagnostic.mjs` | 2·3단계 문항에서 봇의 반죽별 첫 제출 정답률(훅으로 단계 이동·수리, 그 훅 정답은 집계 제외) |
| `oracle-playthrough.mjs` | 타이틀 → 연습 → 다리 연습 2회 → 12문항을 진짜 획으로 끝까지(무실수 / 1회 실수 후 수리) |
| `math-recheck.mjs` | 문제 은행 387문항 전수: 발문 수치만으로 정답 쌍 독립 계산·정삼각형/삼각형/각의 합/금지어·기존 연결≠정답, 화면 대상 최소 간격 |
| `shots.mjs`·`shots2.mjs` | 눈 확인용 캡처(세로 390×844·가로 1280×800) |

`?bot=N` 은 봇 측정 전용 배속(Unity Time.timeScale)이며 규칙·판정은 같다.
