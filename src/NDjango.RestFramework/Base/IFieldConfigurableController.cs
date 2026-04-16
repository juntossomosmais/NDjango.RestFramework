using System;
using System.Collections.Generic;

namespace NDjango.RestFramework.Base;

internal interface IFieldConfigurableController
{
    string[] GetFieldsConfiguration();
    string[] GetAllowedFieldsConfiguration();
    Type GetDestinationType();
    IReadOnlyList<string> GetMisnamedValidationHooks();
}
