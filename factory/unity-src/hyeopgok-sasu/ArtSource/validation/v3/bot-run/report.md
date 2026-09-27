# 협곡 사수 v3 — 4지선다 무뇌 봇

- 13팩(현재 index 기준) × 봇 5종 × 각 200게임 × 10문항을 `CommandChoicePad → Tick` 경로로 측정한다.
- 네 입력 봇은 동일 시드의 보기 배열 4회전 층화로 각 물리 패드의 정답 위치를 한 번씩 만나므로 정확히 25%, idle은 0%가 기대값이다.
- 보상 난수는 문항 순서/보기 배치/정오 난수와 분리하며, 오답은 보상 draw 0, 빈 부지의 3연속 정답은 새 타워를 보장해야 한다.

| 팩 | 봇 | 정답/첫 시도 | 관측률 | 시간초과 | 층화 | 실제 경로 | ≤25% |
|---|---|---:|---:|---:|---|---|---|
| m2s1-u1 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u1 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u1 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u1 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s1-u1 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u2 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u2 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u2 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u2 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s1-u2 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u3 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u3 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u3 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u3 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s1-u3 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u4 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u4 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u4 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u4 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s1-u4 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u5 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u5 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u5 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u5 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s1-u5 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u6 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u6 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u6 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s1-u6 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s1-u6 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u1 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u1 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u1 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u1 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s2-u1 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u2 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u2 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u2 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u2 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s2-u2 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u3 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u3 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u3 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u3 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s2-u3 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u4 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u4 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u4 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u4 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s2-u4 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u5 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u5 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u5 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u5 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s2-u5 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u6 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u6 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u6 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u6 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s2-u6 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u7 | repeat/fixed | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u7 | cycle | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u7 | random | 500/2000 | 25.000% | 0 | pass | pass | pass |
| m2s2-u7 | idle | 0/2000 | 0.000% | 2000 | pass | pass | pass |
| m2s2-u7 | nearest | 500/2000 | 25.000% | 0 | pass | pass | pass |

## 봇별 전체

| 봇 | 팩 | 정답/첫 시도 | 관측률 | ≤25% | 경로 |
|---|---:|---:|---:|---|---|
| repeat/fixed | 13 | 6500/26000 | 25.000% | pass | pass |
| cycle | 13 | 6500/26000 | 25.000% | pass | pass |
| random | 13 | 6500/26000 | 25.000% | pass | pass |
| idle | 13 | 0/26000 | 0.000% | pass | pass |
| nearest | 13 | 6500/26000 | 25.000% | pass | pass |

## 보상 RNG

- 문항/보기/정오 직교성: **pass** (200문항, quiet 83 draws / noisy 1367 draws)
- 오답 draw 0: **pass** (64건, 0→0)
- 3연속 정답 타워 보장: **pass** (256/256)

## 판정

- 각 팩·봇 ≤25%: **pass**, 전체: **pass**, 최종: **pass**.
