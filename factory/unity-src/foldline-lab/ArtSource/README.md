# 폴드라인 아트·구현 기록

- 2차 수정 정체성: 폐쇄형 기계식 돛 직물 공방. `prism-mobil`과 겹치던 반투명 광학판·이중 광선·원형 헤일로 축을 버리고 불투명 돛천·황동 솔기 클램프·등거리 측정 리브로 바꿨다.
- 생성 에셋: `public/g/foldline-lab/assets/mylar-noise.png` — “Seamless subtle pearlescent translucent mylar texture, fine polarization sheen only, no objects, no letters, no baked lighting”. Unity 런타임 사본은 `Resources/FoldlineLab/mylar-noise.png`다.
- 최초 표지 원화: `public/g/foldline-lab/assets/title.png`와 `Resources/FoldlineLab/title.png`. 포토리얼 원화와 로우폴리 플레이 세계가 분리돼 보인다는 1차 검수 뒤 참고 원화로만 남겼다. 런타임 타이틀·`thumb.png`·`square.png`는 모두 최종 절차형 3D 조율장 자체를 캡처해 같은 코발트 골조·마일러 돛·헤일로 사건을 쓴다.
- 런타임 세계는 외부 모델이나 원격 텍스처 없이 C# 절차 메시와 MGF 킷 재질로 만든다. 캐릭터는 없다. 생성 마일러 텍스처는 `ArtSource` 기록으로만 남기고 최종 돛은 무늬 없는 불투명 캔버스 재질이다.
- 정답 피드백은 접는 선을 따라 황동 솔기 클램프가 순서대로 잠기고, 외심·내심에서 같은 거리를 실제 세 리브로 보여 주는 사건이다. 오답은 포개지지 않은 두 면과 기준 자국의 어긋남이다. 광선 합치기와 원형 헤일로는 더 이상 쓰지 않는다.
- `validation/b3-bot-selftest.mjs`는 런타임의 직선 판정을 같은 `(theta, signed offset)` 공간에 옮겨 고정선·36방향 순환·무작위·무입력 각 10,000판을 재현한다. 결과는 `validation/b3-bot-results.json`에 엔진 SHA와 함께 남긴다.
