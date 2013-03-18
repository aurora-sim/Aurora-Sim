namespace Aurora.Framework.Physics
{
    public interface IPhysicsPlugin
    {
        bool Init();
        PhysicsScene GetScene();
        string GetName();
        void Dispose();
    }
}
