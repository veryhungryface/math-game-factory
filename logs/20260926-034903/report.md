🎮 [중학교] **새 게임이 나왔습니다** — 원 찍어

**제목**: 원 찍어
**슬러그**: `won-jjigeo`
**한 줄**: 중심에 핀을 박아라
**단원**: 중학교 2학년 2학기 · 삼각형의 성질
**차용 메커닉**: Topmarks Hit the Button — 중독은 버튼 6개가 아니라 '규칙 설명 0초 · 1분 세션 · 개인 최고기록 한 판 더'의 짧은 아케이드 뼈대다. 원작의 n지선다는 가져오지 않는다. 답 입력은 실측 판정(네모시티 도형 구조대 / GeoGebra 류)의 '점에 도구를 대고, 벡터+허용오차로 판정'만 가져온다. 아슬아슬하게 오차 원 안에 들어갈 때의 긴장이 훅이다.
**성취기준**: [9수03-09], [9수03-10]
**검수 점수**: 82/100 (커트라인 80)
**수정 루프**: 1/3회

**검수 세부**
  - curriculum: 22
  - math: 23
  - fun: 15
  - visual: 14
  - mobile: 8

**좋은 점**
  - 핀 좌표가 곧 외심/내심이라 수학이 동사다. n지선다 없음.
  - 1차 지적(조준 후 재탭) 제거됨 — 베드 탭 1회로 박히고 첫 플레이 frame-05가 증거.
  - 오답에 진짜 O/I 링·서로 다른 반지름·한 줄 이유(등거리/둔각은 바깥/빗변 위)가 떠서 왜 틀렸는지가 보인다.

**지적 사항**
  - [medium] 허용 반지름이 외접원 R×0.08뿐이고, 기획서의 '390폭 10~16px 클램프'가 없다. 검수 실플레이에서 예각 이등변의 진짜 O를 본 뒤에도 탭 3회가 링 바로 옆을 스쳐 전부 오답이었다. 손가락 44px 대비 타깃이 작다.
  - [medium] 은행 문자열이 ∠BOC=2∠A(204°~242°)를 쓴다. 호 BC의 중심각으로는 맞지만 중2가 △BOC의 안각으로 읽으면 360°−2∠A다. 화면 판정은 핀 위치라 오답은 아니다(mathcheck도 phrasing medium).

⚙️ **빌드 폴백**: 수정 러너 대체(hit your usage limit) → claude_run

⚙️ **판정 폴백**: mathcheck-1: codex_run→grok_run (hit your usage limit); review-1: codex_run→grok_run (hit your usage limit); mathcheck-2: codex_run→grok_run (hit your usage limit); review-2: codex_run→grok_run (hit your usage limit)


_소요 62분 · 로그 `logs/20260926-034903`_
