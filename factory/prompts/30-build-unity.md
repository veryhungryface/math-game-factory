# Unity 트랙 보충 지시 (BUILD_TECH=unity — 본문과 충돌하면 이 절이 이긴다)

이번 게임은 **Unity(6000.3.24f1) 로 만들어 WebGL 로 빌드**한다. 정본 문서는 `docs/unity-track.md` —
**코드를 쓰기 전에 끝까지 읽어라.** CLI 는 `source ~/.unity/env` 후 `unity` (스킬: `~/.agents/skills/unity-cli/SKILL.md`).

## 본문 중 대체되는 것
- 「`public/g/<slug>/index.html` 을 직접 작성」→ **아니다.** `index.html`·`Build/`·`TemplateData/` 는
  빌드 스크립트가 공용 WebGL 템플릿(`factory/unity/kit/`)으로 만든다. 손으로 고치지 마라.
  `meta.json`·`thumb.png`·`square.png` 는 종전대로 `public/g/<slug>/` 에 네가 둔다.
- 캔버스 2D 성능 팁·KaTeX·three.js vendor·`docs/playfield-spec.md` 의 CSS 판형(`--stageW`, `html.land`)
  → 해당 없음. 대신 **카메라·UI 스케일이 390×844 세로와 1280×800 가로 둘 다에서 성립**해야 한다
  (`MgfLook.FitWidth`, `MgfText.Ui` 기준 스케일 — 가로에서 좌우 여백을 게임 세계로 채워라).
- `node factory/lib/qa.mjs <slug>` 와 B3 무뇌 봇 자가 테스트는 **그대로 의무**다.

## 소스 위치와 빌드
- 게임 소스의 정본: **`factory/unity-src/<slug>/`** (저장소 안 — 검산·검수관이 C# 을 읽는다).
  네임스페이스는 `Mgf.<PascalSlug>`. 씬 YAML 을 손으로 쓰지 말고 **코드 부트스트랩**으로 장면을 만든다.
- 빌드: `bash factory/unity/build.sh <slug>` — 공용 워크스페이스(`~/UnityProjects/MGF-Workspace`)에
  소스를 동기화하고 배치 빌드한 뒤 결과를 `public/g/<slug>/` 에 배치한다. 컴파일 에러는
  `error CS…` 줄로 출력된다. **고치고 다시 빌드하는 루프를 스스로 돌려라.**
- 워크스페이스 프로젝트를 Unity 에디터 GUI 로 열어 두지 마라(배치 빌드가 막힌다).

## 브리지 계약 (`__GAME_TEST__`)
- 게임은 킷의 `IMgfGame` 을 구현하고 `MgfBridge` 에 등록한다. JS 쪽 `window.__GAME_TEST__` 는 템플릿이
  제공한다 — 너는 C# 쪽만 채운다: `TestStart`·`TestAnswerCorrect`·`TestAnswerWrong`·`StateJson`·
  `ProblemBankJson`(부팅 시 1회, **고유 문항 300개 이상**, 실제 게임 판정 경로와 같은 생성기에서 뽑는다).
- 실입력 게이트(`input.real`)는 진짜 pointer 이벤트로 본다 — 캔버스 탭/드래그가 Unity 입력으로 들어와
  상태를 바꿔야 한다. 판정 함수를 JS 에서 직접 부르는 우회는 감점이다.

## 수학
- 문제 생성기와 정답 판정은 **C# 정수 연산**으로. `float`/`double` 비교 금지 — 분수는 분자/분모 `int`,
  확률은 기약분수 정수 쌍, 닮음비는 정수 비. **중2 에는 √ 가 없다**(제곱근은 중3) — 피타고라스 문항은
  피타고라스 수(3·4·5, 5·12·13, 8·15·17 …와 그 배수)로만 만든다(`expression_traps` 참조).
- 화면의 수식 표기(분수 막대, 거듭제곱, 각 기호 ∠, 합동 ≡, 닮음 ∽, 삼각형 △)는 **실제 수식처럼** 그려라
  (`MgfText` + TMP 리치텍스트 `<sup>`·`<sub>`, 또는 메시로 직접 — KaTeX 는 못 쓴다). `3/4` 같은 평문 분수는 감점이다.

## 퀄리티 (이 트랙을 쓰는 이유)
- 3D 조명(키 라이트 + 환경광), 재질, 그림자, 카메라 연출(정답 순간 짧은 줌/흔들림/슬로모션),
  파티클, 사운드(합성 가능)로 **첫 10초 훅**을 만든다. 타이틀은 움직이는 장면이어야 한다.
- 모델·텍스처: 필요하면 Blender MCP 로 모델을 만들고 FBX/GLB 로 가져오거나, 이미지 생성(codex 내장
  gpt-image / grok 이미지)으로 텍스처·UI 아트를 만든다. 생성 기록은 `factory/unity-src/<slug>/ArtSource/`.
- 성능: 모바일에서 60fps 목표, QA 는 30fps 미만·15초 뒤 저하를 fatal 로 본다. 매 프레임 할당(new, 문자열
  결합, LINQ)·Instantiate/Destroy 남발 금지(풀링), 그림자 거리·해상도는 보수적으로. 빌드 합계는 **gzip 12 MB 미만**(QA `static.assets` 검사 — 게임 폴더 전체)이다.
  성능 예산 세부(드로콜·삼각형·텍스처)는 `docs/unity-track.md` §4-9.
- 한국어 글꼴은 킷 문서의 방식(OFL 글꼴 서브셋 + 라이선스 동봉)을 따른다. 두부(□) 글자 하나면 실격이다.

## 최종 응답 (본문 「마지막」 대체)
4줄 이내: 만든 파일(소스 폴더·산출 크기) / QA 결과(total·fatal·fps) / 무뇌 봇 첫 시도 정답률 + 우연 수준 / 특이사항.
