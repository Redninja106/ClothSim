using System.Numerics;

class ClothScene : IWorldProvider
{
    public int width, height, anchorFrequency;
    public float gridSize;

    public ClothScene(int width, int height, float gridSize, int anchorFrequency)
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
