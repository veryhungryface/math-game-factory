# 한 끗 골프 — 아트·검증 기록

## 런타임 아트
- 화면의 모든 것(파란 펠트 섬·바느질 점선·흰 원형 벙커·가리비 테·노란 레일·고무 손잡이·공·깃발·덤불·두더지 캐디)은
  `Scripts/HanKkeutGolfView.cs` 가 Unity 프리미티브·코드 메시로 만든다. 텍스처·외부 모델·외부 음원 없음(효과음은 킷 합성음).
- 정적 장식은 색(재질)별로 메시 하나에 구워 드로콜을 줄였다(`Decor.Bake`).
- 두더지 캐디는 오리지널 캐릭터(크림 몸통·크림 조끼+노란 테두리·파란 주머니·분홍 코). 모양 참고만
  `ai-keyart/hero-ai.png`(아트 단계 codex 내장 이미지 생성, 투명 배경)에서 했고 런타임에 스프라이트로 쓰지 않는다.
- 글꼴: 킷 NotoSansKR Bold 서브셋(OFL). 서브셋에 없는 그리스 문자(α·θ)는 화면 문구에서 쓰지 않는다(「레일 88°」「OA의 각」).

## 표지
- `public/g/han-kkeut-golf/thumb.png`(1200×630)·`square.png`(1080×1080)는 **완성 런타임 캡처**다:
  `node ArtSource/validation/capture-covers.mjs` (`?cover=1` → 타이틀 UI 크롬과 음소거 버튼만 숨긴다).
- 아트 단계가 만든 AI 키아트(구도 참고용)는 `ai-keyart/thumb-ai.png`·`square-ai.png` 로 옮겨 보존했다
  (codex 내장 이미지 생성 — 프롬프트는 `factory/work/chosen.json` 의 `art_direction.assets_needed`).
- `public/g/han-kkeut-golf/assets/hero.png`·`title.png` 는 아트 단계 산출물로 남아 있지만 런타임은 참조하지 않는다.

## 검증 (`validation/`)
| 스크립트 | 내용 | 결과 파일 |
|---|---|---|
| `typecheck.sh` | 번들 Roslyn 으로 킷+게임 C# 형식 검사 | — |
| `run-headless.sh` → `HeadlessCheck.cs` | 은행 540문항 검산, θ×φ×제출각 전수(통과 5방향·오개념 우연 참 0·레일-원 기하 일치), 덱 독립성, B3 무뇌 봇 6종 200판(+20000판) | `headless-results.json` |
| `pointer-playthrough.mjs [W H]` | 실제 마우스 드래그: 연습 → 8코스(5코스 첫 실수 후 수리) clear · 두 코스 첫 실패 gameover · 공 0 gameover | `pointer-playthrough-*.json`, `pointer-playthrough/*/` |
| `pointer-bots.mjs [runs]` | 실제 pointer 무뇌 봇(연타·순환·무작위 각) + 무입력 95초 | `pointer-bots-results.json` |
| `capture-covers.mjs` | 런타임 표지 캡처 | thumb.png·square.png |
