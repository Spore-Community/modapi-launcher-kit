namespace ModAPI.Common.Types
{
    public enum ResultType
    {
        Success = 0,
        InstallerExecuted,  // we have found a custom installer, close Easy installer and execute that one
        UnsupportedFile,
        GalacticAdventuresNotFound,
        UnauthorizedAccess,
        InvalidPath,
        ModNotInstalled,
        NoInstallerFound
    }
}
