# AGENTS.md — 에이전트 빠른 참고

**하루 1작이 목표**인 게임 공장. 크론이 2시간마다 `factory/run.sh`를 호출해 자동 기획→제작→QA→게시하고, 오늘(KST) 이미 게시한 게임이 있으면 `run.sh` 최상단 가드가 신규 생산을 건너뛴다(= 하루 최대 12회 시도, 첫 성공에서 종료. 해제는 `DAILY_TARGET=0`/`FORCE_PRODUCE=1`). 테스트 프레임워크·lint 설정은 없다 — 검증은 QA 하네스와 게이트(80점)가 대신한다.

## 먼저 읽을 문서 (이 순서)

1. `CLAUDE.md` — 게임 폴더 계약·meta.json 스키마·`window.__GAME_TEST__` 훅·한국 교과서 표현 함정. **모든 하위 에이전트가 지켜야 하는 계약**
2. `docs/OPERATIONS.md` — 크론·에이전트 CLI 규약·폴백·핫픽스/부활 절차·실제 사고 기반 함정 목록
3. `docs/loop-engineering.md` — 5겹 피드백 루프. **게이트를 고치거나 자동화를 추가하기 전 필독**
4. `factory/state/HANDOVER.md` — 현재 상태·백로그. **작업 상태가 바뀌면 갱신하고 커밋**

## 명령어 (package.json + OPERATIONS §12 검증)

```bash
node factory/lib/qa.mjs <slug>        # 단일 게임 QA (검증의 기본 단위)
DEPLOY=0 REPORT=0 npm run produce     # 1회 사이클 드라이런 (배포·디스코드 보고 생략)
RESUME_FROM=qa bash factory/run.sh    # 중간 단계 재개: design|art|build|qa|review (factory/work 산출물 재사용)
npm run slot                          # 다음 생산 슬롯 미리보기 (pick-slot.mjs)
npm run build && npm run serve        # 허브 재빌드 + 로컬 미리보기
bash factory/preflight.sh             # run.sh 를 수정했다면 반드시 실행 — 스냅샷 재실행 버그는 bash -n 으로 안 잡힘
```

게시는 스크립트가 하므로 수동 게시할 일은 부활(salvage) 때뿐이다:
```bash
node factory/lib/publish-game.mjs <slug> --score <점수> --notes-from <review.json>
node factory/lib/verify-catalog.mjs   # 허브 실노출 확인 — 이걸 통과해야 게시 성공
```

## 하드 규칙 (위반 시 실제 사고 발생)

- **`public/index.html`·`public/catalog.json` 직접 수정 금지** — `factory/lib/build-index.mjs` 빌드 산출물이다.
- **게시 단일 진입점은 `publish-game.mjs`.** `cp -r`로 `public/g/`에 복사만 하면 `meta.json`의 `qa.passed=false`가 남아 `build-index.mjs` 필터에 걸려 허브에서 사라진다(5작 누락 사고). 어떤 `--gate` 값으로도 80점 아래 게시 불가(코드 상수).
- **`curriculum/` 수정 금지** (사람만 갱신). `curriculum/sources/`는 저작권 원문이자 gitignore — **절대 커밋 금지.** 커밋할 수 있는 사실만 `curriculum/*.json`으로 재서술한다.
- 게임 폴더는 `public/g/<slug>/` 안에서 자기완결: 외부 CDN 0, 상대경로만, `index.html`+`meta.json`+`thumb.png`(1200×630)+`square.png`(1080×1080) 필수. `meta.json.standards` 코드는 `curriculum/2022-elementary-math.json`에 실재해야 QA가 대조한다.
- three.js는 `public/vendor/` import map 방식(`three` bare specifier + `three.module.js`/`three.core.js` 쌍 필요). 애드온은 OrbitControls·GLTFLoader만 존재 — 나머지는 직접 구현. vendor 폴더는 건드리지 않는다.
- 수학 정답은 100% 정확: 분수는 분자/분모 정수 연산, 부동소수점 비교 금지. 어림 문항은 `TO_PLACE` convention만 사용 — 「○의 자리에서」는 어림이 아니라 받아올림(carry)으로 읽힌다. 상세는 CLAUDE.md의 `promptConvention` 절.

## 에이전트 오케스트레이션 규약

- LLM 호출은 구독 CLI만: `claude -p` / `codex exec` / `grok -p`. **`ANTHROPIC_API_KEY`는 unset** (`config.sh`가 보장). 단계별 러너·모델·타임아웃은 `factory/config.sh`가 관리.
- 러너 폴백: `resolve_runner`가 없는 바이너리는 codex→grok→claude 순으로 대체하되 **모델 인자를 버린다**(타사 모델명 전달 400 사고 재발 방지). 단 바이너리 존재만 보므로 grok 402 같은 API 사망은 빌드 단계의 결과 기반 폴백만 잡는다.
- 병렬 에이전트는 **서로 다른 파일만** 건드린다. 같은 파일 두 에이전트 금지.
- macOS에 `timeout`이 없다 — `gtimeout` 또는 `run.sh`의 `run_timeout`(백그라운드+폴링) 패턴 사용.
- 프로세스 정리: `pkill -f "factory/run.sh"`만으로 부족 — run.sh는 `/tmp/mgf-run-$$.sh` 스냅샷으로 재실행되므로 **`pkill -f "mgf-run-"`를 함께** 해야 한다(스냅샷 좀비가 옛 코드로 완주하며 큐 오염 사고 있음). 고아 크롬 정리는 **PID 지정 kill만** — pkill하면 형제 puppeteer를 죽인다.

## 상태 파일

- `factory/state/queue.json` — produced/failed 장부. 실패도 반드시 기록(빌드 실패 누락 사고 있었음).
- `factory/state/feedback.md` — 사용자 피드백 인박스. 다음 사이클이 읽고 자동 아카이브한다. 여기 적으면 코드 수정 없이 파이프라인에 반영된다.
- 디스코드 보고는 **스레드 ID로만** 가능(채널 본문은 403): `hermes send discord:<thread-id> "..."` (ID는 OPERATIONS §2).
- 빌드 산출물과 별개로, 새 게임/수정 커밋은 자동으로 main push → Vercel 배포된다. 수동 배포는 `vercel deploy --prod`.
