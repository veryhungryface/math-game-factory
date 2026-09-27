"""Deterministic geometric ambient occlusion baked by Blender into vertex colour.

No procedural height-darkening stands in for AO: every colour corner fires 48
cosine-weighted hemisphere rays into the model's BVH, with a temporary receiving
ground plane for standalone assets. The plane is never exported. This script is
loaded by build_models.py and runs unchanged in Blender -b.

Export contract: Color.rgb = authored sRGB, Color.a = linear baked AO;
UV0.x = original team mask, UV0.y = 1 (baked encoding version marker).
"""
from mathutils.bvhtree import BVHTree

AO_SAMPLES = 48
AO_STRENGTH = .58

def bake_vertex_ao(ob):
    mesh = ob.data
    points = [v.co.copy() for v in mesh.vertices]
    polygons = [tuple(p.vertices) for p in mesh.polygons]
    minimum = Vector(tuple(min(v[i] for v in points) for i in range(3)))
    maximum = Vector(tuple(max(v[i] for v in points) for i in range(3)))
    extent = maximum - minimum
    radius = min(2.1, max(.32, max(extent) * .52))
    if ob.name == 'terrain':
        radius = 3.1
    else:
        # True contact occlusion from an implicit supporting ground surface.
        span = max(4.0, max(extent) * 3)
        base = minimum.z - .006
        offset = len(points)
        points.extend([Vector((-span, -span, base)), Vector((span, -span, base)),
                       Vector((span, span, base)), Vector((-span, span, base))])
        polygons.append((offset, offset + 1, offset + 2, offset + 3))
    tree = BVHTree.FromPolygons(points, polygons, all_triangles=False, epsilon=0)
    color = mesh.color_attributes['Color']
    ao_color = mesh.color_attributes.new(name='BakedAO', type='FLOAT_COLOR', domain='CORNER')
    uv = mesh.uv_layers.new(name='TeamMask_BakedAO')
    rays = []
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(AO_SAMPLES):
        r = math.sqrt((i + .5) / AO_SAMPLES)
        phi = i * golden
        rays.append(Vector((math.cos(phi) * r, math.sin(phi) * r, math.sqrt(1 - r*r))))
    values = []
    blocked_count = 0
    for polygon in mesh.polygons:
        normal = polygon.normal.normalized()
        tangent = normal.cross(Vector((0, 0, 1)) if abs(normal.z) < .92 else Vector((0, 1, 0))).normalized()
        bitangent = normal.cross(tangent).normalized()
        # Sampling just inside the face avoids seams at unwelded hard edges.
        center = polygon.center
        for loop_index in polygon.loop_indices:
            point = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            origin = point.lerp(center, .035) + normal * .0025
            obscured = 0.0
            for ray in rays:
                direction = tangent * ray.x + bitangent * ray.y + normal * ray.z
                location, hit_normal, hit_face, distance = tree.ray_cast(origin, direction, radius)
                if location is not None:
                    # A smooth finite-distance kernel prevents harsh proximity bands.
                    obscured += (1 - (distance/radius)**2)**2
                    blocked_count += 1
            ao = max(.42, 1 - AO_STRENGTH * obscured / AO_SAMPLES)
            authored = tuple(color.data[loop_index].color_srgb)
            color.data[loop_index].color_srgb = (*authored[:3], ao)
            ao_color.data[loop_index].color = (ao, ao, ao, 1)
            uv.data[loop_index].uv = (authored[3], 1)
            values.append(ao)
    # Export the first colour channel as COLOR; BakedAO is an inspectable source
    # witness while UV carries the legacy mask without altering shader semantics.
    mesh.color_attributes.active_color = color
    mesh.color_attributes.render_color_index = 0
    return {'method': 'Blender BVHTree cosine-weighted geometric hemisphere ray bake',
            'samples_per_corner': AO_SAMPLES, 'rays': len(values) * AO_SAMPLES,
            'hits': blocked_count, 'radius_m': round(radius, 4),
            'min': round(min(values), 5), 'max': round(max(values), 5),
            'mean': round(sum(values)/len(values), 5),
            'darkened_corners': sum(v < .97 for v in values),
            'color_channel': 'Color alpha', 'team_mask': 'UV0.x', 'encoding_marker': 'UV0.y = 1'}
