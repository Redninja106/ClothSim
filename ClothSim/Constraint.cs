using SimulationFramework.Drawing;

abstract class Constraint : IPhysicsObject
{
    public Body A { get; set; }
    public Body B { get; set; }

    protected Constraint(Body a, Body b)
    {
        A = a;
        B = b;
    }

    public abstract void Render(ICanvas canvas);
    public abstract void Update(float dt, World world);

}
