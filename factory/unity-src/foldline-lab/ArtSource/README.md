# 폴드라인 아트·구현 기록

- 기획 정체성: 구름 바다 위 편광 태양돛 폴드 조율장.
- 생성 에셋: `public/g/foldline-lab/assets/mylar-noise.png` — “Seamless subtle pearlescent translucent mylar texture, fine polarization sheen only, no objects, no letters, no baked lighting”. Unity 런타임 사본은 `Resources/FoldlineLab/mylar-noise.png`다.
- 표지 원화: `public/g/foldline-lab/assets/title.png`, `thumb.png`, `square.png`. 표지에는 생성 에이전트가 만든 코발트 공중 골조·구름 바다·태양돛·청록/주홍 편광띠·헤일로 구도를 썼다. `title.png`는 `Resources/FoldlineLab/title.png` 사본을 통해 움직이는 타이틀 키 아트로 쓰고, 로드되지 않으면 뒤의 절차형 3D 조율장이 그대로 폴백한다.
- 런타임 세계는 외부 모델이나 원격 텍스처 없이 C# 절차 메시와 MGF 킷 재질로 만든다. 캐릭터는 없다.
- 정답 피드백은 청록/주홍 이중 띠가 흰 자국으로 합쳐지고 헤일로가 펼쳐지는 사건, 오답은 이중 띠와 흰 정답 자국의 어긋남이다. 잉크·종이 동물·파티클·점수 팝업을 쓰지 않는다.
- `validation/b3-bot-selftest.mjs`는 런타임의 직선 판정을 같은 `(theta, signed offset)` 공간에 옮겨 고정선·36방향 순환·무작위·무입력 각 1,000판을 재현한다. 결과는 `validation/b3-bot-results.json`에 엔진 SHA와 함께 남긴다.
