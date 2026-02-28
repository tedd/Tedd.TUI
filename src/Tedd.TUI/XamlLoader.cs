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
    public static UIElement Load(string xml, object? controller = null)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        if (doc.DocumentElement == null)
            throw new InvalidOperationException("XML document is empty.");

        return (UIElement)ParseElement(doc.DocumentElement, controller);
    }

    private static object ParseElement(XmlElement element, object? controller)
    {
        // 1. Create Instance
        string typeName = "Tedd.TUI." + element.Name;
        // Also support sub-namespaces if needed, but for now flat is assumed or user specifies?
        // Let's assume standard controls are in Tedd.TUI.
        // If element name contains '.', it might be Property Element Syntax (e.g. <Table.Columns>).
        if (element.Name.Contains("."))
        {
            // This is handled by parent, so ParseElement shouldn't be called on it directly usually.
            // But if called recursively, we need to distinguish.
            // Actually, my recursive logic should handle property elements before calling ParseElement.
            throw new InvalidOperationException($"Unexpected property element {element.Name} in ParseElement.");
        }

        // Handle specific sub-namespaces if any (e.g. MarkdownView in Tedd.TUI.Markdown)
        Type? type = ResolveType(element.Name);
        if (type == null)
        {
            throw new InvalidOperationException($"Type {element.Name} not found.");
        }

        var instance = Activator.CreateInstance(type);
        if (instance == null)
            throw new InvalidOperationException($"Failed to create instance of {type.Name}.");

        // 2. Set Properties (Attributes)
        foreach (XmlAttribute attr in element.Attributes)
        {
            if (attr.Name.Contains("."))
            {
                 // Attached Property? e.g. Grid.Row
                 // For now, support attached properties
                 SetAttachedProperty(instance, attr.Name, attr.Value);
            }
            else
            {
                SetProperty(instance, attr.Name, attr.Value, controller);
            }
        }

        // 3. Handle Children
        foreach (XmlNode childNode in element.ChildNodes)
        {
            if (childNode is XmlElement childElement)
            {
                if (childElement.Name.Contains("."))
                {
                    // Property Element Syntax: <Table.Columns> ... </Table.Columns>
                    string propName = childElement.Name.Substring(childElement.Name.LastIndexOf('.') + 1);
                    // The property name is "Columns" on "Table".
                    // Find property on instance.
                    var propInfo = instance.GetType().GetProperty(propName);
                    if (propInfo != null)
                    {
                        // Add children of this property element to the property
                        object? propValue = propInfo.GetValue(instance);
                        if (propValue is IList list)
                        {
                            foreach(XmlNode grandChild in childElement.ChildNodes)
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
                             foreach(XmlNode grandChild in childElement.ChildNodes)
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
        foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
             foreach(var ns in namespaces)
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
                         catch(Exception)
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

    private static void SetAttachedProperty(object instance, string name, string value)
    {
        // Format: Grid.Row="1"
        var parts = name.Split('.');
        if (parts.Length != 2) return;

        string ownerType = parts[0];
        string propName = parts[1];

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
                method.Invoke(null, new object[] { instance, val });
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

        // Fallback
        return Convert.ChangeType(value, targetType);
    }
}
