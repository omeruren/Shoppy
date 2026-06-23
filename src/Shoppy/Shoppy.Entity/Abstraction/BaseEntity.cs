namespace Shoppy.Entity.Abstraction;

public abstract class BaseEntity
{
    protected BaseEntity()
    {
        Id = Guid.CreateVersion7();
    }
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
