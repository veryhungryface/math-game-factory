# 루트 레코드 — 아트·검증 소스

## 에셋 출처
- `public/g/route-record/thumb.png`, `square.png`, `assets/title.png`: 2026-10-15 공장 아트 단계 생성(gpt-image, 프롬프트는
  `factory/work-lanes/20261015-100006-L3/art-*.json`). 게임 본편은 이 이미지를 쓰지 않는다(표지·공유용).
- 게임 화면은 전부 런타임 코드 생성이다: Unity 프리미티브·둥근 상자(MgfLook.Block)·96각 원판 메시,
  절차 텍스처(비닐 홈 결·라벨·카메라 고정 반사광·잉크 고리), 외부 모델·텍스처·폰트 추가 없음(킷 한글 폰트만).
- 소리: 런타임 합성만. 펜타토닉 12음(경로 도착음·완성 화음), 비닐 표면 잡음 루프, 킷 MgfSfx. 외부 음원 없음.
- 팔레트: `#090A0F` `#3A1D2D` `#E8DCC2` `#20D9D2` `#F04E98` (검은 비닐·버건디 방음재가 주, 청록·마젠타는 경로 빛 전용).

## 구조 (`Scripts/`)
| 파일 | 내용 |
|---|---|
| `RouteRecordRules.cs` | 문제 은행(제약 만족 격자 전수), 판정 `PathCount`(한 면 안 곱 · 두 면 합, 정수), 무뇌 배치 배제, 오개념 진단, 밴드 선택 |
| `RouteRecordGeometry.cs` | 음반 위 홈 반지름·노치 각도·경로 점열(펄스 궤적), 화면점 → 가장 가까운 노치 |
| `RouteRecordGame.cs` | 흐름(타이틀→연습→본판 6장→결과), 실입력(노치 탭·브리지 드래그·톤암 드래그), 제출·판정, QA 훅 |
| `RouteRecordView.cs` | 부스·턴테이블·톤암·펄스·UI·온보딩·사운드 |

## 검증 (`validation/`)
- `run-headless.sh` → `headless-results.json`: 게임과 같은 `RouteRecordRules.cs` 를 번들 Roslyn 으로 컴파일해
  (1) 은행 385문항·핵심 문장 100% 고유·법적 상태 ≥24·균등 우연율 ≤1/8·무뇌 배치 정답 0건,
  (2) 노치 부분집합 전수 × 그래프 DFS 로 경로 수 독립 검산(73만 배치, 불일치 0),
  (3) 오개념 진단 모순 0건, (4) B3 무뇌 봇 8정책 × 1000판.
- `pointer-play.mjs` → `pointer-play-results.json` + `shots/`: 답 훅 없이 실제 pointer 탭·드래그로
  타이틀→연습→본판 6장을 390×844·1280×800 에서 정상 경로와 「첫 재킷 1회 오답 후 회복」 경로 모두 완주.
  두 경로가 오류 없이 clear이고 실행 중 WebGL 해시가 불변일 때만 현재 `MGF_RUN_ID`와 같은
  `artifact_hash`를 이 결과·레인 QA report·firstplay manifest 세 파일에 원자적 파일 교체로 기록하고,
  세 필드의 일치와 현재 `index.html`+`Build/` 재계산 해시 일치를 즉시 다시 검사한다. QA와 firstplay 하네스도
  생성 시점부터 같은 두 필드를 직접 쓰므로 이후 재실행이 증거 귀속을 지우지 않는다.
