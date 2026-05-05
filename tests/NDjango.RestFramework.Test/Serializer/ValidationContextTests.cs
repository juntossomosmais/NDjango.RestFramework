using System;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Serializer;
using NDjango.RestFramework.Test.Support;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NDjango.RestFramework.Test.Serializer;

public class ValidationContextTests
{
    public class IsSet
    {
        [Fact]
        public void IsSet_WhenOperationIsCreate_ShouldReturnTrueForAnyField()
        {
            // Arrange
            var context = new ValidationContext<Guid>(SerializerOperation.Create, default);

            // Act
            var result = context.IsSet(nameof(CustomerDto.Name));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSet_WhenOperationIsUpdate_ShouldReturnTrueForAnyField()
        {
            // Arrange
            var context = new ValidationContext<Guid>(SerializerOperation.Update, Guid.NewGuid());

            // Act
            var result = context.IsSet(nameof(CustomerDto.CNPJ));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSet_WhenOperationIsBulkUpdate_ShouldReturnTrueForAnyField()
        {
            // Arrange
            var context = new ValidationContext<Guid>(SerializerOperation.BulkUpdate, default);

            // Act
            var result = context.IsSet("AnyName");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSet_WhenPartialAndFieldSent_ShouldReturnTrue()
        {
            // Arrange
            var json = JObject.Parse("{ \"name\": \"x\" }");
            var partial = new PartialJsonObject<CustomerDto>(json);
            var context = new ValidationContext<Guid>(SerializerOperation.PartialUpdate, Guid.NewGuid(), partial);

            // Act
            var result = context.IsSet(nameof(CustomerDto.Name));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSet_WhenPartialAndFieldNotSent_ShouldReturnFalse()
        {
            // Arrange
            var json = JObject.Parse("{ \"name\": \"x\" }");
            var partial = new PartialJsonObject<CustomerDto>(json);
            var context = new ValidationContext<Guid>(SerializerOperation.PartialUpdate, Guid.NewGuid(), partial);

            // Act
            var result = context.IsSet(nameof(CustomerDto.CNPJ));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSet_WhenPartialAndFieldExplicitlyNull_ShouldReturnTrue()
        {
            // Arrange
            // The "sent as null" vs "not sent" distinction is the entire reason IsSet exists.
            // Sending {"cnpj": null} must register as IsSet=true.
            var json = JObject.Parse("{ \"cnpj\": null }");
            var partial = new PartialJsonObject<CustomerDto>(json);
            var context = new ValidationContext<Guid>(SerializerOperation.PartialUpdate, Guid.NewGuid(), partial);

            // Act
            var result = context.IsSet(nameof(CustomerDto.CNPJ));

            // Assert
            Assert.True(result);
        }
    }

    public class Construction
    {
        [Fact]
        public void Construct_WhenUpdateWithoutEntityId_ShouldThrow()
        {
            // Arrange & Act
            var ex = Assert.Throws<ArgumentException>(
                () => new ValidationContext<Guid>(SerializerOperation.Update, default));

            // Assert
            Assert.Contains("requires a non-default entityId", ex.Message);
        }

        [Fact]
        public void Construct_WhenPartialUpdateWithoutEntityId_ShouldThrow()
        {
            // Arrange & Act
            var ex = Assert.Throws<ArgumentException>(
                () => new ValidationContext<Guid>(SerializerOperation.PartialUpdate, default));

            // Assert
            Assert.Contains("requires a non-default entityId", ex.Message);
        }

        [Fact]
        public void Construct_WhenCreateWithDefaultEntityId_ShouldNotThrow()
        {
            // Arrange & Act
            var context = new ValidationContext<Guid>(SerializerOperation.Create, default);

            // Assert
            Assert.True(context.IsCreate);
        }

        [Fact]
        public void Construct_WhenBulkUpdateWithDefaultEntityId_ShouldNotThrow()
        {
            // Arrange & Act
            var context = new ValidationContext<Guid>(SerializerOperation.BulkUpdate, default);

            // Assert
            Assert.True(context.IsBulkUpdate);
        }
    }

    public class OperationFlags
    {
        [Fact]
        public void IsCreate_WhenOperationIsCreate_ShouldBeTrue()
        {
            // Arrange & Act
            var context = new ValidationContext<Guid>(SerializerOperation.Create, default);

            // Assert
            Assert.True(context.IsCreate);
            Assert.False(context.IsUpdate);
            Assert.False(context.IsPartialUpdate);
            Assert.False(context.IsBulkUpdate);
            Assert.False(context.IsPartial);
        }

        [Fact]
        public void IsPartialUpdate_WhenOperationIsPartialUpdate_ShouldBeTrueAndAliasIsPartialAlsoTrue()
        {
            // Arrange & Act
            var context = new ValidationContext<Guid>(SerializerOperation.PartialUpdate, Guid.NewGuid());

            // Assert
            Assert.True(context.IsPartialUpdate);
            Assert.True(context.IsPartial);
            Assert.False(context.IsCreate);
            Assert.False(context.IsUpdate);
        }
    }
}
