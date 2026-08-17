#!/usr/bin/env bash
# 공장 설정 — run.sh 가 source 한다. 값만 바꾸면 파이프라인 동작이 바뀐다.

# ── 품질 ──────────────────────────────────────────────
export GATE_SCORE="${GATE_SCORE:-80}"          # 게시 커트라인 (100점 만점)
export DESIGN_VARIANTS="${DESIGN_VARIANTS:-3}" # 병렬 기획 에이전트 수
export FOCUS="${FOCUS:-5-2,6-2}"               # 집중 학년-학기

# ── 모델 ──────────────────────────────────────────────
export CLAUDE_MODEL="${CLAUDE_MODEL:-opus}"
export CODEX_MODEL="${CODEX_MODEL:-gpt-5.6-terra}"

# ── 단계별 제한 시간(초) ───────────────────────────────
export T_DESIGN="${T_DESIGN:-600}"     # 10분
export T_JUDGE="${T_JUDGE:-420}"       # 7분
export T_ART="${T_ART:-900}"           # 15분
export T_BUILD="${T_BUILD:-3000}"      # 50분
export T_MATHCHECK="${T_MATHCHECK:-600}"  # 10분 — 수학 전수 검산
export T_REVIEW="${T_REVIEW:-900}"     # 15분
export T_FIX="${T_FIX:-1500}"          # 25분

# ── 배포 ──────────────────────────────────────────────
export VERCEL_PROJECT="${VERCEL_PROJECT:-math-game-factory}"
export GH_REPO="${GH_REPO:-veryhungryface/math-game-factory}"
export DEPLOY="${DEPLOY:-1}"           # 0 이면 배포 생략 (드라이런)

# ── 보고 ──────────────────────────────────────────────
# #썜알트먼 채널 안의 스레드. 봇이 채널 본문에는 못 쓰고(403 Missing Permissions)
# 스레드에는 쓸 수 있어서 스레드 id 를 쓴다. 다른 곳으로 바꾸려면 이 값만 교체.
export REPORT_TARGET="${REPORT_TARGET:-discord:1516271245937868890}"
export REPORT="${REPORT:-1}"           # 0 이면 에르메스 보고 생략

# ── 기타 ──────────────────────────────────────────────
unset ANTHROPIC_API_KEY                # 구독 CLI 인증을 쓴다 (API 키 아님)
export PATH="/opt/homebrew/bin:/usr/local/bin:$HOME/.local/bin:$PATH"
