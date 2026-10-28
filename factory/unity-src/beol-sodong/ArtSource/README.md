# 벌 소동 — 아트 소스 기록

- 런타임 세계는 Unity 저폴리 프리미티브와 `MgfLook` 재질로 직접 조립한다. 외부 모델·텍스처·CDN은 사용하지 않는다.
- 공장 아트 단계가 생성한 참고 이미지: `public/g/beol-sodong/assets/hero.png`, `assets/title.png`, `thumb.png`, `square.png`.
- 생성 주체: Codex 내장 이미지 생성(gpt-image), 2026-10-28. 원 프롬프트는 `factory/work/chosen.json`의 `art_direction.assets_needed`와 `factory/work/art.json`에 기록돼 있다.
- 런타임 물성 번역: 석회암 방사형 꽃시계, 흑단 몸통, 황동 핀·연결선, 운모 날개, 상아 도자 숫자표, 은염 유리건판.
- 표지 이미지는 공장 아트 단계의 산출물을 그대로 사용하며 런타임에서는 네트워크 요청이나 이미지 파일 로드를 하지 않는다.
