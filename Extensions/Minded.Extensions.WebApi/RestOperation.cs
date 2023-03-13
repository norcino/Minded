using System;

namespace Minded.Extensions.WebApi
{
    [Flags]
    public enum RestOperation : int
    {
        Any                     = 0,

        Action                  = 1 << 0,
        ActionWithContent       = 1 << 1,
        ActionWithResultContent = 1 << 2,
        AnyAction               = Action | ActionWithContent | ActionWithResultContent,

        Create                  = 1 << 3,
        CreateWithContent       = 1 << 4,
        AnyCreate               = Create | CreateWithContent,

        Delete                  = 1 << 5,
        GetMany                 = 1 << 6,
        GetSingle               = 1 << 7,

        Patch                   = 1 << 8,
        PatchWithContent        = 1 << 9,
        AnyPatch                = Patch | PatchWithContent,

        Update                  = 1 << 10,
        UpdateWithContent       = 1 << 11,
        AnyUpdate               = Update | UpdateWithContent
    }
}
