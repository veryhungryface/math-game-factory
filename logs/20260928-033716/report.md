🎮 [중학교] **새 게임이 나왔습니다** — 협곡 사수

**제목**: 협곡 사수
**슬러그**: `hyeopgok-sasu`
**한 줄**: 정답으로 전선 역전
**단원**: 중학교 2학년 2학기 · 확률
**차용 메커닉**: Kingshot 광고 속 협곡 디펜스 — 메커닉만 차용, 이름·에셋 오리지널
**성취기준**: [9수04-06]
**검수 점수**: 85/100 (커트라인 80)
**수정 루프**: 1/3회

**검수 세부**
  - curriculum: 23
  - math: 24
  - fun: 13
  - visual: 18
  - mobile: 7

**좋은 점**
  - problems_sample 112문항 전수 손검산 무오류, 독립 mathcheck verdict=pass(112문항 + 런타임 팩 444문항 대조). 판정은 {frac:n/d} 정수 기약 비교라 부동소수점 여지 없음.
  - 1차 must_fix high(포인팅 없음)가 실제로 해소됐다: 손이 네 패드를 고정 순서로 눌러 보이고(정답 누설 없음), 말풍선이 첫 입력까지 유지되며, 첫 문항 시간이 +12초다. 첫 플레이 15장 전 구간에서 확인.
  - 썸네일·정사각 이미지가 현재 빌드(4지 분수 패드·HUD·양 군세)로 교체됐고, HUD 칩 넘침과 분수 막대 비대칭도 고쳐졌다.

**지적 사항**
  - [medium] '보기 패드를 탭하세요' 말풍선이 왕을 따라다녀서 왕이 패드 구역에 들어가면 패드 분수의 분모를 가린다(firstplay frame-05 '1/6', frame-07 '1/6', frame-09 '2/3'). 말풍선이 첫 입력까지 유지되도록 바뀌면서 가리는 시간도 길어졌다.

⚙️ **빌드 폴백**: 수정 러너 대체(hit your usage limit) → claude_run

⚙️ **판정 폴백**: mathcheck-1: codex_run→grok_run (hit your usage limit); mathcheck-2: codex_run→grok_run (hit your usage limit)

▶ **플레이**: https://math-game-factory.vercel.app/g/hyeopgok-sasu/
🏠 **전체 목록**: https://math-game-factory.vercel.app

_소요 46분 · 로그 `logs/20260928-033716`_
