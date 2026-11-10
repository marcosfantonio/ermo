#ifndef ERMO_HOUSE_INTERIOR_INCLUDED
#define ERMO_HOUSE_INTERIOR_INCLUDED

// All inputs are per-house material properties. Unconfigured/exterior materials remain identity.
float3 ErmoRoomLighting(float3 world, float3 normal, float3 sunDirection)
{
    if (_ErmoInteriorEnabled < .5) return float3(1, 1, 0);
    float3 localNormal = normalize(mul((float3x3)_ErmoInteriorWorldToLocal, normal));
    float3 positionLS = mul(_ErmoInteriorWorldToLocal, float4(world, 1)).xyz;
    // Sample air on the visible side, including doors/shutters recessed into the .25 m walls.
    // Outward-facing facade normals move OUT of the room, not through the wall into it.
    float3 air = positionLS + localNormal * .15;
    if (any(air < _ErmoInteriorMin.xyz) || any(air > _ErmoInteriorMax.xyz)) return float3(1, 1, 0);
    if (_ErmoInteriorCeiling.w > .5 &&
        air.y > _ErmoInteriorCeiling.y - abs(air.x - _ErmoInteriorCeiling.x) * _ErmoInteriorCeiling.z)
        return float3(1, 1, 0);
    // A sloping roof's exterior can lie over the room footprint, but is NOT an interior surface.
    if (_ErmoInteriorRoof > .5 && localNormal.y >= -.05) return float3(1, 1, 0);

    float ambient = .025;
    float direct = 0;
    float3 ray = normalize(mul((float3x3)_ErmoInteriorWorldToLocal, sunDirection));
    for (int i = 0; i < _ErmoPortalCount; i++)
    {
        float opening = saturate(_ErmoPortalMin[i].w);
        if (opening <= .001) continue;
        float3 low = _ErmoPortalMin[i].xyz;
        float3 high = _ErmoPortalMax[i].xyz;
        float3 center = (low + high) * .5;
        float3 size = high - low;
        bool xPlane = size.x < size.z;
        float3 nearest = clamp(air, low, high);
        float distanceToOpening = length(air - nearest);
        float area = size.y * (xPlane ? size.z : size.x);
        float reach = max(.5, sqrt(area) * 2);
        float falloff = 1 - smoothstep(0, reach, distanceToOpening);
        ambient += opening * falloff * falloff * .8;

        // Direct sunlight only travels along rays that actually cross an open aperture.
        float direction = xPlane ? ray.x : ray.z;
        float planeDistance = xPlane ? center.x - air.x : center.z - air.z;
        if (abs(direction) < .0001) continue;
        float t = planeDistance / direction;
        float3 hit = air + ray * t;
        bool withinWidth = xPlane ? (hit.z >= low.z && hit.z <= high.z) : (hit.x >= low.x && hit.x <= high.x);
        if (t > 0 && withinWidth && hit.y >= low.y && hit.y <= high.y)
            direct = max(direct, opening);
    }
    return float3(saturate(ambient), direct, 1);
}
#endif
