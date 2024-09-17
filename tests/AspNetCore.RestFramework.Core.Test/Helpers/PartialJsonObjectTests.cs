using System.Collections.Generic;
using System.Linq;
using AspNetCore.RestFramework.Core.Helpers;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AspNetCore.RestFramework.Core.Test.Helpers;

public class PartialJsonObjectTests
{
    public class BasicContract
    {
        public static IEnumerable<object[]> listOKStubs => FakeData.ListOK.Select(item => (new object[] { item }));

        [Fact(DisplayName = "Should populate JSON and instance")]
        public void ShouldPopulateJsonAndInstance()
        {
            // act
            var jsonObj = JsonConvert.SerializeObject(FakeData.OK);
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj);

            // assert
            obj.Instance.Should().NotBeNull();
            obj.JsonObject.Should().NotBeNull();
        }

        [Theory(DisplayName = "Should populate original JSON")]
        [MemberData(nameof(listOKStubs))]
        public void ShouldPopulateOriginalJSON(TestModel testObj)
        {
            // act
            var jsonObj = JsonConvert.SerializeObject(testObj);
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj);

            // assert
            obj.JsonObject.Should().BeEquivalentTo(JToken.Parse(jsonObj));
        }

        [Theory(DisplayName = "Should create a instance object from the original JSON")]
        [MemberData(nameof(listOKStubs))]
        public void ShouldCreateInstanceFromOriginalJSON(TestModel testObj)
        {
            // act
            var jsonObj = JsonConvert.SerializeObject(testObj);
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj);

            // assert
            obj.Instance.Should().BeEquivalentTo(testObj);
        }

        [Fact(DisplayName = "Field should be represented as set if it is populated")]
        public void ShouldRepresentFieldAsSetIfItIsPopulated()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            jsonObj["Description"].Parent.Remove();
            jsonObj["ChildItemsList"].Parent.Remove();
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            TestModel instance = obj;

            // assert
            instance.Should().Be(obj.Instance);

            obj.IsSet(inst => inst.Id).Should().BeTrue();
            obj.IsSet(inst => inst.Name).Should().BeTrue();
            obj.IsSet(inst => inst.Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj).Should().BeTrue();
        }

        [Fact(DisplayName = "Should represent field as set if it is populated and receive string as parameter")]
        public void ShouldFieldRepresentAsSetWhenIsPopulatedAndReceiveStringAsParam()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet("Id").Should().BeTrue();

            obj.IsSet("childItemsList").Should().BeTrue();
            obj.IsSet("ChildItemsList.0").Should().BeTrue();
            obj.IsSet("childItemsList.0.Id").Should().BeTrue();
            obj.IsSet("ChildItemsList.$last").Should().BeTrue();
            obj.IsSet("ChildItemsList.$last.Id").Should().BeTrue();

            obj.IsSet("ChildItemsArray").Should().BeTrue();
            obj.IsSet("childItemsArray.0").Should().BeTrue();
            obj.IsSet("ChildItemsArray.0.id").Should().BeTrue();
            obj.IsSet("ChildItemsArray.$last").Should().BeTrue();
            obj.IsSet("ChildItemsArray.$last.Id").Should().BeTrue();

            obj.IsSet("ChildMatrixArray").Should().BeTrue();
            obj.IsSet("ChildMatrixArray.1").Should().BeTrue();
            obj.IsSet("ChildMatrixArray.1.2").Should().BeTrue();
            obj.IsSet("ChildMatrixArray.1.2.Id").Should().BeTrue();
            obj.IsSet("ChildMatrixArray.$last").Should().BeTrue();
            obj.IsSet("ChildMatrixArray.1.$last").Should().BeTrue();
            obj.IsSet("ChildMatrixArray.1.$last.id").Should().BeTrue();

            obj.IsSet("ChildMatrixList").Should().BeTrue();
            obj.IsSet("ChildMatrixList.0").Should().BeTrue();
            obj.IsSet("ChildMatrixList.0.0").Should().BeTrue();
            obj.IsSet("ChildMatrixList.0.0.Id").Should().BeTrue();
            obj.IsSet("ChildMatrixList.$last").Should().BeTrue();
            obj.IsSet("ChildMatrixList.0.$last").Should().BeTrue();
            obj.IsSet("ChildMatrixList.0.$last.id").Should().BeTrue();

            obj.IsSet("SubObj").Should().BeTrue();
            obj.IsSet("SubObj.SubObj").Should().BeTrue();
            obj.IsSet("SubObj.SubObj.ChildItemsList").Should().BeTrue();
            obj.IsSet("SubObj.SubObj.ChildItemsList.1.Id").Should().BeTrue();
        }

        [Fact(DisplayName = "Should represent field as not set if it is not populated and receive string as parameter")]
        public void ShouldFieldRepresentAsNotSetWhenIsNotPopulatedAndReceiveStringAsParam()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet("NonExistingProp").Should().BeFalse();
            obj.IsSet("ChildItemsList.2").Should().BeFalse();
            obj.IsSet("ChildItemsList.$first").Should().BeFalse();
            obj.IsSet("ChildItemsList.$Last").Should().BeFalse();
        }

        [Fact(DisplayName = "Should represent field as set if it is populated and receive multiple string parameters")]
        public void ShouldFieldRepresentAsSetWhenIsPopulatedAndReceiveMultipleStringParams()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet("ChildItemsList", "0").Should().BeTrue();
            obj.IsSet("childItemsList", "0", "Id").Should().BeTrue();
            obj.IsSet("ChildItemsList", "$last").Should().BeTrue();
            obj.IsSet("ChildItemsList", "$last", "id").Should().BeTrue();

            obj.IsSet("ChildItemsArray").Should().BeTrue();
            obj.IsSet("childItemsArray", "0").Should().BeTrue();
            obj.IsSet("ChildItemsArray", "0", "id").Should().BeTrue();
            obj.IsSet("ChildItemsArray", "$last").Should().BeTrue();
            obj.IsSet("ChildItemsArray", "$last", "id").Should().BeTrue();

            obj.IsSet("ChildMatrixArray").Should().BeTrue();
            obj.IsSet("ChildMatrixArray", "1").Should().BeTrue();
            obj.IsSet("ChildMatrixArray", "1", "2").Should().BeTrue();
            obj.IsSet("ChildMatrixArray", "1", "2", "Id").Should().BeTrue();
            obj.IsSet("ChildMatrixArray", "$last").Should().BeTrue();
            obj.IsSet("ChildMatrixArray", "1", "$last").Should().BeTrue();
            obj.IsSet("ChildMatrixArray", "1", "$last", "Id").Should().BeTrue();

            obj.IsSet("ChildMatrixList").Should().BeTrue();
            obj.IsSet("ChildMatrixList", "0").Should().BeTrue();
            obj.IsSet("ChildMatrixList", "0", "0").Should().BeTrue();
            obj.IsSet("ChildMatrixList", "0", "0", "Id").Should().BeTrue();
            obj.IsSet("ChildMatrixList", "$last").Should().BeTrue();
            obj.IsSet("ChildMatrixList", "0", "$last").Should().BeTrue();
            obj.IsSet("ChildMatrixList", "0", "$last", "Id").Should().BeTrue();

            obj.IsSet("SubObj").Should().BeTrue();
            obj.IsSet("SubObj", "SubObj").Should().BeTrue();
            obj.IsSet("SubObj", "SubObj", "ChildItemsList").Should().BeTrue();
            obj.IsSet("SubObj", "SubObj", "ChildItemsList", "1", "Id").Should().BeTrue();
        }

        [Fact(DisplayName =
            "Should represent field as not set if it is not populated and receive multiple string parameter")]
        public void ShouldFieldRepresentAsNotSetWhenIsNotPopulatedAndReceiveMultipleStringParameters()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet("ChildItemsList", "NonExistingProp").Should().BeFalse();
            obj.IsSet("ChildItemsList", "2").Should().BeFalse();
            obj.IsSet("ChildItemsList", "$first").Should().BeFalse();
            obj.IsSet("ChildItemsList", "$Last").Should().BeFalse();
        }

        [Fact(DisplayName = "All fields should be represented as set")]
        public void ShouldAllFieldsReturnTrueToIsSet()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.Id).Should().BeTrue();
            obj.IsSet(inst => inst.Name).Should().BeTrue();
            obj.IsSet(inst => inst.Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields should be represented as not set")]
        public void ShouldAllFieldsReturnFalseToIsSet()
        {
            // arrange
            string json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.Id).Should().BeFalse();
            obj.IsSet(inst => inst.Name).Should().BeFalse();
            obj.IsSet(inst => inst.Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj).Should().BeFalse();
        }

        private int _index1 = 2;
        private const int _index2 = 3;
        private static int _index3 = 3;

        [Fact(DisplayName = "Field parse expression to path string")]
        public void ShouldParseExpressionToPathString()
        {
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.First()).Should()
                .Be("ChildItemsList.0");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.Last()).Should()
                .Be("ChildItemsList.$last");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList[1]).Should().Be("ChildItemsList.1");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray[1]).Should()
                .Be("ChildItemsArray.1");

            for (var index = 0; index < 3; index++)
            {
                PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(index)).Should()
                    .Be($"ChildItemsList.{index}");
                PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList[index]).Should()
                    .Be($"ChildItemsList.{index}");
                PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray[index]).Should()
                    .Be($"ChildItemsArray.{index}");
            }

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(_index1)).Should()
                .Be($"ChildItemsList.{_index1}");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(_index2)).Should()
                .Be($"ChildItemsList.{_index2}");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(_index3)).Should()
                .Be($"ChildItemsList.{_index3}");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray.ElementAt(1)).Should()
                .Be("ChildItemsArray.1");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList[1].Name).Should()
                .Be("ChildItemsList.1.Name");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray[1].Name).Should()
                .Be("ChildItemsArray.1.Name");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(1).Name).Should()
                .Be("ChildItemsList.1.Name");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray.ElementAt(1).Name).Should()
                .Be("ChildItemsArray.1.Name");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray[1]).Should()
                .Be("ChildMatrixArray.1");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList[1]).Should()
                .Be("ChildMatrixList.1");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray[1][2]).Should()
                .Be("ChildMatrixArray.1.2");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList[1][2]).Should()
                .Be("ChildMatrixList.1.2");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray.ElementAt(1).ElementAt(2)).Should()
                .Be("ChildMatrixArray.1.2");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList.ElementAt(1).ElementAt(2)).Should()
                .Be("ChildMatrixList.1.2");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray.ElementAt(1).ElementAt(2).Name)
                .Should().Be("ChildMatrixArray.1.2.Name");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList.ElementAt(1).ElementAt(2).Name)
                .Should()
                .Be("ChildMatrixList.1.2.Name");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray[1].ElementAt(2).Name).Should()
                .Be("ChildMatrixArray.1.2.Name");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList[1].ElementAt(2).Name).Should()
                .Be("ChildMatrixList.1.2.Name");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray.ElementAt(1)[2].Name).Should()
                .Be("ChildMatrixArray.1.2.Name");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList.ElementAt(1)[2].Name).Should()
                .Be("ChildMatrixList.1.2.Name");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.SubObj.SubObj.ChildItemsList.ElementAt(1)).Should()
                .Be("SubObj.SubObj.ChildItemsList.1");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.SubObj.SubObj.ChildItemsList[1]).Should()
                .Be("SubObj.SubObj.ChildItemsList.1");

            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.SubObj.SubObj.ChildItemsList.ElementAt(1).Name)
                .Should()
                .Be("SubObj.SubObj.ChildItemsList.1.Name");
            PartialJsonObject<TestModel>.GetMemberPath(inst => inst.SubObj.SubObj.ChildItemsList[1].Name).Should()
                .Be("SubObj.SubObj.ChildItemsList.1.Name");
        }

        [Fact(DisplayName = "Field should be represented as set if it is added manually")]
        public void ShouldRepresentFieldAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.Name).Should().BeFalse();
            obj.IsSet(inst => inst.Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeFalse();

            // act
            obj.Add(inst => inst.Name);
            obj.Add(inst => inst.Description);
            obj.Add(inst => inst.ChildItemsList);

            // assert
            obj.IsSet(inst => inst.Id).Should().BeFalse();
            obj.IsSet(inst => inst.Name).Should().BeTrue();
            obj.IsSet(inst => inst.Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj).Should().BeFalse();
        }

        [Fact(DisplayName = "Field should be represented as not set if it is removed manually")]
        public void ShouldRepresentFieldAsSetIfItIsRemovedManually()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeTrue();

            // act
            obj.Remove(inst => inst.Id);
            obj.Remove(inst => inst.ChildMatrixArray);

            // assert
            obj.IsSet(inst => inst.Id).Should().BeFalse();
            obj.IsSet(inst => inst.Name).Should().BeTrue();
            obj.IsSet(inst => inst.Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj).Should().BeTrue();
        }

        [Fact(DisplayName = "Should get JSON Path")]
        public void ShouldGetJSONPath()
        {
            // arrange
            var testObj = FakeData.OK;
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.GetJSONPath(x => x.Id).Should().Be("Id");
            obj.GetJSONPath(x => x.ChildItemsList[0].Id).Should().Be("ChildItemsList[0].Id");
            obj.GetJSONPath(x => x.ChildItemsArray[0].Id).Should().Be("ChildItemsArray[0].Id");
            obj.GetJSONPath(x => x.ChildMatrixArray[0][0].Id).Should().Be("ChildMatrixArray[0][0].Id");
            obj.GetJSONPath(x => x.ChildMatrixList[0][0].Id).Should().Be("ChildMatrixList[0][0].Id");
            obj.GetJSONPath(x => x.SubObj.ChildItemsList[0].Name).Should().Be("SubObj.ChildItemsList[0].Name");
        }

        [Fact(DisplayName = "Should reset the add configurations if clear the cache")]
        public void ShouldResetAddConfigurationsIfClearCache()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.Name).Should().BeFalse();
            obj.IsSet(inst => inst.Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeFalse();

            // act
            obj.Add(inst => inst.Name);
            obj.Add(inst => inst.Description);
            obj.Add(inst => inst.ChildItemsList);

            // assert
            obj.IsSet(inst => inst.Name).Should().BeTrue();
            obj.IsSet(inst => inst.Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();

            // act
            obj.ClearCache();

            // assert
            obj.IsSet(inst => inst.Name).Should().BeFalse();
            obj.IsSet(inst => inst.Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeFalse();
        }

        [Fact(DisplayName = "Should reset the remove configurations if clear the cache")]
        public void ShouldResetRemoveConfigurationsIfClearCache()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.Name).Should().BeTrue();
            obj.IsSet(inst => inst.Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();

            // act
            obj.Remove(inst => inst.Name);
            obj.Remove(inst => inst.Description);
            obj.Remove(inst => inst.ChildItemsList);

            // assert
            obj.IsSet(inst => inst.Name).Should().BeFalse();
            obj.IsSet(inst => inst.Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeFalse();

            // act
            obj.ClearCache();

            // assert
            obj.IsSet(inst => inst.Name).Should().BeTrue();
            obj.IsSet(inst => inst.Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();
        }

        [Fact(DisplayName =
            "Should object properties be equal and the object reference be different when calling ToObject")]
        public void ShouldObjectPropretiesBeEqualAndTheReferenceBeDifferentWhenCallingToObject()
        {
            // arrange
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(FakeData.OK));
            var obj1 = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // act
            var obj2 = obj1.ToObject();

            // assert
            obj2.Should().NotBeSameAs(obj1.Instance);
            obj2.Should().BeEquivalentTo(obj1.Instance);
        }

        [Fact(DisplayName = "Should get property value if it was populated when calling GetIfSet")]
        public void ShouldGetPropertyValueIfItWasPopulatedWhenCallingGetIfSet()
        {
            // arrange
            var testObj = FakeData.OK;
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // act
            var nameProp = obj.GetIfSet(x => x.Name, "default value");
            var childItemListItemIdProp = obj.GetIfSet(x => x.ChildItemsList[0].Id, 10000);
            var childItemArrayItemIdProp = obj.GetIfSet(x => x.ChildItemsList[1].Id, 10001);
            var childItemMatrixItemNameProp = obj.GetIfSet(x => x.ChildMatrixList[0][0].Name, "default value");
            var childMatrixListItemDescriptionProp =
                obj.GetIfSet(x => x.ChildMatrixList[0][0].Description, "default value");
            var subObjChildListItemValueProp = obj.GetIfSet(x => x.SubObj.ChildItemsList[0].Value, 10002);

            // assert
            nameProp.Should().Be(obj.Instance.Name);
            childItemListItemIdProp.Should().Be(1);
            childItemArrayItemIdProp.Should().Be(2);
            childItemMatrixItemNameProp.Should().Be("Child 1");
            childMatrixListItemDescriptionProp.Should().Be("Description 1");
            subObjChildListItemValueProp.Should().Be(10);
        }

        [Fact(DisplayName = "Should get the default value if property was not populated when calling GetIfSet")]
        public void ShouldGetDefaultValueIfDefaultValueIfPropertyWasNotPopulatedCallingGetIfSet()
        {
            // arrange
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>("{}");

            // act
            var nameProp = obj.GetIfSet(x => x.Name, "default value");
            var childItemListItemIdProp = obj.GetIfSet(x => x.ChildItemsList[0].Id, 10000);
            var childItemArrayItemIdProp = obj.GetIfSet(x => x.ChildItemsList[1].Id, 10001);
            var childItemMatrixItemNameProp = obj.GetIfSet(x => x.ChildMatrixList[0][0].Name, "default value");
            var childMatrixListItemDescriptionProp =
                obj.GetIfSet(x => x.ChildMatrixList[0][0].Description, "default value");
            var subObjChildListItemValueProp = obj.GetIfSet(x => x.SubObj.ChildItemsList[0].Value, 10002);

            // assert
            nameProp.Should().Be("default value");
            childItemListItemIdProp.Should().Be(10000);
            childItemArrayItemIdProp.Should().Be(10001);
            childItemMatrixItemNameProp.Should().Be("default value");
            childMatrixListItemDescriptionProp.Should().Be("default value");
            subObjChildListItemValueProp.Should().Be(10002);
        }
    }

    public class Array
    {
        [Fact(DisplayName = "Field in array should be represented as set if it is populated")]
        public void ShouldRepresentFieldInArrayAsSetIfItIsPopulated()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            jsonObj["ChildItemsArray"][0]["Name"].Parent.Remove();
            jsonObj["ChildItemsArray"][0]["Value"].Parent.Remove();
            jsonObj["ChildItemsArray"][1]["Description"].Parent.Remove();
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in array should be represented as set")]
        public void ShouldAllFieldsInArrayReturnTrueToIsSet()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Value).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in array should be represented as not set")]
        public void ShouldAllFieldsInArrayReturnFalseToIsSet()
        {
            // arrange
            string json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in array should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInArrayAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.ChildItemsArray[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Description).Should().BeFalse();

            // act
            obj.Add(inst => inst.ChildItemsArray[0].Name);
            obj.Add(inst => inst.ChildItemsArray[1].Description);

            // assert
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in array should be represented as not set if it is removed manually")]
        public void ShouldRepresentFieldInArrayAsSetIfItIsRemovedManually()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildItemsArray[0].Value).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Description).Should().BeTrue();

            // act
            obj.Remove(inst => inst.ChildItemsArray[0].Value);
            obj.Remove(inst => inst.ChildItemsArray[1].Description);

            // assert
            obj.IsSet(inst => inst.ChildItemsArray).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsArray[1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsArray[1].Value).Should().BeTrue();
        }
    }

    public class ArrayMatrix
    {
        [Fact(DisplayName = "Field in matrix array should be represented as set if it is populated")]
        public void ShouldRepresentFieldInMatrixArrayAsSetIfItIsPopulated()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            jsonObj["ChildMatrixArray"][0][0]["Value"].Parent.Remove();
            jsonObj["ChildMatrixArray"][0][1]["Description"].Parent.Remove();
            jsonObj["ChildMatrixArray"][1][0]["Description"].Parent.Remove();
            jsonObj["ChildMatrixArray"][1][1]["Name"].Parent.Remove();
            jsonObj["ChildMatrixArray"][1][2]["Name"].Parent.Remove();
            jsonObj["ChildMatrixArray"][1][2]["Value"].Parent.Remove();
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "All fields in matrix array should be represented as set")]
        public void ShouldAllFieldsInMatrixArrayReturnTrueToIsSet()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in matrix array should be represented as not set")]
        public void ShouldAllFieldsInMatrixArrayReturnFalseToIsSet()
        {
            // arrange
            string json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in matrix array should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInMatrixArrayAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name).Should().BeFalse();

            obj.Add(inst => inst.ChildMatrixArray[0][0].Description);
            obj.Add(inst => inst.ChildMatrixArray[0][1].Id);
            obj.Add(inst => inst.ChildMatrixArray[1][0].Name);
            obj.Add(inst => inst.ChildMatrixArray[1][1].Description);
            obj.Add(inst => inst.ChildMatrixArray[1][2].Name);

            // assert
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value).Should().BeFalse();

            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in matrix array should be represented as not set if it is removed manually")]
        public void ShouldRepresentFieldInMatrixArrayAsSetIfItIsRemovedManually()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name).Should().BeTrue();

            obj.Remove(inst => inst.ChildMatrixArray[0][0].Description);
            obj.Remove(inst => inst.ChildMatrixArray[0][1].Id);
            obj.Remove(inst => inst.ChildMatrixArray[1][0].Name);
            obj.Remove(inst => inst.ChildMatrixArray[1][1].Description);
            obj.Remove(inst => inst.ChildMatrixArray[1][2].Name);

            // assert
            obj.IsSet(inst => inst.ChildMatrixArray).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value).Should().BeTrue();

            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value).Should().BeTrue();
        }
    }

    public class List
    {
        [Fact(DisplayName = "Field in list should be represented as set if it is populated")]
        public void ShouldRepresentFieldInListAsSetIfItIsPopulated()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            jsonObj["ChildItemsList"][0]["Name"].Parent.Remove();
            jsonObj["ChildItemsList"][1]["Description"].Parent.Remove();
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Value).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in list should be represented as set")]
        public void ShouldAllFieldsInListReturnTrueToIsSet()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Value).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in list should be represented as not set")]
        public void ShouldAllFieldsInListReturnFalseToIsSet()
        {
            // arrange
            string json = "{}";

            // assert
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            obj.IsSet(inst => inst.ChildItemsList).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in list should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInListAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.ChildItemsList[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Description).Should().BeFalse();

            // act
            obj.Add(inst => inst.ChildItemsList[0].Value);
            obj.Add(inst => inst.ChildItemsList[1].Description);

            // assert
            obj.IsSet(inst => inst.ChildItemsList).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Value).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in list should be represented as not set if it is removed manually")]
        public void ShouldRepresentFieldInListAsSetIfItIsRemovedManually()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildItemsList[0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Id).Should().BeTrue();

            // act
            obj.Remove(inst => inst.ChildItemsList[0].Id);
            obj.Remove(inst => inst.ChildItemsList[0].Name);
            obj.Remove(inst => inst.ChildItemsList[1].Id);

            // assert
            obj.IsSet(inst => inst.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[0].Value).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildItemsList[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildItemsList[1].Value).Should().BeTrue();
        }
    }

    public class ListMatrix
    {
        [Fact(DisplayName = "Field in matrix list should be represented as set if it is populated")]
        public void ShouldRepresentFieldInMatrixListAsSetIfItIsPopulated()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            jsonObj["ChildMatrixList"][0][0]["Name"].Parent.Remove();
            jsonObj["ChildMatrixList"][0][0]["Description"].Parent.Remove();
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildMatrixList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in matrix list should be represented as set")]
        public void ShouldAllFieldsInMatrixListReturnTrueToIsSet()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildMatrixList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in matrix list should be represented as not set")]
        public void ShouldAllFieldsInMatrixListReturnFalseToIsSet()
        {
            // arrange
            string json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.ChildMatrixList).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in list matrix should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInListMatrixAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Name).Should().BeFalse();

            // act
            obj.Add(inst => inst.ChildMatrixList[0][0].Id);
            obj.Add(inst => inst.ChildMatrixList[0][0].Name);

            // assert
            obj.IsSet(inst => inst.ChildMatrixList).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in list matrix should be represented as not set if it is removed manually")]
        public void ShouldRepresentFieldInListMatrixAsSetIfItIsRemovedManually()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Value).Should().BeTrue();

            // act
            obj.Remove(inst => inst.ChildMatrixList[0][0].Description);
            obj.Remove(inst => inst.ChildMatrixList[0][0].Value);

            // assert
            obj.IsSet(inst => inst.ChildMatrixList).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.ChildMatrixList[0][0].Value).Should().BeFalse();
        }
    }

    public class NestedObj
    {
        [Fact(DisplayName = "Field in nested object should be represented as set if it is populated")]
        public void ShouldRepresentFieldInNestedObjectAsSetIfItIsPopulated()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            jsonObj["SubObj"]["Name"].Parent.Remove();
            jsonObj["SubObj"]["Description"].Parent.Remove();
            jsonObj["SubObj"]["ChildItemsList"][0]["Name"].Parent.Remove();
            jsonObj["SubObj"]["ChildItemsList"][0]["Value"].Parent.Remove();
            jsonObj["SubObj"]["ChildItemsList"][1]["Description"].Parent.Remove();
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.SubObj).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Id).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Name).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in nested object should be represented as set")]
        public void ShouldAllFieldsInNestedObjectReturnTrueToIsSet()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.SubObj).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Id).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Description).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "All fields in nested object should be represented as not set")]
        public void ShouldAllFieldsInNestedObjectReturnFalseToIsSet()
        {
            // arrange
            string json = "{}";

            // assert
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.SubObj).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.Id).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.Name).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value).Should().BeFalse();
        }

        [Fact(DisplayName = "Field in nested object should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInNestedObjectAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            obj.IsSet(inst => inst.SubObj.Name).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value).Should().BeFalse();

            // act
            obj.Add(inst => inst.SubObj.Name);
            obj.Add(inst => inst.SubObj.ChildItemsList[0].Name);
            obj.Add(inst => inst.SubObj.ChildItemsList[1].Value);

            // assert
            obj.IsSet(inst => inst.SubObj).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.Id).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value).Should().BeTrue();
        }

        [Fact(DisplayName = "Field in nested object should be represented as not set if it is removed manually")]
        public void ShouldRepresentFieldInNestedObjectAsSetIfItIsRemovedManually()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            obj.IsSet(inst => inst.SubObj.Description).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id).Should().BeTrue();

            // act
            obj.Remove(inst => inst.SubObj.Description);
            obj.Remove(inst => inst.SubObj.ChildItemsList[0].Id);

            // assert
            obj.IsSet(inst => inst.SubObj).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Id).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.Description).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id).Should().BeFalse();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description).Should().BeTrue();
            obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value).Should().BeTrue();
        }
    }
}

public class FakeData
{
    public static TestModel OK
    {
        get
        {
            return new TestModel
            {
                Id = 1,
                Name = "Class Name",
                Description = "Class Description",
                ChildItemsList = FakeData.ChildItems,
                ChildItemsArray = FakeData.ChildItems.ToArray(),
                ChildMatrixList = new List<List<ChildTestModel>>
                {
                    new List<ChildTestModel>
                    {
                        new ChildTestModel
                        {
                            Id = 1,
                            Name = "Child 1",
                            Description = "Description 1",
                            Value = 10
                        }
                    }
                },
                ChildMatrixArray = FakeData.ChildMatrix,
                SubObj = FakeData.SubObj,
            };
        }
    }

    public static List<TestModel> ListOK
    {
        get
        {
            return new List<TestModel>
            {
                new TestModel
                {
                    Id = 1,
                    Name = "Class Name",
                    Description = "Class Description"
                },
                new TestModel
                {
                    Id = 2,
                    Name = "Class Name"
                },
                new TestModel
                {
                    Id = 3,
                    Name = "Class Name",
                    Description = null
                },
                new TestModel
                {
                    Id = 4,
                    Name = "Class Name",
                    Description = string.Empty
                },
                new TestModel
                {
                    Id = 5,
                    Name = "Class Name",
                    Description = "Class Description",
                    ChildItemsList = null
                },
                new TestModel
                {
                    Id = 6,
                    Name = "Class Name",
                    Description = "Class Description",
                    SubObj = FakeData.SubObj,
                    SubObj2 = FakeData.SubObj,
                    ChildItemsList = FakeData.ChildItems,
                    ChildMatrixArray = FakeData.ChildMatrix
                }
            };
        }
    }

    public static List<ChildTestModel> ChildItems
    {
        get
        {
            return new List<ChildTestModel>
            {
                new ChildTestModel
                {
                    Id = 1,
                    Name = "Child 1",
                    Description = "Description 1",
                    Value = 10
                },
                new ChildTestModel
                {
                    Id = 2,
                    Name = "Child 2",
                    Description = "Description 2",
                    Value = 10
                }
            };
        }
    }

    public static ChildTestModel[][] ChildMatrix
    {
        get
        {
            return new ChildTestModel[][]
            {
                new ChildTestModel[]
                {
                    new ChildTestModel
                    {
                        Id = 1,
                        Name = "Child 1",
                        Description = "Description 1",
                        Value = 10
                    },
                    new ChildTestModel
                    {
                        Id = 2,
                        Name = "Child 2",
                        Description = "Description 2",
                        Value = 10
                    }
                },
                new ChildTestModel[]
                {
                    new ChildTestModel
                    {
                        Id = 2,
                        Name = "Child 2",
                        Description = "Description 2",
                        Value = 10
                    },
                    new ChildTestModel
                    {
                        Id = 2,
                        Name = "Child 2",
                        Description = "Description 2",
                        Value = 10
                    },
                    new ChildTestModel
                    {
                        Id = 2,
                        Name = "Child 2",
                        Description = "Description 2",
                        Value = 10
                    }
                }
            };
        }
    }


    public static TestModel SubObj
    {
        get
        {
            return new TestModel
            {
                Name = "Class Name",
                Description = "Class Description",
                ChildItemsList = new List<ChildTestModel>
                {
                    new ChildTestModel
                    {
                        Id = 1,
                        Name = "Child 1",
                        Description = "Description 1",
                        Value = 10
                    },
                    new ChildTestModel
                    {
                        Id = 2,
                        Name = "Child 2",
                        Description = "Description 2",
                        Value = 10
                    }
                },
                SubObj = new TestModel
                {
                    Name = "Class Name",
                    Description = "Class Description",
                    ChildItemsList = new List<ChildTestModel>
                    {
                        new ChildTestModel
                        {
                            Id = 1,
                            Name = "Child 1",
                            Description = "Description 1",
                            Value = 10
                        },
                        new ChildTestModel
                        {
                            Id = 2,
                            Name = "Child 2",
                            Description = "Description 2",
                            Value = 10
                        }
                    }
                }
            };
        }
    }
}

public class ChildTestModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Value { get; set; }
}

public class TestModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ChildTestModel> ChildItemsList { get; set; }
    public ChildTestModel[] ChildItemsArray { get; set; }
    public ChildTestModel[][] ChildMatrixArray { get; set; }
    public List<List<ChildTestModel>> ChildMatrixList { get; set; }
    public TestModel SubObj { get; set; }
    public TestModel SubObj2 { get; set; }
}
