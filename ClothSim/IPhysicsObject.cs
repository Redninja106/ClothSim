using SimulationFramework.Drawing;

interface IPhysicsObject
{
    void Render(ICanvas canvas);
    void Update(float dt, World world);
}
