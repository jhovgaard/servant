namespace Servant.Business.Objects.Enums
{
    public enum DeploymentResponseType
    {
        DeploymentRequestReceived = 0,
        CreateDirectory = 1,
        PackageDownload = 2,
        PackageUnzipping = 3,
        Warmup = 4,
        Rollback = 5,
        Deployment = 6
    }
}