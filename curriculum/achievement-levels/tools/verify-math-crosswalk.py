#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""성취수준 추출본(수학)과 curriculum/2022-elementary-math.json 성취기준 대조."""
import json, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
AL = os.path.join(ROOT, 'curriculum/achievement-levels')

def norm(t):
    return re.sub(r'[\s‘’\'"“”·ㆍ]', '', t)

base = {s['code']: s for s in json.load(
    open(os.path.join(ROOT, 'curriculum/2022-elementary-math.json')))['standards']}
al = {s['code']: s for s in json.load(open(os.path.join(AL, '5-6/math.json')))['standards']}

missing = sorted(set(base) - set(al))
extra = sorted(set(al) - set(base))
diff = [c for c in sorted(set(base) & set(al)) if norm(base[c]['text']) != norm(al[c]['text'])]
matched = len(set(base) & set(al))

print(f'기준(2022-elementary-math.json) {len(base)}개 / 성취수준 추출본 {len(al)}개')
print(f'코드 매칭 {matched}/{len(base)} ({matched / len(base) * 100:.1f}%)')
print(f'추출본에 없음: {missing or "없음"}')
print(f'기준에 없음: {extra or "없음"}')
print(f'문장 불일치: {diff or "없음"}')
for c in diff:
    print(f'  {c}\n    기준: {base[c]["text"]}\n    추출: {al[c]["text"]}')
noab = [c for c, s in al.items() if sorted(s['levels']) != ['A', 'B', 'C']]
print(f'A/B/C 3수준 미충족: {noab or "없음"}')
sys.exit(1 if (missing or extra or diff or noab) else 0)
