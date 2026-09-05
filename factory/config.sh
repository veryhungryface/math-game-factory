#!/usr/bin/env bash
# 공장 설정 — run.sh 가 source 한다. 값만 바꾸면 파이프라인 동작이 바뀐다.

# ── 생산 리듬 ─────────────────────────────────────────
# 2026-09-06 체제 전환: 「틱마다 신작」 → **「하루 1작 목표」**.
# 크론은 그대로 2시간마다 run.sh 를 부르지만, 오늘(KST) 이미 게시한 게임이 있으면
# run.sh 최상단 가드가 신규 생산을 건너뛴다. 즉 하루 최대 12번 시도하되 첫 성공에서 멈춘다.
# 근거: docs/gpt6-factory-audit-20260905.md §5 — 「2시간마다 새 게임」이라는 약속은
# 실제 예산과 맞지 않았고, 실패를 생산량으로 덮는 유인이 됐다.
export DAILY_TARGET="${DAILY_TARGET:-1}"       # 하루 게시 목표. 0 이면 가드를 끈다(무제한 시도)
export FORCE_PRODUCE="${FORCE_PRODUCE:-0}"     # 1 이면 오늘 게시작이 있어도 강행 (수동 운영용)

# ── 품질 ──────────────────────────────────────────────
export GATE_SCORE="${GATE_SCORE:-80}"          # 게시 커트라인 (100점 만점)
export DESIGN_VARIANTS="${DESIGN_VARIANTS:-3}" # 병렬 기획 에이전트 수
export FOCUS="${FOCUS:-3-2,4-2}"               # 집중 학년-학기 (2026-09-01: 3·4학년 2학기 전용 — 사용자 지시)
# 검수 미달 시 수정→재검수 루프의 상한 (2026-08-31: 1회→3회, 폐기 전에 부족한 부분을 살린다).
# 게이트(80)는 불변 — 상한이 있으므로 루프가 게이트를 무의미하게 만들지 않는다.
# 라운드가 많아지면 사이클이 크론 주기(120m)를 넘을 수 있다 — run.lock 이 다음 틱을 스킵하므로 안전.
export MAX_FIX_ROUNDS="${MAX_FIX_ROUNDS:-3}"
# 게임 수가 같은 단원들 사이의 우선순위 (design-bible 7.2 · 킹수학 시장 공백 기준):
# 가능성 재도전 → 공간과 입체 → 원기둥·원뿔·구 → 원의 둘레와 넓이 → 비례식 → (과밀: 분수·소수 나눗셈은 마지막)
export PRIORITY_UNITS="${PRIORITY_UNITS:-g5s2-u6,g6s2-u2,g6s2-u6,g6s2-u5,g6s2-u4}"

# ── 모델 ──────────────────────────────────────────────
# 티어 대응표 (사용자 확인, 2026-08-18):
#   Claude:  opus(상) / sonnet(중)
#   GPT-5.6: sol(상) / terra(중) / luna(하)
#   Grok:    grok-4.6(상) / grok-4.5
# 역할 배정 원칙: 판단이 어려운 단계(심사·빌드·검수)는 상위 티어,
# 기획·수정은 중간 티어, 수학 검산은 **다른 회사 모델**(교차 검증 —
# 같은 모델이 만들고 같은 모델이 검산하면 같은 맹점을 공유한다).
export CLAUDE_MODEL="${CLAUDE_MODEL:-sonnet}"           # claude_run 기본
export CLAUDE_MODEL_SMART="${CLAUDE_MODEL_SMART:-opus}" # 심사·빌드·검수용
export CODEX_MODEL="${CODEX_MODEL:-gpt-5.6-terra}"      # codex_run 기본 (아트 등)
export CODEX_MODEL_SMART="${CODEX_MODEL_SMART:-gpt-5.6-sol}" # 수학 교차 검산용
export GROK_MODEL="${GROK_MODEL:-grok-4.6}"             # 기획 3번(관점 다양화)

# ── 단계별 러너 (클로드 토큰 절약 · 2026-08-26 사용자 지시) ──
# 방침: **클로드는 폴백으로만 쓴다.** 실작업은 codex·grok 이 맡는다.
# 근거: 8/25 신작 3연작(유리를 불어 81 / 등불을 켜 92 / 칸자물쇠 88)이 클로드 무사용
# (빌드=grok, 검수=codex sol)으로 전부 게이트를 통과했다 — 품질 손실 없이 대체 가능함이
# 실증됐다. 교차 검증 원칙은 유지된다: 빌드(grok)와 검수·수학검산(codex)이 다른 회사다.
# 러너 바이너리가 아예 없으면 run.sh 의 resolve_runner() 가 codex → claude 순으로 폴백한다.
# 바이너리는 있는데 API 가 죽는 경우(grok 402 잔액 소진 — 2026-08-27~28 4연속 사망)는
# resolve_runner 가 못 잡으므로, **빌드 단계는 실행 결과를 보고 codex(sol) 로 1회 폴백
# 재시도**한다 (run.sh step 5, 2026-08-28 도입).
# 빌드 기본은 **GPT-6(astra)** 다 (2026-09-06). 근거: 수동 1호기 「접으면 입체」가
# `codex exec --model gpt-6-astra` 빌드로 87점 게시됐다(HANDOVER 2026-09-05).
# grok 기본값은 2026-08-27 이후 402(잔액 소진)로 계속 죽어 폐기한다.
export BUILD_RUNNER="${BUILD_RUNNER:-codex_run}"    # 게임 구현
export BUILD_MODEL="${BUILD_MODEL:-gpt-6-astra}"    # 비우면 러너 기본 모델
# 결과 기반 폴백 1회(run.sh step 5): 비정상 종료·402/인증/쿼터·index.html 미생성이면 이 조합으로 재시도.
export BUILD_FALLBACK_RUNNER="${BUILD_FALLBACK_RUNNER:-codex_run}"
export BUILD_FALLBACK_MODEL="${BUILD_FALLBACK_MODEL:-$CODEX_MODEL_SMART}"  # gpt-5.6-sol
export REVIEW_RUNNER="${REVIEW_RUNNER:-codex_run}"  # 검수 — 빌드와 다른 회사(교차 검증)
export REVIEW_MODEL="${REVIEW_MODEL:-$CODEX_MODEL_SMART}"  # codex 러너는 모델 인자 필수
export FIX_RUNNER="${FIX_RUNNER:-grok_run}"         # 수정 — puppeteer 실측이 필요하다
export SCOUT_RUNNER="${SCOUT_RUNNER:-grok_run}"     # 레퍼런스 스카우트 — 웹 접근 필요
# 기획 3안은 서로 다른 관점이어야 한다. 기본값은 grok-4.6 / gpt-5.6-sol / gpt-5.6-terra —
# 세 관점이 전부 클로드 밖에 있다. 클로드를 다시 넣고 싶으면 이 값만 바꿔라.
export DESIGN_RUNNERS="${DESIGN_RUNNERS:-grok_run,codex_smart_run,codex_run}"

# ── 단계별 제한 시간(초) ───────────────────────────────
export T_DESIGN="${T_DESIGN:-900}"     # 15분 — grok 기획자가 claude 보다 오래 걸린다(실측 ~8분)
export T_JUDGE="${T_JUDGE:-420}"       # 7분
export T_ART="${T_ART:-900}"           # 15분
export T_BUILD="${T_BUILD:-3000}"      # 50분
export T_MATHCHECK="${T_MATHCHECK:-600}"  # 10분 — 수학 전수 검산
export T_REVIEW="${T_REVIEW:-1200}"    # 20분 — 900초에서 상향(2026-08-26). 900초 타임아웃이
                                       # 실제로 1회 터졌고, 주기가 3시간이 되어 여유가 생겼다
export T_FIX="${T_FIX:-1500}"          # 25분
export T_SCOUT="${T_SCOUT:-900}"       # 15분 — 레퍼런스 스카우트 (게임 N개마다 1회)

# ── 레퍼런스 갱신 루프 ─────────────────────────────────────────
export SCOUT_EVERY="${SCOUT_EVERY:-10}"  # 게임 몇 개 게시할 때마다 새 레퍼런스를 찾을지

# ── 배포 ──────────────────────────────────────────────
export VERCEL_PROJECT="${VERCEL_PROJECT:-math-game-factory}"
export GH_REPO="${GH_REPO:-veryhungryface/math-game-factory}"
export DEPLOY="${DEPLOY:-1}"           # 0 이면 배포 생략 (드라이런)

# ── 보고 ──────────────────────────────────────────────
# 사용자가 만든 "수학 게임 공장" 전용 스레드. 봇이 채널 본문에는 못 쓰고(403 Missing
# Permissions) 스레드에는 쓸 수 있어서 스레드 id 를 쓴다. 2026-08-18 사용자 요청으로
# 이 스레드로 변경 (이전: 1516271245937868890, #썜알트먼 안의 기존 잡담 스레드).
export REPORT_TARGET="${REPORT_TARGET:-discord:1539073913777291344}"
export REPORT="${REPORT:-1}"           # 0 이면 에르메스 보고 생략

# ── 기타 ──────────────────────────────────────────────
unset ANTHROPIC_API_KEY                # 구독 CLI 인증을 쓴다 (API 키 아님)
# cron/launchd 는 최소 PATH 로 실행된다. node·claude·codex·vercel 이 사는 곳을 전부 넣어둔다.
export PATH="/opt/homebrew/bin:/usr/local/bin:$HOME/.local/nodejs/bin:$HOME/.local/bin:$PATH"
