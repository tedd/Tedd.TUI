using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.ComponentModel;

namespace Tedd.TUI;

public static class XamlLoader
{
    /// <summary>XAML language namespace (x: prefix by convention). x:Name maps to UIElement.Name.</summary>
    private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static UIElement Load(string xml, object? controller = null)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        if (doc.DocumentElement == null)
            throw new InvalidOperationException("XML document is empty.");

        var root = (UIElement)ParseElement(doc.DocumentElement, controller);

        // ElementName bindings may reference elements declared later in the document
        // than their target; re-evaluate every binding now that the tree is complete.
        root.RefreshBindingsRecursive();

        return root;
    }

    private static object ParseElement(XmlElement element, object? controller)
    {
        // Element names may carry a namespace prefix (e.g. <tui:Button>) when the file is
        // authored for a XAML designer; LocalName strips it so resolution stays name-based.
        string localName = element.LocalName;

        // If element name contains '.', it is Property Element Syntax (e.g. <Table.Columns>)
        // which is handled by the parent before recursing.
        if (localName.Contains("."))
        {
            throw new InvalidOperationException($"Unexpected property element {localName} in ParseElement.");
        }

        // Handle specific sub-namespaces if any (e.g. MarkdownView in Tedd.TUI.Markdown)
        Type? type = ResolveType(localName);
        if (type == null)
        {
            throw new InvalidOperationException($"Type {localName} not found.");
        }

        var instance = Activator.CreateInstance(type);
        if (instance == null)
            throw new InvalidOperationException($"Failed to create instance of {type.Name}.");

        // 2. Set Properties (Attributes)
        foreach (XmlAttribute attr in element.Attributes)
        {
            if (IsIgnoredAttribute(attr))
                continue;

            if (IsXamlNameAttribute(attr))
            {
                SetProperty(instance, "Name", attr.Value, controller);
            }
            else if (attr.LocalName.Contains("."))
            {
                // Attached Property? e.g. Grid.Row
                // For now, support attached properties
                SetAttachedProperty(instance, attr.LocalName, attr.Value);
            }
            else
            {
                SetProperty(instance, attr.LocalName, attr.Value, controller);
            }
        }

        // 3. Handle Children
        foreach (XmlNode childNode in element.ChildNodes)
        {
            if (childNode is XmlElement childElement)
            {
                if (childElement.LocalName.Contains("."))
                {
                    // Property Element Syntax: <Table.Columns> ... </Table.Columns>
                    string childLocalName = childElement.LocalName;
                    string propName = childLocalName.Substring(childLocalName.LastIndexOf('.') + 1);
                    // The property name is "Columns" on "Table".
                    // Find property on instance.
                    var propInfo = instance.GetType().GetProperty(propName);
                    if (propInfo != null)
                    {
                        // Add children of this property element to the property
                        object? propValue = propInfo.GetValue(instance);
                        if (propValue is IList list)
                        {
                            foreach (XmlNode grandChild in childElement.ChildNodes)
                            {
                                if (grandChild is XmlElement grandChildElement)
                                {
                                    object childObj = ParseElement(grandChildElement, controller);
                                    list.Add(childObj);
                                }
                            }
                        }
                        else if (propInfo.CanWrite)
                        {
                            // Single property? e.g. <Border.Child>
                            // Should be only one child
                            foreach (XmlNode grandChild in childElement.ChildNodes)
                            {
                                if (grandChild is XmlElement grandChildElement)
                                {
                                    object childObj = ParseElement(grandChildElement, controller);
                                    propInfo.SetValue(instance, childObj);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Regular Child
                    object childObj = ParseElement(childElement, controller);
                    AddChild(instance, childObj);
                }
            }
            else if (childNode is XmlText textNode)
            {
                string? text = textNode.Value?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    SetContentProperty(instance, text);
                }
            }
        }

        // 4. Register Name
        if (instance is UIElement uie && !string.IsNullOrEmpty(uie.Name))
        {
            // If controller has a field with this name, set it.
            if (controller != null)
            {
                var field = controller.GetType().GetField(uie.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType.IsAssignableFrom(type))
                {
                    field.SetValue(controller, instance);
                }
            }
        }

        return instance;
    }

    /// <summary>
    /// Attributes a XAML designer adds that carry no runtime meaning for the TUI loader:
    /// xmlns declarations, markup-compatibility (mc:), designer hints (d:) and any other
    /// prefixed attribute except x:Name. Keeping these ignorable lets one file load both
    /// in a WPF/XAML editor and through this loader.
    /// </summary>
    private static bool IsIgnoredAttribute(XmlAttribute attr)
    {
        if (attr.Prefix == "xmlns" || attr.Name == "xmlns")
            return true;

        if (string.IsNullOrEmpty(attr.Prefix))
            return false;

        if (IsXamlNameAttribute(attr))
            return false;

        // mc:Ignorable, d:DesignWidth, x:Class, … — anything namespaced other than x:Name.
        return true;
    }

    private static bool IsXamlNameAttribute(XmlAttribute attr)
    {
        if (attr.LocalName != "Name")
            return false;
        // Match by namespace URI when declared, by conventional prefix otherwise.
        return attr.NamespaceURI == XamlNamespace || attr.Prefix == "x";
    }

    private static Type? ResolveType(string name)
    {
        // Try common namespaces
        string[] namespaces = new[]
        {
            "Tedd.TUI",
            "Tedd.TUI.Markdown",
            "Tedd.TUI.CodeColoring"
        };

        foreach (var ns in namespaces)
        {
            string typeName = ns + "." + name;
            Type? type = Type.GetType(typeName);
            if (type != null) return type;

            // Also check loaded assemblies?
            // Assembly.GetEntryAssembly().GetType(typeName) ??
            // Assembly.GetExecutingAssembly().GetType(typeName);
        }

        // Fallback: iterate all assemblies?
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var ns in namespaces)
            {
                var t = asm.GetType(ns + "." + name);
                if (t != null) return t;
            }
        }

        return null;
    }

    private static void SetProperty(object instance, string name, string value, object? controller)
    {
        if (name == "Name" && instance is UIElement uie)
        {
            uie.Name = value;
            return;
        }

        // Markup extensions: {Binding ...}, {x:Null}, {TemplateBinding ...}.
        // "{}" is the XAML escape for a literal value starting with '{'.
        if (value.StartsWith("{}", StringComparison.Ordinal))
        {
            value = value.Substring(2);
        }
        else if (MarkupExtensionParser.IsExtension(value))
        {
            ApplyMarkupExtension(instance, name, value, controller);
            return;
        }

        var type = instance.GetType();

        // 1. Event Wiring
        var eventInfo = type.GetEvent(name);
        if (eventInfo != null)
        {
            if (controller == null) return;
            // Expect value to be method name on controller
            var method = controller.GetType().GetMethod(value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                try
                {
                    // Create delegate
                    // Note: Delegate.CreateDelegate requires non-null target if instance method
                    // controller is not null here.
                    Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, controller, method);
                    eventInfo.AddEventHandler(instance, handler);
                }
                catch (Exception)
                {
                    // Fallback for RoutedEventHandler if signature mismatch?
                    // RoutedEventHandler is void(object, RoutedEventArgs)
                    // If method is void(), wrap it?
                    if (eventInfo.EventHandlerType == typeof(RoutedEventHandler))
                    {
                        if (method.GetParameters().Length == 0)
                        {
                            RoutedEventHandler wrapper = (s, e) => method.Invoke(controller, null);
                            eventInfo.AddEventHandler(instance, wrapper);
                        }
                    }
                    else if (eventInfo.EventHandlerType == typeof(EventHandler))
                    {
                        if (method.GetParameters().Length == 0)
                        {
                            EventHandler wrapper = (s, e) => method.Invoke(controller, null);
                            eventInfo.AddEventHandler(instance, wrapper);
                        }
                    }
                    else if (eventInfo.EventHandlerType == typeof(Action))
                    {
                        Action wrapper = () => method.Invoke(controller, null);
                        eventInfo.AddEventHandler(instance, wrapper);
                    }
                }
            }
            return;
        }

        // 2. Property
        var prop = type.GetProperty(name);
        if (prop != null && prop.CanWrite)
        {
            if (typeof(Delegate).IsAssignableFrom(prop.PropertyType))
            {
                // Handle Delegate property (like Action Command)
                if (controller != null)
                {
                    var method = controller.GetType().GetMethod(value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (method != null)
                    {
                        try
                        {
                            // Create delegate of property type
                            Delegate del = Delegate.CreateDelegate(prop.PropertyType, controller, method);
                            prop.SetValue(instance, del);
                        }
                        catch (Exception)
                        {
                            // Try wrapping Action if target is void()
                            if (prop.PropertyType == typeof(Action) && method.GetParameters().Length == 0)
                            {
                                Action wrapper = () => method.Invoke(controller, null);
                                prop.SetValue(instance, wrapper);
                            }
                        }
                    }
                }
            }
            else
            {
                object val = ConvertValue(value, prop.PropertyType);
                prop.SetValue(instance, val);
            }
            return;
        }

        // 3. DependencyProperty (if not found as CLR property, though usually wrapper exists)
        // ...
    }

    /// <summary>
    /// Applies a parsed markup extension attribute. Supported: {Binding ...} (wired to
    /// the matching DependencyProperty), {TemplateBinding Path} (Binding with
    /// RelativeSource TemplatedParent), {x:Null} and {StaticResource Key} (resolved
    /// against the controller's fields/properties, this loader's stand-in for a
    /// resource dictionary). Unknown extensions throw, matching WPF's loud failure.
    /// </summary>
    private static void ApplyMarkupExtension(object instance, string propertyName, string text, object? controller)
    {
        var ext = MarkupExtensionParser.Parse(text);
        string extName = StripXmlPrefix(ext.Name);

        switch (extName)
        {
            case "Binding":
                SetBindingOnProperty(instance, propertyName, BuildBinding(ext, controller));
                return;

            case "TemplateBinding":
            {
                var binding = new Binding(ext.Positional ?? throw new InvalidOperationException("TemplateBinding requires a property name."))
                {
                    RelativeSource = RelativeSource.TemplatedParent,
                    Mode = BindingMode.OneWay
                };
                SetBindingOnProperty(instance, propertyName, binding);
                return;
            }

            case "Null":
            {
                var prop = instance.GetType().GetProperty(propertyName);
                if (prop == null || !prop.CanWrite)
                    throw new InvalidOperationException($"Cannot assign {{x:Null}}: no writable property '{propertyName}' on {instance.GetType().Name}.");
                prop.SetValue(instance, null);
                return;
            }

            case "StaticResource":
            {
                object resource = ResolveControllerResource(controller, ext.Positional ?? "")
                    ?? throw new InvalidOperationException($"StaticResource '{ext.Positional}' was not found on the controller.");
                var prop = instance.GetType().GetProperty(propertyName);
                if (prop == null || !prop.CanWrite)
                    throw new InvalidOperationException($"Cannot assign StaticResource: no writable property '{propertyName}' on {instance.GetType().Name}.");
                prop.SetValue(instance, resource);
                return;
            }

            default:
                throw new InvalidOperationException($"Unsupported markup extension '{ext.Name}'.");
        }
    }

    private static void SetBindingOnProperty(object instance, string propertyName, Binding binding)
    {
        if (instance is not UIElement element)
            throw new InvalidOperationException($"A '{{Binding}}' can only be set on a UIElement; {instance.GetType().Name} is not one.");

        var dp = FindDependencyProperty(instance.GetType(), propertyName)
            ?? throw new InvalidOperationException(
                $"A '{{Binding}}' cannot be set on the '{propertyName}' property of '{instance.GetType().Name}': no dependency property '{propertyName}Property' was found.");

        element.SetBinding(dp, binding);
    }

    /// <summary>Finds the conventional static '<paramref name="propertyName"/>Property' field, walking base types.</summary>
    private static DependencyProperty? FindDependencyProperty(Type type, string propertyName)
    {
        var field = type.GetField(propertyName + "Property",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return field?.GetValue(null) as DependencyProperty;
    }

    private static Binding BuildBinding(ParsedMarkupExtension ext, object? controller)
    {
        var binding = new Binding();

        foreach (var (key, value) in ext.Arguments)
        {
            switch (key)
            {
                case null:
                case "Path":
                    binding.Path = value;
                    break;
                case "Mode":
                    binding.Mode = Enum.Parse<BindingMode>(value);
                    break;
                case "ElementName":
                    binding.ElementName = value;
                    break;
                case "StringFormat":
                    // The value-level "{}" escape ("{}{0} items") is common here.
                    binding.StringFormat = value.StartsWith("{}", StringComparison.Ordinal) ? value.Substring(2) : value;
                    break;
                case "FallbackValue":
                    binding.FallbackValue = value;
                    break;
                case "TargetNullValue":
                    binding.TargetNullValue = value;
                    break;
                case "ConverterParameter":
                    binding.ConverterParameter = value;
                    break;
                case "Converter":
                    binding.Converter = ResolveControllerResource(controller, value) as IValueConverter
                        ?? throw new InvalidOperationException($"Converter '{value}' was not found on the controller or does not implement IValueConverter.");
                    break;
                case "Source":
                    binding.Source = ResolveControllerResource(controller, value)
                        ?? throw new InvalidOperationException($"Binding Source '{value}' was not found on the controller.");
                    break;
                case "RelativeSource":
                    binding.RelativeSource = ParseRelativeSource(value);
                    break;
                case "UpdateSourceTrigger":
                    // Accepted for WPF markup compatibility; this engine always updates
                    // on property change.
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported Binding property '{key}'.");
            }
        }

        return binding;
    }

    private static RelativeSource ParseRelativeSource(string value)
    {
        if (!MarkupExtensionParser.IsExtension(value))
            throw new InvalidOperationException($"Invalid RelativeSource value '{value}'.");

        var ext = MarkupExtensionParser.Parse(value);
        var relativeSource = new RelativeSource(RelativeSourceMode.None);

        foreach (var (key, val) in ext.Arguments)
        {
            switch (key)
            {
                case null:
                case "Mode":
                    relativeSource.Mode = Enum.Parse<RelativeSourceMode>(val);
                    break;
                case "AncestorType":
                    relativeSource.AncestorType = ResolveTypeReference(val);
                    break;
                case "AncestorLevel":
                    relativeSource.AncestorLevel = int.Parse(val);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported RelativeSource property '{key}'.");
            }
        }

        // WPF infers FindAncestor when only AncestorType is given.
        if (relativeSource.AncestorType != null && relativeSource.Mode == RelativeSourceMode.None)
            relativeSource.Mode = RelativeSourceMode.FindAncestor;

        return relativeSource;
    }

    /// <summary>Resolves a type name, accepting "{x:Type tui:Grid}", "tui:Grid" or "Grid".</summary>
    private static Type ResolveTypeReference(string value)
    {
        if (MarkupExtensionParser.IsExtension(value))
        {
            var ext = MarkupExtensionParser.Parse(value);
            value = ext.Positional ?? "";
        }
        value = StripXmlPrefix(value);
        return ResolveType(value)
            ?? throw new InvalidOperationException($"Type '{value}' not found.");
    }

    /// <summary>
    /// Looks up a named member (field or property, any visibility) on the controller.
    /// Serves as the loader's stand-in for StaticResource lookup, so converters and
    /// source objects can live on the code-behind object.
    /// </summary>
    private static object? ResolveControllerResource(object? controller, string value)
    {
        string key = value;
        if (MarkupExtensionParser.IsExtension(value))
        {
            var ext = MarkupExtensionParser.Parse(value);
            string name = StripXmlPrefix(ext.Name);
            if (name is not ("StaticResource" or "StaticResourceExtension"))
                throw new InvalidOperationException($"Unsupported markup extension '{ext.Name}' in resource reference.");
            key = ext.Positional ?? "";
        }

        if (controller == null || key.Length == 0) return null;

        var type = controller.GetType();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        var field = type.GetField(key, flags);
        if (field != null) return field.GetValue(field.IsStatic ? null : controller);

        var prop = type.GetProperty(key, flags);
        if (prop != null) return prop.GetValue(prop.GetMethod?.IsStatic == true ? null : controller);

        return null;
    }

    private static string StripXmlPrefix(string name)
    {
        int colon = name.IndexOf(':');
        return colon >= 0 ? name.Substring(colon + 1) : name;
    }

    private static void SetAttachedProperty(object instance, string name, string value)
    {
        if (MarkupExtensionParser.IsExtension(value))
            throw new NotSupportedException($"Markup extensions are not supported on attached property '{name}'.");

        // Format: Grid.Row="1"
        // Optimization: Span slicing replaces String.Split array allocations O(1) allocation instead of O(n)
        ReadOnlySpan<char> nameSpan = name.AsSpan();
        int dotIdx = nameSpan.IndexOf('.');
        if (dotIdx == -1 || nameSpan.Slice(dotIdx + 1).IndexOf('.') != -1) return; // Ensure exactly one dot

        string ownerType = nameSpan.Slice(0, dotIdx).ToString();
        string propName = nameSpan.Slice(dotIdx + 1).ToString();

        // Find owner type
        Type? type = ResolveType(ownerType);
        if (type == null) return;

        // Find Set method: SetRow(UIElement element, int value)
        var method = type.GetMethod("Set" + propName, BindingFlags.Public | BindingFlags.Static);
        if (method != null)
        {
            var p = method.GetParameters();
            if (p.Length == 2)
            {
                object val = ConvertValue(value, p[1].ParameterType);
                method.Invoke(null, [instance, val]);
            }
        }
    }

    private static void SetContentProperty(object instance, string text)
    {
        // Try "Content"
        var prop = instance.GetType().GetProperty("Content");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(instance, text); // Assuming string content is valid
            return;
        }

        // Try "Text"
        prop = instance.GetType().GetProperty("Text");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(instance, text);
            return;
        }
    }

    private static void AddChild(object parent, object child)
    {
        if (parent == null) return;

        // 1. Panel (Children)
        if (parent is Panel panel && child is UIElement uieChild)
        {
            panel.AddChild(uieChild);
            return;
        }

        // Fallback for non-Panel collections named "Children" (if any)
        var childrenProp = parent.GetType().GetProperty("Children");
        if (childrenProp != null && childrenProp.PropertyType.IsGenericType && childrenProp.GetValue(parent) is System.Collections.IList list)
        {
            list.Add(child);
            if (parent is UIElement uieParentFallback && child is UIElement uieChildFallback)
            {
                uieChildFallback.Parent = uieParentFallback;
            }
            return;
        }

        // 2. ContentControl (Content)
        var contentProp = parent.GetType().GetProperty("Content");
        if (contentProp != null && contentProp.CanWrite)
        {
            contentProp.SetValue(parent, child);
            return;
        }

        // 3. Border (Child)
        var childProp = parent.GetType().GetProperty("Child");
        if (childProp != null && childProp.CanWrite)
        {
            childProp.SetValue(parent, child);
            return;
        }

        // 4. ItemsControl (Items)
        var itemsProp = parent.GetType().GetProperty("Items");
        if (itemsProp != null && itemsProp.GetValue(parent) is IList itemsList)
        {
            itemsList.Add(child);
            return;
        }

        // 5. Table specific
        if (parent is Table table)
        {
            if (child is TableRow row)
            {
                table.AddRow(row);
                return;
            }
            if (child is TableColumn col)
            {
                table.Columns.Add(col);
                return;
            }
        }

        // 6. TableRow specific
        if (parent is TableRow tableRow)
        {
            if (child is UIElement cell)
            {
                tableRow.AddCell(cell);
                return;
            }
            if (child is string s)
            {
                tableRow.AddCell(s);
                return;
            }
        }
    }

    private static object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int)) return int.Parse(value);
        if (targetType == typeof(double)) return double.Parse(value);
        if (targetType == typeof(bool)) return bool.Parse(value);
        if (targetType.IsEnum) return Enum.Parse(targetType, value);

        if (targetType == typeof(GridLength))
        {
            if (value == "*") return GridLength.Star;
            if (value.Equals("Auto", StringComparison.OrdinalIgnoreCase)) return GridLength.Auto;
            if (value.EndsWith("*"))
            {
                // 2* logic? Not implemented in GridLength struct yet, it takes double.
                // Struct: Value, Type.
                // "2*" -> Value=2, Type=Star
                string v = value.TrimEnd('*');
                if (double.TryParse(v, out double d)) return new GridLength(d, GridUnitType.Star);
                return GridLength.Star;
            }
            if (int.TryParse(value, out int i)) return GridLength.Pixel(i);
            throw new FormatException($"Invalid GridLength: {value}");
        }

        if (targetType == typeof(ConsoleColor))
        {
            return Enum.Parse(typeof(ConsoleColor), value);
        }

        // TuiColor supports the full CSS-style color grammar plus the legacy ConsoleColor names.
        if (targetType == typeof(TuiColor))
        {
            return TuiColor.FromHex(value);
        }

        if (targetType == typeof(TuiColor?))
        {
            if (string.IsNullOrWhiteSpace(value)
                || value.Equals("null", StringComparison.OrdinalIgnoreCase)
                || value.Equals("transparent", StringComparison.OrdinalIgnoreCase))
                return (TuiColor?)null;
            return (TuiColor?)TuiColor.FromHex(value);
        }

        Type? underlyingNullable = Nullable.GetUnderlyingType(targetType);
        if (underlyingNullable != null)
        {
            if (string.IsNullOrWhiteSpace(value)
                || value.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            return ConvertValue(value, underlyingNullable);
        }

        // Fallback
        return Convert.ChangeType(value, targetType);
    }
}
