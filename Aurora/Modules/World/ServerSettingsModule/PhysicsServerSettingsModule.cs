using Aurora.Framework;
using Nini.Config;
using System;

namespace Aurora.Modules.World.ServerSettingsModule
{
    public class PhysicsServerSettingsModule : INonSharedRegionModule
    {
        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            IServerSettings serverSettings = scene.RequestModuleInterface<IServerSettings>();
            ServerSetting gravitySetting = new ServerSetting
                                               {
                                                   Name = "Gravity",
                                                   Comment = "The forces of gravity that are on this sim",
                                                   Type = "Color4" //All arrays are color4
                                               };
            gravitySetting.OnGetSetting += delegate()
                                               {
                                                   return
                                                       string.Format(
                                                           "<array><real>{0}</real><real>{1}</real><real>{2}</real><real>1.0</real></array>",
                                                           scene.PhysicsScene.GetGravityForce()[0],
                                                           scene.PhysicsScene.GetGravityForce()[1],
                                                           scene.PhysicsScene.GetGravityForce()[2]);
                                               };
            gravitySetting.OnUpdatedSetting += delegate(string value) { };

            serverSettings.RegisterSetting(gravitySetting);
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "PhysicsServerSettingsModules"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }
    }
}