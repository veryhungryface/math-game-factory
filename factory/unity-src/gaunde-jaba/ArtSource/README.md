# 가운데 잡아 — 아트 출처

- `Resources/GaundeJaba/title.png`: 아트 단계에서 gpt-image로 생성된 `public/g/gaunde-jaba/assets/title.png` 사본. 세로 타이틀 키 아트.
- 가로 화면도 같은 `title.png`를 cover-crop하고 코드 수관 오브젝트가 좌우 세계를 채운다. 공개용 `thumb.png`는 게임 폴더에만 한 번 둔다.
- 런타임 주인공·수관·그물·보금자리는 `GaundeJabaView.cs`의 로우폴리 프리미티브와 킷 재질로 제작했다.
- 생성 프롬프트와 원본 크기·출처 기록은 `factory/work/art.json`과 `factory/work/chosen.json`의 `art_direction.assets_needed`가 정본이다.
- 두 텍스처는 `Resources.Load` 결과가 없으면 자동으로 숨고 코드 수관이 그대로 보이도록 폴백한다.
