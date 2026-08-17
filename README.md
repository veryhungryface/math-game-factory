# 수학 놀이터 — 초등 수학 게임 공장

2시간마다 게임 1개를 자동으로 기획·제작·검수·배포하는 파이프라인.
모든 게임은 **2022 개정 교육과정** 초등 수학 단원과 성취기준에 매핑된다.

🎮 **https://math-game-factory.vercel.app** — 게임은 모두 `/g/<slug>/` 하위 경로에 산다 (Vercel 프로젝트 1개).

## 파이프라인

```
① 슬롯 선택      게임이 부족한 단원 + 안 쓴 메커닉을 고른다        pick-slot.mjs
       ↓
② 기획 ×3 (병렬)  서로 다른 각도의 컨셉 3개                        claude -p × 3
       ↓
③ 심사           5개 기준 채점 → 1개 선택 + 나머지 장점 이식        claude -p
       ↓
④ 아트 (병렬)    에셋 1장당 에이전트 1개                          codex exec × N
       ↓
⑤ 구현           자기완결형 index.html + meta.json                claude -p
       ↓
⑥ 자동 QA        35개 기계 검사 (콘솔·404·FPS·모바일·문제 정합성)   puppeteer
       ↓
⑦ 검수           스크린샷 보고 100점 채점, 수학은 손으로 검산        claude -p
       ↓
   80점 미만 → ⑧ 수정 1회 → 재QA → 재검수 → 그래도 미달이면 폐기
       ↓
⑨ 게시           허브 재빌드 → git push → vercel --prod
       ↓
⑩ 보고           에르메스가 디스코드로 리포트 + 썸네일 전송
```

## 구조

| 경로 | 설명 |
|---|---|
| `curriculum/2022-elementary-math.json` | 교육과정 단원·성취기준·오개념·문제유형 원본 |
| `references/game-references.json` | 레퍼런스 게임/메커닉 광산 (아이디어 소스) |
| `factory/run.sh` | 1회 생산 사이클 (cron 진입점) |
| `factory/prompts/` | 단계별 에이전트 프롬프트 |
| `factory/lib/` | 슬롯 선택 · 허브 빌드 · QA 하네스 |
| `factory/state/queue.json` | 생산 이력 (게시/폐기) |
| `public/g/<slug>/` | 게임 1개 = 폴더 1개 |
| `public/vendor/` | three.js 등 공용 라이브러리 (외부 CDN 금지) |

## 사용법

```bash
npm install

# 다음에 만들 슬롯 확인
npm run slot

# 1회 생산 (드라이런 — 배포·보고 없음)
DEPLOY=0 REPORT=0 npm run produce

# 실제 생산
npm run produce

# 특정 학년-학기만
FOCUS=6-2 npm run produce

# 개별 게임 QA
node factory/lib/qa.mjs <slug>

# 허브 재빌드 + 로컬 미리보기
npm run build && npm run serve
```

## 설정

`factory/config.sh` — 커트라인 점수, 병렬 기획안 수, 단계별 제한시간, 배포·보고 스위치.

| 변수 | 기본값 | 설명 |
|---|---|---|
| `GATE_SCORE` | 80 | 게시 커트라인 |
| `DESIGN_VARIANTS` | 3 | 병렬 기획 에이전트 수 |
| `FOCUS` | `5-2,6-2` | 집중 학년-학기 |
| `DEPLOY` / `REPORT` | 1 / 1 | 0이면 생략 |

## 스케줄

```bash
hermes cron list          # 등록된 잡 확인
hermes cron run <id>      # 다음 틱에 즉시 실행
hermes cron pause <id>    # 일시정지
```

## 게임 제작 규칙

`CLAUDE.md` 참조. 요약하면:

- 외부 CDN·원격 리소스 **0건**. three.js는 `public/vendor/`에서 import map으로.
- 경로는 전부 상대경로 (`./assets/x.png`).
- `window.__GAME_TEST__` 훅 필수 — QA가 이걸로 자동 플레이한다.
- 모바일 세로 우선, 30fps 이상, 콘솔 에러 0건.
- **수학이 동사여야 한다.** 게임 위에 문제창을 얹으면 검수에서 떨어진다.
- 메커닉만 차용하고 상표·캐릭터·에셋은 전부 오리지널.
