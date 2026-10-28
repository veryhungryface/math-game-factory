# 도토리 낙하 — 지하 뿌리 기록고 소스·에셋·검증 기록

- 3D 에셋: 전부 코드 생성 로우폴리(`Scripts/DotoriMesh.cs` — 32색 팔레트 텍스처 + 합친 메시). 외부 모델·텍스처 없음.
- `ai-keyart/thumb-ai.png`·`square-ai.png`: 아트 단계의 폐기된 야외 수관 키아트(보관용, 게임 미사용). 게시 표지
  `public/g/dotori-nakha/thumb.png`·`square.png`는 1차 수정의 지하 뿌리 기록고 런타임을
  `validation/capture-covers.mjs`로 재촬영했다(실제 화면과 다른 AI 표지 금지).
- `public/g/dotori-nakha/assets/hero.png`·`title.png`: 아트 단계의 폐기 산출물. 게임은 참조하지 않는다.
- `gen-glyphs.py`: 소스 문자열의 한글을 모아 `Scripts/DotoriGlyphs.cs`(SDF Prewarm)를 만든다. 문구를 바꾸면 빌드 전 실행.

## 검증 (2026-10-28, 1차 수정)
- `validation/run-headless.sh` → `headless-results.json`: 은행 696문항 전수(중앙값 재계산·23눈금 중 유일해·버킷 균등·정답에 오개념 진단 0건),
  덱 정답 균등성 χ²(홀수 df11 18.2 / 짝수 df22 22.0, 99% 임계 미만), B3 무뇌 봇 200판×정책(고정 눈금은 23위치×200판 합산).
- `validation/pointer-playthrough.mjs` → `pointer-playthrough-results.json`: 진짜 마우스 드래그로 연습→손 정렬→8무리 클리어(8/8),
  첫 오답 뒤 수리 클리어(7/8·걸쇠 2), 걸쇠 0 즉시 종료+입력 잠금, 연습 무입력 대기(시계 정지), 실전 무입력 120초 시간 종료.
  중앙값은 getState().rawWeights 로 계산했으므로 독립 사람 플레이(화면만 읽기) 증거는 아니다.
- `validation/manual-screen-playthrough.mjs` → `manual-screen-playthrough/*/result.json`: 상태 투영에서 `rawWeights`를 제외하고
  각 PNG의 화면 숫자만 사람이 읽어 입력한다. 최종 artifact `f2f36c…`에서
  `screen-recovery-20261028063533`은 일반 실수→자발적 수리→7/8·걸쇠 2 클리어,
  `screen-clean-20261028063725`는 별도 무실수 8/8·걸쇠 3 클리어, 두 판 모두 콘솔 오류 0.
- 호스트 firstplay `fix1-20261028-firstplay-final`: 수정 전 연습→실전 51초에서 수정 후 25초로 단축(30초 정체 게이트 통과).
