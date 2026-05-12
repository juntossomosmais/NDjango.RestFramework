using System;
using System.Collections.Generic;
using System.Linq;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Test.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NDjango.RestFramework.Test.Helpers;

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
            Assert.NotNull(obj);
            Assert.NotNull(obj.Instance);
            Assert.NotNull(obj.JsonObject);
        }

        [Theory(DisplayName = "Should populate original JSON")]
        [MemberData(nameof(listOKStubs))]
        public void ShouldPopulateOriginalJSON(TestModel testObj)
        {
            // act
            var jsonObj = JsonConvert.SerializeObject(testObj);
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj);

            // assert
            Assert.NotNull(obj);
            Assert.True(JToken.DeepEquals(JToken.Parse(jsonObj), obj.JsonObject));
        }

        [Theory(DisplayName = "Should create a instance object from the original JSON")]
        [MemberData(nameof(listOKStubs))]
        public void ShouldCreateInstanceFromOriginalJSON(TestModel testObj)
        {
            // act
            var jsonObj = JsonConvert.SerializeObject(testObj);
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj);

            // assert
            Assert.NotNull(obj);
            Assert.Equivalent(testObj, obj.Instance);
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
            Assert.NotNull(obj);
            Assert.Equal(obj.Instance, instance);

            Assert.True(obj.IsSet(inst => inst.Id));
            Assert.True(obj.IsSet(inst => inst.Name));
            Assert.False(obj.IsSet(inst => inst.Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray));
            Assert.True(obj.IsSet(inst => inst.SubObj));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet("Id"));

            Assert.True(obj.IsSet("childItemsList"));
            Assert.True(obj.IsSet("ChildItemsList.0"));
            Assert.True(obj.IsSet("childItemsList.0.Id"));
            Assert.True(obj.IsSet("ChildItemsList.$last"));
            Assert.True(obj.IsSet("ChildItemsList.$last.Id"));

            Assert.True(obj.IsSet("ChildItemsArray"));
            Assert.True(obj.IsSet("childItemsArray.0"));
            Assert.True(obj.IsSet("ChildItemsArray.0.id"));
            Assert.True(obj.IsSet("ChildItemsArray.$last"));
            Assert.True(obj.IsSet("ChildItemsArray.$last.Id"));

            Assert.True(obj.IsSet("ChildMatrixArray"));
            Assert.True(obj.IsSet("ChildMatrixArray.1"));
            Assert.True(obj.IsSet("ChildMatrixArray.1.2"));
            Assert.True(obj.IsSet("ChildMatrixArray.1.2.Id"));
            Assert.True(obj.IsSet("ChildMatrixArray.$last"));
            Assert.True(obj.IsSet("ChildMatrixArray.1.$last"));
            Assert.True(obj.IsSet("ChildMatrixArray.1.$last.id"));

            Assert.True(obj.IsSet("ChildMatrixList"));
            Assert.True(obj.IsSet("ChildMatrixList.0"));
            Assert.True(obj.IsSet("ChildMatrixList.0.0"));
            Assert.True(obj.IsSet("ChildMatrixList.0.0.Id"));
            Assert.True(obj.IsSet("ChildMatrixList.$last"));
            Assert.True(obj.IsSet("ChildMatrixList.0.$last"));
            Assert.True(obj.IsSet("ChildMatrixList.0.$last.id"));

            Assert.True(obj.IsSet("SubObj"));
            Assert.True(obj.IsSet("SubObj.SubObj"));
            Assert.True(obj.IsSet("SubObj.SubObj.ChildItemsList"));
            Assert.True(obj.IsSet("SubObj.SubObj.ChildItemsList.1.Id"));
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
            Assert.NotNull(obj);
            Assert.False(obj.IsSet("NonExistingProp"));
            Assert.False(obj.IsSet("ChildItemsList.2"));
            Assert.False(obj.IsSet("ChildItemsList.$first"));
            Assert.False(obj.IsSet("ChildItemsList.$Last"));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet("ChildItemsList", "0"));
            Assert.True(obj.IsSet("childItemsList", "0", "Id"));
            Assert.True(obj.IsSet("ChildItemsList", "$last"));
            Assert.True(obj.IsSet("ChildItemsList", "$last", "id"));

            Assert.True(obj.IsSet("ChildItemsArray"));
            Assert.True(obj.IsSet("childItemsArray", "0"));
            Assert.True(obj.IsSet("ChildItemsArray", "0", "id"));
            Assert.True(obj.IsSet("ChildItemsArray", "$last"));
            Assert.True(obj.IsSet("ChildItemsArray", "$last", "id"));

            Assert.True(obj.IsSet("ChildMatrixArray"));
            Assert.True(obj.IsSet("ChildMatrixArray", "1"));
            Assert.True(obj.IsSet("ChildMatrixArray", "1", "2"));
            Assert.True(obj.IsSet("ChildMatrixArray", "1", "2", "Id"));
            Assert.True(obj.IsSet("ChildMatrixArray", "$last"));
            Assert.True(obj.IsSet("ChildMatrixArray", "1", "$last"));
            Assert.True(obj.IsSet("ChildMatrixArray", "1", "$last", "Id"));

            Assert.True(obj.IsSet("ChildMatrixList"));
            Assert.True(obj.IsSet("ChildMatrixList", "0"));
            Assert.True(obj.IsSet("ChildMatrixList", "0", "0"));
            Assert.True(obj.IsSet("ChildMatrixList", "0", "0", "Id"));
            Assert.True(obj.IsSet("ChildMatrixList", "$last"));
            Assert.True(obj.IsSet("ChildMatrixList", "0", "$last"));
            Assert.True(obj.IsSet("ChildMatrixList", "0", "$last", "Id"));

            Assert.True(obj.IsSet("SubObj"));
            Assert.True(obj.IsSet("SubObj", "SubObj"));
            Assert.True(obj.IsSet("SubObj", "SubObj", "ChildItemsList"));
            Assert.True(obj.IsSet("SubObj", "SubObj", "ChildItemsList", "1", "Id"));
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
            Assert.NotNull(obj);
            Assert.False(obj.IsSet("ChildItemsList", "NonExistingProp"));
            Assert.False(obj.IsSet("ChildItemsList", "2"));
            Assert.False(obj.IsSet("ChildItemsList", "$first"));
            Assert.False(obj.IsSet("ChildItemsList", "$Last"));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.Id));
            Assert.True(obj.IsSet(inst => inst.Name));
            Assert.True(obj.IsSet(inst => inst.Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray));
            Assert.True(obj.IsSet(inst => inst.SubObj));
        }

        [Fact(DisplayName = "All fields should be represented as not set")]
        public void ShouldAllFieldsReturnFalseToIsSet()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.Id));
            Assert.False(obj.IsSet(inst => inst.Name));
            Assert.False(obj.IsSet(inst => inst.Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray));
            Assert.False(obj.IsSet(inst => inst.SubObj));
        }

        private readonly int _index1 = 2;
        private const int _index2 = 3;
        private static readonly int _index3 = 3;

        [Fact(DisplayName = "Field parse expression to path string")]
        public void ShouldParseExpressionToPathString()
        {
            Assert.Equal("ChildItemsList.0", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.First()));
            Assert.Equal("ChildItemsList.$last", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.Last()));
            Assert.Equal("ChildItemsList.1", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList[1]));
            Assert.Equal("ChildItemsArray.1", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray[1]));

            for (var index = 0; index < 3; index++)
            {
                Assert.Equal($"ChildItemsList.{index}", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(index)));
                Assert.Equal($"ChildItemsList.{index}", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList[index]));
                Assert.Equal($"ChildItemsArray.{index}", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray[index]));
            }

            Assert.Equal($"ChildItemsList.{_index1}", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(_index1)));
            Assert.Equal($"ChildItemsList.{_index2}", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(_index2)));
            Assert.Equal($"ChildItemsList.{_index3}", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(_index3)));
            Assert.Equal("ChildItemsArray.1", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray.ElementAt(1)));

            Assert.Equal("ChildItemsList.1.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList[1].Name));
            Assert.Equal("ChildItemsArray.1.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray[1].Name));

            Assert.Equal("ChildItemsList.1.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsList.ElementAt(1).Name));
            Assert.Equal("ChildItemsArray.1.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildItemsArray.ElementAt(1).Name));

            Assert.Equal("ChildMatrixArray.1", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray[1]));
            Assert.Equal("ChildMatrixList.1", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList[1]));

            Assert.Equal("ChildMatrixArray.1.2", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray[1][2]));
            Assert.Equal("ChildMatrixList.1.2", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList[1][2]));

            Assert.Equal("ChildMatrixArray.1.2", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray.ElementAt(1).ElementAt(2)));
            Assert.Equal("ChildMatrixList.1.2", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList.ElementAt(1).ElementAt(2)));

            Assert.Equal("ChildMatrixArray.1.2.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray.ElementAt(1).ElementAt(2).Name));
            Assert.Equal("ChildMatrixList.1.2.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList.ElementAt(1).ElementAt(2).Name));

            Assert.Equal("ChildMatrixArray.1.2.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray[1].ElementAt(2).Name));
            Assert.Equal("ChildMatrixList.1.2.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList[1].ElementAt(2).Name));

            Assert.Equal("ChildMatrixArray.1.2.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixArray.ElementAt(1)[2].Name));
            Assert.Equal("ChildMatrixList.1.2.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.ChildMatrixList.ElementAt(1)[2].Name));

            Assert.Equal("SubObj.SubObj.ChildItemsList.1", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.SubObj.SubObj.ChildItemsList.ElementAt(1)));
            Assert.Equal("SubObj.SubObj.ChildItemsList.1", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.SubObj.SubObj.ChildItemsList[1]));

            Assert.Equal("SubObj.SubObj.ChildItemsList.1.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.SubObj.SubObj.ChildItemsList.ElementAt(1).Name));
            Assert.Equal("SubObj.SubObj.ChildItemsList.1.Name", PartialJsonObject<TestModel>.GetMemberPath(inst => inst.SubObj.SubObj.ChildItemsList[1].Name));
        }

        [Fact(DisplayName = "Field should be represented as set if it is added manually")]
        public void ShouldRepresentFieldAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.Name));
            Assert.False(obj.IsSet(inst => inst.Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList));

            // act
            obj.Add(inst => inst.Name);
            obj.Add(inst => inst.Description);
            obj.Add(inst => inst.ChildItemsList);

            // assert
            Assert.False(obj.IsSet(inst => inst.Id));
            Assert.True(obj.IsSet(inst => inst.Name));
            Assert.True(obj.IsSet(inst => inst.Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray));
            Assert.False(obj.IsSet(inst => inst.SubObj));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray));

            // act
            obj.Remove(inst => inst.Id);
            obj.Remove(inst => inst.ChildMatrixArray);

            // assert
            Assert.False(obj.IsSet(inst => inst.Id));
            Assert.True(obj.IsSet(inst => inst.Name));
            Assert.True(obj.IsSet(inst => inst.Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray));
            Assert.True(obj.IsSet(inst => inst.SubObj));
        }

        [Fact(DisplayName = "Should get JSON Path")]
        public void ShouldGetJSONPath()
        {
            // arrange
            var testObj = FakeData.OK;
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());

            // assert
            Assert.NotNull(obj);
            Assert.Equal("Id", obj.GetJSONPath(x => x.Id));
            Assert.Equal("ChildItemsList[0].Id", obj.GetJSONPath(x => x.ChildItemsList[0].Id));
            Assert.Equal("ChildItemsArray[0].Id", obj.GetJSONPath(x => x.ChildItemsArray[0].Id));
            Assert.Equal("ChildMatrixArray[0][0].Id", obj.GetJSONPath(x => x.ChildMatrixArray[0][0].Id));
            Assert.Equal("ChildMatrixList[0][0].Id", obj.GetJSONPath(x => x.ChildMatrixList[0][0].Id));
            Assert.Equal("SubObj.ChildItemsList[0].Name", obj.GetJSONPath(x => x.SubObj.ChildItemsList[0].Name));
        }

        [Fact(DisplayName = "Should reset the add configurations if clear the cache")]
        public void ShouldResetAddConfigurationsIfClearCache()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.Name));
            Assert.False(obj.IsSet(inst => inst.Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList));

            // act
            obj.Add(inst => inst.Name);
            obj.Add(inst => inst.Description);
            obj.Add(inst => inst.ChildItemsList);

            // assert
            Assert.True(obj.IsSet(inst => inst.Name));
            Assert.True(obj.IsSet(inst => inst.Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));

            // act
            obj.ClearCache();

            // assert
            Assert.False(obj.IsSet(inst => inst.Name));
            Assert.False(obj.IsSet(inst => inst.Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.Name));
            Assert.True(obj.IsSet(inst => inst.Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));

            // act
            obj.Remove(inst => inst.Name);
            obj.Remove(inst => inst.Description);
            obj.Remove(inst => inst.ChildItemsList);

            // assert
            Assert.False(obj.IsSet(inst => inst.Name));
            Assert.False(obj.IsSet(inst => inst.Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList));

            // act
            obj.ClearCache();

            // assert
            Assert.True(obj.IsSet(inst => inst.Name));
            Assert.True(obj.IsSet(inst => inst.Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));
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
            Assert.NotNull(obj1);
            Assert.NotSame(obj1.Instance, obj2);
            Assert.Equivalent(obj1.Instance, obj2);
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
            Assert.NotNull(obj);
            Assert.Equal(obj.Instance.Name, nameProp);
            Assert.Equal(1, childItemListItemIdProp);
            Assert.Equal(2, childItemArrayItemIdProp);
            Assert.Equal("Child 1", childItemMatrixItemNameProp);
            Assert.Equal("Description 1", childMatrixListItemDescriptionProp);
            Assert.Equal(10, subObjChildListItemValueProp);
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
            Assert.NotNull(obj);
            Assert.Equal("default value", nameProp);
            Assert.Equal(10000, childItemListItemIdProp);
            Assert.Equal(10001, childItemArrayItemIdProp);
            Assert.Equal("default value", childItemMatrixItemNameProp);
            Assert.Equal("default value", childMatrixListItemDescriptionProp);
            Assert.Equal(10002, subObjChildListItemValueProp);
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Value));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Value));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Value));
        }

        [Fact(DisplayName = "All fields in array should be represented as not set")]
        public void ShouldAllFieldsInArrayReturnFalseToIsSet()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Value));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Value));
        }

        [Fact(DisplayName = "Field in array should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInArrayAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Description));

            // act
            obj.Add(inst => inst.ChildItemsArray[0].Name);
            obj.Add(inst => inst.ChildItemsArray[1].Description);

            // assert
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Value));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Value));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Description));

            // act
            obj.Remove(inst => inst.ChildItemsArray[0].Value);
            obj.Remove(inst => inst.ChildItemsArray[1].Description);

            // assert
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[0].Value));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsArray[1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsArray[1].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value));
        }

        [Fact(DisplayName = "All fields in matrix array should be represented as not set")]
        public void ShouldAllFieldsInMatrixArrayReturnFalseToIsSet()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value));
        }

        [Fact(DisplayName = "Field in matrix array should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInMatrixArrayAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name));

            obj.Add(inst => inst.ChildMatrixArray[0][0].Description);
            obj.Add(inst => inst.ChildMatrixArray[0][1].Id);
            obj.Add(inst => inst.ChildMatrixArray[1][0].Name);
            obj.Add(inst => inst.ChildMatrixArray[1][1].Description);
            obj.Add(inst => inst.ChildMatrixArray[1][2].Name);

            // assert
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value));
        }

        [Fact(DisplayName = "Field in matrix array should be represented as not set if it is removed manually")]
        public void ShouldRepresentFieldInMatrixArrayAsSetIfItIsRemovedManually()
        {
            // arrange
            var testObj = FakeData.OK;

            // act
            var jsonObj = JObject.Parse(JsonConvert.SerializeObject(testObj));
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(jsonObj.ToString());
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name));

            obj.Remove(inst => inst.ChildMatrixArray[0][0].Description);
            obj.Remove(inst => inst.ChildMatrixArray[0][1].Id);
            obj.Remove(inst => inst.ChildMatrixArray[1][0].Name);
            obj.Remove(inst => inst.ChildMatrixArray[1][1].Description);
            obj.Remove(inst => inst.ChildMatrixArray[1][2].Name);

            // assert
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][0].Value));

            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[0][1].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][0].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][1].Value));

            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixArray[1][2].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Value));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Value));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Value));
        }

        [Fact(DisplayName = "All fields in list should be represented as not set")]
        public void ShouldAllFieldsInListReturnFalseToIsSet()
        {
            // arrange
            var json = "{}";

            // assert
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);
            Assert.NotNull(obj);

            Assert.False(obj.IsSet(inst => inst.ChildItemsList));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Value));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Value));
        }

        [Fact(DisplayName = "Field in list should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInListAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Value));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Description));

            // act
            obj.Add(inst => inst.ChildItemsList[0].Value);
            obj.Add(inst => inst.ChildItemsList[1].Description);

            // assert
            Assert.False(obj.IsSet(inst => inst.ChildItemsList));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Value));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Description));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Id));

            // act
            obj.Remove(inst => inst.ChildItemsList[0].Id);
            obj.Remove(inst => inst.ChildItemsList[0].Name);
            obj.Remove(inst => inst.ChildItemsList[1].Id);

            // assert
            Assert.True(obj.IsSet(inst => inst.ChildItemsList));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[0].Value));
            Assert.False(obj.IsSet(inst => inst.ChildItemsList[1].Id));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Name));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Description));
            Assert.True(obj.IsSet(inst => inst.ChildItemsList[1].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Name));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Value));
        }

        [Fact(DisplayName = "All fields in matrix list should be represented as not set")]
        public void ShouldAllFieldsInMatrixListReturnFalseToIsSet()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Value));
        }

        [Fact(DisplayName = "Field in list matrix should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInListMatrixAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Id));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Name));

            // act
            obj.Add(inst => inst.ChildMatrixList[0][0].Id);
            obj.Add(inst => inst.ChildMatrixList[0][0].Name);

            // assert
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Description));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Value));

            // act
            obj.Remove(inst => inst.ChildMatrixList[0][0].Description);
            obj.Remove(inst => inst.ChildMatrixList[0][0].Value);

            // assert
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Id));
            Assert.True(obj.IsSet(inst => inst.ChildMatrixList[0][0].Name));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Description));
            Assert.False(obj.IsSet(inst => inst.ChildMatrixList[0][0].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.SubObj));
            Assert.True(obj.IsSet(inst => inst.SubObj.Id));
            Assert.False(obj.IsSet(inst => inst.SubObj.Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.SubObj));
            Assert.True(obj.IsSet(inst => inst.SubObj.Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.Name));
            Assert.True(obj.IsSet(inst => inst.SubObj.Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value));
        }

        [Fact(DisplayName = "All fields in nested object should be represented as not set")]
        public void ShouldAllFieldsInNestedObjectReturnFalseToIsSet()
        {
            // arrange
            var json = "{}";

            // assert
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);
            Assert.NotNull(obj);

            // assert
            Assert.False(obj.IsSet(inst => inst.SubObj));
            Assert.False(obj.IsSet(inst => inst.SubObj.Id));
            Assert.False(obj.IsSet(inst => inst.SubObj.Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.Description));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value));
        }

        [Fact(DisplayName = "Field in nested object should be represented as set if it is added manually")]
        public void ShouldRepresentFieldInNestedObjectAsSetIfItIsAddedManually()
        {
            // arrange
            var json = "{}";

            // act
            var obj = JsonConvert.DeserializeObject<PartialJsonObject<TestModel>>(json);

            // assert
            Assert.NotNull(obj);
            Assert.False(obj.IsSet(inst => inst.SubObj.Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value));

            // act
            obj.Add(inst => inst.SubObj.Name);
            obj.Add(inst => inst.SubObj.ChildItemsList[0].Name);
            obj.Add(inst => inst.SubObj.ChildItemsList[1].Value);

            // assert
            Assert.False(obj.IsSet(inst => inst.SubObj));
            Assert.False(obj.IsSet(inst => inst.SubObj.Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.Description));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value));
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
            Assert.NotNull(obj);
            Assert.True(obj.IsSet(inst => inst.SubObj.Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id));

            // act
            obj.Remove(inst => inst.SubObj.Description);
            obj.Remove(inst => inst.SubObj.ChildItemsList[0].Id);

            // assert
            Assert.True(obj.IsSet(inst => inst.SubObj));
            Assert.True(obj.IsSet(inst => inst.SubObj.Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.Name));
            Assert.False(obj.IsSet(inst => inst.SubObj.Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList));
            Assert.False(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Name));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[0].Value));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Id));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Name));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Description));
            Assert.True(obj.IsSet(inst => inst.SubObj.ChildItemsList[1].Value));
        }
    }

    public class SetValueGuard
    {
        [Fact]
        public void SetValue_PresentTopLevelField_ShouldReplaceValue()
        {
            // Arrange
            var json = "{\"Name\": \"OriginalName\", \"CNPJ\": \"12345\"}";
            var partial = new PartialJsonObject<CustomerDto>(json);

            // Act
            partial.SetValue(d => d.Name, "ReplacedName");

            // Assert
            Assert.Equal("ReplacedName", partial.Instance.Name);
            Assert.True(partial.IsSet(d => d.Name));
        }

        [Fact]
        public void SetValue_AbsentTopLevelField_ShouldAddValue()
        {
            // Arrange
            var json = "{\"Name\": \"OnlyName\"}";
            var partial = new PartialJsonObject<CustomerDto>(json);

            // Act
            partial.SetValue(d => d.CNPJ, "99999999000199");

            // Assert
            Assert.Equal("99999999000199", partial.Instance.CNPJ);
            Assert.True(partial.IsSet(d => d.CNPJ));
        }

        [Fact]
        public void SetValue_NestedPathAbsentFromJson_ShouldThrowNotSupportedException()
        {
            // Arrange
            var json = "{\"Name\": \"OnlyName\"}";
            var partial = new PartialJsonObject<CustomerDto>(json);

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() =>
                partial.SetValue(d => d.CustomerDocuments.First().Document, "x"));
            Assert.Contains("nested", ex.Message, StringComparison.OrdinalIgnoreCase);
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
