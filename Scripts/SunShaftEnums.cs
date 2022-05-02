// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

namespace SunShaft {
    public enum Resolution { // divide resoltuion by raw value
        Low = 4,
        Normal = 2,
        High = 1
    }

    public enum BlendMode { // raw value -> pass index
        Screen = 0,
        Add = 4
    }

    public enum BackgroundMode {
        Depth = 0,
        Skybox = 1,
        //Color = 2
    }
}