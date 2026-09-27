"""Reimport every exported FBX and verify the actual baked channel contract.

Blender -b --factory-startup --python ArtSource/blender/verify_ao.py
"""
import bpy, json, hashlib
from pathlib import Path

HERE=Path(__file__).resolve().parent
OUT=HERE.parent.parent/'Resources'/'HyeopgokSasu'/'Models'
manifest=json.loads((HERE/'model-manifest.json').read_text())
results=[]
for entry in manifest['assets']:
 for obj in list(bpy.data.objects):bpy.data.objects.remove(obj,do_unlink=True)
 path=OUT/(entry['id']+'.fbx')
 bpy.ops.import_scene.fbx(filepath=str(path),use_custom_normals=True)
 meshes=[obj.data for obj in bpy.data.objects if obj.type=='MESH']
 colors=[tuple(c.color_srgb) for mesh in meshes for c in mesh.color_attributes[0].data]
 uv=[tuple(c.uv) for mesh in meshes for c in mesh.uv_layers[0].data]
 assert colors and len(colors)==len(uv),entry['id']+': missing colour/UV corners'
 assert all(.415<=c[3]<=1.001 for c in colors),entry['id']+': invalid AO alpha'
 assert max(c[3] for c in colors)-min(c[3] for c in colors)>.025,entry['id']+': flat AO'
 assert all(abs(v[1]-1)<.001 for v in uv),entry['id']+': missing baked marker'
 assert all(abs(v[0])<.001 or abs(v[0]-1)<.001 for v in uv),entry['id']+': invalid team mask'
 results.append({'id':entry['id'],'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
                 'triangles':entry['triangles'],'exported_corners':len(colors),
                 'alpha_ao_min':round(min(c[3] for c in colors),5),
                 'alpha_ao_max':round(max(c[3] for c in colors),5),
                 'team_corners':sum(v[0]<.5 for v in uv),
                 'encoding_verified':True,'bake':entry['ao_bake']})
report={'verdict':'pass','asset_count':len(results),
        'method':'48 cosine-weighted hemisphere BVHTree rays per colour corner in Blender',
        'total_rays':sum(entry['ao_bake']['rays'] for entry in manifest['assets']),
        'total_hits':sum(entry['ao_bake']['hits'] for entry in manifest['assets']),
        'runtime_contract':'COLOR.rgb base sRGB; COLOR.a geometric AO; UV0.x team mask; UV0.y=1 marker',
        'verification':'Independent FBX reimport checks actual alpha and UV values, not only source arrays.',
        'assets':results}
(HERE/'ao-bake-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('AO VERIFY PASS:',len(results),'assets;',report['total_rays'],'rays')
