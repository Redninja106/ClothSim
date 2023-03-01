using SimulationFramework;
using SimulationFramework.Drawing;
using System.Data;
using System.Numerics;

class World
{
    float simulatedTime;
    float totalTime;

    public float timescale = 1f;
    public float timestep;
    public List<Body> bodies = new();
    public List<Constraint> constraints = new();

    public Vector2 Gravity { get; set; }

    public World(float timestep, IWorldProvider provider)
    {
        this.timestep = timestep;
        this.simulatedTime = Time.TotalTime;
        this.totalTime = Time.TotalTime;
        provider.Populate(this.bodies, this.constraints);

        bodies = bodies.OrderBy(b => Random.Shared.Next()).ToList();
        constraints = constraints.OrderBy(b => Random.Shared.Next()).ToList();
        Gravity = -Vector2.UnitY;
    }

    public void Render(ICanvas canvas)
    {
        foreach (var b in bodies)
        {
            b.Render(canvas);
        }

        foreach (var c in constraints)
        {
            c.Render(canvas);
        }
    }

    public void Update()
    {
        totalTime += Time.DeltaTime * timescale;
        while (timescale != 0 && this.simulatedTime < totalTime)
        {
            float start = Time.TotalTime;

            Step(timestep);
            simulatedTime += timestep;

            float end = Time.TotalTime;

            if (end - start > timestep)
                this.simulatedTime = totalTime;
        }
    }

    private void Step(float dt)
    {
        foreach (var body in bodies)
        {
            body.Update(dt, this);
        }

        foreach (var constraint in constraints)
        {
            constraint.Update(dt, this);
        }
    }

    public void Sever(Constraint constraint)
    {
        this.constraints.Remove(constraint);
    }
}
