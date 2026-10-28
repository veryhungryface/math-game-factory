# 소스의 모든 한글·기호 글자를 모아 Prewarm 상수 파일을 만든다(빌드 전 실행).
import glob, os, re
here = os.path.dirname(os.path.abspath(__file__))
src = os.path.join(here, '..', 'Scripts')
chars = set()
for f in glob.glob(os.path.join(src, '*.cs')):
    if f.endswith('DotoriGlyphs.cs'): continue
    for lit in re.findall(r'"((?:[^"\\]|\\.)*)"', open(f, encoding='utf-8').read()):
        chars.update(c for c in lit if ord(c) > 127)
s = ''.join(sorted(chars)).replace('\\', '\\\\').replace('"', '\\"')
open(os.path.join(src, 'DotoriGlyphs.cs'), 'w', encoding='utf-8').write(
    '// 자동 생성(ArtSource/gen-glyphs.py): 소스에 쓰인 한글·기호 — 부팅 때 SDF 를 미리 굽는다.\n'
    'namespace Mgf.DotoriNakha\n{\n    static class DotoriGlyphs\n    {\n        public const string All = "' + s + '";\n    }\n}\n')
print(len(chars))
