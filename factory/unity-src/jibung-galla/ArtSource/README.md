# 지붕 갈라 ArtSource

- 세계·지붕·방·고양이·표식은 `Scripts/JibungView.cs`가 로컬 Unity 프리미티브와 런타임 메시로 만든다.
- 팔레트: cobalt `#315ED0`, ivory `#F1E9D6`, amber `#E6A249`, ink `#202A38`, sage `#86B9AD`.
- `public/g/jibung-galla/assets/hero.png`는 통통한 상아·호박색 고양이 메시의 원본 생성 참고 시트다.
- `public/g/jibung-galla/assets/title.png`와 최초 `thumb.png`/`square.png`는 구도 참고용 생성 키 아트다. 런타임은 외부 이미지나 CDN을 불러오지 않는다.
- 생성 프롬프트와 파일 치수/출처는 `factory/work/art.json`에 기록되어 있다.
- 마이크로 인터랙션은 Animata의 Spring Scale In(방 번호), Icon Ripple(A 손잡이·무효 입력), Counter(결과 수치)의 움직임만 참고하고 React 코드·패키지는 사용하지 않았다.
