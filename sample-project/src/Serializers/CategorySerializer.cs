using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

public class CategorySerializer : Serializer<CategoryDto, Category, int, AppDbContext>
{
    public CategorySerializer(AppDbContext db) : base(db) { }
}
