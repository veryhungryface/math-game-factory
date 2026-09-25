💀 **생산 실패**

**제목**: 별 딱총
**슬러그**: `star-cork`
**한 줄**: 큰 별을 접시에 담아
**단원**: 3학년 2학기 · 그림그래프
**차용 메커닉**: Topmarks Hit the Button의 중독은 규칙 설명 0초·60초 세션·최고기록의 극단적 단순함이고, 축제 딱총 사격 부스(오리 사격/Duck Hunt류)의 중독은 움직이는 표적이 화면 밖으로 나가기 직전의 긴장이다. 둘을 겹치면 '콤보가 끊기는 순간'이 아니라 '접시 값이 주문을 넘기기 직전·필요한 별이 막 사라지려는 순간'이 한 판만 더의 이유다.
**성취기준**: [4수04-01]
**수정 루프**: 3/3회

⚙️ **빌드 폴백**: 빌드 러너(codex_run / gpt-6-astra) 실패(rc=0, 402 Payment Required) → codex_run / gpt-5.6-sol 로 1회 재시도 — 폴백도 인프라 실패(402 Payment Required) / 수정 러너 대체(402 Payment Required) → claude_run / 수정 러너 대체(402 Payment Required) → claude_run


> 수정 러너 인프라 실패 — 402 Payment Required / 대체도 실패. 게임은 `public/g/star-cork/` 에 그대로 두었다. 러너를 복구한 뒤 `RESUME_FROM=qa bash factory/run.sh` 로 이어서 돌려라.

_소요 372분 · 로그 `logs/20260909-000424`_
