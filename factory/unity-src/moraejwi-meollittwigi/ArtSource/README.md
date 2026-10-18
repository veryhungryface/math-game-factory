# 모래쥐 멀리뛰기 아트·검증 기록

- 아트 방향: `factory/work/chosen.json`의 「모래쥐 발자국이 이어지는 붉은 협곡 굴길」.
- 런타임 월드는 Unity 프리미티브와 `MgfLook` 재질로 제작했다. 회화 배경은 사용하지 않는다.
- 아트 에이전트 참고 원본: `public/g/moraejwi-meollittwigi/assets/hero.png` (1024×1024, 투명 배경), `assets/title.png` (1024×1536). `hero.png`는 모델링 참고이며 런타임 2D 오버레이로 사용하지 않았다.
- 생성 프롬프트와 치수·출처는 `factory/work/art.json`이 정본이다. 1차 수정부터 카탈로그 표지는 `capture-runtime-covers.mjs`로 현재 Unity WebGL의 타이틀(1200×630)과 연습 장면(1080×1080)을 직접 캡처한다. 별도 삽화나 후처리는 사용하지 않으며 산출 해시·치수·바이트는 `cover-capture-results.json`에 남긴다.
- 수학 생성기 주석에는 중2 표현 함정(근호 금지, 길이/넓이 단위 구분, 지시형 발문)과 오개념 ID를 옮겼다.
- `MoraejwiRules.BuildBank()`가 부팅 시 569문항의 정답·해 개수·중복과 세 오개념식의 우연 정답 0건을 전수 검사한다.

검증 산출물은 `ArtSource/validation/`에 저장한다.
