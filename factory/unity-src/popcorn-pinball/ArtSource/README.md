# 팝콘 핀볼 아트 기록

- 초기 아트 에이전트 산출물은 시안 기록으로만 보존하며 최종 표지·런타임에는 사용하지 않는다.
- 런타임 타이틀은 Unity 프리미티브로 만든 최종 유리 컷어웨이 장면이다.
- 생성 프롬프트와 크기·출처 기록은 `factory/work/art.json`, `art-title.json`, `art-thumb.json`, `art-square.json`에 있다.
- 런타임 세계는 외부 모델 없이 Unity 프리미티브와 절차적 UI 스프라이트로 만든다.

## 2차 수정 표지 정책

`thumb.png`와 `square.png`는 생성 시안을 쓰지 않고 `validation/capture-runtime-covers.mjs`로 최종 Unity 타이틀 장면을 직접 캡처한다. 따라서 표지와 런타임의 유리 컷어웨이, 종이 접힘 장치, 팝콘 생물, 재질과 배치가 동일하다.
