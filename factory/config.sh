#!/usr/bin/env bash
# 공장 설정 — run.sh 가 source 한다. 값만 바꾸면 파이프라인 동작이 바뀐다.

# ── 품질 ──────────────────────────────────────────────
export GATE_SCORE="${GATE_SCORE:-80}"          # 게시 커트라인 (100점 만점)
export DESIGN_VARIANTS="${DESIGN_VARIANTS:-3}" # 병렬 기획 에이전트 수
export FOCUS="${FOCUS:-5-2,6-2}"               # 집중 학년-학기
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
# 러너가 없거나 죽으면 run.sh 의 resolve_runner() 가 codex → claude 순으로 폴백한다.
export BUILD_RUNNER="${BUILD_RUNNER:-grok_run}"     # 게임 구현 — 로컬 무샌드박스라 qa.mjs 직접 실행 가능
export BUILD_MODEL="${BUILD_MODEL:-}"               # 비우면 러너 기본 모델
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
