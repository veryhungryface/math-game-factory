# 삐뚤 대문 아트 소스

기획 정체성은 **자정 벨벳 봉제극장의 삐뚤 대문 퍼레이드**다. 아트 단계에서 생성한
`title.png`, `velvet_tile.png`, `wool_lamb_ref.png`를 콘셉트 참조로 만들었다. 1차 수정에서
타이틀과 플레이의 재질 차이를 없애기 위해 `title.png` 오버레이를 제거하고, 타이틀에도 런타임의
로우폴리 봉제극장을 그대로 보여 준다. 꿰미 양은 참조 이미지의 둥근 펠트 몸·각진 검은 얼굴판·리본
표식을 Unity 구·둥근 상자·원기둥으로 다시 구성했다.

- `title.png`: built-in image generation, vertical midnight plum velvet toy theatre key art.
- `velvet_tile.png`: built-in image generation, seamless deep plum velvet patchwork tile.
- `wool_lamb_ref.png`: built-in image generation, transparent original needle-felt parade lamb reference.
- 원본 공개 에셋: `public/g/ppittul-daemun/assets/`
- Unity 보존 참조(런타임 미사용): `Resources/PpittulDaemun/title.png`

상표·기존 캐릭터를 참조하지 않았으며 외부 네트워크 런타임 요청은 없다.
