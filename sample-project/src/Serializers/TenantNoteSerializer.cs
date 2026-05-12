using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

public class TenantNoteSerializer : Serializer<TenantNoteDto, TenantNote, int, AppDbContext>
{
    public TenantNoteSerializer(AppDbContext db) : base(db) { }
}
