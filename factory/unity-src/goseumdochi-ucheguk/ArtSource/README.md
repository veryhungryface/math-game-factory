# 아트 출처와 구현 기록

- 기획/프롬프트 정본: `factory/work-lanes/20261014-015549-L3/chosen.json`
- 생성 결과 정본: `factory/work-lanes/20261014-015549-L3/art.json`
- 생성 도구: 공장 art 단계의 OpenAI 내장 이미지 생성 도구
- 납품 에셋: `public/g/goseumdochi-ucheguk/assets/{hero,title,digit_tiles,house_kit}.png`, `thumb.png`, `square.png`

Unity 런타임은 생성 이미지의 흰 배경과 12 MB 전송 예산을 피하기 위해, 같은 프롬프트의 물성(방해석·백자·에나멜·인디고 케이블)을 절차적 3D 메시와 재질로 재구성한다. 표지 파일은 art 단계 납품본을 보존한다. 모리는 상표 캐릭터가 아닌 이 게임 전용 프리미티브 조합 모델이다.
