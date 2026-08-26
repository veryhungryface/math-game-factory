🛠 **정합성 복구** — 게시했는데 허브에 안 뜨던 게임 5작 복구

**무슨 일이었나**
`factory/run.sh` 의 "10. 게시 준비" 단계가 `meta.qa.passed=true` 갱신·`queue.json` 기록·허브
재빌드를 인라인으로 처리했는데, **폐기 후 수리해서 부활시키는 경로는 사람이 `cp -r` 로 올려서
이 단계를 통째로 건너뛰었다.** 그래서 `qa.passed` 가 `false` 로 남았고, 허브 빌더
(`factory/lib/build-index.mjs`)의 필터에 걸려 **게시 커밋까지 됐는데 카탈로그·허브에서
사라진 게임이 5작 누적**됐다.

**복구된 게임 5작** (전부 자동 QA 40/40 재통과 확인 후 게시)
  - 쩍쩍 `ice-snap` 87점 — 8/20부터 6일간 안 보였음
  - 첨벙 `splash-flat` 86점 — 8/20부터 6일간 안 보였음
  - 유리를 불어 `glass-puff` 81점
  - 등불을 켜 `lantern-disk` 92점
  - 칸자물쇠 `cell-latch` 88점

**카탈로그**: 13작 → **18작** (`node factory/lib/verify-catalog.mjs` → 정합성 이상 없음)

**근본 수정**
  - `factory/lib/publish-game.mjs` — 게시 단일 진입점(멱등). 게이트는 어떤 플래그로도 80점
    아래로 못 내린다. 최초 게시와 부활이 같은 경로를 쓴다.
  - `factory/lib/verify-catalog.mjs` — 게시 직후 정합성 감사. 누락·고아·필수파일 결손을
    그 자리에서 잡고 exit 1.
  - `run.sh` 10단계가 위 둘을 호출하도록 교체. 부활 절차(`docs/OPERATIONS.md` §8)도 동일.

**곁다리로 정리한 것**
  - `queue.json` 의 `mechanic_history` 13개 → 18개 복구 (메커닉 중복 회피가 5작치 무력화돼 있었음)
  - `public/g/cube-sortie` (게임 본체 없이 아트만 있던 잔해) → `factory/state/salvage/cube-sortie-art/` 로 이동
  - `public/g/` 안의 이상한 이름 빈 디렉터리 2개 삭제 (인용부호 없는 변수 확장 사고 흔적)
  - `queue.json` 의 네모공장 단원 빈값 → `g5s2-u5` 로 채움

이 보고서는 다음 생산 회차가 끝나면 그 회차 보고서로 덮인다.
