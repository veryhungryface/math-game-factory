# 지붕 갈라 ArtSource

- 세계·지붕 골목·천 깃발·황금 실 이음매·표식은 `Scripts/JibungView.cs`가 로컬 Unity 프리미티브와 런타임 메시로 만든다.
- 1차 검수에서 `bolt-pureo`와 겹친 밝은 도자 생물집·반복 방 격자·생물 입장 보상을 버렸다. 현재 팔레트는 dusk plum `#342947`, coral paper `#C95E55`, ochre paper `#D79A52`, fiber ivory `#E9D8B4`, gold thread `#F6C85A`다.
- 재설계는 새 외부 에셋을 생성하지 않았으며, 거친 종이/천 느낌은 낮은 smoothness의 로컬 머티리얼과 비대칭 배치로 만든다. 기존 고양이 생성 참고 이미지는 런타임에서 사용하지 않는다.
- `thumb.png`/`square.png`는 수정 후 최종 Unity 런타임 타이틀을 다시 캡처한다. 런타임은 외부 이미지나 CDN을 불러오지 않는다.
- 생성 프롬프트와 파일 치수/출처는 `factory/work/art.json`에 기록되어 있다.
- 마이크로 인터랙션은 Animata의 Spring Scale In(방 번호), Icon Ripple(A 손잡이·무효 입력), Counter(결과 수치)의 움직임만 참고하고 React 코드·패키지는 사용하지 않았다.
