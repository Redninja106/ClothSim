using SimulationFramework;
using SimulationFramework.Drawing;
using System.Diagnostics;
using System.Numerics;

class RepelConstraint : Constraint
{
    public static float strength = 1f;
    public static bool show;
    public float distance;

    public RepelConstraint(Body a, Body b, float? distance = null) : base(a, b)
    {
        this.distance = distance ?? Vector2.Distance(b.position, a.position);
    }

    public override void Render(ICanvas canvas)
    {
        if (!show)
            return;

        canvas.PushState();
        canvas.Fill(Color.Gray);
        canvas.DrawLine(A.position, B.position);
        canvas.PopState();
    }

    public override void Update(float dt, World world)
    {
        if (B.position == A.position)
            return;

        var axis = B.position - A.position;
        float bodyDist = axis.Length();
        
        Debug.Assert(bodyDist is not 0);

        axis /= bodyDist;

        float repelForce = (strength * distance) / bodyDist;

        repelForce = MathF.Min(repelForce, 10);

        var (wa, wb) = GetWeights();

        A.Accelerate(axis * wa * -repelForce);
        B.Accelerate(axis * wb * repelForce);
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