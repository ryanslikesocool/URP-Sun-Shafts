// Made with love by Ryan Boyer http://ryanjboyer.com <3

namespace SunShaft
{
    public enum SunShaftResolution
    {
        Low = 4,
        Normal = 2,
        High = 1
    }

    public enum SunShaftBlendMode // value == pass index
    {
        Screen = 0,
        Add = 4
    }

    public enum SunShaftRenderMode
    {
        //Depth = 0,
        Skybox = 1,
        Color = 2
    }
}