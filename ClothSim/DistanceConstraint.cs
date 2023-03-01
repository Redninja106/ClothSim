using SimulationFramework;
using SimulationFramework.Drawing;
using System.Numerics;

class DistanceConstraint : Constraint
{
    public static float stretchiness;
    public float length;

    public DistanceConstraint(Body a, Body b, float? length = null) : base(a, b)
    {
        this.length = length ?? Vector2.Distance(b.position, a.position);
    }

    public override void Render(ICanvas canvas)
    {
        canvas.DrawLine(A.position, B.position);
    }

    public override void Update(float dt, World world)
    {
        var axis = B.position - A.position;
        var distance = axis.Length();

        if (distance is 0)
            return;

        axis /= distance;

        var diff = (distance - length);

        var (wa, wb) = GetWeights();

        diff = MathF.Sign(diff) * MathHelper.Lerp(MathF.Abs(diff), MathF.Min(1, diff * diff), stretchiness);
        // diff *= MathF.Exp(-stretchiness);
        A.position += axis * diff * wa;
        B.position += axis * -diff * wb;
    }

    public (float a, float b) GetWeights()
    {
        if (A.pinned)
            return (0, 1);
        if (B.pinned)
            return (1, 0);
        
        return (.5f, .5f);
    }
}
