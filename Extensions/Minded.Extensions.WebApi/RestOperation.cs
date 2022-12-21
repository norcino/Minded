namespace Minded.Extensions.WebApi
{
    public enum RestOperation
    {
        Action = 0,
        ActionWithContent = 0,
        Create = 1,
        CreateWithContent = 2,
        Delete = 3,
        GetMany = 4,
        GetSingle = 5,
        HeadMany = 6,
        HeadSingle = 7,
        Patch = 8,
        PatchWithContent = 9,
        Update = 10,
        UpdateWithContent = 11
    }
}
