# 모래쥐 멀리뛰기 아트·검증 기록

- 아트 방향: `factory/work/chosen.json`의 「모래쥐 발자국이 이어지는 붉은 협곡 굴길」.
- 런타임 월드는 Unity 프리미티브와 `MgfLook` 재질로 제작했다. 회화 배경은 사용하지 않는다.
- 아트 에이전트 참고 원본: `public/g/moraejwi-meollittwigi/assets/hero.png` (1024×1024, 투명 배경), `assets/title.png` (1024×1536). `hero.png`는 모델링 참고이며 런타임 2D 오버레이로 사용하지 않았다.
- 생성 프롬프트와 치수·출처는 `factory/work/art.json`이 정본이다. 카탈로그 표지는 제공된 `thumb.png`·`square.png`를 보존한다.
- 수학 생성기 주석에는 중2 표현 함정(근호 금지, 길이/넓이 단위 구분, 지시형 발문)과 오개념 ID를 옮겼다.
- `MoraejwiRules.BuildBank()`가 부팅 시 569문항의 정답·해 개수·중복과 세 오개념식의 우연 정답 0건을 전수 검사한다.

검증 산출물은 `ArtSource/validation/`에 저장한다.
