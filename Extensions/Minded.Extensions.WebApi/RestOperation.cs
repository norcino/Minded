using System;

namespace Minded.Extensions.WebApi
{
    [Flags]
    public enum RestOperation : int
    {
        Any                 = 0,

        Action              = 1 << 0,
        ActionWithContent   = 1 << 1,
        AnyAction           = Action | ActionWithContent,

        Create              = 1 << 2,
        CreateWithContent   = 1 << 3,
        AnyCreate           = Create | CreateWithContent,

        Delete              = 1 << 4,
        GetMany             = 1 << 5,
        GetSingle           = 1 << 6,

        Patch               = 1 << 7,
        PatchWithContent    = 1 << 8,
        AnyPatch            = Patch | PatchWithContent,

        Update              = 1 << 9,
        UpdateWithContent   = 1 << 10,
        AnyUpdate           = Update | UpdateWithContent
    }
}
