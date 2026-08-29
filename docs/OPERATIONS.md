# 수학 게임 공장 — 운영 매뉴얼 (세션·모델 무관 인수인계 문서)

> 이 문서는 어떤 AI 세션/모델이 와도 공장을 이어 운영할 수 있게 하는 **불변 운영 규약**이다.
>
> **필독 (이 순서로)**
> 1. 이 문서 — 운영 규약(크론·에이전트 CLI·게이트·핫픽스/부활 절차·함정 목록)
> 2. `docs/loop-engineering.md` — **왜 이 구조가 품질을 만드는가.** 공장의 5겹 피드백 루프(L0~L4),
>    횡단 원칙, 모델 티어링, 서브에이전트 오케스트레이션, 루프 설계 체크리스트.
>    **게이트를 손대거나 새 자동화를 붙이기 전에 반드시 읽어라.**
> 3. `factory/state/HANDOVER.md` — 현재 상태·진행 중 작업·백로그 (살아있는 문서)
>
> 게임 제작 규칙은 저장소 루트 `CLAUDE.md`, 디자인 철학은 `docs/design-bible.html`, 타이틀 규격은 `docs/title-screen-spec.md`.

## 1. 시스템 한 줄 요약
3시간마다 크론이 `factory/run.sh`를 돌려 2022 개정 교육과정(초등 5-6학년 2학기 중심) 단원별 수학 게임을 자동 기획→제작→검증→게시하고, 사람(교사)의 실사용 피드백을 받아 즉시 수정하는 공장.

## 2. 인프라 좌표
| 항목 | 값 |
|---|---|
| 저장소 | `/Users/sitpo/math-game-factory` (GitHub: veryhungryface/math-game-factory, main 브랜치) |
| 배포 | Vercel 단일 프로젝트 → https://math-game-factory.vercel.app (push 시 자동 배포, 게임은 `/g/<slug>/`) |
| 크론 | Hermes cron job ID **`7c43f44b8c54`** ("초등 수학 게임 공장 (3시간마다 1개)", `every 180m`). `hermes cron pause|resume 7c43f44b8c54`. 크론은 `~/.hermes/scripts/math_game_factory.sh`(미러: `factory/cron-entry.sh`)를 호출 — 600초 제한 우회를 위해 nohup 백그라운드 발사 후 즉시 종료하는 래퍼다 |
| 보고 채널 | Discord 스레드 **`1539073913777291344`** ("수학게임공장"). `hermes send discord:1539073913777291344 "메시지"` — 채널 본문은 403, 반드시 스레드 ID로 |
| 교육과정 | `curriculum/2022-elementary-math.json` (45 성취기준, 24 단원, generation_constraints 18조) |
| 상태 | `factory/state/queue.json` (produced/failed), `factory/state/rejected/` (폐기작 보관), `factory/state/salvage/` (수리용 스냅샷), `factory/state/feedback-archive/` |
| 로그 | `logs/<RUN_ID>/` (단계별), `logs/cron-launch-*.log`, `logs/latest-report.md` |

## 3. 에이전트 CLI 규약 (구독 CLI만, API 키 금지 — ANTHROPIC_API_KEY unset)
오케스트레이터(이 문서를 읽는 너)는 **계획·지시·검증만** 하고 실작업은 서브에이전트에 맡긴다. 2026-08-26 사용자 방침에 따라 **클로드는 폴백 전용**이며, 기본 실작업은 codex·grok이 맡는다. 단계별 기본 러너는 `factory/config.sh`에서 관리한다.

| CLI | 호출 | 용도 | 특성 |
|---|---|---|---|
| Claude | `claude -p "..." --model opus\|sonnet --dangerously-skip-permissions` | codex·grok을 쓸 수 없을 때의 폴백 | 세션 한도 있음(한도 초과 시 리셋 시각 표시됨) |
| Codex (GPT-5.6) | `codex exec --model gpt-5.6-sol --sandbox workspace-write --cd <dir> "..."` | 수학 검산, 디자인 스펙/재설계, 규범 감사, 이미지 생성(공장 art 단계) | 티어: sol(상)/terra(중)/luna(하). 샌드박스는 네트워크 차단 — 오프라인 분석·파일 작업만. puppeteer 불가 |
| Grok | `grok -p "..." --always-approve` | 코드 수정, puppeteer 실측(QA·무뇌 봇·스크린샷), git 커밋·푸시 | 로컬 비샌드박스라 브라우저 가능. 장시간 작업은 "진행 로그 파일에 append하며 끝까지" 지시 필수(중도 이탈 이력) |

단계별 기본값은 `BUILD_RUNNER=grok_run`, `REVIEW_RUNNER=codex_run`(모델 `$CODEX_MODEL_SMART`), `FIX_RUNNER=grok_run`, `SCOUT_RUNNER=grok_run`, `DESIGN_RUNNERS=grok_run,codex_smart_run,codex_run`이다. `codex_smart_run`은 codex를 sol 티어로 실행한다. `resolve_runner`는 지정 러너를 쓸 수 없으면 **codex → grok → claude** 순으로 폴백하고, `stage_run`은 폴백 시 원래 모델 인자를 버리고 대체 러너의 기본 모델을 쓴다. 2026-08-25 다른 회사 모델 이름을 그대로 넘겨 400을 맞은 사고를 구조적으로 막는 규칙이다. 단 `resolve_runner`는 바이너리 존재만 보므로, **빌드 단계는 실행 결과까지 검사해**(비정상 종료·402류 출력·index.html 미생성) codex(`$CODEX_MODEL_SMART`)로 1회 폴백 재시도하고, codex도 실패하면 queue.json failed에 기입 후 실패 보고한다 — 2026-08-27~28 grok 402 잔액 소진 4연속 사망 후 도입.

**백그라운드 실행 패턴**: `nohup <cli> ... > /tmp/로그 2>&1 &` 후 PID를 잡고, 모니터로 `while kill -0 <PID>; do sleep 60; done` 감시. pgrep에 한글·괄호 패턴은 정규식 함정이 있으니 **PID 기준**으로 감시해라.

## 4. 생산 파이프라인 (`factory/run.sh`)
단계: 슬롯 선택(pick-slot) → 기획 3안 병렬(grok/codex sol/codex) → 심사(codex sol) → 아트(codex 이미지: bg/hero/thumb 1200×630/square 1080×1080/title 세로) → 빌드(grok) → QA(`factory/lib/qa.mjs` 40항목, puppeteer 풀크롬) → 수학 검산(codex sol, `35-mathcheck.md`) → 검수(codex sol, `40-review.md`, **80점 게이트**) → 미달 시 grok 수정 1회 후 재검수 → `publish-game.mjs` 게시 → `verify-catalog.mjs` 정합성 확인 → 커밋·푸시 → 디스코드 보고 → (10작마다) 레퍼런스 스카우트. 각 단계 러너는 `factory/config.sh` 값과 §3 폴백 규칙을 따른다.
- 프롬프트: `factory/prompts/10-design.md, 30-build.md, 35-mathcheck.md, 40-review.md, 50-reference-scout.md`
- 설정: `factory/config.sh` (모델·타임아웃·GATE_SCORE=80·PRIORITY_UNITS·REPORT_TARGET)
- 검수 제한 시간: `T_REVIEW=1200`초. 900초에서 실제 타임아웃 1회 후 2026-08-26 상향했다.
- 중간 재개: `RESUME_FROM=design|art|build|qa|review bash factory/run.sh`
- 슬롯 선택: 단원별 게시 수가 적은 단원 우선 + `PRIORITY_UNITS` 타이브레이크. 전 단원 1작 이상이면 2회전.
- 수동 1회 실행: `nohup bash factory/run.sh > /tmp/mgf-run.log 2>&1 &`

## 5. 게이트 원칙 (절대 우회 금지)
- 80점 미만·수학 오류 1건·실수 관용도 결함·퇴화 전략 성공 → 게시 불가.
- 게시 단일 진입점은 `factory/lib/publish-game.mjs`다. 어떤 `--gate` 값으로도 80점 아래로 게이트를 내릴 수 없다.
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
4. 승격 후 아래 순서를 그대로 지킨다.
   ```bash
   cp -r factory/state/rejected/<run>-<slug> public/g/<slug>
   node factory/lib/qa.mjs <slug>            # 치명적 결함 0건 확인
   # 재검수 게이트 통과 후에만:
   node factory/lib/publish-game.mjs <slug> --score <점수> --notes-from <review.json>
   node factory/lib/verify-catalog.mjs       # 허브에 실제로 떴는지 확인
   ```
5. 재검수 게이트(codex sol, `40-review.md` 기준 그대로) 미달 시 public에서 제거한다. 통과했을 때만 위 `publish-game.mjs`를 호출하고 커밋·푸시·디스코드 보고로 이어간다.

**부활도 반드시 `publish-game.mjs`를 거쳐라 — 손으로 `cp`만 하면 허브에서 사라진다.** 2026-08-26 감사에서 이 우회 때문에 `meta.json`의 `qa.passed=false`가 남아 쩍쩍·첨벙·유리를 불어·등불을 켜·칸자물쇠 5작이 게시 커밋 뒤에도 허브·카탈로그에서 누락된 사실이 확인됐다.

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
# 게시(meta.qa + queue + 허브를 멱등 갱신)와 정합성 확인
node factory/lib/publish-game.mjs <slug> --score <점수> --notes-from <review.json>
node factory/lib/verify-catalog.mjs
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
- 검수 재검수 900초 타임아웃 사례 1건 — 2026-08-26 `T_REVIEW=1200`초로 상향했다.
- 모니터 이벤트가 latest-report.md 갱신 전에 발화해 옛 보고를 보여줄 수 있음 — queue.json 개수로 교차 확인.
- **파이프라인 중지 시**: `pkill -f "factory/run.sh"` 만으로는 부족하다 — run.sh는 자신을 `/tmp/mgf-run-$$.sh` 스냅샷으로 복사해 실행하므로 **`pkill -f "mgf-run-"` 를 함께** 실행해야 한다. 스냅샷 좀비가 옛 코드로 완주하며 큐를 오염시킨 실제 사고(등불을 켜 1차)가 있었다.
- **게시 경로 우회 금지**: `public/g/`에 복사만 하면 `qa.passed=false`가 남아 `build-index.mjs` 필터에 걸리고, 게시 커밋까지 있어도 허브에서 사라진다. 반드시 `publish-game.mjs`로 게시하고 `verify-catalog.mjs`로 실제 노출을 확인해라.
- `public/g/`에서 인용부호 없는 변수 확장으로 슬러그 목록 전체가 폴더명이 된 빈 디렉터리 2개가 생긴 적이 있다. 경로 변수는 항상 인용하고 대상 경로를 확인해라.

## 14. 루프 엔지니어링 원칙 (요약 — 전문은 `docs/loop-engineering.md`)
이 공장의 강점은 단선 파이프라인이 아니라 **5겹 피드백 루프**다 — L0 생산(사이클) / L1 사이클 내 자기수정(수리 1회·러너 폴백·무뇌봇 자가테스트) / L2 사이클 간 학습(폐기 패턴 → 프롬프트 게이트 승격) / L3 캠페인(사용자 지적 → 전수 감사 → 일괄 수리 → **재발 방지 게이트 신설**) / L4 부활(게시작 24편 중 9편이 부활 산물).
1. **신뢰하지 말고 실측하라** — 스크린샷은 Read 로 직접 보고, 퇴화 전략은 봇 수치로, 수학은 전수 검산으로 확인한다(§6).
2. **판정자와 생산자를 분리하라** — 빌드 grok / 검수·수학검산 codex sol. 같은 모델은 같은 맹점을 공유한다.
3. **게이트는 불변식이다** — 점수 유도 금지, 게시 진입점은 `publish-game.mjs` 하나, 80점 하한은 코드 상수(§5).
4. **모든 사고는 규범이 된다** — HANDOVER 기록 → 일반화되면 OPERATIONS §13 / CLAUDE.md / 프롬프트로 승격(§9).
5. **게이트는 획일화를 낳는다** — 하한을 정의하면 생산자는 그 형태에 최적화한다. 재발 방지책이 다음 획일화의 원인이 된 사례가 3회 있다(640px 컬럼 표준, 고정 스타일 문구, 타이틀 CSS 레시피). 새 게이트에는 **다양성 게이트를 짝지어라.**

새 자동화·새 게이트를 붙이기 전에 `docs/loop-engineering.md` §10 「루프 설계 체크리스트」 19문항을 통과시켜라.
