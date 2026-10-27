# 가운데 잡아 — 아트 출처

- 최초 `Resources/GaundeJaba/title.png`와 `public/g/gaunde-jaba/assets/title.png`는 아트 단계의 gpt-image 생성물이었다. 생성 프롬프트와 원본 출처는 `factory/work/art.json`·`factory/work/chosen.json`에 남아 있다. 공개 폴더의 사용하지 않는 `assets/title.png`·`hero.png` 복제본은 1차 수정에서 제거했다.
- 1차 수정에서는 생성 키 아트와 인게임의 불일치를 없애기 위해 런타임에서 이 텍스처를 로드하지 않는다. 타이틀·플레이·thumb·square 모두 같은 WebGL 관측소 장면을 정본으로 쓴다.
- 최종 장면은 `GaundeJabaView.cs`가 만드는 야간 시간 표본 관측소다. 남청 금속 구조, 청록 유리판, 황동 클램프, 각진 유리나방 기록관과 표본 봉인 사건으로 구성했다.
- 이 수정에는 새 생성 이미지·외부 모델·외부 텍스처를 사용하지 않았다. 표지는 최종 WebGL 런타임을 직접 캡처한다.
