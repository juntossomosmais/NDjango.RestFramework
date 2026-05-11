using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

public class MenuItemSerializer : Serializer<MenuItemDto, MenuItem, int, AppDbContext>
{
    public MenuItemSerializer(AppDbContext db) : base(db) { }
}
