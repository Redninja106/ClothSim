using ImGuiNET;
using SimulationFramework;
using SimulationFramework.Desktop;
using SimulationFramework.Drawing;
using System.Numerics;

internal class Program
{
    public static float cx = 0, cy = 0, cz = 0;
    public static World w = null!;
    public static float windMin = 0, windMax = 1, windRate = 1;
    public static int windDir = 0;
    public static bool windEnabled = false;
    public static int width = 31, height = 20, anchorFreq = 3;
    public static float steps = 100, separation = .125f;
    public static int scene = 0;
    public static bool windowOpen = true;
    public static int ropeSegments = 2;
    public static float ropeLength = .75f;
    public static float dragSize = .1f;
    public static bool strongGrab = true;

    private static void Main(string[] args)
    {
        Simulation.Create(Init, Render).RunDesktop();
    }

    private static void Init(AppConfig config)
    {
        config.Title = "Verlet Integration";
        CreateWorld();
    }

    private static void CreateWorld()
    {
        IWorldProvider provider = scene switch
        {
            0 => new ClothScene(width, height, separation, anchorFreq),
            1 => new RopeScene(ropeSegments, ropeLength)
        };
        var prevGravity = w?.Gravity;
        w = new(1f / steps, provider);
        if (prevGravity is not null)
            w.Gravity = prevGravity.Value;
    }

    private static void Render(ICanvas canvas)
    {
        Layout();
        
        w.Update();

        canvas.Clear(Color.Black);
        canvas.Translate(canvas.Width / 2, canvas.Height / 2);
        canvas.Scale(canvas.Height / 5);
        canvas.Scale(1, -1);

        canvas.Scale(MathF.Pow(1.1f, cz));
        canvas.Translate(-cx, -cy);

        HandleInput(canvas);

        float wp = MathF.Sin(Time.TotalTime * windRate);
        var wind = MathHelper.Lerp(windMin, windMax, MathHelper.Normalize(wp));

        if (!windEnabled)
            wind = 0;

        foreach (var b in w.bodies)
        {
            if (windDir == 0)
                wind = -wind;

            b.Accelerate(new(wind, 0));
        }

        w.Render(canvas);
    }

    private static void Layout()
    {
        if (Keyboard.IsKeyPressed(Key.F1))
            windowOpen = !windowOpen;

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
                ImGui.DragFloatRange2("Wind Min/Max", ref windMin, ref windMax, 0.01f, 0, 10);
                ImGui.Combo("Wind Direction", ref windDir, "Left\0Right");
                ImGui.SliderFloat("Wind Frequency", ref windRate, 0.01f, 10f);
            }
        }

    }

    private static void HandleInput(ICanvas canvas)
    {
        if (Keyboard.IsKeyPressed(Key.R))
            CreateWorld();

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

    }
}