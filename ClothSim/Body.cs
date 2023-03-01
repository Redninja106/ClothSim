using SimulationFramework.Drawing;
using System.Diagnostics;
using System.Numerics;

class Body : IPhysicsObject
{
    public static float dampening = .2f;
    public static bool drawBodies = false;
    public static float bodyRadius = .025f;

    public Vector2 position { get => _position; set { Debug.Assert(value.X is not float.NaN && value.Y is not float.NaN); _position = value; } }

    private Vector2 _position;
    public Vector2 lastPosition;
    public bool pinned;
    public Vector2 storedAcceleration;
    public Vector2? grabOffset;

    public Body(Vector2 position, bool pinned)
    {
        this.position = position;
        this.lastPosition = position;
        this.pinned = pinned;
    }

    public void Render(ICanvas canvas)
    {
        if (drawBodies)
            canvas.DrawCircle(position, bodyRadius);
    }

    public void Accelerate(Vector2 acceleration)
    {
        storedAcceleration += acceleration;
    }

    public void Update(float dt, World world)
    {
        if (pinned)
            return;

        Vector2 velocity = position - lastPosition;

        lastPosition = position;

        var accel = storedAcceleration + world.Gravity;
        storedAcceleration = Vector2.Zero;

        var dampeningFactor = MathF.Pow(1f - dampening, dt);
        position += (dampeningFactor * velocity) + (accel * dt * dt);
    }
}
