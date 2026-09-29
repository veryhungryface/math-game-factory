🎮 [중학교] **새 게임이 나왔습니다** — 유리칼

**제목**: 유리칼
**슬러그**: `yuri-kal`
**한 줄**: 교점을 밀어 쪼개라
**단원**: 중학교 2학년 2학기 · 사각형의 성질
**차용 메커닉**: 실측 판정(네모시티 도형 구조대 / GeoGebra 류) — 점을 끌면 길이가 따라 바뀌고, 허용오차 안에 들어가는 순간의 긴장이 훅이다. 90초·최고기록 뼈대는 Topmarks Hit the Button의 '설명 0초·한 판이 짧아 한 판만 더'. 빗나간 쪼개짐이 서리 금으로 남아 다음 장의 작업 면을 깎는 압박만 Stack에서 가져온다(어긋난 만큼 잘려 다음이 어려워지는 그 한 줄). 과일·닌자·Ketchapp 블록 에셋은 쓰지 않는다.
**성취기준**: [9수03-11]
**검수 점수**: 88/100 (커트라인 80)

**검수 세부**
  - curriculum: 25
  - math: 24
  - fun: 16
  - visual: 16
  - mobile: 7

**좋은 점**
  - 중2 [9수03-11] 대각선 성질·포함 관계·「그」 함정·연 모양 반례를 쪼개기/치우기 동사로 정면 다룬다.
  - 판정은 정수 mm·정수 도(Judge.Bisect/Perp/EqDiag). 표본 106문항을 규칙으로 재계산해 전부 일치, 독립 mathcheck pass.
  - 무뇌 봇 첫 시도가 우연 수준을 넘지 않는다(판 규칙 켜면 0%). 실패가 예비 아크릴 금·서리로 보인다.

**지적 사항**
  - [medium] 길이 숫자가 유리칼 머리·꼭짓점 라벨과 겹친다. mobile.png는 7.5 cm가 주황 테에 올라타고, 두 번째 판에서 O가 꼭짓점 근처면 「B1 cm」처럼 붙는다. 한눈에 이등분을 읽어야 하는 게임인데 근거 정보가 뭉개진다.
  - [medium] 연 모양 잠금 문장이 「AC⊥BD, AO = CO」만 적고 BO ≠ DO를 빠뜨린다. 텍스트만 보면 마름모와 구별이 안 된다. 이번 표본 106장에는 연 모양이 없어 오답은 안 났지만 생성 풀에는 있다.

⚙️ **빌드 폴백**: 빌드 러너(codex_run / gpt-5.6-sol) 실패(rc=1, hit your usage limit) → claude_run / opus 로 1회 재시도

⚙️ **판정 폴백**: mathcheck-1: codex_run→grok_run (hit your usage limit); review-1: codex_run→grok_run (hit your usage limit)

▶ **플레이**: https://math-game-factory.vercel.app/g/yuri-kal/
🏠 **전체 목록**: https://math-game-factory.vercel.app

_소요 81분 · 로그 `logs/20260929-002729`_
