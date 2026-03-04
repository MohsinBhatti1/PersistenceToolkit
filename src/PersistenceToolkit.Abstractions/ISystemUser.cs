namespace PersistenceToolkit.Abstractions
{
    public interface ISystemUser
    {
        public int UserId { get; set; }
        public int TenantId { get; set; }
    }
}
