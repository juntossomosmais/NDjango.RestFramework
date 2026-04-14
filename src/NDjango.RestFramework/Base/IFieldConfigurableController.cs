using System;

namespace NDjango.RestFramework.Base;

internal interface IFieldConfigurableController
{
    string[] GetFieldsConfiguration();
    string[] GetAllowedFieldsConfiguration();
    Type GetDestinationType();
}
