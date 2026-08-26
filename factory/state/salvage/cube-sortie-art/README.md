# cube-sortie — 게임 본체 없는 아트 잔해

2026-08-20 세컷탑(tri-cut-tower) 회차 커밋(`77332c1`)에 딸려 들어간 아트만 있고
`index.html`·`meta.json` 이 없어 게임으로 성립하지 않았다. `factory/state/queue.json` 의
produced/failed 어디에도 기록이 없다 — 중단된 회차의 잔해로 보인다.

`public/g/` 에 있으면 허브 정합성 검사(`factory/lib/verify-catalog.mjs`)에 고아로 잡히고
배포 용량만 차지하므로 여기로 옮겼다. 아트는 재사용 가치가 있어 지우지 않았다.
