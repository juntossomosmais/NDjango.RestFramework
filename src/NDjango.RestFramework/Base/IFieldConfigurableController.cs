using System;
using System.Collections.Generic;

namespace NDjango.RestFramework.Base;

internal interface IFieldConfigurableController
{
    public string[] GetFieldsConfiguration();
    public string[] GetAllowedFieldsConfiguration();
    public Type GetDestinationType();
    public IReadOnlyList<string> GetMisnamedValidationHooks();
}
