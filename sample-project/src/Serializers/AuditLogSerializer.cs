using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

public class AuditLogSerializer : Serializer<AuditLogDto, AuditLog, int, AppDbContext>
{
    public AuditLogSerializer(AppDbContext db) : base(db) { }
}
