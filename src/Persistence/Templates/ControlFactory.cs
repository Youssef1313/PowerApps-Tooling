// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public class ControlFactory : IControlFactory
{
    private readonly IControlTemplateStore _controlTemplateStore;

    public ControlFactory(IControlTemplateStore controlTemplateStore)
    {
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
    }

    public Control Create(string name, string template, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null)
    {
        if (TryCreateFirstClassControl(name, template, variant ?? string.Empty, properties, children, out var control))
            return control;

        if (_controlTemplateStore.TryGetTemplateByName(template, out var controlTemplate))
        {
            return new BuiltInControl(name, variant ?? string.Empty, controlTemplate)
            {
                Properties = properties ?? new(),
                Children = children
            };
        }

        return new CustomControl(name, variant ?? string.Empty, new ControlTemplate(template))
        {
            Properties = properties ?? new(),
            Children = children
        };
    }

    public Control Create(string name, ControlTemplate template, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null)
    {
        if (TryCreateFirstClassControl(name, template.Name, variant ?? string.Empty, properties, children, out var control))
            return control;

        return new BuiltInControl(name, variant ?? string.Empty, template)
        {
            Properties = properties ?? new(),
            Children = children
        };
    }

    public App CreateApp(string name, ControlPropertiesCollection? properties = null)
    {
        return new App(name, string.Empty, _controlTemplateStore)
        {
            Properties = properties ?? new(),
            Children = new Control[] { Create("Host", BuiltInTemplates.Host) }
        };
    }

    public Screen CreateScreen(string name, ControlPropertiesCollection? properties = null, IList<Control>? children = null)
    {
        return new Screen(name, string.Empty, _controlTemplateStore)
        {
            Properties = properties ?? new(),
            Children = children
        };
    }

    private bool TryCreateFirstClassControl(string name, string template, string variant, ControlPropertiesCollection? properties, IList<Control>? children, [MaybeNullWhen(false)] out Control control)
    {
        control = null;
        if (!_controlTemplateStore.TryGetControlTypeByName(template, out var controlType))
            return false;

        var instance = Activator.CreateInstance(controlType, name, variant, _controlTemplateStore);
        if (instance is not Control controlInstance)
            throw new InvalidOperationException($"Failed to create control of type {controlType.Name}.");

        if (properties is not null)
            foreach (var prop in properties)
                controlInstance.Properties.Add(prop.Key, prop.Value);

        controlInstance.Children = children;

        control = controlInstance;

        return true;
    }
}
