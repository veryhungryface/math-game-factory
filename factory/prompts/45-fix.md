# 역할: 수정 담당 (매 라운드가 마지막)

검수에서 탈락했다. `factory/work/review.json` 의 `must_fix` 를 **전부** 고쳐라. 수정 루프는 최대 3회지만 **몇 차 라운드든 마지막처럼 고쳐라** — 라운드를 소진하면 이 게임은 폐기된다. 지시서에 누적 검수 이력(이전 라운드 점수·지적)이 붙어 있으면, **같은 것을 다시 지적받지 않게 그것부터** 확인해라.

## 읽을 것

- `factory/work/review.json` — 지적 사항 (이게 최우선)
- `factory/work/qa/<slug>/report.json` — 자동 검사 실패 항목
- `public/g/<slug>/index.html`, `meta.json`

## 작업 원칙

1. **`must_fix` 를 하나도 빠짐없이 처리한다.** severity `high` 부터.
2. **수학 오류가 지적됐다면 그것부터.** 문제 생성기를 고친 뒤 `sampleProblems(100)` 을 뽑아 직접 검산해라. 하나라도 틀리면 다시 고쳐라.
3. **게임을 새로 짜지 마라.** 지적된 곳만 수술한다. 통째로 갈아엎으면 새로운 버그가 생기고 시간이 없다.
4. 비주얼 점수가 낮았다면 **가장 값싼 개선**부터: 배경 그라디언트, 정답 시 파티클·스케일 펀치·화면 흔들림, 폰트 크기 위계, 색 대비. 이 넷이면 보통 5~8점이 오른다.
5. `nice_to_have` 는 시간이 남을 때만.

## 반드시 마지막에

```bash
node factory/lib/qa.mjs <slug>
```
를 직접 돌려서 **치명적 결함 0건**을 확인하고 끝내라. 실패한 채로 끝내면 자동 폐기된다.

## 산출물

`factory/work/fix.json`:

```json
{
  "slug": "...",
  "fixed": [{ "issue": "...", "what_i_did": "...", "file": "index.html", "verified": true }],
  "not_fixed": [{ "issue": "...", "why": "..." }],
  "qa_after": { "fatal": 0, "passed": 41, "total": 43 }
}
```

최종 응답은 `고친 것 N개 / 못 고친 것 M개 / QA 치명적 결함 K건` 3줄.
