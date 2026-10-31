# Art source

- `generated/hero.png`, `generated/fabric.png`: 아트 단계 생성 원본. 프롬프트는 `factory/work/art.json`·`chosen.json` 기록.
- `Resources/PoksinhanKkokdaegi/gecko.png`: hero.png 흰 배경을 지워 640px로 줄인 런타임 스프라이트(위 절반 서기, 아래 절반 배 깔기).
- `Resources/PoksinhanKkokdaegi/quilt.png`: fabric.png에서 누비 한 칸만 잘라 256px 타일로 만든 것. UV 1 = 눈금 1칸이라 방석 위 칸 수가 곧 넓이다.
- `thumb.png`·`square.png`는 최종 WebGL 타이틀을 `validation/capture-runtime-covers.mjs`로 촬영한다.

검증: `validation/screen-eval-playthrough.mjs`(OCR로 풀이하는 3판), `validation/pointer-bot-audit.mjs`(실제 pointer 무뇌 봇), `validation/look-capture.mjs`(장면 캡처).
