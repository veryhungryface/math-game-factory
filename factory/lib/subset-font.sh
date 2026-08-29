#!/usr/bin/env bash
# =============================================================================
# subset-font.sh — 타이틀 로고용 한글 디스플레이 폰트 서브셋 생성기
# =============================================================================
#
# 왜 필요한가
#   한글 폰트 원본은 풀셋 2350자(+) 때문에 1~3MB다. 타이틀 화면에서 실제로 쓰는
#   글자는 제목 2~7자 + 부제 몇 자뿐이다. 그 글자만 뽑아 woff2 로 구우면
#   게임당 3~15KB 로 떨어져 자기완결(외부 CDN 금지) 원칙을 지키면서도
#   로고 서체를 게임마다 다르게 줄 수 있다.
#
# 사전 준비 (1회)
#   pip3 install --user fonttools brotli
#   # pyftsubset 이 PATH 에 없으면: export PATH="$HOME/Library/Python/3.9/bin:$PATH"
#
# 사용법
#   factory/lib/subset-font.sh <원본.ttf|otf> <출력.woff2> "<포함할 글자들>"
#
# 예 (꽝도장: 제목 "꽝도장" + 부제 "잘못 찍으면 찢어져")
#   factory/lib/subset-font.sh \
#     ~/fonts/BlackHanSans-Regular.ttf \
#     public/g/clang-stamp/assets/fonts/logo.woff2 \
#     "꽝도장잘못찍으면찢어져"
#
# 규약
#   - 출력은 반드시 각 게임의 public/g/<slug>/assets/fonts/ 안에 둔다 (자기완결).
#   - 원본 TTF 는 저장소에 커밋하지 않는다. 라이선스 파일(OFL.txt)은 동봉한다.
#   - 글자 문자열에는 제목·부제·버튼 문구 중 "이 폰트로 렌더되는 것"만 넣는다.
#     본문/UI 는 시스템 폰트 스택을 그대로 쓰므로 넣을 필요가 없다.
#   - 중복 글자는 알아서 무시되므로 그냥 이어 붙여도 된다.
#   - 자모 조합 렌더링에 대비해 완성형 글자 외에 필요한 기호(·, !, ?, 숫자)도 넣어라.
#
# HTML 쪽 로딩 패턴 (docs/title-screen-spec.md §2.0 참고)
#   @font-face{
#     font-family:"LogoFace";
#     src:url("./assets/fonts/logo.woff2") format("woff2");
#     font-weight:400; font-style:normal; font-display:swap;
#   }
#   .logo{font-family:"LogoFace","Apple SD Gothic Neo","Malgun Gothic",sans-serif}
#
# =============================================================================
set -euo pipefail

if [ "$#" -lt 3 ]; then
  sed -n '2,45p' "$0"
  exit 2
fi

SRC="$1"
OUT="$2"
TEXT="$3"

[ -f "$SRC" ] || { echo "원본 폰트가 없다: $SRC" >&2; exit 1; }

PYFTSUBSET="$(command -v pyftsubset || true)"
if [ -z "$PYFTSUBSET" ]; then
  for c in "$HOME"/Library/Python/*/bin/pyftsubset /usr/local/bin/pyftsubset; do
    [ -x "$c" ] && PYFTSUBSET="$c" && break
  done
fi
[ -n "$PYFTSUBSET" ] || { echo "pyftsubset 없음. pip3 install --user fonttools brotli" >&2; exit 1; }

mkdir -p "$(dirname "$OUT")"

"$PYFTSUBSET" "$SRC" \
  --output-file="$OUT" \
  --flavor=woff2 \
  --text="$TEXT" \
  --layout-features='' \
  --no-hinting \
  --desubroutinize \
  --drop-tables+=GSUB,GPOS,GDEF,BASE,JSTF,DSIG,LTSH,PCLT,VDMX,hdmx \
  --name-IDs='' \
  --notdef-outline \
  --recommended-glyphs

SIZE=$(wc -c < "$OUT" | tr -d ' ')
echo "생성: $OUT  (${SIZE} bytes, 글자 $(printf '%s' "$TEXT" | wc -m | tr -d ' ')자)"
