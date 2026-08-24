# 수학 게임 공장 — 운영 매뉴얼 (세션·모델 무관 인수인계 문서)

> 이 문서는 어떤 AI 세션/모델이 와도 공장을 이어 운영할 수 있게 하는 **불변 운영 규약**이다.
> 현재 상태·백로그는 `factory/state/HANDOVER.md` (살아있는 문서)를 봐라.
> 게임 제작 규칙은 저장소 루트 `CLAUDE.md`, 디자인 철학은 `docs/design-bible.html`, 타이틀 규격은 `docs/title-screen-spec.md`.

## 1. 시스템 한 줄 요약
2시간마다 크론이 `factory/run.sh`를 돌려 2022 개정 교육과정(초등 5-6학년 2학기 중심) 단원별 수학 게임을 자동 기획→제작→검증→게시하고, 사람(교사)의 실사용 피드백을 받아 즉시 수정하는 공장.

## 2. 인프라 좌표
| 항목 | 값 |
|---|---|
| 저장소 | `/Users/sitpo/math-game-factory` (GitHub: veryhungryface/math-game-factory, main 브랜치) |
| 배포 | Vercel 단일 프로젝트 → https://math-game-factory.vercel.app (push 시 자동 배포, 게임은 `/g/<slug>/`) |
| 크론 | Hermes cron job ID **`7c43f44b8c54`** ("초등 수학 게임 공장"). `hermes cron pause|resume 7c43f44b8c54`. 크론은 `~/.hermes/scripts/math_game_factory.sh`(미러: `factory/cron-entry.sh`)를 호출 — 600초 제한 우회를 위해 nohup 백그라운드 발사 후 즉시 종료하는 래퍼다 |
| 보고 채널 | Discord 스레드 **`1539073913777291344`** ("수학게임공장"). `hermes send discord:1539073913777291344 "메시지"` — 채널 본문은 403, 반드시 스레드 ID로 |
| 교육과정 | `curriculum/2022-elementary-math.json` (45 성취기준, 24 단원, generation_constraints 18조) |
| 상태 | `factory/state/queue.json` (produced/failed), `factory/state/rejected/` (폐기작 보관), `factory/state/salvage/` (수리용 스냅샷), `factory/state/feedback-archive/` |
| 로그 | `logs/<RUN_ID>/` (단계별), `logs/cron-launch-*.log`, `logs/latest-report.md` |

## 3. 에이전트 CLI 규약 (구독 CLI만, API 키 금지 — ANTHROPIC_API_KEY unset)
오케스트레이터(이 문서를 읽는 너)는 **계획·지시·검증만** 하고 실작업은 서브에이전트에 맡긴다. 사용자 방침: 클로드만 쓰지 말고 codex·grok을 적극 활용.

| CLI | 호출 | 용도 | 특성 |
|---|---|---|---|
| Claude | `claude -p "..." --model opus\|sonnet --dangerously-skip-permissions` | 빌드·검수(리뷰) | 세션 한도 있음(한도 초과 시 리셋 시각 표시됨) |
| Codex (GPT-5.6) | `codex exec --model gpt-5.6-sol --sandbox workspace-write --cd <dir> "..."` | 수학 검산, 디자인 스펙/재설계, 규범 감사, 이미지 생성(공장 art 단계) | 티어: sol(상)/terra(중)/luna(하). 샌드박스는 네트워크 차단 — 오프라인 분석·파일 작업만. puppeteer 불가 |
| Grok | `grok -p "..." --always-approve` | 코드 수정, puppeteer 실측(QA·무뇌 봇·스크린샷), git 커밋·푸시 | 로컬 비샌드박스라 브라우저 가능. 장시간 작업은 "진행 로그 파일에 append하며 끝까지" 지시 필수(중도 이탈 이력) |

**백그라운드 실행 패턴**: `nohup <cli> ... > /tmp/로그 2>&1 &` 후 PID를 잡고, 모니터로 `while kill -0 <PID>; do sleep 60; done` 감시. pgrep에 한글·괄호 패턴은 정규식 함정이 있으니 **PID 기준**으로 감시해라.

## 4. 생산 파이프라인 (`factory/run.sh`)
단계: 슬롯 선택(pick-slot) → 기획 3안 병렬(claude/codex/grok) → 심사(codex sol) → 아트(codex 이미지: bg/hero/thumb 1200×630/square 1080×1080/title 세로) → 빌드(claude opus) → QA(`factory/lib/qa.mjs` 40항목, puppeteer 풀크롬) → 수학 검산(codex sol, `35-mathcheck.md`) → 검수(claude opus, `40-review.md`, **80점 게이트**) → 미달 시 수정 1회 후 재검수 → 게시(커밋·푸시·허브 재빌드) → 디스코드 보고 → (10작마다) 레퍼런스 스카우트.
- 프롬프트: `factory/prompts/10-design.md, 30-build.md, 35-mathcheck.md, 40-review.md, 50-reference-scout.md`
- 설정: `factory/config.sh` (모델·타임아웃·GATE_SCORE=80·PRIORITY_UNITS·REPORT_TARGET)
- 중간 재개: `RESUME_FROM=design|art|build|qa|review bash factory/run.sh`
- 슬롯 선택: 단원별 게시 수가 적은 단원 우선 + `PRIORITY_UNITS` 타이브레이크. 전 단원 1작 이상이면 2회전.
- 수동 1회 실행: `nohup bash factory/run.sh > /tmp/mgf-run.log 2>&1 &`

## 5. 게이트 원칙 (절대 우회 금지)
- 80점 미만·수학 오류 1건·실수 관용도 결함·퇴화 전략 성공 → 게시 불가.
- 검수관 must_fix는 게시 후에도 **즉시 후속 수정**으로 소화한다 (아래 §7 패턴).
- 폐기작이라도 검수관이 "원인이 국소적"이라 판단하면 부활 트랙(§8) 가치가 있다.

## 6. 검증 규약 (오케스트레이터의 의무)
- 에이전트가 "고쳤다"고 해도 **스크린샷을 Read로 직접 봐라**. 거짓 verified 사례가 실제로 있었다(첨벙 태블릿).
- 수학 수정은 예시 1개 이상 손검산으로 재확인.
- 퇴화 전략 의심 시 무뇌 봇 실측(성공률 수치)을 요구해라.
- 라이브 반영은 `curl`로 확인 (Vercel 전파 수십 초 — until-loop 모니터 사용).

## 7. 게시 후 수정(핫픽스) 표준 패턴
1. `/tmp/mgf-fix-<slug>.md` 지시서 작성: 대상 파일, 사용자 지적 원문, 원인 진단, 수정 방법, **검증 항목(수치 기준)**, "진행 로그 append", "git pull --rebase --autostash 선행", 커밋 메시지.
2. grok 백그라운드 발진 + PID 모니터.
3. 완료 시 스크린샷 검증 → 미흡하면 재지시.
4. 같은 파일에 두 에이전트 동시 투입 금지 — 모니터로 연쇄 발진(앞 작업 종료 시 자동 발진).
5. git: 에이전트는 자기 게임 폴더만 add. 파이프라인과 동시 실행 시 rebase 재시도 지시.

## 8. 폐기작 부활(salvage) 절차 — 실적: 쩍쩍 75→87, 첨벙 79→86
1. 폐기작은 `factory/state/rejected/<RUN>-<slug>/`에 자동 보관됨. work 스냅샷을 `factory/state/salvage/<slug>-<날짜>/`로 복사(다음 사이클이 work를 리셋하기 전에).
2. grok이 rejected 폴더에서 직접 수리 (public/g 반입 금지, 커밋 금지).
3. codex sol 독립 수학 재검산 (전수).
4. 통과 시 `cp -r`로 `public/g/<slug>` 승격 → `node factory/lib/qa.mjs <slug>` 40항목.
5. 재검수 게이트 (claude opus 또는 codex sol, `40-review.md` 기준 그대로) — passed 시에만:
6. 게시: queue.json 갱신(failed에서 제거, produced에 추가) → `node factory/lib/build-index.mjs` → 커밋·푸시 → 디스코드 보고. 미달 시 public에서 제거.

## 9. 신설 규범 (2026-08-20 실사용 피드백에서 승격 — 프롬프트에 이미 반영됨)
`30-build.md`·`40-review.md` 후반부에 있다. 요지:
1. **물리 은유 정합성**: 은유의 숨은 가정(등질량 등)을 화면 문구로 명시, 등확률 가정과 충돌 금지.
2. **연출 감정 어휘 분리**: screen shake는 오답 전용. 정답 = 상승·확산·빛 + 카드/오브젝트는 빛나며 소멸(손패 복귀 금지).
3. **실시간 계산 부하**: 압박 아래 무거운 계산 금지. 계산형은 친숙수(r∈{1,2,5,10})·압박 정지·풀이 타일.
4. **퇴화 전략 게이트**: 무뇌 입력(홀드·연타·같은 버튼)으로 이기면 불합격. 검수 때 무뇌 봇 실측.
5. **확인 버튼 장식화 금지**: 판단 없이 눌러도 되는 버튼이 채점 경로에 있으면 결함.
6. **교착 방지**: 진행 불가 상태는 자비 스폰(pity)+구조 장치(출고/교체)+감지 알림 3겹.
7. **표기**: 5학년 가능성은 분수 금지(밴드+0·½·1), 근사를 등식으로 단정 금지, 답에 단위, 조사(이/가·은/는·을/를)는 받침 판별 헬퍼, 화면에 없는 수 인용 금지.
8. **타이틀**: `docs/title-screen-spec.md` (부록 A: 음절 수 비례 로고, 44px 터치 타깃 / 부록 B: 글자면 색은 --logo-side, h1 명시도 함정).

## 10. 레퍼런스 시스템
- 풀: `references/game-references.json` (+ 문서 `game-references.md`는 `node factory/lib/build-references-doc.mjs`로 재생성)
- 스카우트: 10작마다 자동 → `references/pending/<날짜>.json` → **사람(사용자) 검토 후** `node factory/lib/merge-references.mjs approve <파일>` 로 병합.
- 사례 연구: `references/case-studies/` (vibe 22게임, 킹수학 400게임 분석 13건)

## 11. 사용자(교사) 프로필과 소통
- 실사용(학생 수업) 피드백을 스크린샷과 함께 준다. 지적은 거의 항상 정확하다 — 먼저 코드로 원인을 확정하고, 고치기 전에 진단을 보고해라.
- 원칙: "품질이 형편없으면 안 만드니만 못하다", "수학이 동사", 게임은 하고 싶은 게임이어야, 디자인은 닌텐도급, 매 게임 2022 교육과정 성취기준 매핑.
- 폐기·실패도 숨기지 말고 보고 (게이트가 일한 것).
- 이미지: GPT(codex) 생성, 병렬 세션 이미지 경로 혼선 주의("도구가 반환한 경로만 사용").

## 12. 자주 쓰는 명령 모음
```bash
# 크론
hermes cron pause|resume 7c43f44b8c54
# QA 단일 게임
node factory/lib/qa.mjs <slug>
# 허브 재빌드 (meta.json → public/catalog.json, index)
node factory/lib/build-index.mjs
# 다음 슬롯 미리보기
node factory/lib/pick-slot.mjs | jq '.unit'
# 사이클 수동 재개
RESUME_FROM=qa bash factory/run.sh
# 디스코드 보고
hermes send discord:1539073913777291344 "..."
```

## 13. 함정 목록 (실제로 밟은 것)
- bash 3.2: `timeout` 없음, 스크립트 스냅샷 실행(MGF_ROOT 전달 필수), `${VAR}` 뒤 비ASCII 주의.
- pgrep 한글/괄호 패턴 → 정규식 오작동. PID로 감시.
- Claude 세션 한도가 새벽에 걸리면 빌드 단계 사망 → 크론이 다음 사이클에서 자동 재시도하므로 대개 개입 불요.
- Vercel 캐시/열린 탭: 사용자가 옛 버전을 보고 있을 수 있음 — 배포본 확인 후 새로고침 안내.
- 검수 재검수 900초 타임아웃 사례 1건 — 재발 시 run.sh의 리뷰 타임아웃 상향.
- 모니터 이벤트가 latest-report.md 갱신 전에 발화해 옛 보고를 보여줄 수 있음 — queue.json 개수로 교차 확인.
- **파이프라인 중지 시**: `pkill -f "factory/run.sh"` 만으로는 부족하다 — run.sh는 자신을 `/tmp/mgf-run-$$.sh` 스냅샷으로 복사해 실행하므로 **`pkill -f "mgf-run-"` 를 함께** 실행해야 한다. 스냅샷 좀비가 옛 코드로 완주하며 큐를 오염시킨 실제 사고(등불을 켜 1차)가 있었다.
