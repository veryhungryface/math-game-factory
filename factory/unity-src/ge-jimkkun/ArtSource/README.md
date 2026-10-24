# 게 짐꾼 아트 소스 기록

- 게임 세계·게·부두·수면·뗏목은 Unity 프리미티브와 런타임 UI 메시로 직접 구성했다.
- `public/g/ge-jimkkun/assets/hero.png`: GPT 이미지 생성, 원본 프롬프트는 `factory/work/chosen.json`의 `art_direction.assets_needed[id=hero]`. 프리미티브 게의 둥근 실루엣 참고용이며 런타임에서 직접 로드하지 않는다.
- `public/g/ge-jimkkun/assets/title.png`: GPT 이미지 생성, 환초 항구의 색·밀도 참고용 키 아트. 런타임 타이틀은 같은 팔레트의 실제 Unity 디오라마다.
- `public/g/ge-jimkkun/thumb.png`, `square.png`: GPT 이미지 생성, `factory/work/chosen.json`에 기록된 각 프롬프트로 생성 후 정확한 납품 크기로 최적화했다.
- 외부 상표·기존 캐릭터·원격 런타임 에셋은 사용하지 않았다.
