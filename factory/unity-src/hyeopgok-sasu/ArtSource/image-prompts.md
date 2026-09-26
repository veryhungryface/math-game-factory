# 표지 제작 기록

내장 `image_gen` / `imagegen` 스킬로 생성. 플레이 장면은 이 이미지를 사용하지 않고 Blender 모델과 Unity 실시간 렌더링으로 구성한다.

가로 프롬프트: Original polished low-poly 3D mobile game cover; Korean title “협곡 사수”; emerald high plateau and dark navy cliffs; ochre U-shaped canyon; hundreds of original red troops clash with azure defenders; stone crossbows, crowned teal king, gold sparks; 55° overhead; no existing branding or characters. Landscape 1200×630 composition.

정사각 프롬프트: Recompose the original cover into a square 1080×1080 with the complete Korean title in the upper safe area; preserve emerald plateau, navy cliff, ochre U-road, red and blue armies and crossbows; no other text.

도구 원본:
- `/Users/sitpo/.codex/generated_images/01a0de12-2320-73c0-a932-82ee532ff304/exec-74717f93-3b9f-42b0-b17e-553fc5fc69da.png`
- `/Users/sitpo/.codex/generated_images/01a0de12-2320-73c0-a932-82ee532ff304/exec-4176735c-d61a-47cc-a9ff-7d73638addae.png`

생성 결과를 검토한 뒤 `sips`로 요구 픽셀 규격을 맞추고 `pngquant --quality=45-85 --speed 1`로 PNG 용량을 줄였다. 최종 프로젝트 파일은 `public/g/hyeopgok-sasu/thumb.png`, `square.png`이다.

## 1차 수정 — 실물 일치 표지

검수의 표지-실물 불일치를 고치기 위해 실제 Unity GPU 장면 `ArtSource/validation/playing-1280.png`를 참조 이미지로 사용했다. 내장 `image_gen`에 가로와 정사각 구도를 각각 요청했으며, 실제 게임의 플랫 로우폴리 재질·비취 지형·짙은 절벽·석궁·적/수비대만 유지하고 폭포·회화 질감·문자·외부 IP를 금지했다.

- 가로 프롬프트 핵심: `1200×630 landscape store thumbnail; faithfully preserve the actual game's clean flat low-poly geometry, matte materials, jade canyon, dark block cliffs, golden ballistae, blue defenders and red attackers; no waterfalls, painterly texture, photorealism or text.`
- 정사각 프롬프트 핵심: `1080×1080 square app cover; same actual-game geometry and palette, centered icon-readable battle; no waterfalls, painterly texture, photorealism or text.`
- 도구 원본: `/Users/sitpo/.codex/generated_images/01a0de57-a2f9-75c3-be0f-0e5ebd6de528/exec-25d23b46-436d-4212-87e5-498ffa10d2f2.png`
- 도구 원본: `/Users/sitpo/.codex/generated_images/01a0de57-a2f9-75c3-be0f-0e5ebd6de528/exec-8c6d439e-99bf-48ed-ad8c-d97c56bb5bc1.png`

육안으로 실제 플레이와 같은 색·재질·U자 전선·병력 규모를 확인한 뒤 `sips`로 필수 규격을 맞추고 `pngquant --quality=55-88 --speed 1`로 최적화했다.
