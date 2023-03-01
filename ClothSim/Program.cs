using ImGuiNET;
using SimulationFramework;
using SimulationFramework.Desktop;
using SimulationFramework.Drawing;
using System.Data;
using System.Diagnostics;
using System.Numerics;

Silk.NET.Windowing.Glfw.GlfwWindowing.Use();

float cx = 0, cy = 0, cz = 0;
World w = null!;
float windMin = 0, windMax = 1, windRate = 1;
int windDir = 0;
bool windEnabled = false;
int width = 31, height = 20, anchorFreq = 3;
float steps = 100, separation = .125f;
int scene = 0;
bool windowOpen = true;
int ropeSegments = 2;
float ropeLength = .75f;
float dragSize = .1f;
bool strongGrab = true;

Simulation.Create(Init, Render).RunDesktop();
void Init(AppConfig config)
{
    config.Title = "Verlet Integration";
    CreateWorld();
}

void CreateWorld()
{
    IWorldProvider provider = scene switch
    {
        0 => new Cloth(width, height, separation, anchorFreq),
        1 => new Rope(ropeSegments, ropeLength)
    };
    var prevGravity = w?.Gravity;
    w = new(1f / steps, provider);
    if (prevGravity is not null)
        w.Gravity = prevGravity.Value;
}

void Render(ICanvas canvas)
{
    if (Keyboard.IsKeyPressed(Key.F1))
        windowOpen = !windowOpen;

    float wp = MathF.Sin(Time.TotalTime * windRate);
    var wind = MathHelper.Lerp(windMin, windMax, MathHelper.Normalize(wp));

    if (!windEnabled)
        wind = 0;

    if (windowOpen && ImGui.Begin("Debug Window (F1)", ref windowOpen)) 
    {
        ImGui.Text(@"Controls:
WASD - Camera Movement
Mouse Wheel - Zoom
R - Reset
Left Click - Drag Objects
Right Click - Cut Connections
Middle Click - Pin Objects

");

        ImGui.Separator();
        ImGui.Text("Physics:");
        ImGui.SliderFloat("Time Scale", ref w.timescale, 0, 10);
        var g = w.Gravity.Y;
        ImGui.SliderFloat("Gravity", ref g, -10, 10);
        w.Gravity = Vector2.UnitY * g;

        if (ImGui.Button("No Gravity"))
        {
            w.Gravity = Vector2.Zero;
        }
        ImGui.SameLine();
        if (ImGui.Button("Default Gravity"))
        {
            w.Gravity = Vector2.UnitY * -1;
        }
        ImGui.SameLine();
        if (ImGui.Button("Flip Gravity"))
        {
            w.Gravity *= Vector2.UnitY * -1;
        }


        ImGui.SliderFloat("Dampening", ref Body.dampening, 0, 1);
        ImGui.SliderFloat("Stretchiness", ref DistanceConstraint.stretchiness, 0, 5);

        ImGui.Separator();
        ImGui.Text("Controls:");

        ImGui.SliderFloat("Drag/Cut Size", ref dragSize, 0.01f, 10f);
        ImGui.Checkbox("Strong Grab", ref strongGrab);

        ImGui.Separator();
        ImGui.Text("Debug:");

        ImGui.Checkbox("Show Repel Constraints", ref RepelConstraint.show);
        ImGui.SliderFloat("Repel Constraint Strength", ref RepelConstraint.strength, 0, 10);
        ImGui.Checkbox("Draw Bodies", ref Body.drawBodies);
        ImGui.SliderFloat("Body Size (visual only)", ref Body.bodyRadius, 0, .1f);

        ImGui.Separator();

        ImGui.Text("Scene Select:");
        ImGui.Combo("", ref scene, "Cloth Simulation\0Rope Simulation");

        ImGui.Separator();

        ImGui.Text("Scene Options (applied upon reset)");

        switch (scene) 
        {
            case 0:
                ImGui.SliderFloat("Steps Per Second", ref steps, 10, 250);
                ImGui.DragInt("Width", ref width);
                ImGui.DragInt("Height", ref height);
                ImGui.SliderInt("Anchor Frequency", ref anchorFreq, 0, 10);
                ImGui.SliderFloat("Grid Size", ref separation, 0.01f, 1f);
                break;
            case 1:
                ImGui.SliderFloat("Steps Per Second", ref steps, 10, 250);
                ImGui.SliderInt("Rope Segments", ref ropeSegments, 1, 1000);
                ImGui.SliderFloat("Rope Length", ref ropeLength, 1f, 20f);
                break;
        }

        if (ImGui.Button("Reset (R)"))
        {
            CreateWorld();
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Camera"))
        {
            var pos = new Vector2(cx, cy);
            ImGui.DragFloat2("Position", ref pos, .1f);
            (cx, cy) = pos;

            ImGui.DragFloat("Zoom Level", ref cz);

            if (ImGui.Button("Reset Camera"))
            {
                cx = cy = cz = 0;
            }
        }


        if (ImGui.CollapsingHeader("Wind"))
        {
            ImGui.Checkbox("Enable Wind", ref windEnabled);
            ImGui.Text($"Current Wind Strength: {wind}");
            ImGui.DragFloatRange2("Wind Min/Max", ref windMin, ref windMax, 0.01f, 0, 10);
            ImGui.Combo("Wind Direction", ref windDir, "Left\0Right");
            ImGui.SliderFloat("Wind Frequency", ref windRate, 0.01f, 10f);
        }
    }

    if (Keyboard.IsKeyPressed(Key.R))
        CreateWorld();

    w.Update();

    canvas.Clear(Color.Black);
    canvas.Translate(canvas.Width / 2, canvas.Height / 2);
    canvas.Scale(canvas.Height / 5);
    canvas.Scale(1, -1);

    canvas.Scale(MathF.Pow(1.1f, cz));
    canvas.Translate(-cx, -cy);

    if (Keyboard.IsKeyDown(Key.W)) cy += 5 * Time.DeltaTime;
    if (Keyboard.IsKeyDown(Key.S)) cy -= 5 * Time.DeltaTime;
    if (Keyboard.IsKeyDown(Key.A)) cx -= 5 * Time.DeltaTime;
    if (Keyboard.IsKeyDown(Key.D)) cx += 5 * Time.DeltaTime;

    cz += Mouse.ScrollWheelDelta;

    Matrix3x2.Invert(canvas.State.Transform, out var mat);
    var mp = Vector2.Transform(Mouse.Position, mat);
    var lastMp = Vector2.Transform(Mouse.Position - Mouse.DeltaPosition, mat);

    if (Mouse.IsButtonDown(MouseButton.Left) | Mouse.IsButtonDown(MouseButton.Right) | Mouse.IsButtonDown(MouseButton.Middle))
    {
        canvas.PushState();
        canvas.Stroke(Color.Red);
        canvas.DrawCircle(mp, dragSize);
        canvas.PopState();
    }

    if (strongGrab)
    {
        if (Mouse.IsButtonPressed(MouseButton.Left))
        {
            foreach (var b in w.bodies)
            {
                var distSq = Vector2.DistanceSquared(b.position, mp);

                if (distSq > dragSize * dragSize)
                    continue;

                b.grabOffset = b.position - mp;
            }
        }

        foreach (var b in w.bodies)
        {
            if (b.pinned || b.grabOffset is null)
                continue;

            b.position = mp + b.grabOffset.Value;
        }

        if (Mouse.IsButtonReleased(MouseButton.Left))
        {
            foreach (var b in w.bodies)
            {
                b.grabOffset = null;
            }
        }
    }
    else
    {
        if (Mouse.IsButtonDown(MouseButton.Left))
        {
            foreach (var b in w.bodies)
            {
                var distSq = Vector2.DistanceSquared(b.position, mp);

                if (b.pinned || distSq > dragSize * dragSize)
                    continue;

                b.position -= mp - lastMp;
            }
        }
    }

    if (Mouse.IsButtonDown(MouseButton.Right))
    {
        foreach (var c in w.constraints.ToArray())
        {
            var distSqA = Vector2.DistanceSquared(c.A.position, mp);
            var distSqB = Vector2.DistanceSquared(c.B.position, mp);

            if (distSqA < dragSize * dragSize || distSqB < dragSize * dragSize)
            {
                w.Sever(c);
            }
        }
    }

    if (Mouse.IsButtonPressed(MouseButton.Middle))
    {
        canvas.PushState();
        canvas.Stroke(Color.Red);
        canvas.DrawCircle(mp, dragSize);
        canvas.PopState();

        foreach (var b in w.bodies)
        {
            var distSq = Vector2.DistanceSquared(b.position, mp);

            if (distSq > dragSize * dragSize)
                continue;

            b.pinned = !b.pinned;
        }
    }


    foreach (var b in w.bodies)
    {
        if (windDir == 0)
            wind = -wind;

        b.Accelerate(new(wind, 0));
    }

    w.Render(canvas);
}

class Rope : IWorldProvider
{
    int Segments;
    float Length;
    public Rope(int segments, float length)
    {
        Segments = segments;
        Length = length;
    }

    public void Populate(List<Body> bodies, List<Constraint> constraints)
    {
        float segmentSize = Length / Segments;
        for (int i = 0; i < Segments + 1; i++)
        {
            bodies.Add(new Body(-Vector2.UnitY * segmentSize * i, i == 0));
        }

        for (int i = 0; i < Segments; i++)
        {
            constraints.Add(new DistanceConstraint(bodies[i], bodies[i + 1]));
        }
    }
}

class Cloth : IWorldProvider
{
    public int width, height, anchorFrequency;
    public float gridSize;

    public Cloth(int width, int height, float gridSize, int anchorFrequency)
    {
        this.width = width;
        this.height = height;
        this.anchorFrequency = anchorFrequency;
        this.gridSize = gridSize;
    }

    public void Populate(List<Body> bodies, List<Constraint> constraints)
    {
        var bodyArray = new Body[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var b = new Body(new Vector2(x, -y) * gridSize, y == 0 && (anchorFrequency is 0 ? false : (x % anchorFrequency == 0)));
                bodyArray[x, y] = b;
                bodies.Add(b);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y + 1 < height)
                {
                    constraints.Add(new DistanceConstraint(bodyArray[x, y], bodyArray[x, y + 1]));
                }

                if (x + 1 < width)
                {
                    constraints.Add(new DistanceConstraint(bodyArray[x, y], bodyArray[x + 1, y]));
                }

                if (y + 1 < height && x + 1 < width)
                {
                    constraints.Add(new RepelConstraint(bodyArray[x, y], bodyArray[x + 1, y + 1]));
                    constraints.Add(new RepelConstraint(bodyArray[x, y + 1], bodyArray[x + 1, y]));
                }
            }
        }
    }
}

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

interface IWorldProvider
{
    void Populate(List<Body> bodies, List<Constraint> constraints);
}

interface IPhysicsObject
{
    void Render(ICanvas canvas);
    void Update(float dt, World world);
}

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