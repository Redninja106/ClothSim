using System.Numerics;

class RopeScene : IWorldProvider
{
    int Segments;
    float Length;
    public RopeScene(int segments, float length)
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
