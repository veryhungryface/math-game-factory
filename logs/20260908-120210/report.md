💀 **생산 실패**

**제목**: 한 통
**슬러그**: `one-can`
**한 줄**: 컵을 넣어 채워라
**단원**: 3학년 2학기 · 들이와 무게
**차용 메커닉**: Stack (Ketchapp) — 움직이는 블록을 정확한 순간에 떨어뜨려 탑을 쌓고, 어긋난 만큼이 잘려 나가 다음 발판이 좁아지는 훅. 레일 위 컵을 탭해 1 L 깡통에 떨어뜨리고, 남은 들이를 넘는 컵은 넘친 페인트가 림에서 잘려 나가 통째로 불량 슈트에 빠진다. Make Ten Deluxe의 '합이 딱 10이 되는 손맛'을 합 1000 mL=1 L 뚜껑 닫힘으로, Hit the Button의 설명 0초·90초 최고기록 루프를 판 길이에 옮긴다. Stack 로고·네모 블록 아트·Make Ten 보드·Hit the Button UI는 쓰지 않는다.
**성취기준**: [4수03-17], [4수03-18], [4수03-19], [4수03-20], [4수03-21], [4수03-22], [4수03-23]
**수정 루프**: 2/3회

⚙️ **빌드 폴백**: 빌드 러너(codex_run / gpt-6-astra) 실패(rc=0, 402 Payment Required) → codex_run / gpt-5.6-sol 로 1회 재시도 — 폴백도 인프라 실패(402 Payment Required) / 수정 러너 대체(402 Payment Required) → claude_run


> 수정 러너 인프라 실패 — 402 Payment Required / 대체도 실패. 게임은 `public/g/one-can/` 에 그대로 두었다. 러너를 복구한 뒤 `RESUME_FROM=qa bash factory/run.sh` 로 이어서 돌려라.

_소요 256분 · 로그 `logs/20260908-120210`_
