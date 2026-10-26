# 비누 썰어 아트 기록

- 기획 아트 단계의 `forming-idle.png`, `forming-cross.png`는 참고 원화이며 Unity Resources에는 넣지 않는다. 최종 `thumb.png`·`square.png`·`assets/title.png`는 `capture-runtime-covers.mjs`로 실제 WebGL 타이틀을 캡처한 화면이다.
- 런타임은 포도빛 건조 절단 지층·결정 파편·상아 비누·삼각 장력 프레임·노란 작업등·원본 포밍 프리미티브 조합으로 재구성한다. 둥근 협곡 블록과 생물 횡단 보상 대신, 절단 조각 자체가 각진 결정 리브로 기립하고 장력 프레임이 맞물리는 사건을 쓴다.
- 외부 모델·텍스처·음원·CDN을 사용하지 않는다. 사운드는 `MgfSfx` 합성음만 사용한다.
- `validation/math-selftest.mjs`는 C#과 독립인 정수식으로 624개 문제와 14,976개 절단 후보를 전수 검사한다. `runtime-bank-selftest.mjs`는 최종 WebGL이 푸시한 실제 624문항의 발문·답·판정 의미를 다시 전수 검사하고, `b3-bot-selftest.mjs`는 무뇌 정책 4종을 각 200판 측정한다.
- `validation/pointer-playthrough.mjs`는 `manual-visible-play.mjs`의 호환 진입점이다. 평가자가 캔버스에 보이는 발문만 읽어 계산 근거와 높이를 입력하며, problem id·문제은행·정답/시작 훅 없이 첫 오답 복구판과 새 덱 무실수판을 실제 Chrome pointer로 완주한다.
