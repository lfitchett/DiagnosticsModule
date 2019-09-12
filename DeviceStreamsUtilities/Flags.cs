using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceStreamsUtilities
{
    public enum Flag
    {
        Close,
        SendFile,
        ListFiles,
        Response,
        MultiPartStart,
        MultiPartEnd,
        FileStart,
        FilePart,
        FileEnd,
    }
}
