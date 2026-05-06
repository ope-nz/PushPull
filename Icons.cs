using System;
using System.Drawing;
using System.IO;

namespace PushPull
{
    static class Icons
    {
        const string RefreshB64 =
            "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAA8ElEQVR4AaTSi1HDMBAE0EAl0AlUAlQCVAKd" +
            "QCpJOkn2ybmJYmvizMSj1X28t/qcHjd3fmsCT2v6IwFFXyk8BLsOcgnb+GtzprnAS3KKPmN/g+8T/mPl/H" +
            "uPjwebkQDyc0gfgVUJ8V8TGz+mwlxAAeK+CJ21gy6c3LnAlF3O7kWWsB0Cf3EEpBGQ7ayH3M0CI9GW64+g" +
            "NT0aYW0qAS0p1HnLXtUogTlpm0Q7Y2wNglqoU5Vb3IF++6llRVYAHpFd6gBOQ+3Aan6Am/YCrUgI3sKW88" +
            "BwEk6jF1BICKwoVvAQKisX93KUwGX2HBE7RwPvCAAA//8lOKZbAAAABklEQVQDANdoLyEVUFjlAAAAAElF" +
            "TkSuQmCC";

        const string PushB64 =
            "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAiElEQVR4AbSS0QmAMAxEg4u4mk6iTqKjOYp3" +
            "H1IpvVxBGhKw2Pd6VKf4WcMEC4LtGNtZgg20lSjBBfjAWIkSgA2ebiWZIFBW4gRwRCrpEQRqxjS7R3CC5G" +
            "flxTINlqWd4AuvBStPmcDC1CgBo76xmycT5ijBjZf8B1IYe0IJmhdGoB4lqPfJ9QMAAP//wvomrwAAAAZJ" +
            "REFUAwAKdxUh870DlQAAAABJRU5ErkJggg==";

        const string PushPlusB64 =
            "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAqElEQVR4AaySUQ5AMBBEGydxFUdxUm7CUexr" +
            "dDOp6YcgRuvtzlhiKh+P3wO2GGgO6elY1nUCjEgN3CNlaWajAWeAJcTZDI5RT2kA0Bkco7eqDwA6g2P0Fh" +
            "dAoTeM2DAAA+ID6qr7WhtNQPGIbiZZ79UxO0HfuEeAY4HLI8A1OlbNXPpX0LF5Mj2Owas0gCdh4p1ZaXAM" +
            "ntIAPhh/YjPT5Bg8pQEJ32wuAAAA//9aruLWAAAABklEQVQDALrcNCG3feptAAAAAElFTkSuQmCC";

        const string PullB64 =
            "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAhklEQVR4AbyQUQ5AMBBExUk5CU7C0biJeR+k" +
            "TXZ1REJ2orTzOpm++/j8BpgVFOlVj5NglWWSwmkBMA9ybtLrBKV5FCCcLIFlhhgBiHrFTm/GjCLAzoZ0SM2" +
            "JABS2yEnzpNEynwjAaYwWJAPYkCeABWkBSshVLv9uOQAO0wnlsq7kAipT+XECAAD//yR4BDAAAAAGSURB" +
            "VQMA7sQXIV+O9zEAAAAASUVORK5CYII=";

        static Image Load(string b64)
        {
            byte[] bytes = Convert.FromBase64String(b64);
            using (var ms = new MemoryStream(bytes))
                return new Bitmap(ms);
        }

        public static Image Refresh  { get { return Load(RefreshB64);  } }
        public static Image Push     { get { return Load(PushB64);     } }
        public static Image PushPlus { get { return Load(PushPlusB64); } }
        public static Image Pull     { get { return Load(PullB64);     } }
    }
}
