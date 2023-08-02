// 圆锥
struct Cone
{
    float3 C;
    float3 H;
    float r;
};

// 平面
struct Plane
{
    float3 origin;
    float3 normal;
};

// 圆片
struct RoundPlane
{
    float3 origin;
    float3 normal;
    float r;
};

// 线
struct Line
{
    float3 origin;
    float3 direction;
};

// 球
struct Sphere
{
    float3 origin;
    float r;
};

// 半球
struct Hemisphere
{
    Sphere sphere;
    float3 normal;
    float angle;
};

float CosAngle(float3 v1, float3 v2)
{
    return dot(v1, v2) / (length(v1) * length(v2));
}

// 计算点到直线的距离
float PointToLineDistance(Line _line, float3 apoint)
{
    float3 lineDir = _line.direction;
    float3 pointDir = apoint - _line.origin;
    float distance = length(cross(pointDir, lineDir));
    return distance;
}

// 直线与平面的交点
int LineToPlane(Line _line, Plane p, out float3 intersectionPoint)
{
    float3 L0 = _line.origin;
    float3 v = _line.direction;

    float3 lineDir = v;
    float3 planeNormal = normalize(p.normal);

    float dotLinePlane = dot(lineDir, planeNormal);

    float3 lineToPlane = L0 - p.origin;
    float t = -dot(lineToPlane, planeNormal) / dotLinePlane;

    intersectionPoint = L0 + t * lineDir;
    return 1;
}

// 直线与圆片的交点
int LineToRoundPlane(Line _line, RoundPlane p, out float3 intersectionPoint)
{
    float3 L0 = _line.origin;
    float3 v = _line.direction;

    float3 lineDir = v;
    float3 planeNormal = normalize(p.normal);

    float dotLinePlane = dot(lineDir, planeNormal);

    float3 lineToPlane = L0 - p.origin;
    float t = -dot(lineToPlane, planeNormal) / dotLinePlane;

    intersectionPoint = L0 + t * lineDir;

    float3 v1 = intersectionPoint - p.origin;
    float d = length(v1);
    if (d > p.r)
    {
        intersectionPoint = float3(0.0f, 0.0f, 0.0f);
        return 0;
    }

    return 1;
}

// 直线与球的交点，在线的方向上，L1在L2的后面
int LineToSpherePoint(Line _line, Sphere sphere, out float3 P1, out float3 P2)
{
    P1 = 0;
    P2 = 0;

    float3 v = _line.direction;
    float3 L0 = _line.origin;

    float3 C = sphere.origin;
    float r = sphere.r;

    float3 w = L0 - C;

    float a = dot(v, v);
    float b = 2 * dot(v, w);
    float c = dot(w, w) - r * r;

    float delta = b * b - 4 * a * c;

    if (delta < 0)
    {
        return -1;
    }

    float t1 = (-b - sqrt(delta)) / (2 * a);
    float t2 = (-b + sqrt(delta)) / (2 * a);

    P1 = L0 + t1 * v;
    P2 = L0 + t2 * v;

    return 2;
}

// 直线与半球的交点，在线的方向上，L1在L2的后面
int LineToHemispherePoint(Line _line, Hemisphere hemisphere, out float3 P1, out float3 P2)
{
    P1 = 0;
    P2 = 0;

    if (LineToSpherePoint(_line, hemisphere.sphere, P1, P2) <= 0)
    {
        return 0;
    }

    // P1 = TP1;
    // P2 = TP2;
    //
    // return 1;

    float cosAngle = cos(hemisphere.angle);

    float3 v1 = normalize(P1 - hemisphere.sphere.origin);
    float3 v2 = normalize(P2 - hemisphere.sphere.origin);


    float a1 = CosAngle(v1, hemisphere.normal);
    float a2 = CosAngle(v2, hemisphere.normal);

    int i = 2;
    if (a1 < cosAngle)
    {
        P1 = 0;
        i--;
    }
    if (a2 < cosAngle)
    {
        P2 = 0;
        i--;
    }

    return i;
}

// 直线与圆锥斜面的交点，在线的方向上，L1在L2的后面
int LineToConePoint(Line _line, Cone cone, out float3 L1, out float3 L2)
{
    L1 = 0;
    L2 = 0;
    float3 L0 = _line.origin;
    float3 v = _line.direction;

    float3 C = cone.C;
    // 圆锥的顶点
    float3 H = cone.H;
    // 圆锥的半径
    float r = cone.r;

    float3 h = C - H;
    float3 hN = normalize(h);
    float m = r * r / (dot(h, h));

    float3 w = L0 - H;

    float VdH = dot(v, hN);
    float WdH = dot(w, hN);

    float a = dot(v, v) - m * VdH * VdH - VdH * VdH;
    float b = 2 * (dot(v, w) - m * VdH * WdH - VdH * WdH);
    float c = dot(w, w) - m * WdH * WdH - WdH * WdH;

    float delta = b * b - 4 * a * c;

    if (delta < 0)
    {
        return -1;
    }

    float t1 = (-b - sqrt(delta)) / (2 * a);
    float t2 = (-b + sqrt(delta)) / (2 * a);

    L1 = L0 + t1 * v;
    L2 = L0 + t2 * v;

    float d1 = dot(L1 - H, hN);
    float d2 = dot(L2 - H, hN);

    float hl = length(h);
    int i = 2;

    if (d1 > hl || d1 < 0)
    {
        L1 = 0;
        i--;
    }
    if (d2 > hl || d2 < 0)
    {
        L2 = 0;
        i--;
    }
    return i;
}
