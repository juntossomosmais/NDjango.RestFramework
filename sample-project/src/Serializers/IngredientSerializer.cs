using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

public class IngredientSerializer : Serializer<IngredientDto, Ingredient, int, AppDbContext>
{
    public IngredientSerializer(AppDbContext db) : base(db) { }
}
